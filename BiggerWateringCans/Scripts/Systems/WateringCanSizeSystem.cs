using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BiggerWateringCans.Systems
{
    /// <summary>
    /// Safety net that runs in both worlds:
    ///  - Once: makes sure the PugDatabase blob (what the Burst equipment code and the aim preview actually
    ///    read) has the mod sizes for both cans. If the authoring-time edits already landed this is a no-op.
    ///  - Per player entity, once: resets the WaterCanSlot size-variation to "full size". The game persists
    ///    the chosen variation on the player, so a character that used a vanilla 3x3 can would otherwise
    ///    keep a 3x3 iron can until the rotate key is pressed. Index 3 = full size for the 4x4 iron can and
    ///    is clamped to 2x2 for the basic can (GetTileSizeFromVariation clamps to prefabTileSize.x - 1).
    ///    New player entities change the buffer's component order version, so the player pass only runs
    ///    when that changed, plus a slow safety rescan every <see cref="RescanSeconds"/> s.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class WateringCanSizeSystem : PugSimulationSystemBase
    {
        private EntityQuery _database;
        private EntityQuery _players;
        private bool _blobChecked;
        private readonly HashSet<Entity> _resetPlayers = new HashSet<Entity>();
        private const double RescanSeconds = 10.0;
        private int _playersOrder = -1;
        private double _nextRescan;

        protected override void OnCreate()
        {
            base.OnCreate();
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            _players = GetEntityQuery(ComponentType.ReadOnly<PlacementSizeByEquipmentTypeBuffer>());
        }

        protected override void OnUpdate()
        {
            if (!_blobChecked && !_database.IsEmptyIgnoreFilter)
            {
                _blobChecked = true;
                FixBlob();
            }

            // Only after a player entity appeared/moved archetype (order version), or as a slow safety net.
            int order = EntityManager.GetComponentOrderVersion<PlacementSizeByEquipmentTypeBuffer>();
            double now = World.Time.ElapsedTime;
            if (order == _playersOrder && now < _nextRescan) return;
            _playersOrder = order;
            _nextRescan = now + RescanSeconds;

            if (_players.IsEmptyIgnoreFilter) return;
            var entities = _players.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (!_resetPlayers.Add(e)) continue;
                var readOnly = EntityManager.GetBuffer<PlacementSizeByEquipmentTypeBuffer>(e, true);
                if (readOnly.Length <= 1) continue;
                var element = readOnly[1]; // index 1 == EquipmentSlotType.WaterCanSlot (GetElementForEquipment)
                if (element.sizeVariationToPlace == PlacementSizeByEquipmentTypeBuffer.UNINITIALIZED_SIZE ||
                    element.sizeVariationToPlace >= CanSizes.MaxVariation) continue;
                element.sizeVariationToPlace = CanSizes.MaxVariation;
                var buffer = EntityManager.GetBuffer<PlacementSizeByEquipmentTypeBuffer>(e); // read-write only to write
                buffer[1] = element;
            }
            entities.Dispose();
        }

        private void FixBlob()
        {
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            if (!bank.databaseBankBlob.IsCreated) return;
            string side = World.IsServer() ? "server" : "client";
            foreach (var id in new[] { ObjectID.WaterCan, ObjectID.LargeWaterCan })
            {
                ref var info = ref PugDatabase.GetEntityObjectInfo(id, bank.databaseBankBlob);
                if (CanSizes.Apply(ref info, id))
                {
                    Debug.Log($"[{BiggerWateringCansMod.Name}] patched {id} in the database blob to {info.prefabTileSize} ({side}).");
                }
                else if (info.objectID == id)
                {
                    Debug.Log($"[{BiggerWateringCansMod.Name}] {id} blob size already {info.prefabTileSize} ({side}).");
                }
            }
        }
    }
}
