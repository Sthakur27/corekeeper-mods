using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ItemSpawner
{
    /// <summary>
    /// Server side. The CreateItem command puts an item straight into an inventory, but the game only sets a
    /// new pet up (random skin + 9 talents, PetInitSystem) when the pet is dropped into the world. A spawned
    /// pet therefore has no aux data, and vanilla draws a pet without a PetSkinCD as an empty slot (bag,
    /// cursor, pedestal) and won't hold it. For every inventory holding such a pet we queue the game's own
    /// PetInitializeAuxDataCD request, so the pet gets set up exactly like a hatched one.
    /// Players (bag + cursor, which is a slot of the player's own buffer) are checked every second; every
    /// other inventory (chests, pedestals) once, a few seconds after the world loads, which repairs pets
    /// spawned before this fix.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PetInitRepairSystem : SystemBase
    {
        private const double PlayerInterval = 1.0;
        private const double FullScanDelay = 5.0;

        private EntityQuery _players;
        private EntityQuery _inventories;
        private double _nextPlayerScan;
        private double _fullScanAt = -1;
        private bool _fullScanDone;

        protected override void OnCreate()
        {
            _players = GetEntityQuery(ComponentType.ReadOnly<PlayerGhost>(), ComponentType.ReadOnly<ContainedObjectsBuffer>());
            _inventories = GetEntityQuery(ComponentType.ReadOnly<ContainedObjectsBuffer>());
        }

        protected override void OnUpdate()
        {
            double now = World.Time.ElapsedTime;
            if (!_fullScanDone)
            {
                if (_fullScanAt < 0) _fullScanAt = now + FullScanDelay;
                if (now >= _fullScanAt)
                {
                    _fullScanDone = true;
                    int n = Scan(_inventories);
                    if (n > 0) Debug.Log($"[{ItemSpawnerMod.Name}] set up {n} inventor{(n == 1 ? "y" : "ies")} holding uninitialized pets");
                    return;
                }
            }
            if (now < _nextPlayerScan) return;
            _nextPlayerScan = now + PlayerInterval;
            Scan(_players);
        }

        private int Scan(EntityQuery query)
        {
            if (query.IsEmptyIgnoreFilter) return 0;
            int queued = 0;
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (!NeedsPetInit(EntityManager.GetBuffer<ContainedObjectsBuffer>(entity, true))) continue;
                var request = EntityManager.CreateEntity(typeof(PetInitializeAuxDataCD), typeof(BlockSaveCD));
                EntityManager.SetComponentData(request, new PetInitializeAuxDataCD { EntityContainingPet = entity });
                queued++;
            }
            return queued;
        }

        private static bool NeedsPetInit(DynamicBuffer<ContainedObjectsBuffer> items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.objectID != ObjectID.None && item.auxDataIndex == 0 && PugDatabase.HasComponent<PetCD>(item.objectID))
                    return true;
            }
            return false;
        }
    }
}
