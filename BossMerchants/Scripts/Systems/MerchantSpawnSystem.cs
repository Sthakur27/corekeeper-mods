using Pug.UnityExtensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace BossMerchants.Systems
{
    /// <summary>
    /// Server side. Brings a boss merchant into a base the vanilla way: an unclaimed bed in a detected room
    /// whose objects include that merchant's idol gets the merchant, once per world (one of each merchant).
    ///
    /// Mirrors vanilla SpawnMerchantSystem (every 5 s), whose candidate list is hard-coded to the six vanilla
    /// merchants, so modded merchants need their own pass. Spawn: create the entity on a random empty room
    /// tile, play the NPC spawn effect, give it a fresh CharacterGuid and claim the bed with it.
    /// Moving a merchant whose bed was destroyed to another idol room is left to vanilla: it asks
    /// GetRequiredObjectForMerchant, which <see cref="Patches.RequiredIdolPatch"/> answers for our merchants.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MerchantSpawnSystem : PugSimulationSystemBase
    {
        private const float IntervalSeconds = 5f;
        private float _timer = 3.7f; // staggered against vanilla's 5 s timer and the other merchant mods

        private EntityQuery _npcs;
        private EntityQuery _beds;
        private EntityQuery _database;
        private EntityQuery _effects;

        protected override void OnCreate()
        {
            base.OnCreate();
            _npcs = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<MerchantCD>(), ComponentType.ReadOnly<ObjectDataCD>() },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });
            _beds = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<BedCD>(),
                    ComponentType.ReadOnly<RoomObjectBuffer>(),
                    ComponentType.ReadOnly<RoomEmptyPositions>(),
                    ComponentType.ReadOnly<ClaimedByCharacterGuidCD>(),
                    ComponentType.ReadOnly<DetectRoomCD>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            _effects = GetEntityQuery(ComponentType.ReadOnly<EffectEventBuffer>());
        }

        protected override void OnUpdate()
        {
            _timer -= World.Time.DeltaTime;
            if (_timer > 0f) return;
            _timer = IntervalSeconds;
            if (_beds.IsEmptyIgnoreFilter || _database.IsEmpty) return;
            Catalog.Resolve();

            // Which of our merchants already exist (one per world, like vanilla).
            System.Collections.Generic.List<Catalog.Merchant> missing = null;
            var npcObjects = _npcs.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            foreach (var m in Catalog.Merchants)
            {
                if (m.MerchantId == ObjectID.None || m.IdolId == ObjectID.None) continue;
                bool present = false;
                for (int i = 0; i < npcObjects.Length && !present; i++) present = npcObjects[i].objectID == m.MerchantId;
                if (present) continue;
                if (missing == null) missing = new System.Collections.Generic.List<Catalog.Merchant>();
                missing.Add(m);
            }
            npcObjects.Dispose();
            if (missing == null) return;

            var beds = _beds.ToEntityArray(Allocator.Temp);
            var free = new NativeList<Entity>(beds.Length, Allocator.Temp);
            foreach (var bed in beds)
            {
                if (EntityManager.GetComponentData<ClaimedByCharacterGuidCD>(bed).isClaimed) continue;
                if (!EntityManager.GetComponentData<DetectRoomCD>(bed).roomDetected) continue;
                if (EntityManager.HasComponent<EntityDestroyedCD>(bed) && EntityManager.IsComponentEnabled<EntityDestroyedCD>(bed)) continue;
                free.Add(bed);
            }
            beds.Dispose();
            if (free.Length == 0) { free.Dispose(); return; }

            var database = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            bool haveEffects = _effects.TryGetSingletonEntity<EffectEventBuffer>(out var effectEntity);
            var rng = PugRandom.GetRng();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            int spawned = 0;
            foreach (var m in missing)
            {
                for (int b = 0; b < free.Length; b++)
                {
                    var bed = free[b];
                    var empty = EntityManager.GetBuffer<RoomEmptyPositions>(bed, true);
                    var objects = EntityManager.GetBuffer<RoomObjectBuffer>(bed, true);
                    if (empty.Length == 0 || objects.Length == 0) continue;
                    bool hasIdol = false;
                    for (int o = 0; o < objects.Length && !hasIdol; o++) hasIdol = objects[o].Value.objectID == m.IdolId;
                    if (!hasIdol) continue;

                    int2 tile = empty[rng.NextInt(0, empty.Length)].Value;
                    var pos = new float3(tile.x, 0f, tile.y);
                    var npc = EntityUtility.CreateEntity(ecb, pos, m.MerchantId, 1, database);
                    if (haveEffects)
                        EntityUtility.PlayEffectEventServer(ecb, effectEntity, new EffectEventCD
                        {
                            effectID = EffectID.SpawnNPC,
                            value1 = (int)m.MerchantId,
                            position1 = pos
                        });
                    var guid = PugRandom.GenerateGuid(rng);
                    ecb.SetComponent(npc, new CharacterGuidCD { Value = guid });
                    ecb.RemoveComponent<CreateNewGuidCD>(npc);
                    ecb.SetComponent(bed, new ClaimedByCharacterGuidCD { characterGuid = guid });
                    free.RemoveAtSwapBack(b);
                    spawned++;
                    Debug.Log($"[{BossMerchantsMod.Name}] {m.Key} merchant moved into the room at {tile}.");
                    break;
                }
                if (free.Length == 0) break;
            }
            free.Dispose();
            if (spawned > 0) ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
