using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace BossBonusLoot.Systems
{
    /// <summary>
    /// Server side. When a boss from <see cref="Bonus"/> dies (EntityDestroyedCD enabled, health 0),
    /// drops its bonus stacks at the body, pulled towards the killing player like vanilla loot.
    /// Each boss entity is handled once; the vanilla chest is untouched.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class BossBonusLootSystem : PugSimulationSystemBase
    {
        /// <summary>Boss -> (item, amount, chance 0..1) rolled independently per item.</summary>
        internal static readonly Dictionary<ObjectID, (ObjectID item, int amount, float chance)[]> Bonus =
            new Dictionary<ObjectID, (ObjectID, int, float)[]>
            {
                // Ghorm the Devourer
                [ObjectID.BossLarva] = new[] { (ObjectID.LarvaMeat, 80, 1f), (ObjectID.GoldenLarvaMeat, 80, 1f) },
                // Azeos the Sky Titan (Wilderness): Chipped Blade, Clear Gemstone, Jungle Emerald, pouches
                [ObjectID.BirdBoss] = new[]
                {
                    (ObjectID.LegendarySwordBlade, 1, 0.25f), (ObjectID.LegendarySwordGemstone, 1, 0.25f),
                    (ObjectID.NatureGemstone, 1, 1f),
                    (ObjectID.LargeSeedAndCropsPouch, 1, 0.4f), (ObjectID.LargeCritterPouch, 1, 0.4f),
                },
                // Omoroth the Sea Titan (Sunken Sea): Ocean Sapphire, pouches
                [ObjectID.OctopusBoss] = new[]
                {
                    (ObjectID.SeaGemstone, 1, 1f),
                    (ObjectID.LargeFishPouch, 1, 0.4f), (ObjectID.LargePotionPouch, 1, 0.4f),
                },
                // Ra-Akar the Sand Titan (Desert of Beginnings): Desert Ruby, pouches
                [ObjectID.ScarabBoss] = new[]
                {
                    (ObjectID.DesertGemstone, 1, 1f),
                    (ObjectID.LargeValuablePouch, 1, 0.4f), (ObjectID.LargeOreAndBlockPouch, 1, 0.4f),
                },
                // Mimite and Orbital Turret (not bosses): 10% each biome gem. Their own loot tables' gem
                // entries are zeroed by HydraGemSystem so these rolls are the whole chance.
                [ObjectID.Mimite] = new[]
                {
                    (ObjectID.NatureGemstone, 1, 0.1f), (ObjectID.SeaGemstone, 1, 0.1f), (ObjectID.DesertGemstone, 1, 0.1f),
                },
                [ObjectID.OrbitalTurret] = new[]
                {
                    (ObjectID.NatureGemstone, 1, 0.1f), (ObjectID.SeaGemstone, 1, 0.1f), (ObjectID.DesertGemstone, 1, 0.1f),
                },
            };

        private static readonly System.Random Rng = new System.Random();

        private readonly HashSet<Entity> _handled = new HashSet<Entity>();
        private EntityQuery _deadBosses;
        private EntityQuery _database;

        protected override void OnCreate()
        {
            // Any dying entity, not just BossCD ones: the table also lists regular enemies (Mimite).
            _deadBosses = GetEntityQuery(
                ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<HealthCD>(),
                ComponentType.ReadOnly<EntityDestroyedCD>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            if (_handled.Count > 0)
                _handled.RemoveWhere(e => !EntityManager.Exists(e));
            if (_deadBosses.IsEmpty || _database.IsEmpty)
            {
                base.OnUpdate();
                return;
            }

            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            var entities = _deadBosses.ToEntityArray(Allocator.Temp);
            var objects = _deadBosses.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            var transforms = _deadBosses.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var healths = _deadBosses.ToComponentDataArray<HealthCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (healths[i].health > 0 || _handled.Contains(entities[i])) continue;
                if (!Bonus.TryGetValue(objects[i].objectID, out var drops)) continue;
                _handled.Add(entities[i]);

                Entity killer = Entity.Null;
                if (EntityManager.HasComponent<KilledByPlayerCD>(entities[i])
                    && EntityManager.IsComponentEnabled<KilledByPlayerCD>(entities[i]))
                    killer = EntityManager.GetComponentData<KilledByPlayerCD>(entities[i]).playerEntity;
                if (killer != Entity.Null && !EntityManager.Exists(killer)) killer = Entity.Null;

                float3 pos = transforms[i].Position;
                foreach (var (item, amount, chance) in drops)
                {
                    if (chance < 1f && Rng.NextDouble() >= chance) continue;
                    EntityUtility.DropNewEntity(World, new ContainedObjectsBuffer
                    {
                        objectData = new ObjectDataCD { objectID = item, amount = amount, variation = 0 }
                    }, pos, bank, killer);
                }
                Debug.Log($"[{BossBonusLootMod.Name}] {objects[i].objectID} died: bonus loot rolled.");
            }
            entities.Dispose();
            objects.Dispose();
            transforms.Dispose();
            healths.Dispose();
            base.OnUpdate();
        }
    }
}
