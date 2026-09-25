using HarmonyLib;

namespace MasterPet.Patches
{
    [HarmonyPatch]
    public static class MasterPetUIPatches
    {
        [HarmonyPatch(typeof(UIMouse), "LateUpdate")]
        [HarmonyPostfix]
        private static void Cursor_LateUpdate(UIMouse __instance)
        {
            if (MasterPetMod.IsUIOpen && __instance.pointerSR != null && __instance.pointerSR.enabled)
                __instance.pointerSR.enabled = false;
        }

        [HarmonyPatch(typeof(PlayerController), "HandleHotBarSlotNavigation")]
        [HarmonyPrefix]
        private static bool HotbarScroll_Prefix()
        {
            return !MasterPetMod.IsUIOpen;
        }

        [HarmonyPatch(typeof(PlayerController), "get_isInteractionBlocked")]
        [HarmonyPostfix]
        private static void InteractionBlocked(ref bool __result)
        {
            if (MasterPetMod.IsUIOpen)
                __result = true;
        }

        [HarmonyPatch(typeof(PlayerController), "get_isUIShortCutsBlocked")]
        [HarmonyPostfix]
        private static void UIShortCutsBlocked(ref bool __result)
        {
            if (MasterPetMod.IsUIOpen)
                __result = true;
        }

        [HarmonyPatch(typeof(PlayerController), "get_isMovingBlocked")]
        [HarmonyPostfix]
        private static void MovingBlocked(ref bool __result)
        {
            if (MasterPetMod.IsUIOpen)
                __result = true;
        }

        [HarmonyPatch(typeof(UIManager), "get_isMouseShowing")]
        [HarmonyPostfix]
        private static void MouseShowing(ref bool __result)
        {
            if (MasterPetMod.IsUIOpen)
                __result = true;
        }

        [HarmonyPatch(typeof(UIMouse), "UpdateMouseMode")]
        [HarmonyPostfix]
        private static void UpdateMouseMode(UIMouse __instance)
        {
            if (MasterPetMod.IsUIOpen)
            {
                if (__instance.mouseMode != UIMouse.MouseMode.Normal)
                    __instance.SetMouseMode(UIMouse.MouseMode.Normal);
                if (__instance.mouseInventory != null && __instance.mouseInventory.HasObject(0))
                    __instance.ReleaseGrabbedItemBackToInventory();
            }
        }

        [HarmonyPatch(typeof(SendClientInputSystem), "PlayerInteractionBlocked")]
        [HarmonyPostfix]
        private static void SendInput_InteractionBlocked(ref bool __result)
        {
            if (MasterPetMod.IsUIOpen)
                __result = true;
        }

        [HarmonyPatch(typeof(SendClientInputSystem), "PlayerInputBlocked")]
        [HarmonyPostfix]
        private static void SendInput_InputBlocked(ref bool __result)
        {
            if (MasterPetMod.IsUIOpen && !Manager.input.singleplayerInputModule.PrefersKeyboardAndMouse() && !Manager.input.textInputIsActive)
                __result = true;
        }

        [HarmonyPatch(typeof(UIManager), "get_isAnyInventoryShowing")]
        [HarmonyPostfix]
        private static void AnyInventoryShowing(ref bool __result)
        {
            if (MasterPetMod.IsUIOpen)
                __result = true;
        }

        [HarmonyPatch(typeof(ShortCutsWindow), "LateUpdate")]
        [HarmonyPostfix]
        private static void HideShortcuts(ShortCutsWindow __instance)
        {
            if (MasterPetMod.IsUIOpen)
                __instance.HideUI();
        }

        [HarmonyPatch(typeof(UIScrollWindow), "UpdateScroll")]
        [HarmonyPrefix]
        private static bool UpdateScroll(UIScrollWindow __instance)
        {
            if (MasterPetMod.IsUIOpen)
                return false;
            return true;
        }
    }
}