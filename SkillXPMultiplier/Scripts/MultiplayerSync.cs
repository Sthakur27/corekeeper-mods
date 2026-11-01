using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using HarmonyLib;
using PugMod;
using Unity.Entities;
using Unity.NetCode;

namespace SkillXPMultiplier
{
    /// <summary>
    /// Who decides the multipliers and talent points in multiplayer. Only the host's choice counts.
    ///
    /// XP is granted on the server only (PlayerController creates AddSkillValueCD when isServer), so the
    /// server needs each player's multipliers; talent points are worked out on each client, so clients
    /// need the host's values. Both travel over CoreLib's command channel:
    ///   client → server  /xpsync m0 … m11          each player's own multipliers (on join and on change)
    ///   server → clients /xphost mode talents m0 … m11   the host's choice, broadcast as a relay command
    /// The replies those commands produce start with <see cref="Marker"/> and are kept out of chat by
    /// <see cref="HideSyncChatPatch"/>.
    /// </summary>
    public static class MultiplayerSync
    {
        public const string HostDecides = "Host decides";
        public const string EachPlayer = "Each player picks";
        public static readonly string[] Modes = { HostDecides, EachPlayer };

        public const string Marker = "[xp-sync]";

        /// <summary>This process's own choice; only used when it is the host (or a dedicated server).</summary>
        public static bool LocalEachPlayer;

        // ---------------------------------------------------------------- server side

        /// <summary>Multipliers sent by remote players, by player entity. Used only in "Each player picks".</summary>
        private static readonly Dictionary<Entity, float[]> _playerMultipliers = new Dictionary<Entity, float[]>();

        /// <summary>XP multiplier for a grant to <paramref name="player"/>: their own in "Each player picks", else the host's.</summary>
        public static float MultiplierFor(Entity player, int skillIndex)
        {
            if (LocalEachPlayer && _playerMultipliers.TryGetValue(player, out float[] own))
                return skillIndex >= 0 && skillIndex < own.Length ? own[skillIndex] : 1f;
            return SkillXPTable.Get(skillIndex);
        }

        internal static void StorePlayer(Entity player, float[] multipliers)
        {
            _playerMultipliers[player] = multipliers;
        }

