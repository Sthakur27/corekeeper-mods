using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Server: keeps one ConditionsBuffer entry per enchant condition on each player, summed from the held item
    /// (weapons, tools, rods) and the equipped armor. Entries are marked by an infinite duration plus
    /// <see cref="Marker"/> as valueMultiplier: infinite conditions never expire and survive death, and the
    /// vanilla summary (SummarizeConditionsSystem) adds every entry's value to the stats. Writes only on change,
    /// since any ConditionsBuffer write makes the game re-summarize that player.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class EnchantEffectSystem : PugSimulationSystemBase
    {
        public const float Marker = 0.4242f;

        private EntityQuery _players;
        private readonly Dictionary<ConditionID, int> _wanted = new Dictionary<ConditionID, int>();

        public static bool IsOurs(in Condition c) =>
            float.IsPositiveInfinity(c.conditionData.duration) && Mathf.Approximately(c.conditionData.valueMultiplier, Marker);

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(
                ComponentType.ReadOnly<PlayerGhost>(),
                ComponentType.ReadOnly<EquipmentCD>(),
                ComponentType.ReadOnly<EquippedObjectCD>(),
                ComponentType.ReadOnly<ContainedObjectsBuffer>(),
                ComponentType.ReadWrite<ConditionsBuffer>());
            RequireForUpdate(_players);
        }

        protected override void OnUpdate()
        {
            if (!EnchantStorage.TryGetAux(EntityManager, out var aux)) return;
            var players = _players.ToEntityArray(Allocator.Temp);
            foreach (var player in players)
            {
                Collect(player, aux);
                Apply(player);
            }
            players.Dispose();
        }

        private void Collect(Entity player, InventoryAuxDataSystemDataCD aux)
        {
            _wanted.Clear();
            var items = EntityManager.GetBuffer<ContainedObjectsBuffer>(player);
            var equipment = EntityManager.GetComponentData<EquipmentCD>(player);
            int held = EntityManager.GetComponentData<EquippedObjectCD>(player).equippedSlotIndex;

            AddFrom(items, held, aux, heldSlot: true);
            AddFrom(items, equipment.helmSlotIndex, aux, heldSlot: false);
            AddFrom(items, equipment.breastSlotIndex, aux, heldSlot: false);
            AddFrom(items, equipment.pantsSlotIndex, aux, heldSlot: false);
        }

        private void AddFrom(DynamicBuffer<ContainedObjectsBuffer> items, int slot, InventoryAuxDataSystemDataCD aux, bool heldSlot)
        {
            if (slot < 0 || slot >= items.Length) return;
            var item = items[slot];
            if (item.objectID == ObjectID.None || item.auxDataIndex == 0) return;
            var kind = Enchants.SlotOf(item.objectID);
            if (kind == EnchantSlot.None || Enchants.IsHeldSlot(kind) != heldSlot) return;
            // Broken gear gives no stats in vanilla either.
            if (item.amount <= 0 && PugDatabase.HasComponent<DurabilityCD>(item.objectData)) return;

            foreach (var e in EnchantStorage.Read(EntityManager, aux, item))
            {
                var def = e.Def;
                if (def == null) continue;
                int value = EnchantUnits.Raw(def, e.Level);
                _wanted[def.Condition] = _wanted.TryGetValue(def.Condition, out int v) ? v + value : value;
            }
        }

        private void Apply(Entity player)
        {
            var conditions = EntityManager.GetBuffer<ConditionsBuffer>(player, isReadOnly: true);
            bool dirty = false;
            int ours = 0;
            for (int i = 0; i < conditions.Length; i++)
            {
                var c = conditions[i].condition;
                if (!IsOurs(c)) continue;
                ours++;
                if (!_wanted.TryGetValue(c.conditionData.conditionID, out int want) || want != c.conditionData.value || c.toBeRemoved)
                {
                    dirty = true;
                    break;
                }
            }
            if (!dirty && ours == _wanted.Count) return;

            var parts = new List<string>();
            foreach (var kv in _wanted) parts.Add($"{kv.Key}={kv.Value}");
            Debug.Log($"[{EnchantingMod.Name}] player {player.Index} enchant bonuses: {(parts.Count == 0 ? "none" : string.Join(", ", parts))}");

            var rw = EntityManager.GetBuffer<ConditionsBuffer>(player);
            for (int i = rw.Length - 1; i >= 0; i--)
                if (IsOurs(rw[i].condition)) rw.RemoveAt(i);
            foreach (var kv in _wanted)
            {
                if (kv.Value == 0) continue;
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
        }
    }
}
