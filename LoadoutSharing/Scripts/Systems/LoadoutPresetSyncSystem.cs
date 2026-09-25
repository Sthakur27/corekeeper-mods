using System.Collections.Generic;
using Inventory;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace LoadoutSharing.Systems
{
    /// <summary>
    /// Two things vanilla never has to do because its bag and pet slots are fixed:
    ///  1. point the main inventory's "extra size comes from this slot" at the active loadout's bag slot;
    ///  2. point PetOwnerCD at the active loadout's pet slot.
    /// Both honour the fallback rule: an empty own slot means loadout 1's bag or pet is used.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LoadoutLayoutSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class LoadoutPresetSyncSystem : PugSimulationSystemBase
    {
        private EntityQuery _players;
        private EntityQuery _changeBufferQuery;
        private readonly HashSet<Entity> _seen = new HashSet<Entity>();
        private bool _isServer;

        protected override void OnCreate()
        {
            base.OnCreate();
            _players = GetEntityQuery(
                ComponentType.ReadOnly<PlayerGhost>(),
                ComponentType.ReadOnly<EquipmentCD>(),
                ComponentType.ReadOnly<ActiveEquipmentPresetCD>(),
                ComponentType.ReadOnly<ContainedObjectsBuffer>(),
                ComponentType.ReadWrite<InventoryBuffer>(),
                ComponentType.ReadWrite<PetOwnerCD>());
            _changeBufferQuery = GetEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
            _isServer = World.IsServer();
            RequireForUpdate(_players);
        }

        protected override void OnUpdate()
        {
            if (!SlotLayout.Ready) return;

            bool hasChangeBuffer = _isServer && !_changeBufferQuery.IsEmptyIgnoreFilter;
            Entity changeBufferEntity = hasChangeBuffer ? _changeBufferQuery.GetSingletonEntity() : Entity.Null;

            var entities = _players.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];

                // Skip the very first frame we see a player; its buffers may still be settling.
                if (_seen.Add(entity)) continue;

                var invBuffers = EntityManager.GetBuffer<InventoryBuffer>(entity);
                if (invBuffers.Length == 0) continue;
                var contained = EntityManager.GetBuffer<ContainedObjectsBuffer>(entity);
                int containedLength = contained.Length;
                var equipment = EntityManager.GetComponentData<EquipmentCD>(entity);
                int preset = EntityManager.GetComponentData<ActiveEquipmentPresetCD>(entity).Value;
                var petOwner = EntityManager.GetComponentData<PetOwnerCD>(entity);

                bool changed = false;

                // 1. Bag -> inventory size.
                var mainInv = invBuffers[0];
                int wantBag = equipment.bagIndex;
                if (wantBag >= 0 && wantBag < containedLength && mainInv.extraInventorySizeSlot != wantBag)
                {
                    mainInv.extraInventorySizeSlot = wantBag;
                    invBuffers[0] = mainInv;
                    changed = true;
                }

                // 2. Pet slot.
                int wantPet = SlotLayout.EffectiveSlot(preset, SlotKind.Pet, contained);
                if (wantPet >= 0 && wantPet < containedLength && petOwner.SlotIndex != wantPet)
                {
                    petOwner.SlotIndex = wantPet;
                    EntityManager.SetComponentData(entity, petOwner);
                    changed = true;
                }

                if (changed && hasChangeBuffer)
                {
                    var changes = EntityManager.GetBuffer<InventoryChangeBuffer>(changeBufferEntity);
                    changes.Add(new InventoryChangeBuffer
                    {
                        playerEntity = entity,
                        inventoryChangeData = new InventoryChangeData
                        {
                            inventoryAction = InventoryAction.ClaimInventory,
                            inventory1 = entity
                        }
                    });
                }
            }
            entities.Dispose();
        }
    }
}
