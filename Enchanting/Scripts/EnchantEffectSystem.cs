using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Server: keeps one ConditionsBuffer entry per enchant condition on each player, summed from the held item
    /// (weapons, tools), the equipped armor, the worn accessories (ring1, ring2, necklace, offhand) and the carried
    /// summoning tome. Entries are marked by an infinite
    /// duration plus <see cref="Marker"/> as valueMultiplier: infinite conditions never expire and survive death,
    /// and the vanilla summary (SummarizeConditionsSystem) adds every entry's value to the stats.
    ///
    /// Value per mode (EnchantMath): Flat and ItemLevel are fixed per item. Relative (Model B: damage %, attack
    /// speed, minion damage/speed) is recomputed from the player's current summary minus our own entry, so it acts as
    /// a multiplier on the whole bucket. That needs a summary that already contains our last write: the summary runs
    /// at the start of the next tick, so a player is evaluated only when at least <see cref="SettleUpdates"/> updates
    /// have passed since our last write to them. Relative drift (buffs changing the bucket) is rewritten only past
    /// the hysteresis (EnchantMath.CloseEnough) and at most every <see cref="DriftInterval"/> updates. Writes touch
    /// only the entries that changed, since any ConditionsBuffer write makes the game re-summarize that player.
    ///
    /// Stack modes (EnchantDef.Stack): Sum enchants add per copy. Max enchants (the accessory pool) keep only the
    /// highest copy per enchant id across all items (<see cref="_maxFlat"/>, <see cref="_maxRel"/>), then fold into
    /// the per-condition totals like one more item. Our totals are then clamped by EnchantMath.Cap.
    /// Accessory multipliers (Might, Haste) use the bucket of the weapon the player holds.
    ///
    /// Carried rule (summoning tomes): the last SummoningWeapon the player held keeps applying its enchants while
    /// it is anywhere in the player's inventory (minion stats are read live from the owner; a held-only rule would
    /// flicker on every weapon swap). Forgotten when it leaves the inventory.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class EnchantEffectSystem : PugSimulationSystemBase
    {
        public const float Marker = 0.4242f;
        public const int SettleUpdates = 2;
        public const int DriftInterval = 15; // ~0.25 s at 60 Hz

        private sealed class PlayerState
        {
            public long LastWrite = -1000;
            public int CarriedSlot = -1;
            public ObjectID CarriedItem;
            public int CarriedAux;
            public long Seen;
            /// <summary>OnMinions enchants of the carried tome: written onto minions by EnchantMinionSystem.</summary>
            public readonly Dictionary<ConditionID, int> Minion = new Dictionary<ConditionID, int>();
            public readonly Dictionary<ConditionID, int> Permille = new Dictionary<ConditionID, int>();
        }

        private struct RelWant
        {
            public EnchantDef Def;      // first def for this write condition (bucket definition)
            public ObjectID Weapon;     // the enchanted item (decides melee/range class and cooldown)
            public int Permille;        // summed p of every enchant writing this condition
        }

        private static readonly Dictionary<Entity, PlayerState> States = new Dictionary<Entity, PlayerState>();
        private EntityQuery _players;
        private EntityQuery _auxQuery;
        private readonly List<EnchantEntry> _entries = new List<EnchantEntry>(); // reused per item (no per-tick lists)
        private long _update;
        private readonly Dictionary<ConditionID, int> _flat = new Dictionary<ConditionID, int>();
        private readonly Dictionary<ConditionID, RelWant> _rel = new Dictionary<ConditionID, RelWant>();
        private readonly Dictionary<ConditionID, int> _ours = new Dictionary<ConditionID, int>();
        private readonly Dictionary<ConditionID, int> _wanted = new Dictionary<ConditionID, int>();
        private readonly Dictionary<ConditionID, int> _minion = new Dictionary<ConditionID, int>();
        // Stack = Max: best copy per enchant id (raw value for Flat/ItemLevel, permille for Relative).
        private readonly Dictionary<int, MaxPick> _maxFlat = new Dictionary<int, MaxPick>();
        private readonly Dictionary<int, MaxPick> _maxRel = new Dictionary<int, MaxPick>();
        private ObjectID _held;

        private struct MaxPick
        {
            public EnchantDef Def;
            public int Value;
        }

        public static bool IsOurs(in Condition c) =>
            float.IsPositiveInfinity(c.conditionData.duration) && Mathf.Approximately(c.conditionData.valueMultiplier, Marker);

        /// <summary>Server: absolute inventory slot of the player's carried summoning tome, or -1.</summary>
        public static int CarriedSlot(Entity player) => States.TryGetValue(player, out var s) ? s.CarriedSlot : -1;

        /// <summary>Server: adds the minion conditions the player's carried tome grants (empty when none).</summary>
        public static void MinionWants(Entity player, Dictionary<ConditionID, int> into)
        {
            if (!States.TryGetValue(player, out var s)) return;
            foreach (var kv in s.Minion) into[kv.Key] = kv.Value;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            States.Clear();
            _players = GetEntityQuery(
                ComponentType.ReadOnly<PlayerGhost>(),
                ComponentType.ReadOnly<EquipmentCD>(),
                ComponentType.ReadOnly<EquippedObjectCD>(),
                ComponentType.ReadOnly<ContainedObjectsBuffer>(),
                ComponentType.ReadOnly<SummarizedConditionEffectsBuffer>(),
                ComponentType.ReadOnly<SummarizedConditionsBuffer>(),
                ComponentType.ReadOnly<ConditionsBuffer>()); // written through EntityManager only when something changed
            _auxQuery = GetEntityQuery(ComponentType.ReadOnly<InventoryAuxDataSystemDataCD>());
            RequireForUpdate(_players);
        }

        protected override void OnUpdate()
        {
            _update++;
            if (_auxQuery.IsEmptyIgnoreFilter) return;
            var aux = _auxQuery.GetSingleton<InventoryAuxDataSystemDataCD>();
            var players = _players.ToEntityArray(Allocator.Temp);
            foreach (var player in players)
            {
                if (!States.TryGetValue(player, out var state)) States[player] = state = new PlayerState();
                state.Seen = _update;
                // Wait until the vanilla summary contains our last write (Relative values read it back).
                if (_update - state.LastWrite < SettleUpdates) continue;
                Collect(player, state, aux);
                Apply(player, state);
            }
            int count = players.Length;
            players.Dispose();
            if (States.Count > count)
            {
                var gone = new List<Entity>();
                foreach (var kv in States) if (kv.Value.Seen != _update) gone.Add(kv.Key);
                foreach (var e in gone) States.Remove(e);
            }
        }

        private void Collect(Entity player, PlayerState state, InventoryAuxDataSystemDataCD aux)
        {
            _flat.Clear();
            _rel.Clear();
            _maxFlat.Clear();
            _maxRel.Clear();
            _minion.Clear();
            var items = EntityManager.GetBuffer<ContainedObjectsBuffer>(player, true);
            var equipment = EntityManager.GetComponentData<EquipmentCD>(player);
            int held = EntityManager.GetComponentData<EquippedObjectCD>(player).equippedSlotIndex;

            if (EnchantDebug.On && held >= 0 && held < items.Length) EnchantDebug.TrackDurability(EntityManager, player, items[held], aux);
            TrackCarried(items, held, state);
            _held = held >= 0 && held < items.Length ? items[held].objectID : ObjectID.None;

            AddFrom(items, held, aux, heldSlot: true);
            AddFrom(items, equipment.helmSlotIndex, aux, heldSlot: false);
            AddFrom(items, equipment.breastSlotIndex, aux, heldSlot: false);
            AddFrom(items, equipment.pantsSlotIndex, aux, heldSlot: false);
            AddFrom(items, equipment.ring1SlotIndex, aux, heldSlot: false);
            AddFrom(items, equipment.ring2SlotIndex, aux, heldSlot: false);
            AddFrom(items, equipment.necklaceSlotIndex, aux, heldSlot: false);
            AddFrom(items, equipment.offHandIndex, aux, heldSlot: false);
            if (state.CarriedSlot >= 0) AddItem(items[state.CarriedSlot], aux);

            state.Minion.Clear();
            foreach (var kv in _minion) if (kv.Value != 0) state.Minion[kv.Key] = kv.Value;

            FoldMax(player);
            foreach (var id in EnchantMath.CappedConditions)
                if (_flat.TryGetValue(id, out int v)) _flat[id] = EnchantMath.Cap(id, v);
        }

        // Stack = Max enchants join the per-condition totals once, with their best copy.
        private void FoldMax(Entity player)
        {
            foreach (var kv in _maxFlat)
            {
                var def = kv.Value.Def;
                var cond = def.FollowsLight ? LightCondition(EntityManager, player) : def.Condition;
                _flat[cond] = _flat.TryGetValue(cond, out int v) ? v + kv.Value.Value : kv.Value.Value;
            }
            foreach (var kv in _maxRel)
            {
                var def = kv.Value.Def;
                var weapon = EnchantMath.BucketWeapon(def, ObjectID.None, _held);
                var cond = EnchantMath.WriteCondition(def, weapon);
                var want = _rel.TryGetValue(cond, out var r) ? r : new RelWant { Def = def, Weapon = weapon };
                want.Permille += kv.Value.Value;
                _rel[cond] = want;
            }
        }

        /// <summary>
        /// Radiance: the glow colour the player already shines brightest in (without our own entries), so our light
        /// adds to that colour's range (the light uses the largest range across colours). OrangeGlow without a light.
        /// Public so the selftest finds the same condition.
        /// </summary>
        public static ConditionID LightCondition(EntityManager em, Entity player)
        {
            var sum = em.GetBuffer<SummarizedConditionsBuffer>(player, true);
            var mine = em.GetBuffer<ConditionsBuffer>(player, true);
            var best = ConditionID.OrangeGlow;
            int bestValue = 0;
            foreach (var id in EnchantMath.GlowConditions)
            {
                int v = (int)id < sum.Length ? sum[(int)id].value : 0;
                for (int i = 0; i < mine.Length; i++)
                {
                    var c = mine[i].condition;
                    if (IsOurs(c) && c.conditionData.conditionID == id) v -= c.conditionData.value;
                }
                if (v > bestValue)
                {
                    best = id;
                    bestValue = v;
                }
            }
            return best;
        }

        // The held tome becomes the carried one; a carried tome that moved is found again by its aux index.
        private static void TrackCarried(DynamicBuffer<ContainedObjectsBuffer> items, int held, PlayerState state)
        {
            if (held >= 0 && held < items.Length && items[held].objectID != ObjectID.None
                && Enchants.SlotOf(items[held].objectID) == EnchantSlot.Summon)
            {
                state.CarriedSlot = held;
                state.CarriedItem = items[held].objectID;
                state.CarriedAux = items[held].auxDataIndex;
                return;
            }
            if (state.CarriedSlot < 0) return;
            if (state.CarriedSlot < items.Length && items[state.CarriedSlot].objectID == state.CarriedItem)
            {
                state.CarriedAux = items[state.CarriedSlot].auxDataIndex;
                return;
            }
            state.CarriedSlot = -1;
            if (state.CarriedAux == 0) return;
            for (int i = 0; i < items.Length; i++)
                if (items[i].objectID == state.CarriedItem && items[i].auxDataIndex == state.CarriedAux)
                {
                    state.CarriedSlot = i;
                    return;
                }
        }

        private void AddFrom(DynamicBuffer<ContainedObjectsBuffer> items, int slot, InventoryAuxDataSystemDataCD aux, bool heldSlot)
        {
            if (slot < 0 || slot >= items.Length) return;
            var kind = Enchants.SlotOf(items[slot].objectID);
            if (kind == EnchantSlot.None || kind == EnchantSlot.Summon || Enchants.IsHeldSlot(kind) != heldSlot) return;
            AddItem(items[slot], aux);
        }

        private void AddItem(ContainedObjectsBuffer item, InventoryAuxDataSystemDataCD aux)
        {
            if (item.objectID == ObjectID.None || item.auxDataIndex == 0) return;
            // Broken gear gives no stats in vanilla either.
            if (item.amount <= 0 && PugDatabase.HasComponent<DurabilityCD>(item.objectData)) return;

            EnchantStorage.ReadInto(EntityManager, aux, item, _entries);
            foreach (var e in _entries)
            {
                var def = e.Def;
                if (def == null) continue;
                if (def.OnMinions)
                {
                    // Minion on-hit: not a player stat (EnchantMinionSystem writes it onto the minions).
                    int mv = EnchantMath.Raw(def, e.Level, item.objectData);
                    _minion[def.Condition] = _minion.TryGetValue(def.Condition, out int m) ? m + mv : mv;
                    continue;
                }
                if (def.Stack == EnchantStack.Max)
                {
                    // Highest copy wins: permille for multipliers, largest magnitude otherwise (Resilience is negative).
                    bool rel = def.Mode == EnchantMode.Relative;
                    int value = rel ? EnchantMath.Permille(def, e.Level) : EnchantMath.Raw(def, e.Level, item.objectData);
                    var picks = rel ? _maxRel : _maxFlat;
                    if (!picks.TryGetValue(def.Id, out var best) || System.Math.Abs(value) > System.Math.Abs(best.Value))
                        picks[def.Id] = new MaxPick { Def = def, Value = value };
                    continue;
                }
                if (def.Mode == EnchantMode.Relative)
                {
                    var weapon = EnchantMath.BucketWeapon(def, item.objectID, _held);
                    var cond = EnchantMath.WriteCondition(def, weapon);
                    var want = _rel.TryGetValue(cond, out var r) ? r : new RelWant { Def = def, Weapon = weapon };
                    want.Permille += EnchantMath.Permille(def, e.Level);
                    _rel[cond] = want;
                }
                else
                {
                    int value = EnchantMath.Raw(def, e.Level, item.objectData);
                    _flat[def.Condition] = _flat.TryGetValue(def.Condition, out int v) ? v + value : value;
                }
            }
        }

        /// <summary>
        /// Model B value for one write condition, from the player's current summary. <paramref name="ourEntry"/> is
        /// the value of our own ConditionsBuffer entry for that condition (already inside the summary).
        /// Public so the selftest computes its expectation with the same function.
        /// </summary>
        public static int RelativeRaw(EntityManager em, Entity player, EnchantDef def, ObjectID weapon, ConditionID write, int permille, int ourEntry)
        {
            var fx = em.GetBuffer<SummarizedConditionEffectsBuffer>(player, true);
            var conds = em.GetBuffer<SummarizedConditionsBuffer>(player, true);
            int others = EnchantMath.BucketSum(def, write, weapon, fx, conds) - ourEntry;
            return EnchantMath.Relative(permille, others);
        }

        private void Apply(Entity player, PlayerState state)
        {
            var conditions = EntityManager.GetBuffer<ConditionsBuffer>(player, isReadOnly: true);
            _ours.Clear();
            bool duplicate = false;
            for (int i = 0; i < conditions.Length; i++)
            {
                var c = conditions[i].condition;
                if (!IsOurs(c)) continue;
                if (c.toBeRemoved || _ours.ContainsKey(c.conditionData.conditionID)) duplicate = true;
                _ours[c.conditionData.conditionID] = c.conditionData.value;
            }

            _wanted.Clear();
            foreach (var kv in _flat)
                if (kv.Value != 0) _wanted[kv.Key] = kv.Value;
            bool relChanged = false;
            // A changed enchant set (other level, other item) is not drift: it writes at once.
            bool relSetChanged = state.Permille.Count != _rel.Count;
            foreach (var kv in _rel)
                if (!state.Permille.TryGetValue(kv.Key, out int p) || p != kv.Value.Permille) relSetChanged = true;
            state.Permille.Clear();
            foreach (var kv in _rel) state.Permille[kv.Key] = kv.Value.Permille;
            foreach (var kv in _rel)
            {
                bool had = _ours.TryGetValue(kv.Key, out int cur);
                int raw = RelativeRaw(EntityManager, player, kv.Value.Def, kv.Value.Weapon, kv.Key, kv.Value.Permille, cur);
                if (_flat.TryGetValue(kv.Key, out int flatPart)) raw += flatPart; // (no enchant mixes modes today)
                if (had && !relSetChanged && EnchantMath.CloseEnough(cur, raw)) raw = cur;
                else if (had) relChanged = true;
                if (raw != 0) _wanted[kv.Key] = raw;
                else _wanted.Remove(kv.Key);
            }

            // Keys added/removed and Flat value changes write at once; Relative drift is throttled.
            bool structural = duplicate || relSetChanged || _ours.Count != _wanted.Count;
            bool flatChanged = false;
            foreach (var kv in _wanted)
            {
                if (!_ours.TryGetValue(kv.Key, out int cur)) { structural = true; break; }
                if (cur != kv.Value && !_rel.ContainsKey(kv.Key)) flatChanged = true;
            }
            if (!structural && !flatChanged && !relChanged) return;
            if (!structural && !flatChanged && _update - state.LastWrite < DriftInterval) return;

            if (EnchantDebug.On && (structural || flatChanged))
            {
                var parts = new List<string>();
                foreach (var kv in _wanted) parts.Add($"{kv.Key}={kv.Value}{(_rel.ContainsKey(kv.Key) ? "(x)" : "")}");
                Debug.Log($"[{EnchantingMod.Name}] player {player.Index} enchant bonuses: {(parts.Count == 0 ? "none" : string.Join(", ", parts))}");
            }

            // Rewrite only what changed: drop stale/duplicate entries, update values in place, add new ones.
            var rw = EntityManager.GetBuffer<ConditionsBuffer>(player);
            var kept = new HashSet<ConditionID>();
            for (int i = rw.Length - 1; i >= 0; i--)
            {
                var elem = rw[i];
                if (!IsOurs(elem.condition)) continue;
                var id = elem.condition.conditionData.conditionID;
                if (elem.condition.toBeRemoved || !_wanted.TryGetValue(id, out int want) || !kept.Add(id))
                {
                    rw.RemoveAt(i);
                    continue;
                }
                if (elem.condition.conditionData.value != want)
                {
                    elem.condition.conditionData.value = want;
                    rw[i] = elem;
                }
            }
            foreach (var kv in _wanted)
            {
                if (kept.Contains(kv.Key)) continue;
                rw.Add(new ConditionsBuffer
                {
                    condition = new Condition
                    {
                        conditionData = new ConditionData
                        {
                            conditionID = kv.Key,
                            value = kv.Value,
                            duration = float.PositiveInfinity,
                            valueMultiplier = Marker,
                        },
                    },
                });
            }
            state.LastWrite = _update;
        }
    }
}
