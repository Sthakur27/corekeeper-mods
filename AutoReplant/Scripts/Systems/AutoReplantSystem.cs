using System.Collections.Generic;
using Inventory;
using Pug.Properties;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace AutoReplant.Systems
{
    /// <summary>
    /// Server-only. Watches for ripe plants that a player just harvested and plants the same seed
    /// back on that tile through the exact inventory action vanilla placement uses.
    ///
    /// How vanilla works (decompiled Pug.Other):
    ///  - Harvest: the player's attack marks the plant entity (PlantCD + GrowingCD) destroyed
    ///    (EntityDestroyedCD enabled) and sets KilledByPlayerCD.playerEntity. DropLootSystem drops
    ///    the crop; PlayerController.OnHarvest grants 1 Gardening XP. Seeds drop from the plant's
    ///    loot table (chance scaled by the Gardening "SeedDropChance" condition) and are pulled to
    ///    the player.
    ///  - Planting: PlaceObjectSlot.PlaceItem / SeederSlot enqueue
    ///    Create.ConsumeEntityAt(player, slot, 1, destroy:false, dontConsume:godMode, tilePos, variation)
    ///    on the InventoryChangeBuffer. On the server InventoryUtility.TryConsume removes one seed
    ///    from that slot and instantiates the seed prefab (with GrowingCD) at the tile. Planting
    ///    grants no XP in vanilla; XP comes at harvest, which stays untouched.
    ///  - Golden: the seed prefab's ObjectPropertiesCD int property 1273594437 is the "golden"
    ///    variation. Vanilla rolls 3% + SummarizedConditionsBuffer[126] (ChanceToGainRarePlant) and
    ///    passes that variation to ConsumeEntityAt. PlantsGrowingSystem later turns the golden seed
    ///    into the golden plant variation, whose PlantCD drops the *Rare (golden) crop.
    ///
    /// This system reproduces the planting call verbatim (same InventoryChangeBuffer action, same
    /// position convention, same variation semantics), so the resulting seed entity is identical to
    /// a manually planted one. The only difference is the golden roll: the configured chance
    /// replaces vanilla's 3% base (the player's rare-plant bonus still adds on top).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class AutoReplantSystem : PugSimulationSystemBase
    {
        /// <summary>ObjectPropertiesCD key: ObjectID the seed becomes when fully grown (used by PlantsGrowingSystem).</summary>
        private const int PropGrowsInto = -1534320058;
        /// <summary>ObjectPropertiesCD key: variation index that makes a placed seed golden (used by PlaceObjectSlot/SeederSlot).</summary>
        private const int PropGoldenSeedVariation = 1273594437;
        /// <summary>SummarizedConditionsBuffer index of ConditionID.ChanceToGainRarePlant (vanilla adds it to the 3% base).</summary>
        private const int CondChanceToGainRarePlant = 126;
        /// <summary>How long we wait for the harvested plant to disappear and for a seed to show up.</summary>
        private const double WindowSeconds = 6.0;

        private struct SeedInfo
        {
            public ObjectID seed;
            public int goldenVariation;
        }

        private sealed class Pending
        {
            public Entity plant;
            public Entity player;
            public ObjectID plantId;
            public SeedInfo seed;
            public float3 position;
            public int baselineSeeds;
            public double expiresAt;
        }

        private EntityQuery _harvested;
        private EntityQuery _changeBuffer;
        private EntityQuery _database;

        private Dictionary<ObjectID, SeedInfo> _seedByPlant;
        private readonly HashSet<Entity> _seen = new HashSet<Entity>();
        private readonly List<Entity> _seenScratch = new List<Entity>();
        private readonly List<Pending> _pending = new List<Pending>();
        private readonly System.Random _rng = new System.Random();

        protected override void OnCreate()
        {
            base.OnCreate();
            // Enableable components in a query only match entities where they are enabled, so this
            // is exactly "plant that is being destroyed and was killed by a player".
            _harvested = GetEntityQuery(
                ComponentType.ReadOnly<PlantCD>(),
                ComponentType.ReadOnly<GrowingCD>(),
                ComponentType.ReadOnly<EntityDestroyedCD>(),
                ComponentType.ReadOnly<KilledByPlayerCD>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<ObjectPropertiesCD>());
            _changeBuffer = GetEntityQuery(ComponentType.ReadWrite<InventoryChangeBuffer>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            RequireForUpdate(_database);
        }

        protected override void OnUpdate()
        {
            if (!World.IsServer()) return;

            if (!AutoReplantMod.Enabled)
            {
                _pending.Clear();
                PruneSeen();
                return;
            }

            if (_seedByPlant == null && !BuildSeedMap()) return;

            double now = World.Time.ElapsedTime;
            DetectHarvests(now);
            ProcessPending(now);
            PruneSeen();
        }

        // ------------------------------------------------------------------ detection

        private void DetectHarvests(double now)
        {
            if (_harvested.IsEmptyIgnoreFilter) return;
            var entities = _harvested.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity plant = entities[i];
                if (!_seen.Add(plant)) continue;

                var growing = EntityManager.GetComponentData<GrowingCD>(plant);
                var props = EntityManager.GetComponentData<ObjectPropertiesCD>(plant);
                if (!growing.HasReachedFinalStage(props)) continue;     // only real harvests, not trampled seedlings

                var killedBy = EntityManager.GetComponentData<KilledByPlayerCD>(plant);
                Entity player = killedBy.playerEntity;
                if (player == Entity.Null || !EntityManager.Exists(player) || !EntityManager.HasBuffer<ContainedObjectsBuffer>(player)) continue;

                ObjectID plantId = EntityManager.GetComponentData<ObjectDataCD>(plant).objectID;
                if (!_seedByPlant.TryGetValue(plantId, out SeedInfo seed)) continue;

                float3 pos = EntityManager.GetComponentData<LocalTransform>(plant).Position;
                pos = new float3(math.round(pos.x), 0f, math.round(pos.z));   // same convention as PlaceItem

                _pending.Add(new Pending
                {
                    plant = plant,
                    player = player,
                    plantId = plantId,
                    seed = seed,
                    position = pos,
                    baselineSeeds = CountSeeds(player, seed.seed),
                    expiresAt = now + WindowSeconds
                });
            }
            entities.Dispose();
        }

        // ------------------------------------------------------------------ replanting

        private void ProcessPending(double now)
        {
            if (_pending.Count == 0) return;
            if (!_changeBuffer.TryGetSingletonEntity<InventoryChangeBuffer>(out Entity changeEntity)) return;

            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                Pending p = _pending[i];
                if (now > p.expiresAt || !EntityManager.Exists(p.player) || !EntityManager.HasBuffer<ContainedObjectsBuffer>(p.player))
                {
                    _pending.RemoveAt(i);
                    continue;
                }
                // The tile must be free: DestroyEntityIfPlacementNotValidSystem would delete the new
                // seed if the old plant entity still physically occupied the tile.
                if (EntityManager.Exists(p.plant)) continue;

                int count = CountSeeds(p.player, p.seed.seed);
                bool haveSeed = AutoReplantMod.UseInventorySeeds ? count > 0 : count > p.baselineSeeds;
                if (!haveSeed) continue;

                int slot = FindSeedSlot(p.player, p.seed.seed, out int itemVariation);
                if (slot < 0) continue;

                int variation = RollGolden(p.player, p.seed) ? p.seed.goldenVariation : itemVariation;

                var changes = EntityManager.GetBuffer<InventoryChangeBuffer>(changeEntity);
                changes.Add(new InventoryChangeBuffer
                {
                    playerEntity = p.player,
                    // Identical to PlaceObjectSlot.PlaceItem / SeederSlot: consume one seed from that
                    // slot and instantiate the seed prefab at the tile with the chosen variation.
                    inventoryChangeData = Create.ConsumeEntityAt(p.player, slot, 1, false, false, p.position, variation, default(float3), p.seed.seed)
                });

                _pending.RemoveAt(i);
            }
        }

        private bool RollGolden(Entity player, SeedInfo seed)
        {
            if (seed.goldenVariation <= 0) return false;
            float chance = AutoReplantMod.GoldenOverride ? AutoReplantMod.GoldenChancePercent : 3f; // vanilla base is 3%
            if (EntityManager.HasBuffer<SummarizedConditionsBuffer>(player))
            {
                var conditions = EntityManager.GetBuffer<SummarizedConditionsBuffer>(player);
                if (conditions.Length > CondChanceToGainRarePlant)
                    chance += conditions[CondChanceToGainRarePlant].value;
            }
            if (chance <= 0f) return false;
            return _rng.NextDouble() < chance / 100.0;
        }

        // ------------------------------------------------------------------ inventory helpers

        private int CountSeeds(Entity player, ObjectID seed)
        {
            var items = EntityManager.GetBuffer<ContainedObjectsBuffer>(player);
            int total = 0;
            for (int i = 0; i < items.Length; i++)
                if (items[i].objectID == seed) total += items[i].amount;
            return total;
        }

        /// <summary>First slot holding that seed. Plain seeds (variation 0) are preferred over odd variations.</summary>
        private int FindSeedSlot(Entity player, ObjectID seed, out int variation)
        {
            var items = EntityManager.GetBuffer<ContainedObjectsBuffer>(player);
            int fallback = -1;
            variation = 0;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.objectID != seed || item.amount <= 0) continue;
                if (item.variation == 0) return i;
                if (fallback < 0) { fallback = i; variation = item.variation; }
            }
            return fallback;
        }

        // ------------------------------------------------------------------ database

        /// <summary>
        /// Plant ObjectID -> seed ObjectID (+ golden variation), derived from the seed prefabs:
        /// every object with GrowingCD whose properties name the ObjectID it grows into is a seed.
        /// </summary>
        private bool BuildSeedMap()
        {
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            if (!bank.IsCreated) return false;

            var map = new Dictionary<ObjectID, SeedInfo>();
            ref var infos = ref bank.Value.objectInfos;
            for (int i = 0; i < infos.Length; i++)
            {
                ref var info = ref infos[i];
                if (info.variation != 0) continue;
                Entity prefab = PugDatabase.GetPrimaryPrefabEntity(info.objectID, bank);
                if (prefab == Entity.Null || !EntityManager.Exists(prefab)) continue;
                if (!EntityManager.HasComponent<GrowingCD>(prefab) || !EntityManager.HasComponent<ObjectPropertiesCD>(prefab)) continue;

                var props = EntityManager.GetComponentData<ObjectPropertiesCD>(prefab);
                if (!props.TryGet<ObjectID>(PropGrowsInto, out ObjectID plant) || plant == ObjectID.None) continue;
                if (map.ContainsKey(plant)) continue;

                int golden = 0;
                props.TryGet<int>(PropGoldenSeedVariation, out golden);
                map[plant] = new SeedInfo { seed = info.objectID, goldenVariation = golden };
            }

            _seedByPlant = map;
            Debug.Log($"[{AutoReplantMod.Name}] seed map built: {map.Count} plants.");
            return true;
        }

        // ------------------------------------------------------------------ bookkeeping

        private void PruneSeen()
        {
            if (_seen.Count == 0) return;
            _seenScratch.Clear();
            foreach (Entity e in _seen)
                if (!EntityManager.Exists(e)) _seenScratch.Add(e);
            for (int i = 0; i < _seenScratch.Count; i++) _seen.Remove(_seenScratch[i]);
        }
    }
}
