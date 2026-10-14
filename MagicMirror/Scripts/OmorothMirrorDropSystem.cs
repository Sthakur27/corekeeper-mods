using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace MagicMirror
{
    // Independent bonus roll, once per dead Omoroth, performed only by the server.
    // Does not replace an entry in the boss chest or depend on Boss Bonus Loot.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial class OmorothMirrorDropSystem : PugSimulationSystemBase
    {
        public const double DropChance = 0.20;
        private readonly System.Random random = new System.Random();
        private readonly HashSet<Entity> handled = new HashSet<Entity>();
        private EntityQuery dying, databaseQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            // EntityDestroyedCD is enableable: this query only matches entities
            // whose death/destruction flag is enabled. Health excludes despawns.
            dying = GetEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(),
                ComponentType.ReadOnly<HealthCD>(), ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<EntityDestroyedCD>());
            databaseQuery = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            handled.RemoveWhere(entity => !EntityManager.Exists(entity));
            var mirror = MagicMirrorMod.Id;
            if (mirror == ObjectID.None || dying.IsEmpty || databaseQuery.IsEmpty) return;
            var bank = databaseQuery.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            using (var entities = dying.ToEntityArray(Allocator.Temp))
            foreach (var entity in entities)
            {
                if (EntityManager.GetComponentData<ObjectDataCD>(entity).objectID != ObjectID.OctopusBoss
                    || EntityManager.GetComponentData<HealthCD>(entity).health > 0) continue;
                // Record misses too; otherwise each later death-animation tick
                // would reroll until a mirror eventually dropped.
                if (!handled.Add(entity) || random.NextDouble() >= DropChance) continue;
                Entity killer = Entity.Null;
                if (EntityManager.HasComponent<KilledByPlayerCD>(entity)
                    && EntityManager.IsComponentEnabled<KilledByPlayerCD>(entity))
                    killer = EntityManager.GetComponentData<KilledByPlayerCD>(entity).playerEntity;
                if (killer != Entity.Null && !EntityManager.Exists(killer)) killer = Entity.Null;
                EntityUtility.DropNewEntity(World, new ContainedObjectsBuffer
                {
                    objectData = new ObjectDataCD { objectID = mirror, amount = 1, variation = 0 }
                }, EntityManager.GetComponentData<LocalTransform>(entity).Position, bank, killer);
            }
        }
    }
}
