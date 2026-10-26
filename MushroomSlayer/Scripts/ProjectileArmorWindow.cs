using System;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace MushroomSlayer
{
    // ProjectileSystem calls AttackSystem.Helper inside Burst. That helper discards
    // the original damage BEFORE writing HealthChangeBuffer. Bracket its dispatch
    // instead of trying to Harmony-patch a function embedded in native Burst code.
    [HarmonyPatch("Unity.Entities.WorldUnmanagedImpl", "UpdateSystem")]
    public static class ProjectileArmorWindow
    {
        public sealed class Window
        {
            public World World;
            public readonly Dictionary<Entity, DamageReductionCD> Armor = new Dictionary<Entity, DamageReductionCD>();
            public readonly Dictionary<Entity, int> HitStarts = new Dictionary<Entity, int>();
            public readonly Dictionary<Entity, GhostEffectEventBuffer[]> Effects = new Dictionary<Entity, GhostEffectEventBuffer[]>();
            public bool Restored;
        }

        private struct HitNumber
        {
            public Entity Target, Source;
            public int Before, After;
        }

        // Queries owned by one world's EntityManager; created once, disposed with the world.
        private sealed class Queries
        {
            public World World;
            public EntityQuery Projectiles, Armored, Hits, Effects;
            public bool Created;
        }

        private static int _logged;

        // This prefix runs for every system in every world each frame, so the common
        // path must be a single dictionary miss. Rescanned only when worlds come or go.
        private static readonly Dictionary<SystemHandle, Queries> ProjectileSystems = new Dictionary<SystemHandle, Queries>();
        private static int _knownWorldCount = -1;
        private static World _lastWorld;
        private static bool _rescan;
        private static int _scanFrame = -1;

        private static Queries Resolve(SystemHandle sh)
        {
            var all = World.All;
            // Count alone misses a world swap (leave + rejoin); new worlds are appended last.
            var last = all.Count > 0 ? all[all.Count - 1] : null;
            if (all.Count != _knownWorldCount || last != _lastWorld)
            {
                _knownWorldCount = all.Count;
                _lastWorld = last;
                ProjectileSystems.Clear();
                _rescan = true;
                _scanFrame = -1;
            }
            // A world can be listed before its systems exist, so keep looking (once per
            // frame) while any world has no ProjectileSystem. Known handles keep their queries.
            if (_rescan && _scanFrame != UnityEngine.Time.frameCount)
            {
                _scanFrame = UnityEngine.Time.frameCount;
                _rescan = false;
                foreach (var candidate in all)
                {
                    var handle = candidate.IsCreated ? candidate.Unmanaged.GetExistingUnmanagedSystem<ProjectileSystem>() : SystemHandle.Null;
                    if (handle == SystemHandle.Null) _rescan = true;
                    else if (!ProjectileSystems.ContainsKey(handle)) ProjectileSystems[handle] = new Queries { World = candidate };
                }
            }
            if (!ProjectileSystems.TryGetValue(sh, out var found) || !found.World.IsCreated) return null;
            if (!found.Created)
            {
                var em = found.World.EntityManager;
                found.Projectiles = em.CreateEntityQuery(ComponentType.ReadOnly<ProjectileCD>(), ComponentType.ReadOnly<Simulate>());
                found.Armored = em.CreateEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<DamageReductionCD>());
                found.Hits = em.CreateEntityQuery(ComponentType.ReadOnly<HealthChangeBuffer>());
                found.Effects = em.CreateEntityQuery(ComponentType.ReadOnly<GhostEffectEventBuffer>());
                found.Created = true;
            }
            return found;
        }

        public static void Prefix(SystemHandle sh, out Window __state)
        {
            __state = null;
            var queries = Resolve(sh);
            if (queries == null || queries.Projectiles.IsEmpty) return;
            var world = queries.World;
            var em = world.EntityManager;
            using (var entities = queries.Armored.ToEntityArray(Allocator.Temp))
            using (var objects = queries.Armored.ToComponentDataArray<ObjectDataCD>(Allocator.Temp))
            {
                var window = new Window { World = world };
                for (int i = 0; i < entities.Length; i++)
                {
                    if (objects[i].objectID != ObjectID.MushroomBrute) continue;
                    var armor = em.GetComponentData<DamageReductionCD>(entities[i]);
                    if (armor.maxDamagePerHit == 1) window.Armor.Add(entities[i], armor);
                }
                if (window.Armor.Count == 0) return;
                // Assign before any mutation so the finalizer can restore on failure.
                __state = window;
                em.CompleteAllTrackedJobs();
                // Snapshots only read, so a read-only buffer avoids bumping change versions.
                using (var owners = queries.Hits.ToEntityArray(Allocator.Temp))
                    foreach (var owner in owners)
                        window.HitStarts.Add(owner, em.GetBuffer<HealthChangeBuffer>(owner, true).Length);
                using (var owners = queries.Effects.ToEntityArray(Allocator.Temp))
                    foreach (var owner in owners)
                    {
                        var buffer = em.GetBuffer<GhostEffectEventBuffer>(owner, true);
                        var snapshot = new GhostEffectEventBuffer[buffer.Length];
                        for (int i = 0; i < buffer.Length; i++) snapshot[i] = buffer[i];
                        window.Effects.Add(owner, snapshot);
                    }
                foreach (var pair in window.Armor)
                {
                    var armor = pair.Value;
                    // Keep it positive: zero also means visibly vulnerable to the game.
                    armor.maxDamagePerHit = int.MaxValue;
                    em.SetComponentData(pair.Key, armor);
                }
            }
        }

        public static void Postfix(SystemHandle sh, Window __state)
        {
            if (__state == null) return;
            try
            {
                ref var system = ref __state.World.Unmanaged.ResolveSystemStateRef(sh);
                system.Dependency.Complete();
                var em = __state.World.EntityManager;
                em.CompleteAllTrackedJobs();
                var numbers = new List<HitNumber>();
                foreach (var pair in __state.HitStarts)
                {
                    var hits = em.GetBuffer<HealthChangeBuffer>(pair.Key);
                    for (int i = pair.Value; i < hits.Length; i++)
                    {
                        var entry = hits[i];
                        var original = entry.healthChange;
                        if (!__state.Armor.TryGetValue(original.entity, out var armor) ||
                            !ArmorRule.Rewrite(ref entry.healthChange, ObjectID.MushroomBrute, armor)) continue;
                        hits[i] = entry;
                        numbers.Add(new HitNumber { Target = original.entity, Source = original.causedByEntity,
                            Before = -original.amount, After = -entry.healthChange.amount });
                        if (__state.World.IsServer() && _logged++ < 40)
                            Debug.Log("[MushroomSlayer:projectile] target=" + original.entity + " incoming=" + (-original.amount) +
                                " applied=" + (-entry.healthChange.amount) + " prematureKill=" + original.wasKilled);
                    }
                }
                CorrectNumbers(__state, numbers);
            }
            finally { Restore(__state); }
        }

        // Finalizer runs even if the original dispatch or another postfix throws.
        public static void Finalizer(Window __state)
        {
            Restore(__state);
        }

        private static void Restore(Window window)
        {
            if (window == null || window.Restored || !window.World.IsCreated) return;
            var em = window.World.EntityManager;
            em.CompleteAllTrackedJobs();
            foreach (var pair in window.Armor)
                if (em.HasComponent<DamageReductionCD>(pair.Key))
                {
                    var armor = em.GetComponentData<DamageReductionCD>(pair.Key);
                    armor.maxDamagePerHit = pair.Value.maxDamagePerHit;
                    em.SetComponentData(pair.Key, armor);
                }
            window.Restored = true;
        }

        private static void CorrectNumbers(Window window, List<HitNumber> numbers)
        {
            if (numbers.Count == 0) return;
            var em = window.World.EntityManager;
            foreach (var pair in window.Effects)
            {
                if (!em.HasBuffer<GhostEffectEventBuffer>(pair.Key) ||
                    !em.HasComponent<GhostEffectEventBufferPointerCD>(pair.Key)) continue;
                var effects = em.GetBuffer<GhostEffectEventBuffer>(pair.Key);
                int next = em.GetComponentData<GhostEffectEventBufferPointerCD>(pair.Key).NextIndex;
                int search = numbers.Count - 1;
                for (int n = 0; n < effects.Length; n++)
                {
                    int index = (next - 1 - n + effects.Length * 2) % effects.Length;
                    var effect = effects[index];
                    if (index < pair.Value.Length && effect.Equals(pair.Value[index])) continue;
                    if (effect.value.effectID != EffectID.WhiteDamageNumber && effect.value.effectID != EffectID.CritNumber) continue;
                    for (int j = search; j >= 0; j--)
                    {
                        var hit = numbers[j];
                        if (effect.value.entity != hit.Target || effect.value.entity2 != hit.Source || effect.value.value1 != hit.Before) continue;
                        effect.value.value1 = hit.After;
                        effects[index] = effect;
                        search = j - 1;
                        break;
                    }
                }
            }
        }
    }
}
