using PugMod;
using Unity.Entities;
using UnityEngine;

namespace BiggerWateringCans
{
    /// <summary>
    /// Bigger Watering Cans: the Watering Can waters a 2x2 area, the Iron Watering Can a 4x4 area.
    ///
    /// Mechanism (three idempotent layers, so the size is right whichever order the game converts prefabs in):
    ///  1. Harmony postfix on ObjectAuthoring.ObjectAuthoringToObjectInfo (new-style authoring builds a fresh
    ///     ObjectInfo on every read, so the field must be patched at the source).
    ///  2. API.Authoring.OnObjectTypeAdded: edits the shared ObjectInfo instance of EntityMonoBehaviourData
    ///     prefabs (also reachable through PugDatabase.objectsByType, which is filled before conversion),
    ///     so PugDatabasePostConverter bakes the new size into the database blob.
    ///  3. WateringCanSizeSystem (client + server): writes the size straight into the already-built
    ///     PugDatabase blob if anything above was too late, and resets a stale saved size-variation.
    /// </summary>
    public class BiggerWateringCansMod : IMod
    {
        public const string Name = "BiggerWateringCans";
        public const string Version = "1.0.0";

        private static bool _appliedViaDatabase;

        public void EarlyInit()
        {
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            Debug.Log($"[{Name}] v{Version} loaded: Watering Can {CanSizes.BasicCanSize}x{CanSizes.BasicCanSize}, Iron Watering Can {CanSizes.IronCanSize}x{CanSizes.IronCanSize}.");
        }

        public void Init() { }

        public void Shutdown()
        {
            API.Authoring.OnObjectTypeAdded -= OnObjectTypeAdded;
        }

        public void ModObjectLoaded(Object obj) { }

        public void Update() { }

        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            if (authoring == null) return;

            // The prefab itself (old-style authoring keeps one ObjectInfo instance; edit it in place).
            if (authoring.TryGetComponent<EntityMonoBehaviourData>(out var data) && CanSizes.Apply(data.objectInfo))
            {
                Debug.Log($"[{Name}] set {data.objectInfo.objectID} prefabTileSize to {data.objectInfo.prefabTileSize} (authoring).");
            }

            // PugDatabase.objectsByType holds the same ObjectInfo instances and is populated before conversion,
            // so fixing them on the first callback (and again right before the database blob is baked, which is
            // when the PugDatabaseAuthoring object comes through) covers prefabs converted after the database.
            if (!_appliedViaDatabase || authoring.TryGetComponent<PugDatabaseAuthoring>(out _))
            {
                _appliedViaDatabase = true;
                ApplyViaDatabase();
            }
        }

        private static void ApplyViaDatabase()
        {
            if (PugDatabase.objectsByType == null) return;
            foreach (var id in new[] { ObjectID.WaterCan, ObjectID.LargeWaterCan })
            {
                if (PugDatabase.TryGetObjectInfo(id, out var info) && CanSizes.Apply(info))
                {
                    Debug.Log($"[{Name}] set {id} prefabTileSize to {info.prefabTileSize} (database).");
                }
            }
        }
    }
}