        /// <summary>
        /// True for the connection of the player hosting this world (same process as the server). On a
        /// dedicated server there is no such player; admins count instead.
        /// </summary>
        public static bool IsHostConnection(Entity connection)
        {
            var server = API.Server.World;
            if (server == null) return false;
            var sem = server.EntityManager;
            if (!sem.Exists(connection)) return false;

            var client = API.Client.World;
            if (client == null)
                return sem.HasComponent<ConnectionAdminLevelCD>(connection) &&
                       sem.GetComponentData<ConnectionAdminLevelCD>(connection).adminPrivileges > 0;

            using (var query = client.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>()))
            {
                if (query.CalculateEntityCount() == 1 && sem.HasComponent<NetworkId>(connection))
                    return query.GetSingleton<NetworkId>().Value == sem.GetComponentData<NetworkId>(connection).Value;
            }
            // No usable network id on the local client: if this is the only connection, it is ours.
            using (var connections = sem.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>()))
                return connections.CalculateEntityCount() <= 1;
        }

        /// <summary>Tell every client the host's mode, talent points and multipliers.</summary>
        public static void BroadcastHost()
        {
            var comm = CommandModule.ServerCommSystem;
            if (comm == null || API.Server.World == null) return;

            var sb = new StringBuilder("/xphost ");
            sb.Append(LocalEachPlayer ? '1' : '0').Append(' ').Append(TalentPoints.Total);
            for (int i = 0; i < SkillXPTable.SkillCount; i++)
                sb.Append(' ').Append(SkillXPTable.Get(i).ToString(CultureInfo.InvariantCulture));
            comm.SendRelayCommand(sb.ToString());
        }

        // ---------------------------------------------------------------- client side

        /// <summary>Set once the host's state has arrived; cleared when leaving the world.</summary>
        public static bool HaveHostState { get; private set; }
        public static bool HostEachPlayer { get; private set; }
        public static int HostTalentTotal { get; private set; } = -1;
        private static readonly float[] _hostMultipliers = new float[SkillXPTable.SkillCount];

        /// <summary>True when this game is joined to someone else's world (no server world in this process).</summary>
        public static bool IsRemoteClient => API.Server.World == null && API.Client.World != null;

        /// <summary>True when this player's own settings are being ignored because the host decides.</summary>
        public static bool HostOverrides => IsRemoteClient && HaveHostState && !HostEachPlayer;

        internal static void ReceiveHost(bool eachPlayer, int talentTotal, float[] multipliers)
        {
            HaveHostState = true;
            HostEachPlayer = eachPlayer;
            HostTalentTotal = TalentPoints.Clamp(talentTotal);
            for (int i = 0; i < _hostMultipliers.Length && i < multipliers.Length; i++)
                _hostMultipliers[i] = multipliers[i];
        }

        public static float HostMultiplier(int skillIndex)
        {
            return skillIndex >= 0 && skillIndex < _hostMultipliers.Length ? _hostMultipliers[skillIndex] : 1f;
        }

        private static bool _sent;
        private static bool _dirty;

        /// <summary>Own settings changed: resend them, and if this is the host, rebroadcast the host state.</summary>
        public static void OnLocalSettingsChanged()
        {
            _dirty = true;
            if (API.Server.World != null) BroadcastHost();
        }

        /// <summary>Called every frame from the mod's Update: send own multipliers once per session and after changes.</summary>
        public static void Tick()
        {
            if (API.Server.World == null) _playerMultipliers.Clear();

            var comm = CommandModule.ClientCommSystem;
            bool inWorld = comm != null && Manager.main != null && Manager.main.player != null;
            if (!inWorld)
            {
                _sent = false;
                HaveHostState = false;
                return;
            }
            if (_sent && !_dirty) return;
            _sent = true;
            _dirty = false;

            var sb = new StringBuilder("/xpsync");
            for (int i = 0; i < SkillXPTable.SkillCount; i++)
                sb.Append(' ').Append(SkillXPTable.Get(i).ToString(CultureInfo.InvariantCulture));
            comm.SendCommand(sb.ToString(), CommandFlags.None);
        }

        internal static bool TryParseFloats(string[] parts, int start, out float[] values)
        {
            values = new float[SkillXPTable.SkillCount];
            if (parts.Length - start < SkillXPTable.SkillCount) return false;
            for (int i = 0; i < SkillXPTable.SkillCount; i++)
            {
                if (!float.TryParse(parts[start + i], NumberStyles.Float, CultureInfo.InvariantCulture, out float v) || v < 0f)
                    return false;
                values[i] = v > 100f ? 100f : v;
            }
            return true;
        }
    }

    /// <summary>
    /// Keeps the sync commands' replies out of chat. ChatWindow.AddPugText is where a rendered line joins
    /// the visible queue; for a sync line we hide it and hand the text and its fade effect back exactly as
    /// AllocPugText took them, so the chat's queues stay in step.
    /// </summary>
    /// (Private fields come in through Harmony's ___field parameters: the mod loader's security check
    /// rejects AccessTools.)
    [HarmonyPatch(typeof(ChatWindow), "AddPugText")]
    public static class HideSyncChatPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ChatWindow.MessageTextType type, PugText text,
            Dictionary<ChatWindow.MessageTextType, Queue<PugText>> ___freePools,
            Queue<PugTextEffectMaxFade> ___fadeEffects)
        {
            string s = text != null ? text.GetText() : null;
            if (s == null || !s.StartsWith(MultiplayerSync.Marker)) return true;

            text.gameObject.SetActive(false);
            ___freePools[type].Enqueue(text);

            // AllocPugText queued this text's fade effect last; drop it again (rotate the rest back into order).
            int keep = ___fadeEffects.Count - 1;
            if (keep >= 0)
            {
                for (int i = 0; i < keep; i++) ___fadeEffects.Enqueue(___fadeEffects.Dequeue());
                ___fadeEffects.Dequeue();
            }
            return false;
        }
    }
}
