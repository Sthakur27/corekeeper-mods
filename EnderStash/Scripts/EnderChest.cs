using PugMod;

namespace EnderStash
{
    /// <summary>
    /// The Ender Chest object. Its prefab (logic + graphics, sprites, name text) lives in this mod's asset
    /// bundle, built with the Core Keeper Mod SDK from <c>Unity/</c>. The graphics prefab uses the vanilla
    /// <see cref="Chest"/> component, so the bundle has no script references of its own and works the same
    /// inside Sid's Overhaul. The object has no inventory: opening it shows the player's stash
    /// (<see cref="StashLayout"/>), see <see cref="EnderStashUI.Open"/>.
    /// </summary>
    public static class EnderChest
    {
        /// <summary>ObjectAuthoring.objectName in the prefab. Saves store modded items by this name.</summary>
        public const string ObjectName = "EnderStash_EnderChest";

        /// <summary>Fishing Merchant stock per restock. The price (9999) is set on the prefab: sellValue 2000 x 5 x 0.9999.</summary>
        public const int MerchantStock = 1;

        private static ObjectID _id;

        /// <summary>The runtime ObjectID (above 32767, assigned by the game at load); None until the prefab is converted.</summary>
        public static ObjectID Id
        {
            get
            {
                if (_id == ObjectID.None && API.Authoring != null) _id = API.Authoring.GetObjectID(ObjectName);
                return _id;
            }
        }

        public static bool Is(EntityMonoBehaviour emb)
        {
            if (emb == null || Id == ObjectID.None) return false;
            try
            {
                return emb.objectData.objectID == Id;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
