using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace DrillUnit.Systems
{
    /// <summary>
    /// The Drill Units being ridden in a world this tick, as (unit, rider) pairs. Several systems need that
    /// list every tick; the first one to ask in a tick builds it from its boat query (every placed boat,
    /// vanilla ones included) and the rest reuse it, so the boats are walked once per world per tick.
    /// </summary>
    public static class RiddenUnits
    {
        private sealed class Snapshot
        {
            public double Time = double.NaN;
            public readonly List<(Entity unit, Entity rider)> Pairs = new List<(Entity, Entity)>();
        }

        private static readonly Dictionary<World, Snapshot> Worlds = new Dictionary<World, Snapshot>();

        /// <summary>Boat query every Drill Unit system uses (also its RequireForUpdate).</summary>
        public static EntityQuery Query(SystemBase system) => system.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<BoatCD>(), ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<ControlledByOtherEntityCD>());

        public static List<(Entity unit, Entity rider)> Get(SystemBase system, EntityQuery boats)
        {
            var world = system.World;
            if (!Worlds.TryGetValue(world, out var snap))
            {
                foreach (var w in new List<World>(Worlds.Keys))   // drop worlds that were disposed (world unload)
                    if (!w.IsCreated) Worlds.Remove(w);
                Worlds[world] = snap = new Snapshot();
            }
            double now = world.Time.ElapsedTime;
            if (snap.Time == now) return snap.Pairs;
            snap.Time = now;
            snap.Pairs.Clear();
            var id = DrillUnitMod.Id;
            if (id == ObjectID.None || boats.IsEmpty) return snap.Pairs;
            var em = system.EntityManager;
            var units = boats.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (em.GetComponentData<ObjectDataCD>(unit).objectID != id) continue;
                var rider = em.GetComponentData<ControlledByOtherEntityCD>(unit).controlledByEntity;
                if (DrillUnitMod.IsRiding(em, rider, out var ridden) && ridden == unit) snap.Pairs.Add((unit, rider));
            }
            units.Dispose();
            return snap.Pairs;
        }
    }
}
