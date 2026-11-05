using PugMod;
using Unity.Entities;
using UnityEngine;

namespace WormholePotion
{
    public sealed class WormholePotionMod : IMod
    {
        public const string Version = "1.1.1";
        public const string PotionName = "WormholePotion_Potion";
        public const string MarkerName = "WormholePotion_Marker";
        internal static bool Active;
        private static ObjectID _potion, _marker;
        // Mod object IDs are fixed once registered; cache to skip the string lookup.
        public static ObjectID Potion
        {
            get
            {
                if (_potion == ObjectID.None) _potion = API.Authoring.GetObjectID(PotionName);
                return _potion;
            }
        }
        public static ObjectID Marker
        {
            get
            {
                if (_marker == ObjectID.None) _marker = API.Authoring.GetObjectID(MarkerName);
                return _marker;
            }
        }
        public void EarlyInit()
        {
            Active = true;
            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
            Debug.Log("[WormholePotion] v" + Version + " loaded: one-use recall that marks a one-use waypoint back, 10 Gold Bars at the Ancient Hologram Pod.");
        }
        public void Init() { }
        public void Update() { }
        public void ModObjectLoaded(Object obj) { }
        public void Shutdown() { API.Authoring.OnObjectTypeAdded -= OnObjectTypeAdded; }
        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager em)
        {
            EnsureRecipe(entity, em);
        }
        internal static void EnsureRecipe(Entity entity, EntityManager em)
        {
            var potion = Potion;
            if (potion == ObjectID.None || !em.HasComponent<ObjectDataCD>(entity)
                || !em.HasBuffer<CanCraftObjectsBuffer>(entity)
                || em.GetComponentData<ObjectDataCD>(entity).objectID != ObjectID.AncientHologramPod) return;
            // Read-only first: a writable GetBuffer is a sync point.
            var recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity, true);
            int slot = -1;
            for (int i = 0; i < recipes.Length; i++)
            {
                if (recipes[i].objectID == potion) return;
                if (slot < 0 && recipes[i].objectID == ObjectID.None) slot = i;
            }
            // Vanilla's pod has one empty slot at the end. Never shift indices or
            // overwrite another mod's recipe.
            if (slot < 0) return;
            recipes = em.GetBuffer<CanCraftObjectsBuffer>(entity);
            recipes[slot] = new CanCraftObjectsBuffer { objectID = potion, amount = 1 };
            Debug.Log("[WormholePotion] Ancient Hologram Pod recipe registered.");
        }
    }
    // No periodic recipe system: PotionRecipeAuthoring puts the recipe in at conversion, so the
    // game's own recipe repair keeps it, and OnObjectTypeAdded covers the live prefab.
}
