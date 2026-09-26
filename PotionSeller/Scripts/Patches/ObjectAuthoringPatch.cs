using HarmonyLib;

namespace PotionSeller.Patches
{
    /// <summary>
    /// ObjectAuthoring (SDK-style prefabs) builds a brand-new ObjectInfo every time its ObjectInfo property is
    /// read, so an in-place edit would be lost. Patch the factory instead. Harmless if the vanilla potions
    /// use EntityMonoBehaviourData.
    /// </summary>
    [HarmonyPatch(typeof(ObjectAuthoring), "ObjectAuthoringToObjectInfo")]
    public static class ObjectAuthoringPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ObjectInfo __result)
        {
            PotionPrices.Apply(__result);
        }
    }
}
