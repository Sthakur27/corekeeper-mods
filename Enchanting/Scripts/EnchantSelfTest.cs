using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// "/enchant selftest": checks every enchant in the pools of the held item, the worn helm/chest/pants and the
    /// carried summoning tome against the game's own stat totals. Per enchant and level (I and III): put only that
    /// enchant on the item, wait a few ticks for EnchantEffectSystem + the vanilla SummarizeConditionsSystem, read
    /// the player's summed value for the condition the enchant writes, and compare it with the same item
    /// enchant-free (baseline). The expected value comes from the same functions the effect system uses:
    ///   Flat / ItemLevel: EnchantMath.Raw (item-level procs use this item's level).
    ///   Relative (Model B): EnchantEffectSystem.RelativeRaw from the player's current bucket. PASS needs our entry
    ///   to match it (within the effect system's hysteresis), and both the condition total and the effect bucket
    ///   to rise by exactly our entry (the latter proves the ConditionID lands in the bucket we multiply).
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
            public EnchantDef Def;   // null = baseline for this slot
            public int Level;
        }

        private static Entity _player = Entity.Null;
        private static readonly List<Step> Steps = new List<Step>();
        private static readonly Dictionary<int, List<EnchantEntry>> Originals = new Dictionary<int, List<EnchantEntry>>();
        private static readonly Dictionary<int, int[]> BaseConds = new Dictionary<int, int[]>();
        private static readonly Dictionary<int, int[]> BaseEffects = new Dictionary<int, int[]>();
        private static readonly List<string> Results = new List<string>();
        private static int _index, _wait, _pass, _fail;
        private static bool _running, _applied;

        public static string Status()
        {
            if (_running) return $"selftest running: step {_index + 1}/{Steps.Count}, {_pass} pass, {_fail} fail so far.";
            if (Results.Count == 0) return "no selftest run yet. Hold a weapon/tool, wear armor, then /enchant selftest.";
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
            foreach (int slot in new[] { held, equipment.helmSlotIndex, equipment.breastSlotIndex, equipment.pantsSlotIndex, carried })
            {
                if (slot < 0 || slot >= items.Length || Originals.ContainsKey(slot)) continue;
                var item = items[slot];
                var kind = Enchants.SlotOf(item.objectID);
                if (kind == EnchantSlot.None) continue;
                bool summon = kind == EnchantSlot.Summon;
                if (slot == held && !summon && !Enchants.IsHeldSlot(kind)) continue;   // armor held in hand gives nothing
                if (slot != held && Enchants.IsHeldSlot(kind)) continue;
                if (summon && slot != held && slot != carried) continue;
                Originals[slot] = EnchantStorage.Read(em, aux, item);
                Steps.Add(new Step { Slot = slot, Item = item.objectID });
                foreach (var def in Enchants.PoolFor(kind))
                    foreach (int level in Levels)
                        if (level <= def.MaxLevel) Steps.Add(new Step { Slot = slot, Item = item.objectID, Def = def, Level = level });
            }
            if (Steps.Count == 0) return "nothing to test: hold a weapon/tool/tome and/or wear armor.";
            _player = player;
            _running = true;
            _applied = false;
            Debug.Log($"[{EnchantingMod.Name}] selftest start: {Steps.Count} steps over {Originals.Count} items");
            return $"selftest started: {Steps.Count} steps (~{Steps.Count * SettleTicks / 60 + 1} s). Don't switch items. /enchant selftest status";
        }

        protected override void OnUpdate()
        {
            if (!_running) return;
            var em = EntityManager;
            if (_player == Entity.Null || !em.Exists(_player)) { Finish(em, "player gone"); return; }
            var step = Steps[_index];
            var items = em.GetBuffer<ContainedObjectsBuffer>(_player, true);
            if (step.Slot >= items.Length || items[step.Slot].objectID != step.Item) { Finish(em, "item moved"); return; }

            if (!_applied)
            {
                var list = new List<EnchantEntry>();
                if (step.Def != null) list.Add(new EnchantEntry { Id = step.Def.Id, Level = step.Level });
                string err = EnchantStorage.Write(em, _player, step.Slot, list);
                if (err != null) { Finish(em, err); return; }
                _applied = true;
                _wait = SettleTicks;
                return;
            }
            if (--_wait > 0) return;

            var sum = em.GetBuffer<SummarizedConditionsBuffer>(_player, true);
            var fx = em.GetBuffer<SummarizedConditionEffectsBuffer>(_player, true);
            if (step.Def == null)
            {
                BaseConds[step.Slot] = Copy(sum);
                BaseEffects[step.Slot] = Copy(fx);
            }
            else
            {
                var item = items[step.Slot];
                var def = step.Def;
                var cond = def.Mode == EnchantMode.Relative ? EnchantMath.WriteCondition(def, item.objectID) : def.Condition;
                int baseline = At(BaseConds[step.Slot], (int)cond);
                int now = (int)cond < sum.Length ? sum[(int)cond].value : 0;
                bool ok;
                string line;
                if (def.Mode == EnchantMode.Relative)
                {
                    int ours = OurEntry(em, _player, cond);
                    int expected = EnchantEffectSystem.RelativeRaw(em, _player, def, item.objectID, cond, EnchantMath.Permille(def, step.Level), ours);
                    var effect = EnchantMath.EffectOf(cond);
                    int fxBase = At(BaseEffects[step.Slot], (int)effect);
                    int fxNow = (int)effect < fx.Length ? fx[(int)effect].value : 0;
                    ok = ours != 0 && (ours == expected || EnchantMath.CloseEnough(ours, expected)) && now - baseline == ours && fxNow - fxBase == ours;
                    line = $"{(ok ? "PASS" : "FAIL")} {step.Item} {def.Name} {Enchants.Roman(step.Level)}: {EnchantMath.MultiplierText(def, step.Level, item.objectID)} -> {cond} entry {ours} (expected {expected}), total +{now - baseline}, {effect} +{fxNow - fxBase}";
                }
                else
                {
                    int expected = EnchantMath.Raw(def, step.Level, item.objectData);
                    ok = now - baseline == expected;
                    string lvl = def.Mode == EnchantMode.ItemLevel ? $" at item level {EnchantMath.ItemLevel(item.objectData)}" : "";
                    line = $"{(ok ? "PASS" : "FAIL")} {step.Item} {def.Name} {Enchants.Roman(step.Level)}: {cond} +{now - baseline} (expected +{expected}{lvl}, total {baseline}->{now})";
                }
                if (ok) _pass++; else _fail++;
                Debug.Log($"[{EnchantingMod.Name}] selftest {line}");
                if (!ok || step.Level == Levels[Levels.Length - 1]) Results.Add(line);
            }

            _index++;
            _applied = false;
            if (_index >= Steps.Count) Finish(em, null);
        }

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
