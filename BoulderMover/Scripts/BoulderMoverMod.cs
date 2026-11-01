using System.Linq;
using ModOptions;
using PugMod;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BoulderMover
{
    /// <summary>
    /// Ore boulders can be mined loose and carried: pickaxe hits that would damage a Maze Block fill a pickup meter
    /// instead of dropping ore, and once the meter holds three Maze Blocks' worth of damage the boulder drops as an item
    /// (vanilla boulders are already placeable items). Drills are untouched and remain the only source of boulder ore.
    /// One setting: whether partly drilled boulders can be moved too. Server side, see <see cref="Systems.BoulderPickupSystem"/>.
    /// </summary>
    public sealed class BoulderMoverMod : IMod
    {
        public const string Name = "BoulderMover";
        public const string Version = "1.2.0";

        public const string SettingsHint = "Off: only boulders that have not been drilled can be moved. On: drilled boulders can be moved too, "
            + "but a moved boulder is placed back full, so this gives free ore. In multiplayer the host's value counts.";

        private static Setting<bool> _moveDrilled;

        /// <summary>Partly drilled boulders can be picked up too (they are placed back full). Default off.</summary>
        public static bool MoveDrilled => _moveDrilled != null && _moveDrilled.Value;

        public void EarlyInit()
        {
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            Debug.Log($"[{Name}] v{Version} loaded: ore boulders can be picked up with Maze Block mining power.");
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Movable Ore Boulders").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section.Toggle(out _moveDrilled, "Move drilled boulders (refills them)", false);
            Debug.Log($"[{Name}] moveDrilled={MoveDrilled}");
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }

        /// <summary>
        /// Vanilla boulders have damage reduction 99999999, so pickaxe hits never even register. Giving the prefab the
        /// Maze Block's reduction (both worlds, so client prediction agrees) makes hits land exactly when they would on a
        /// Maze Block. Drill damage bypasses reduction, so drilling is unchanged.
        /// </summary>
        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            if (!em.HasComponent<ObjectDataCD>(entity)) return;
            var id = em.GetComponentData<ObjectDataCD>(entity).objectID;

            if (id == ObjectID.WallDesertTempleBlock2)
            {
                if (em.HasComponent<DamageReductionCD>(entity))
                    MazeBlock.Reduction = em.GetComponentData<DamageReductionCD>(entity).reduction;
                if (em.HasComponent<HealthCD>(entity))
                    MazeBlock.Health = em.GetComponentData<HealthCD>(entity).maxHealth;
                Debug.Log($"[{Name}] Maze Block: reduction {MazeBlock.Reduction}, health {MazeBlock.Health}.");
                return;
            }

            if (!MazeBlock.IsOreBoulder(id) || !em.HasComponent<DamageReductionCD>(entity)) return;
            var reduction = em.GetComponentData<DamageReductionCD>(entity);
            reduction.reduction = MazeBlock.Reduction;
            em.SetComponentData(entity, reduction);
        }
    }
}
