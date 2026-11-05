using HarmonyLib;
using UnityEngine;

namespace SlotOrganizer
{
    /// <summary>
    /// "Edit character" page for an existing character: the game's own character creation screen,
    /// opened from the slot's "..." button with that character's name and looks filled in. Done
    /// saves the name and appearance and returns to the character list (no game is started); Back
    /// leaves the character unchanged. The role picker is hidden because a role only decides the
    /// starting kit of a new character.
    /// </summary>
    internal static class SlotEdit
    {
        /// <summary>Save id of the character being edited, or -1.</summary>
        internal static int editing = -1;
        private static ChooseCharacterMenu returnMenu;

        public static void Begin(ChooseCharacterMenu menu, int slot)
        {
            int offset = menu.isCreativeMenu ? SaveManager.creativeCharacterStartIndex : 0;
            int id = slot + offset;
            if (slot < 0 || !Manager.saves.CharacterExists(id)) return;
            editing = id;
            returnMenu = menu;
            Manager.saves.SetCharacterId(id);
            Manager.menu.PushMenu(RadicalMenu.MenuType.CHARACTER_CUSTOMIZATION);
            if (Manager.menu.characterCustomizationMenu is CharacterCustomizationMenu m && m.pc != null) Fill(m);
        }

        /// <summary>Puts the saved name and looks into the creation screen (after vanilla randomized it).</summary>
        internal static void Fill(CharacterCustomizationMenu m)
        {
            if (editing < 0 || m.pc == null) return;
            PlayerCustomization c = Manager.saves.GetCharacterCustomization(editing);
            m.nameInput.SetInputText(c.name.Value);
            if (m.roleSelection != null) m.roleSelection.gameObject.SetActive(false);
            c.helm = DataBlockAddress.Empty;
            c.breastArmor = DataBlockAddress.Empty;
            c.pantsArmor = DataBlockAddress.Empty;
            m.pc.activeCustomization = c;
            m.pc.RefreshCustomization();
        }

        internal static void Save(CharacterCustomizationMenu m)
        {
            int id = editing;
            editing = -1;
            PlayerCustomization saved = Manager.saves.GetCharacterCustomization(id);
            PlayerCustomization c = m.pc.activeCustomization;
            string name = m.nameText.displayedTextString.Trim();
            if (name.Length > 0) c.name = name;
            c.helm = saved.helm;
            c.breastArmor = saved.breastArmor;
            c.pantsArmor = saved.pantsArmor;
            Manager.saves.SetCharacterCustomization(id, c);
            Manager.saves.WriteCharacter(id);
            Manager.filesystemManager.Flush();
            Debug.Log($"[{SlotOrganizerMod.Name}] Edited character {id + 1} (\"{c.name.Value}\")");
            Manager.menu.PopMenu();
            if (returnMenu != null) returnMenu.SetupSaveSlots();
        }
    }

    [HarmonyPatch(typeof(CharacterCustomizationMenu), nameof(CharacterCustomizationMenu.SetPlayerController))]
    internal static class EditFillPatch
    {
        // Vanilla randomizes a fresh look here; keep its "already randomized" flag as it was so
        // the next new character still gets a random look.
        private static void Prefix(CharacterCustomizationMenu __instance, out bool __state)
            => __state = Traverse.Create(__instance).Field<bool>("hasRandomized").Value;

        private static void Postfix(CharacterCustomizationMenu __instance, bool __state)
        {
            if (SlotEdit.editing < 0) return;
            Traverse.Create(__instance).Field<bool>("hasRandomized").Value = __state;
            SlotEdit.Fill(__instance);
        }
    }

    [HarmonyPatch(typeof(CharacterCustomizationMenu), nameof(CharacterCustomizationMenu.OnDone))]
    internal static class EditDonePatch
    {
        private static bool Prefix(CharacterCustomizationMenu __instance)
        {
            if (SlotEdit.editing < 0 || Manager.sceneHandler.isInGame) return true;
            SlotEdit.Save(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(RadicalMenu), nameof(RadicalMenu.Deactivate))]
    internal static class EditClosePatch
    {
        // Back/Esc out of the edit page: forget the edit so the next "new character" is normal.
        private static void Postfix(RadicalMenu __instance)
        {
            if (__instance is CharacterCustomizationMenu) SlotEdit.editing = -1;
        }
    }

    [HarmonyPatch(typeof(UIelement), nameof(UIelement.GetHoverTitle))]
    internal static class ButtonTooltipPatch
    {
        private static void Postfix(UIelement __instance, ref TextAndFormatFields __result)
        {
            string text = SlotArrows.Tooltip(__instance);
            if (text != null) __result = new TextAndFormatFields { text = text, dontLocalize = true };
        }
    }
}
