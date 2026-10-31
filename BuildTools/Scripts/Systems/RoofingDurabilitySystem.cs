using Inventory;
using PlayerEquipment;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace BuildTools.Systems
{
    /// <summary>
    /// "Roofing gadget never wears out": on the server, a held Roofing Gadget below full durability is
    /// topped back up to its prefab maximum through the game's InventoryChangeBuffer (a tool's
    /// durability is its stack amount). Only runs while the setting is on and only touches players
    /// whose equipped slot is the roofing gadget, so it costs nothing otherwise.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class RoofingDurabilitySystem : PugSimulationSystemBase
    {
        private EntityQuery _players;
        private EntityQuery _changes;

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(ComponentType.ReadOnly<EquipmentSlotCD>(), ComponentType.ReadOnly<EquippedObjectCD>());
            _changes = GetEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
        }

        protected override void OnUpdate()
        {
            if (!BuildToolsMod.RoofingNeverWears || _changes.IsEmptyIgnoreFilter) return;

            var players = _players.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (EntityManager.GetComponentData<EquipmentSlotCD>(player).slotType != EquipmentSlotType.RoofingToolSlot) continue;

                var held = EntityManager.GetComponentData<EquippedObjectCD>(player);
                var data = held.containedObject.objectData;
                if (data.objectID != ObjectID.RoofingTool) continue;
                if (held.equipmentPrefab == Entity.Null || !EntityManager.HasComponent<DurabilityCD>(held.equipmentPrefab)) continue;

                int max = EntityManager.GetComponentData<DurabilityCD>(held.equipmentPrefab).maxDurability;
                if (max <= 0 || data.amount >= max) continue;

                var changes = EntityManager.GetBuffer<InventoryChangeBuffer>(_changes.GetSingletonEntity());
                changes.Add(new InventoryChangeBuffer
                {
                    playerEntity = player,
                    inventoryChangeData = Create.SetAmount(player, held.equippedSlotIndex, ObjectID.RoofingTool, max),
                });
            }
            players.Dispose();
        }
    }
}
