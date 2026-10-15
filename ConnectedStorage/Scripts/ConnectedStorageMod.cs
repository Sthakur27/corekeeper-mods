using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.ControlMapping;
using PugMod;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using UnityEngine;

namespace ConnectedStorage
{
    public sealed class ConnectedStorageMod : IMod
    {
        public const string Version = "0.7.17-dev";
        private GameObject ui;
        public void EarlyInit()
        {
            CoreLibMod.LoadSubmodule(typeof(ControlMappingModule), typeof(CommandModule));
            var mod = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
            if (mod != null) CommandModule.AddCommands(mod.ModId, "ConnectedStorage");
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            Debug.Log("[ConnectedStorage] v" + Version + " loaded.");
        }
        public void Init()
        {
            if (!Application.isBatchMode)
            {
                ui = new GameObject("Connected Storage UI");
                UnityEngine.Object.DontDestroyOnLoad(ui);
                var mod = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
                if (mod != null) foreach (var bundle in mod.AssetBundles)
                {
                    var prefab = bundle.LoadAsset<GameObject>("Assets/ConnectedStorageBrowser/ConnectedStorageBrowser.prefab");
                    if (prefab != null) { StorageBrowserUI.Prefab = prefab; break; }
                }
                ui.AddComponent<StorageBrowserUI>();
            }
        }
        public void Update() { }
        public void ModObjectLoaded(UnityEngine.Object obj)
        {
            if (obj is Pug.Sprite.SpriteAsset sprite)
                StorageUnitVisualPatch.Register(sprite);
        }
        public void Shutdown()
        {
            API.Authoring.OnObjectTypeAdded -= OnObjectTypeAdded;
            if (ui != null) UnityEngine.Object.Destroy(ui);
        }
        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            var obj = authoring == null ? null : authoring.GetComponent<ObjectAuthoring>();
            if (obj != null && (obj.objectName == "ConnectedStorage_Terminal" || (obj.objectName.StartsWith("ConnectedStorage_") && obj.objectName.EndsWith("Chest"))) && !em.HasComponent<StorageNodeTag>(entity))
                em.AddComponent<StorageNodeTag>(entity);
            if(obj != null && obj.objectName.StartsWith("ConnectedStorage_") && obj.objectName.EndsWith("Chest"))
                EnsureCraftingInventory(entity, em);
            StorageRemoval.OnObjectTypeAdded(entity, authoring, em);
            EnsureRecipes(entity, em);
        }
        internal static void EnsureCraftingInventory(Entity entity, EntityManager em)
        {
            if(!em.HasBuffer<InventoryBuffer>(entity))return;
            // The native nearby-crafting search requires this marker on both worlds.
            if(!em.HasComponent<InventoryAutoTransferEnabledCD>(entity))
                em.AddComponent<InventoryAutoTransferEnabledCD>(entity);
            int limit=em.HasComponent<ObjectDataCD>(entity)
                ? StorageCatalog.Capacity(em.GetComponentData<ObjectDataCD>(entity).objectID):0;
            if(limit<=0)return;
            var inventories=em.GetBuffer<InventoryBuffer>(entity);
            if(inventories.Length==0)return;
            var range=inventories[0];
            if(range.size==limit && range.maxSize==limit)return;
            range.sizeX=2;range.sizeY=limit/2;range.extraSize=0;range.maxSize=limit;
            inventories[0]=range;
            // Never truncate ContainedObjectsBuffer: old overflow stays withdrawable.
        }
        internal static void EnsureRecipes(Entity entity, EntityManager em)
        {
            if (!em.HasComponent<ObjectDataCD>(entity) || !em.HasBuffer<CanCraftObjectsBuffer>(entity)) return;
            var id = em.GetComponentData<ObjectDataCD>(entity).objectID;
            if (id == StorageCatalog.Workbench && id != ObjectID.None) return; // Authored with all nine recipes.
            if (!em.HasBuffer<IncludedCraftingBuildingsBuffer>(entity) && id != ObjectID.WoodenWorkBench && id != ObjectID.CopperWorkbench && id != ObjectID.TinWorkbench
                && id != ObjectID.IronWorkBench && id != ObjectID.ScarletWorkBench && id != ObjectID.OctarineWorkbench
                && id != ObjectID.GalaxiteWorkbench && id != ObjectID.SolariteWorkbench && id != ObjectID.HydraWorkbench) return;
            var recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity);
            // Retire recipes from existing prototype workbenches without shifting vanilla indices.
            for (int i = 0; i < recipes.Length; i++)
                if (StorageCatalog.IsRecipe(recipes[i].objectID) && recipes[i].objectID != StorageCatalog.Workbench)
                    recipes[i] = default(CanCraftObjectsBuffer);
            if (StorageCatalog.Workbench == ObjectID.None) return;
            // Included recipes and their prerequisite source are built together by
            // StorageRecipeAuthoringPatch. Never insert into the live copied list: the
            // vanilla unlock refresh would restore the original indices and undo it.
            if (em.HasBuffer<IncludedCraftingBuildingsBuffer>(entity)) return;
            if (id != ObjectID.WoodenWorkBench) return;
            for (int i = 0; i < recipes.Length; i++)
                if (recipes[i].objectID == StorageCatalog.Workbench) return;
            // Vanilla wood bench: Gear 0-5, Crafting 6-11, Base 12-17. Its last Base slot is empty.
            for (int i = 12; i < 18; i++)
            {
                if (i < recipes.Length && recipes[i].objectID != ObjectID.None) continue;
                while (recipes.Length <= i) recipes.Add(default(CanCraftObjectsBuffer));
                recipes[i] = new CanCraftObjectsBuffer { objectID = StorageCatalog.Workbench, amount = 1 };
                return;
            }
            // Preserve other mods' occupied Base slots; the UI patch exposes an overflow page.
            AddRecipe(em, entity, StorageCatalog.Workbench);
        }
        private static void AddRecipe(EntityManager em, Entity entity, ObjectID id)
        {
            if (id == ObjectID.None) return;
            var recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity);
            for (int i = 0; i < recipes.Length; i++) if (recipes[i].objectID == id) return;
            recipes.Add(new CanCraftObjectsBuffer { objectID = id, amount = 1 });
            Debug.Log("[ConnectedStorage] Registered recipe " + (int)id + " at "
                + em.GetComponentData<ObjectDataCD>(entity).objectID + " in " + em.World.Name);
        }
    }
    // Units reuse vanilla Chest interaction, but always keep their own sealed-unit art.
    // Reapply the mod-owned SpriteAsset when pooled graphics are spawned or reused.
    [HarmonyLib.HarmonyPatch(typeof(Chest), "OnSpawn")]
    public static class StorageUnitVisualPatch
    {
        private static readonly System.Collections.Generic.Dictionary<string, Pug.Sprite.SpriteAsset> Assets
            = new System.Collections.Generic.Dictionary<string, Pug.Sprite.SpriteAsset>();
        public static void Register(Pug.Sprite.SpriteAsset asset)
        {
            foreach (string tier in StorageCatalog.Tiers)
                if (asset.name == tier + "Chest") Assets[asset.name] = asset;
        }
        public static void Postfix(Chest __instance)
        {
            int tier = StorageCatalog.Tier(__instance.objectData.objectID);
            if (tier < 0 || __instance.spriteObjects == null) return;
            string key = StorageCatalog.Tiers[tier] + "Chest";
            foreach (var sprite in __instance.spriteObjects)
            {
                if (sprite == null || sprite.gameObject.name != "Sprite Object") continue;
                string previous = sprite.asset == null ? "null" : sprite.asset.name;
                string skin = sprite.currentSkin == null ? "none" : sprite.currentSkin.name;
                if (Assets.TryGetValue(key, out var expected)) sprite.asset = expected;
                sprite.currentSkin = null;
                sprite.StopAnimation();
                sprite.ResetVariant();
                sprite.ApplyVisualChange();
                Debug.Log("[ConnectedStorage] Unit art " + key + ": previous=" + previous
                    + ", skin=" + skin + ", applied=" + (sprite.asset == null ? "null" : sprite.asset.name));
            }
        }
    }
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial class StorageRecipeSystem : PugSimulationSystemBase
    {
        private EntityQuery stations, storageInventories;
        private double next;
        protected override void OnCreate()
        {
            base.OnCreate();
            storageInventories=GetEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(),ComponentType.ReadWrite<InventoryBuffer>());
            stations = GetEntityQuery(ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadWrite<CanCraftObjectsBuffer>());
        }
        protected override void OnUpdate()
        {
            if (World.Time.ElapsedTime < next || StorageCatalog.Terminal == ObjectID.None) return;
            next = World.Time.ElapsedTime + 1;
            using(var units=storageInventories.ToEntityArray(Allocator.Temp))
                foreach(var unit in units)
                    if(StorageCatalog.IsChest(EntityManager.GetComponentData<ObjectDataCD>(unit).objectID))
                        ConnectedStorageMod.EnsureCraftingInventory(unit,EntityManager);
            using (var entities = stations.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities) ConnectedStorageMod.EnsureRecipes(entity, EntityManager);
        }
    }
}



