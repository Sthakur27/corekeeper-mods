using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace BuildTools.Systems
{
    /// <summary>
    /// Raises the area ceiling (database prefabTileSize) of shovels, hoes, the roofing gadget and
    /// paint tools to the "Max tool size" setting, in both worlds, and re-applies when the setting
    /// changes. A tool's vanilla size is kept as a floor, so lowering the setting never makes a tool
    /// smaller than the game made it. Watering cans are raised only when Bigger Watering Cans is not
    /// running or is overridden (<see cref="ToolSizes.RaiseWateringCans"/>); turning that off puts them back.
    ///
    /// Tools without a ResizableTileSizeCD would use the whole ceiling as their fixed area
    /// (PlacementHandler.GetCurrentSize), so the component is added to their prefab; from then on
    /// the game's own size variation, R key and dig/roof code handle them like the resizable ones.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class ToolCeilingSystem : PugSimulationSystemBase
    {
        // Vanilla edge length per (objectID, variation). Static and recorded once per process: the
        // blob may be shared between worlds, and the second world must not record a patched value.
        private static readonly Dictionary<long, int> VanillaEdge = new Dictionary<long, int>();

        private EntityQuery _database;
        private int _appliedVersion = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            _database = GetEntityQuery(ComponentType.ReadOnly<PugDatabase.DatabaseBankCD>());
        }

        protected override void OnUpdate()
        {
            if (_appliedVersion == ToolSizes.Version || _database.IsEmptyIgnoreFilter) return;
            var bank = _database.GetSingleton<PugDatabase.DatabaseBankCD>();
            if (!bank.databaseBankBlob.IsCreated) return;
            _appliedVersion = ToolSizes.Version;

            int ceiling = math.clamp(ToolSizes.Ceiling, ToolSizes.MinCeiling, ToolSizes.MaxCeiling);
            int changed = 0, madeResizable = 0;
            var prefabsToFix = new List<Entity>();

            ref var infos = ref bank.databaseBankBlob.Value.objectInfos;
            for (int i = 0; i < infos.Length; i++)
            {
                ref var info = ref infos[i];
                if (!ToolSizes.IsToolType(info.objectType)) continue;
                bool raised = ToolSizes.IsRaisedType(info.objectType);

                // The size before Build Tools first raised it (for cans with Bigger Watering Cans running,
                // that is its 2x2 / 4x4, since it applies at conversion). Never-raised entries are left alone.
                long key = ((long)(int)info.objectID << 16) | (uint)(info.variation & 0xFFFF);
                if (!VanillaEdge.TryGetValue(key, out int vanilla))
                {
                    if (!raised) continue;
                    vanilla = math.max(1, info.prefabTileSize.x);
                    VanillaEdge[key] = vanilla;
                }

                int edge = raised ? math.max(vanilla, ceiling) : vanilla;
                if (info.prefabTileSize.x != edge || info.prefabTileSize.y != edge)
                {
                    info.prefabTileSize = new int2(edge, edge);
                    changed++;
                }
                if (!raised) continue;

                for (int p = 0; p < info.prefabEntities.Length; p++)
                {
                    var prefab = info.prefabEntities[p];
                    if (prefab != Entity.Null && EntityManager.Exists(prefab) && !EntityManager.HasComponent<ResizableTileSizeCD>(prefab))
                        prefabsToFix.Add(prefab);
                }
            }

            // Structural changes after the blob walk (the blob reference stays valid either way).
            foreach (var prefab in prefabsToFix)
            {
                if (EntityManager.HasComponent<ResizableTileSizeCD>(prefab)) continue;
                EntityManager.AddComponentData(prefab, new ResizableTileSizeCD { StartOnSmallestSize = false });
                madeResizable++;
            }

            string side = World.IsServer() ? "server" : "client";
            Debug.Log($"[{BuildToolsMod.Name}] Tool ceiling {ceiling}x{ceiling} ({side}): {changed} database entries changed, {madeResizable} tools made resizable.");
        }
    }
}
