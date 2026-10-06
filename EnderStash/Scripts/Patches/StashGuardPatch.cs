using HarmonyLib;

namespace EnderStash.Patches
{
    /// <summary>
    /// The server decodes a joining character here, right before StartGameRPCSystem copies the saved slots
    /// into the player (only up to the current slot count, so a stash that moved down would be cut off).
    /// Relocating in the decoded data keeps every item.
    /// </summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.GetCharacterDataFromSerialized))]
    public static class StashGuardPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CharacterData __result)
        {
            StashGuard.Relocate(__result);
        }
    }
}
