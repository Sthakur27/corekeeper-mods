using HarmonyLib;

namespace BiggerWateringCans.Patches
{
    /// <summary>
    /// ObjectAuthoring (SDK-style prefabs) builds a brand-new ObjectInfo every time its ObjectInfo property is
    /// read, so an in-place edit would be lost. Patch the factory instead (same approach CoreLib uses for its
    /// ModToolSizeAuthoring). Harmless if the vanilla cans use EntityMonoBehaviourData instead.
    /// </summary>
    [HarmonyPatch(typeof(ObjectAuthoring), "ObjectAuthoringToObjectInfo")]
    public static class ObjectAuthoringPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ObjectInfo __result)
        {
            CanSizes.Apply(__result);
        }
    }
}
