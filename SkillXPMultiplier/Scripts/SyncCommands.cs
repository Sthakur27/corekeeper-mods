using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Util;
using Unity.Entities;

namespace SkillXPMultiplier
{
    /// <summary>
    /// Client → server, sent automatically: "/xpsync m0 … m11", the sender's own multipliers.
    /// The server keeps them for "Each player picks" and answers with the host's state.
    /// </summary>
    public sealed class XpSyncCommand : IServerCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            if (!MultiplayerSync.IsHostConnection(sender) && MultiplayerSync.TryParseFloats(parameters, 0, out float[] values))
            {
                Entity player = sender.GetPlayerEntity();
                if (player != Entity.Null) MultiplayerSync.StorePlayer(player, values);
            }
            MultiplayerSync.BroadcastHost();
            return new CommandOutput(MultiplayerSync.Marker, CommandStatus.Info);
        }

        public string GetDescription()
        {
            return "Internal: sends your Skill XP multipliers to the host. Sent automatically.";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "xpsync" };
        }
    }

    /// <summary>
    /// Server → clients relay, sent automatically: "/xphost mode talents m0 … m11", the host's choice.
    /// </summary>
    public sealed class XpHostCommand : IClientCommandHandler
    {
        public CommandOutput Execute(string[] parameters)
        {
            if (parameters.Length >= 2 + SkillXPTable.SkillCount &&
                int.TryParse(parameters[1], out int talents) &&
                MultiplayerSync.TryParseFloats(parameters, 2, out float[] values))
            {
                MultiplayerSync.ReceiveHost(parameters[0] == "1", talents, values);
            }
            return new CommandOutput(MultiplayerSync.Marker, CommandStatus.Info);
        }

        public string GetDescription()
        {
            return "Internal: the host's Skill XP settings. Sent automatically.";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "xphost" };
        }
    }
}
