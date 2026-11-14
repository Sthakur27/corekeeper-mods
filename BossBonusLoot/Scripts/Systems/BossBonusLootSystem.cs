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
                // Ghorm the Devourer. Amber Larva: flat 10%; its entry in Ghorm's own loot table is
                // zeroed by HydraGemSystem so this roll is the whole chance.
                [ObjectID.BossLarva] = new[]
                {
                    (ObjectID.LarvaMeat, 80, 1f), (ObjectID.GoldenLarvaMeat, 80, 1f),
                    (ObjectID.AmberLarva, 1, 0.1f),
                },
                // Permanent-buff foods, one themed boss each:
                // Glurch, the game's first boss -> Anniversary Cake
                // Glurch also: Adventurer's Hat (ExplorerHelm) flat 30%; any entry for it in Glurch's own
                // loot table is zeroed by HydraGemSystem so this roll is the whole chance.
                [ObjectID.SlimeBoss] = new[] { (ObjectID.AnniversaryCake, 1, 0.15f), (ObjectID.ExplorerHelm, 1, 0.3f) },
                // Igneous the Molten Mass (Molten Quarry, home of the Amalgam Basins) -> Splendid Amalgam.
                // Core Iris flat 20%; any entry for it in Igneous' own loot table is zeroed by HydraGemSystem.
                [ObjectID.LavaSlimeBoss] = new[] { (ObjectID.LiquidMetal, 1, 0.15f), (ObjectID.CoreEye, 1, 0.2f) },
                // Crydra the Ice Titan: the cold-season treat -> Golden Chocolate
                [ObjectID.HydraBossSea] = new[] { (ObjectID.GoldenChocolate, 1, 0.15f) },
                // Druidra the Wild Titan -> Paradise Fruit Basket
                [ObjectID.HydraBossNature] = new[] { (ObjectID.FruitBasket, 1, 0.15f) },
                // Azeos the Sky Titan (Wilderness): Chipped Blade, Clear Gemstone, Jungle Emerald, pouches
                [ObjectID.BirdBoss] = new[]
                {
                    (ObjectID.LegendarySwordBlade, 1, 0.25f), (ObjectID.LegendarySwordGemstone, 1, 0.25f),
                    (ObjectID.NatureGemstone, 1, 1f),
                    (ObjectID.LargeSeedAndCropsPouch, 1, 0.4f), (ObjectID.LargeCritterPouch, 1, 0.4f),
                },
                // Omoroth the Sea Titan (Sunken Sea): Ocean Sapphire, pouches, Rusted Necklace flat 20%
                // (any entry for it in Omoroth's own loot table is zeroed by HydraGemSystem)
                [ObjectID.OctopusBoss] = new[]
                {
                    (ObjectID.SeaGemstone, 1, 1f),
                    (ObjectID.LargeFishPouch, 1, 0.4f), (ObjectID.LargePotionPouch, 1, 0.4f),
                    (ObjectID.RustedNecklace, 1, 0.2f),
                },
                // Morpha the Aquatic Mischief: Topaz Ring flat 10%; its own loot table entry is zeroed the same way.
                [ObjectID.SlipperySlimeBoss] = new[] { (ObjectID.TopazRing, 1, 0.1f) },
                // Bubble Crab (not a boss): Topaz Ring flat 3%; its own loot table entry is zeroed the same way.
                [ObjectID.CrabEnemy] = new[] { (ObjectID.TopazRing, 1, 0.03f) },
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
                // Void Larva (not a boss): 20% Oblivion Fragment; its loot table entry is zeroed the same way.
                [ObjectID.VoidLarva] = new[] { (ObjectID.HydraBossVoidCraftingItem, 1, 0.2f) },
            };

        private static readonly System.Random Rng = new System.Random();

        private readonly HashSet<Entity> _handled = new HashSet<Entity>();
        private System.Predicate<Entity> _isGone; // cached: a lambda here would allocate every tick
        private EntityQuery _deadBosses;
        private EntityQuery _database;

        protected override void OnCreate()
        {
            // Dying enemies, not just BossCD ones: the table also lists regular enemies (Mimite, Orbital
            // Turret, Void Larva). EnemyCD keeps plants, chests, stations and other destructibles out of
            // the query. Every entity in Bonus is an enemy (Ghorm, Glurch, Igneous, Morpha, the Hydras, the
            // Titans, Mimite, Orbital Turret, Void Larva). A new non-enemy entry in Bonus would need its own query.
            _deadBosses = GetEntityQuery(
                ComponentType.ReadOnly<EnemyCD>(),
                ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<HealthCD>(),
                ComponentType.ReadOnly<EntityDestroyedCD>());
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
            _isGone = e => !EntityManager.Exists(e);
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            if (_handled.Count > 0)
                _handled.RemoveWhere(_isGone);
            if (_deadBosses.IsEmpty || _database.IsEmpty)
            {
                base.OnUpdate();
                return;
            }

            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            // The query matches every dying enemy, so only the entity and ObjectID arrays are built; health
            // and position are read just for the few listed types.
            var entities = _deadBosses.ToEntityArray(Allocator.Temp);
            var objects = _deadBosses.ToComponentDataArray<ObjectDataCD>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!Bonus.TryGetValue(objects[i].objectID, out var drops)) continue;
                if (_handled.Contains(entities[i])) continue;
                if (EntityManager.GetComponentData<HealthCD>(entities[i]).health > 0) continue;
                _handled.Add(entities[i]);

                Entity killer = Entity.Null;
                if (EntityManager.HasComponent<KilledByPlayerCD>(entities[i])
                    && EntityManager.IsComponentEnabled<KilledByPlayerCD>(entities[i]))
                    killer = EntityManager.GetComponentData<KilledByPlayerCD>(entities[i]).playerEntity;
                if (killer != Entity.Null && !EntityManager.Exists(killer)) killer = Entity.Null;

                float3 pos = EntityManager.GetComponentData<LocalTransform>(entities[i]).Position;
                foreach (var (item, amount, chance) in drops)
                {
                    if (chance < 1f && Rng.NextDouble() >= chance) continue;
                    EntityUtility.DropNewEntity(World, new ContainedObjectsBuffer
                    {
                        objectData = new ObjectDataCD { objectID = item, amount = amount, variation = 0 }
                    }, pos, bank, killer);
                }
                // Log real boss kills only: Mimites, turrets and Void Larvae can die often, and every
                // Debug.Log writes a stack trace to Player.log.
                if (EntityManager.HasComponent<BossCD>(entities[i]))
                    Debug.Log($"[{BossBonusLootMod.Name}] {objects[i].objectID} died: bonus loot rolled.");
            }
            entities.Dispose();
            objects.Dispose();
            base.OnUpdate();
        }
    }
}
