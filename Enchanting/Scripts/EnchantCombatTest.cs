using System.Collections.Generic;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// "/enchant combattest [seconds]": automatic combat measurement for the held weapon (melee, ranged, magic staff). Stand next to a training
    /// dummy (or enemy). For the plain weapon, each enchant in its pool at level III, and the plain weapon again
    /// (drift check), the test puts only that enchant on the weapon, sets the player to half HP, waits until the
    /// target has no tracked status, then aims at it and holds the attack button for N seconds (default 20),
    /// while EnchantDebug records every hit (damage, crits), the target's statuses at each hit (burn, poison,
    /// stun, slow) and the HP the player regained (lifesteal). The weapon's own enchants are restored at the end, then the report is logged.
    /// Input is faked the way the bench runner steers movement: PlayerInput button checks and the static
    /// PlayerController.UpdateAim are overridden only while the test runs. Single player / host only.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class EnchantCombatTestSystem : PugSimulationSystemBase
    {
        // Read by the input patches (client side, same process).
        internal static bool Attacking;
        internal static bool HasTarget;
        internal static float3 TargetPosition;

        private static Entity _player = Entity.Null;
        private static int _slot = -1;
        private static ObjectID _item;
        private static List<EnchantEntry> _original;
        private static readonly List<EnchantDef> Queue = new List<EnchantDef>(); // null = plain weapon (first and last)
        private static int _index;
        private static double _phaseStart, _phaseEnd;
        private static float _seconds = 20f;
        private static bool _running;
        private static bool _magic; // staffs: mana is refilled every tick while attacking
        private static string _summary = "";

        // Per run: Apply (enchant + half HP) -> Cooldown (target free of statuses, stacking buffs worn off) -> Attack.
        private enum Phase { Apply, Cooldown, Attack }
        private static Phase _phase;
        private const double MinCooldown = 6, MaxCooldown = 25;
        private static int _lastHealth;

        public static string Status() => _running
            ? $"combattest running: {_index + 1}/{Queue.Count} ({Label(_index)})."
            : (_summary.Length > 0 ? _summary : "no combattest run yet.");

        private static string Label(int i) => i < Queue.Count && Queue[i] != null ? Queue[i].Name + " III" : "plain";

        public static string Start(EntityManager em, Entity player, float seconds)
        {
            if (_running) return "a combattest is already running.";
            if (!EnchantStorage.TryGetAux(em, out var aux)) return "inventory aux data not ready.";
            int slot = em.GetComponentData<EquippedObjectCD>(player).equippedSlotIndex;
            var items = em.GetBuffer<ContainedObjectsBuffer>(player, true);
            if (slot < 0 || slot >= items.Length) return "hold a weapon first.";
            var item = items[slot];
            var kind = Enchants.SlotOf(item.objectID);
            if (kind == EnchantSlot.Summon) return "tomes: use /enchant selftest; minion combat needs a live enemy.";
            if (kind != EnchantSlot.Melee && kind != EnchantSlot.Ranged && kind != EnchantSlot.Magic) return "hold a melee, ranged or magic weapon.";
            _magic = kind == EnchantSlot.Magic;
            if (!FindTarget(em, player, out _, out _)) return "no training dummy or enemy within 12 tiles.";

            _player = player;
            _slot = slot;
            _item = item.objectID;
            _original = EnchantStorage.Read(em, aux, item);
            _seconds = Mathf.Clamp(seconds, 3f, 120f);
            Queue.Clear();
            Queue.Add(null);
            Queue.AddRange(Enchants.PoolFor(kind));
            Queue.Add(null); // second plain run: if it differs from the first, something drifted (stacking buffs)
            _index = 0;
            _phase = Phase.Apply;
            _running = true;
            EnchantDebug.On = true;
            EnchantDebug.Reset();
            Debug.Log($"[{EnchantingMod.Name}] combattest start: {item.objectID}, {Queue.Count} runs x {_seconds:0} s");
            return $"combattest started: {Queue.Count} runs x {_seconds:0} s + cooldowns (~{Queue.Count * (_seconds + 10) / 60f:0.0} min). Don't move or switch items. /enchant combattest status";
        }

        private static string RunTag(int i) => i == 0 ? " (plain, first)" : i == Queue.Count - 1 ? " (plain, last)" : "";

        protected override void OnUpdate()
        {
            if (!_running) return;
            var em = EntityManager;
            double now = World.Time.ElapsedTime;
            if (_player == Entity.Null || !em.Exists(_player)) { Finish(em, "player gone"); return; }
            var items = em.GetBuffer<ContainedObjectsBuffer>(_player, true);
            if (_slot >= items.Length || items[_slot].objectID != _item
                || em.GetComponentData<EquippedObjectCD>(_player).equippedSlotIndex != _slot)
            {
                Finish(em, "weapon moved or switched");
                return;
            }

            HasTarget = FindTarget(em, _player, out TargetPosition, out Entity target);

            switch (_phase)
            {
                case Phase.Apply:
                {
                    // Only this enchant on the weapon; half HP so lifesteal has room to heal (regen also heals,
                    // which the plain runs measure).
                    Attacking = false;
                    var list = new List<EnchantEntry>();
                    if (Queue[_index] != null) list.Add(new EnchantEntry { Id = Queue[_index].Id, Level = 3 });
                    string err = EnchantStorage.Write(em, _player, _slot, list);
                    if (err != null) { Finish(em, err); return; }
                    var hp = em.GetComponentData<HealthCD>(_player);
                    hp.health = math.max(1, hp.maxHealth / 2);
                    em.SetComponentData(_player, hp);
                    EnchantDebug.RunTag = RunTag(_index);
                    _phase = Phase.Cooldown;
                    _phaseStart = now;
                    return;
                }
                case Phase.Cooldown:
                    // Wait until the target carries no tracked status (no carry-over from the previous run) and
                    // on-hit stacking buffs have expired; give up waiting after MaxCooldown.
                    if (now - _phaseStart < MinCooldown) return;
                    if (now - _phaseStart < MaxCooldown && TargetHasStatus(em, target)) return;
                    _lastHealth = em.GetComponentData<HealthCD>(_player).health;
                    _phase = Phase.Attack;
                    _phaseEnd = now + _seconds;
                    return;
                case Phase.Attack:
                {
                    Attacking = HasTarget;
                    if (_magic && em.HasComponent<ManaCD>(_player))
                    {
                        // Staffs would run dry mid-run; mana enchants (Frugality, Clarity) are verified by selftest instead.
                        var mana = em.GetComponentData<ManaCD>(_player);
                        if (mana.mana < mana.maxMana)
                        {
                            mana.mana = mana.maxMana;
                            em.SetComponentData(_player, mana);
                        }
                    }
                    int health = em.GetComponentData<HealthCD>(_player).health;
                    var item = em.GetBuffer<ContainedObjectsBuffer>(_player, true)[_slot];
                    if (EnchantStorage.TryGetAux(em, out var aux))
                        EnchantDebug.AddHeal(EnchantDebug.Label(item.objectID, EnchantStorage.Read(em, aux, item)) + EnchantDebug.RunTag, health - _lastHealth);
                    _lastHealth = health;
                    if (now < _phaseEnd) return;
                    Attacking = false;
                    _index++;
                    if (_index >= Queue.Count) { Finish(em, null); return; }
                    _phase = Phase.Apply;
                    return;
                }
            }
        }

        private static bool TargetHasStatus(EntityManager em, Entity target)
        {
            if (target == Entity.Null || !em.Exists(target) || !em.HasBuffer<ConditionsBuffer>(target)) return false;
            var conditions = em.GetBuffer<ConditionsBuffer>(target, true);
            for (int i = 0; i < conditions.Length; i++)
                foreach (var s in EnchantDebug.Statuses)
                    if (conditions[i].condition.conditionData.conditionID == s) return true;
            return false;
        }

        private static bool FindTarget(EntityManager em, Entity player, out float3 position, out Entity target)
        {
            position = default;
            target = Entity.Null;
            if (!em.HasComponent<LocalTransform>(player)) return false;
            float3 me = em.GetComponentData<LocalTransform>(player).Position;
            float best = 12f;
            bool found = false;
            using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<HealthCD>()))
            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                foreach (var e in entities)
                {
                    if (e == player) continue;
                    var id = em.GetComponentData<ObjectDataCD>(e).objectID;
                    var info = PugDatabase.GetObjectInfo(id);
                    if (info == null || (info.objectType != ObjectType.TrainingDummy && info.objectType != ObjectType.Creature)) continue;
                    if (em.GetComponentData<HealthCD>(e).health <= 0) continue;
                    float3 p = em.GetComponentData<LocalTransform>(e).Position;
                    float d = math.distance(p.xz, me.xz);
                    if (d < best) { best = d; position = p; target = e; found = true; }
                }
            }
            return found;
        }

        private static void Finish(EntityManager em, string error)
        {
            Attacking = false;
            HasTarget = false;
            EnchantDebug.RunTag = "";
            if (_player != Entity.Null && em.Exists(_player) && _original != null)
            {
                var items = em.GetBuffer<ContainedObjectsBuffer>(_player, true);
                if (_slot >= 0 && _slot < items.Length && items[_slot].objectID == _item)
                    EnchantStorage.Write(em, _player, _slot, _original);
            }
            _running = false;
            _summary = (error != null ? "combattest ABORTED (" + error + "). " : "combattest done. ") + EnchantDebug.Report();
            Debug.Log($"[{EnchantingMod.Name}] {_summary}");
        }
    }

    // While the combat test attacks, the attack button reads as held, and as freshly pressed every few frames so
    // weapons that need separate clicks keep firing.
    [HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.IsButtonCurrentlyDown))]
    public static class EnchantCombatHoldPatch
    {
        public static void Postfix(PlayerInput.InputType inputType, ref bool __result)
        {
            if (EnchantCombatTestSystem.Attacking && inputType == PlayerInput.InputType.INTERACT) __result = true;
        }
    }

    [HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.WasButtonPressedDownThisFrame))]
    public static class EnchantCombatPressPatch
    {
        public static void Postfix(PlayerInput.InputType inputType, ref bool __result)
        {
            if (EnchantCombatTestSystem.Attacking && inputType == PlayerInput.InputType.INTERACT && Time.frameCount % 6 == 0) __result = true;
        }
    }

    // Aim at the test target instead of the mouse.
    // UpdateAim is overloaded: name the static (ref float3, float3, bool, PlayerInput, AimUI) one explicitly. An
    // ambiguous match throws inside PatchAll and silently drops every patch class after it.
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateAim),
        new[] { typeof(float3), typeof(float3), typeof(bool), typeof(PlayerInput), typeof(AimUI) },
        new[] { ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
    public static class EnchantCombatAimPatch
    {
        public static void Postfix(ref float3 aimDirection, float3 position)
        {
            if (!EnchantCombatTestSystem.Attacking || !EnchantCombatTestSystem.HasTarget) return;
            // position is the player's render position (world - render origin), so convert the target too.
            Vector3Int origo = Manager.camera.RenderOrigo;
            float3 target = EnchantCombatTestSystem.TargetPosition - new float3(origo.x, origo.y, origo.z);
            float3 d = target - position;
            d.y = 0f;
            if (math.lengthsq(d) > 1e-4f) aimDirection = math.normalize(d);
        }
    }
}
