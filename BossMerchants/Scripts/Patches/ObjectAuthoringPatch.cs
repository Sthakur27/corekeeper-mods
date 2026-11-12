using HarmonyLib;

namespace BossMerchants.Patches
{
    /// <summary>
    /// ObjectAuthoring (SDK-style prefabs) builds a brand-new ObjectInfo every time it is read, so an in-place
    /// price edit would be lost; patch the factory instead (as Potion Seller does).
    /// </summary>
    [HarmonyPatch(typeof(ObjectAuthoring), "ObjectAuthoringToObjectInfo")]
    public static class BossMerchantsObjectAuthoringPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ObjectInfo __result)
        {
            Catalog.Apply(__result);
        }
    }
}
