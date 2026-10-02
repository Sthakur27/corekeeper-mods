using HarmonyLib;

namespace PetEditor
{
    [HarmonyPatch(typeof(PetTalentsWindow), "PositionUIElements")]
    public static class PetWindowLayoutPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PetTalentsWindow __instance) => !PetEditorUI.Layout(__instance);
    }

    [HarmonyPatch(typeof(PetTalentsWindow), "UpdateTalents")]
    public static class PetWindowTalentsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PetTalentsWindow __instance) => PetEditorUI.AfterUpdateTalents(__instance);
    }

    [HarmonyPatch(typeof(PetTalentsWindow), nameof(PetTalentsWindow.HideTalentTree))]
    public static class PetWindowHidePatch
    {
        [HarmonyPostfix]
        public static void Postfix() => PetEditorUI.ClosePicker();
    }

    /// <summary>Left-click: pick from the picker; Shift + left-click on a slot with a point removes it.</summary>
    [HarmonyPatch(typeof(PetTalentUIElement), nameof(PetTalentUIElement.OnLeftClicked))]
    public static class PetTalentLeftClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PetTalentUIElement __instance, bool mod1)
        {
            if (PetEditorUI.IsPickItem(__instance))
            {
                PetEditorUI.Pick(__instance);
                return false;
            }
            if (mod1 && __instance.GetCurrentPoints() > 0)
            {
                int slot = IndexOf(__instance);
                if (slot >= 0)
                {
                    PetEditorUI.RemovePoint(slot);
                    return false;
                }
            }
            return true;
        }

        internal static int IndexOf(PetTalentUIElement e)
        {
            var w = e.GetComponentInParent<PetTalentsWindow>(true);
            return w != null ? w.petTalentUIElements.IndexOf(e) : -1;
        }
    }

    /// <summary>Right-click a talent slot: open the picker for it (again to close). Right-click in the picker closes it.</summary>
    [HarmonyPatch(typeof(UIelement), nameof(UIelement.OnRightClicked))]
    public static class PetTalentRightClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(UIelement __instance)
        {
            if (!(__instance is PetTalentUIElement talent)) return true;
            if (PetEditorUI.IsPickItem(talent))
            {
                PetEditorUI.ClosePicker();
                return false;
            }
            int slot = PetTalentLeftClickPatch.IndexOf(talent);
            if (slot < 0) return true;
            if (PetEditorUI.EditingSlot == slot) PetEditorUI.ClosePicker();
            else PetEditorUI.OpenPicker(slot);
            return false;
        }
    }
}
