using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// "/enchant selftest": checks every enchant in the pools of the held item, the worn helm/chest/pants, the worn
    /// accessories (ring1, ring2, necklace, offhand) and the carried summoning tome against the game's own stat
    /// totals. Per enchant and level (I and III): put only that enchant on the item, wait a few ticks for
    /// EnchantEffectSystem + the vanilla SummarizeConditionsSystem, read the player's summed value for the condition
    /// the enchant writes, and compare it with every tested item enchant-free (baseline). The expected value comes
    /// from the same functions the effect system uses:
    ///   Flat / ItemLevel: EnchantMath.Raw (item-level procs use this item's level).
    ///   Relative (Model B): EnchantEffectSystem.RelativeRaw from the player's current bucket. PASS needs our entry
    ///   to match it (within the effect system's hysteresis), and both the condition total and the effect bucket
    ///   to rise by exactly our entry (the latter proves the ConditionID lands in the bucket we multiply).
    /// Each slot's baseline step clears the enchants of every tested item, so a copy on another accessory can't skew
    /// the enchant under test. With two rings worn, stack-rule steps follow (Max rule for any Stack = Max def): the
    /// same enchant on both rings must add both copies (Precision III + I, III + III, Might III + III).
    /// Each item's own enchants are restored at the end. Results go to Player.log ("[Enchanting] selftest ...") and
    /// "/enchant selftest status". Server side; single player / host.
    /// Carried rule: hold a tome once, switch to another item (the tome stays in the inventory), run the selftest:
    /// the tome is then tested while not held.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class EnchantSelfTestSystem : PugSimulationSystemBase
    {
        private const int SettleTicks = 6; // effect system waits 2 updates after its last write, summary 1 more
        private static readonly int[] Levels = { 1, 3 };

        private sealed class Step
        {
            public int Slot;
            public ObjectID Item;
            public EnchantDef Def;   // null = baseline for this slot (clears every tested item first)
            public int Level;
            // Two-ring check: the same enchant also goes on Slot2 at Level2. Stack = Sum adds both copies,
            // Stack = Max keeps only the higher one.
            public int Slot2 = -1;
            public ObjectID Item2;
            public int Level2;

            public int ExpectLevel => Slot2 >= 0 && Def != null && Def.Stack == EnchantStack.Max ? Mathf.Max(Level, Level2) : Level;
            /// <summary>Two copies that add (Stack = Sum).</summary>
            public bool Adds => Slot2 >= 0 && Def != null && Def.Stack == EnchantStack.Sum;
        }

        private static Entity _player = Entity.Null;
        private static readonly List<Step> Steps = new List<Step>();
        private static readonly Dictionary<int, List<EnchantEntry>> Originals = new Dictionary<int, List<EnchantEntry>>();
        private static readonly Dictionary<int, int[]> BaseConds = new Dictionary<int, int[]>();
        private static readonly Dictionary<int, int[]> BaseEffects = new Dictionary<int, int[]>();
        private static readonly List<string> Results = new List<string>();
        private static int _index, _wait, _pass, _fail;
        private static bool _running, _applied;

        // A new server world: drop state from a selftest that was running when the old world closed.
        protected override void OnCreate()
        {
            base.OnCreate();
            _running = false;
            _applied = false;
            _player = Entity.Null;
            Steps.Clear();
            Originals.Clear();
            BaseConds.Clear();
            BaseEffects.Clear();
            _index = _wait = 0;
        }

        public static string Status()
        {
            if (_running) return $"selftest running: step {_index + 1}/{Steps.Count}, {_pass} pass, {_fail} fail so far.";
            if (Results.Count == 0) return "no selftest run yet. Hold a weapon/tool, wear armor and accessories, then /enchant selftest.";
            return $"selftest done: {_pass} pass, {_fail} fail. " + string.Join(" | ", Results);
        }

        /// <summary>Queues the test. Returns a message for chat.</summary>
        public static string Start(EntityManager em, Entity player)
        {
            if (_running) return "a selftest is already running.";
            if (!EnchantStorage.TryGetAux(em, out var aux)) return "inventory aux data not ready.";
            var items = em.GetBuffer<ContainedObjectsBuffer>(player, true);
            var equipment = em.GetComponentData<EquipmentCD>(player);
            int held = em.GetComponentData<EquippedObjectCD>(player).equippedSlotIndex;

            Steps.Clear();
            Originals.Clear();
            BaseConds.Clear();
            BaseEffects.Clear();
            Results.Clear();
            _pass = _fail = _index = 0;
            int carried = EnchantEffectSystem.CarriedSlot(player);
            var slots = new[]
            {
                held, equipment.helmSlotIndex, equipment.breastSlotIndex, equipment.pantsSlotIndex,
                equipment.ring1SlotIndex, equipment.ring2SlotIndex, equipment.necklaceSlotIndex, equipment.offHandIndex,
                carried,
            };
            foreach (int slot in slots)
            {
                if (slot < 0 || slot >= items.Length || Originals.ContainsKey(slot)) continue;
                var item = items[slot];
                var kind = Enchants.SlotOf(item.objectID);
                if (kind == EnchantSlot.None) continue;
                bool summon = kind == EnchantSlot.Summon;
                if (slot == held && !summon && !Enchants.IsHeldSlot(kind)) continue;   // armor/rings held in hand give nothing
                if (slot != held && Enchants.IsHeldSlot(kind)) continue;
                if (summon && slot != held && slot != carried) continue;
                Originals[slot] = EnchantStorage.Read(em, aux, item);
                Steps.Add(new Step { Slot = slot, Item = item.objectID });
                foreach (var def in Enchants.PoolFor(kind, item.objectID))
                    foreach (int level in Levels)
                        if (level <= def.MaxLevel) Steps.Add(new Step { Slot = slot, Item = item.objectID, Def = def, Level = level });
            }
            AddMaxRuleSteps(items, equipment.ring1SlotIndex, equipment.ring2SlotIndex);
            if (Steps.Count == 0) return "nothing to test: hold a weapon/tool/tome and/or wear armor or accessories.";
            _player = player;
            _running = true;
            _applied = false;
            Debug.Log($"[{EnchantingMod.Name}] selftest start: {Steps.Count} steps over {Originals.Count} items");
            return $"selftest started: {Steps.Count} steps (~{Steps.Count * SettleTicks / 60 + 1} s). Don't switch items. /enchant selftest status";
        }

        // Two rings worn: the same enchant on both rings must add up (Stack = Sum), or count once if Stack = Max.
        private static void AddMaxRuleSteps(DynamicBuffer<ContainedObjectsBuffer> items, int ring1, int ring2)
        {
            if (!Originals.ContainsKey(ring1) || !Originals.ContainsKey(ring2)) return;
            var precision = Enchants.Find("precision");
            var might = Enchants.Find("might");
            var a = items[ring1].objectID;
            var b = items[ring2].objectID;
            Steps.Add(new Step { Slot = ring1, Item = a });
            if (precision != null)
            {
                Steps.Add(new Step { Slot = ring1, Item = a, Def = precision, Level = 3, Slot2 = ring2, Item2 = b, Level2 = 1 });
                Steps.Add(new Step { Slot = ring1, Item = a, Def = precision, Level = 3, Slot2 = ring2, Item2 = b, Level2 = 3 });
            }
            if (might != null)
                Steps.Add(new Step { Slot = ring1, Item = a, Def = might, Level = 3, Slot2 = ring2, Item2 = b, Level2 = 3 });
        }

        protected override void OnUpdate()
        {
            if (!_running) return;
            var em = EntityManager;
            if (_player == Entity.Null || !em.Exists(_player)) { Finish(em, "player gone"); return; }
            var step = Steps[_index];
            var items = em.GetBuffer<ContainedObjectsBuffer>(_player, true);
            if (Moved(items, step.Slot, step.Item) || (step.Slot2 >= 0 && Moved(items, step.Slot2, step.Item2))) { Finish(em, "item moved"); return; }

            if (!_applied)
            {
                string err = null;
                if (step.Def == null)
                {
                    // Baseline: every tested item enchant-free, so no copy elsewhere (Max rule, caps) skews the delta.
                    foreach (var slot in Originals.Keys)
                    {
                        err = EnchantStorage.Write(em, _player, slot, new List<EnchantEntry>());
                        if (err != null) break;
                    }
                }
                else
                {
                    err = EnchantStorage.Write(em, _player, step.Slot, One(step.Def, step.Level));
                    if (err == null && step.Slot2 >= 0) err = EnchantStorage.Write(em, _player, step.Slot2, One(step.Def, step.Level2));
                }
                if (err != null) { Finish(em, err); return; }
                _applied = true;
                // Minion enchants: EnchantMinionSystem syncs every Interval updates after the effect system.
                _wait = step.Def != null && step.Def.OnMinions ? SettleTicks + EnchantMinionSystem.Interval + 2 : SettleTicks;
                return;
            }
            if (--_wait > 0) return;

            items = em.GetBuffer<ContainedObjectsBuffer>(_player, true);
            var sum = em.GetBuffer<SummarizedConditionsBuffer>(_player, true);
            var fx = em.GetBuffer<SummarizedConditionEffectsBuffer>(_player, true);
            if (step.Def == null)
            {
                BaseConds[step.Slot] = Copy(sum);
                BaseEffects[step.Slot] = Copy(fx);
            }
            else if (step.Def.OnMinions)
            {
                CheckMinions(em, items[step.Slot], step);
            }
            else
            {
                var item = items[step.Slot];
                var def = step.Def;
                int level = step.ExpectLevel;
                int heldSlot = em.GetComponentData<EquippedObjectCD>(_player).equippedSlotIndex;
                var held = heldSlot >= 0 && heldSlot < items.Length ? items[heldSlot].objectID : ObjectID.None;
                var weapon = EnchantMath.BucketWeapon(def, item.objectID, held);
                ConditionID cond;
                if (def.Mode == EnchantMode.Relative) cond = EnchantMath.WriteCondition(def, weapon);
                else if (def.FollowsLight) cond = LightEntry(em, _player);
                else cond = def.Condition;
                int baseline = At(BaseConds[step.Slot], (int)cond);
                int now = (int)cond < sum.Length ? sum[(int)cond].value : 0;
                string prefix = step.Slot2 < 0 ? ""
                    : step.Adds ? $"Stack rule (two rings {def.Name} {Enchants.Roman(step.Level)} + {Enchants.Roman(step.Level2)}, both must count): "
                    : $"Max rule (two rings {def.Name} {Enchants.Roman(step.Level)} + {Enchants.Roman(step.Level2)}, only {Enchants.Roman(level)} may count): ";
                int permille = EnchantMath.Permille(def, level) + (step.Adds ? EnchantMath.Permille(def, step.Level2) : 0);
                bool ok;
                string line;
                if (def.Mode == EnchantMode.Relative)
                {
                    int ours = OurEntry(em, _player, cond);
                    int expected = EnchantEffectSystem.RelativeRaw(em, _player, def, weapon, cond, permille, ours);
                    var effect = EnchantMath.EffectOf(cond);
                    int fxBase = At(BaseEffects[step.Slot], (int)effect);
                    int fxNow = (int)effect < fx.Length ? fx[(int)effect].value : 0;
                    ok = ours != 0 && (ours == expected || EnchantMath.CloseEnough(ours, expected)) && now - baseline == ours && fxNow - fxBase == ours;
                    line = $"{(ok ? "PASS" : "FAIL")} {prefix}{step.Item} {def.Name} {Enchants.Roman(level)}: {EnchantMath.MultiplierText(def, level, weapon)} -> {cond} entry {ours} (expected {expected}), total +{now - baseline}, {effect} +{fxNow - fxBase}";
                }
                else
                {
                    int expected = EnchantMath.Raw(def, level, item.objectData)
                        + (step.Adds ? EnchantMath.Raw(def, step.Level2, items[step.Slot2].objectData) : 0);
                    ok = now - baseline == expected;
                    string lvl = def.Mode == EnchantMode.ItemLevel ? $" at item level {EnchantMath.ItemLevel(item.objectData)}" : "";
                    line = $"{(ok ? "PASS" : "FAIL")} {prefix}{step.Item} {def.Name} {Enchants.Roman(level)}: {cond} +{now - baseline} (expected +{expected}{lvl}, total {baseline}->{now})";
                }
                if (ok) _pass++; else _fail++;
                Debug.Log($"[{EnchantingMod.Name}] selftest {line}");
                if (!ok || step.Slot2 >= 0 || step.Level == Levels[Levels.Length - 1]) Results.Add(line);
            }

            _index++;
            _applied = false;
            if (_index >= Steps.Count) Finish(em, null);
        }

        private static readonly List<Entity> Minions = new List<Entity>();

        // Minion on-hit (OnMinions): every live minion of the player must carry our entry with the raw value, and its
        // summary must include it. No minions out: SKIP (not counted), summon some and rerun.
        private static void CheckMinions(EntityManager em, ContainedObjectsBuffer item, Step step)
        {
            var def = step.Def;
            int expected = EnchantMath.Raw(def, step.Level, item.objectData);
            EnchantMinionSystem.MinionsOf(em, _player, Minions);
            string line;
            if (Minions.Count == 0)
            {
                line = $"SKIP {step.Item} {def.Name} {Enchants.Roman(step.Level)}: no minions out (summon some, then rerun)";
                Debug.Log($"[{EnchantingMod.Name}] selftest {line}");
                if (step.Level == Levels[Levels.Length - 1]) Results.Add(line);
                return;
            }
            int good = 0;
            var detail = new List<string>();
            foreach (var m in Minions)
            {
                int ours = OurEntry(em, m, def.Condition);
                var sum = em.GetBuffer<SummarizedConditionsBuffer>(m, true);
                int total = (int)def.Condition < sum.Length ? sum[(int)def.Condition].value : 0;
                if (ours == expected && total >= expected) good++;
                detail.Add($"#{m.Index} entry {ours} total {total}");
            }
            bool ok = good == Minions.Count;
            line = $"{(ok ? "PASS" : "FAIL")} {step.Item} {def.Name} {Enchants.Roman(step.Level)}: {def.Condition} on {good}/{Minions.Count} minions (expected {expected}; {string.Join(", ", detail)})";
            if (ok) _pass++; else _fail++;
            Debug.Log($"[{EnchantingMod.Name}] selftest {line}");
            if (!ok || step.Level == Levels[Levels.Length - 1]) Results.Add(line);
        }

        private static List<EnchantEntry> One(EnchantDef def, int level) =>
            new List<EnchantEntry> { new EnchantEntry { Id = def.Id, Level = level } };

        private static bool Moved(DynamicBuffer<ContainedObjectsBuffer> items, int slot, ObjectID item) =>
            slot < 0 || slot >= items.Length || items[slot].objectID != item;

        private static int[] Copy(DynamicBuffer<SummarizedConditionsBuffer> b)
        {
            var a = new int[b.Length];
            for (int i = 0; i < a.Length; i++) a[i] = b[i].value;
            return a;
        }

        private static int[] Copy(DynamicBuffer<SummarizedConditionEffectsBuffer> b)
        {
            var a = new int[b.Length];
            for (int i = 0; i < a.Length; i++) a[i] = b[i].value;
            return a;
        }

        private static int At(int[] a, int i) => i >= 0 && i < a.Length ? a[i] : 0;

        // Our ConditionsBuffer entry for a condition (EnchantEffectSystem keeps one per condition).
        private static int OurEntry(EntityManager em, Entity player, ConditionID id)
        {
            var conditions = em.GetBuffer<ConditionsBuffer>(player, true);
            for (int i = 0; i < conditions.Length; i++)
            {
                var c = conditions[i].condition;
                if (EnchantEffectSystem.IsOurs(c) && c.conditionData.conditionID == id) return c.conditionData.value;
            }
            return 0;
        }

        // Radiance: the glow colour our entry was written to (falls back to the colour the effect system would pick).
        private static ConditionID LightEntry(EntityManager em, Entity player)
        {
            foreach (var id in EnchantMath.GlowConditions)
                if (OurEntry(em, player, id) != 0) return id;
            return EnchantEffectSystem.LightCondition(em, player);
        }

        private static void Finish(EntityManager em, string error)
        {
            if (_player != Entity.Null && em.Exists(_player))
                foreach (var kv in Originals)
                {
                    var items = em.GetBuffer<ContainedObjectsBuffer>(_player, true);
                    if (kv.Key < items.Length && items[kv.Key].objectID != ObjectID.None)
                        EnchantStorage.Write(em, _player, kv.Key, kv.Value);
                }
            _running = false;
            if (error != null) Results.Insert(0, "ABORTED: " + error);
            Debug.Log($"[{EnchantingMod.Name}] selftest done: {_pass} pass, {_fail} fail{(error != null ? ", aborted: " + error : "")}");
        }
    }
}
