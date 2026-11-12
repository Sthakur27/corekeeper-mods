using HarmonyLib;

namespace BossMerchants.Patches
{
    /// <summary>
    /// Vanilla SpawnMerchantSystem.GetRequiredObjectForMerchant maps each merchant to the idol its room needs.
    /// The system uses it (main thread, not Burst) to move a merchant whose bed is gone into another free bed
    /// in a room with that idol; answering for our merchants gives them the same behaviour. The spawn list
    /// itself is hard-coded to vanilla merchants, so spawning ours is <see cref="Systems.MerchantSpawnSystem"/>.
    /// Item Browser style tools that read this method also learn which idol summons which boss merchant.
    /// </summary>
    [HarmonyPatch(typeof(SpawnMerchantSystem), "GetRequiredObjectForMerchant")]
    public static class RequiredIdolPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ObjectID merchantID, ref ObjectID __result)
        {
            if (__result != ObjectID.None) return;
            var m = Catalog.ByMerchantId(merchantID);
            if (m != null) __result = m.IdolId;
        }
    }
}
