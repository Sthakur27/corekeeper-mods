using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ArmorDye
{
    /// <summary>
    /// Client side: after every player's late update (which runs for remote players too and keeps their armor
    /// look current), swap each armor layer's sheet for a recolored copy when the displayed item carries a dye.
    /// Only the material's _ReplacementTex is changed; SpriteSheetSkin.skin (the Addressables-loaded original)
    /// is left alone, so the game's own loading/unloading is untouched and "off" just puts the original back.
    /// The character-window and vanity-window previews are separate PlayerControllers without inventories;
    /// they get the local player's dyes (see <see cref="TickPreviews"/>).
    /// </summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.ManagedLateUpdate))]
    public static class ArmorRecolor
    {
        private static readonly int ReplacementTex = Shader.PropertyToID("_ReplacementTex");
        private static readonly int ColorReplaceTex = Shader.PropertyToID("_colorReplaceTexture");
        private static readonly int EmissiveTex = Shader.PropertyToID("_EmissiveTex");
        private static readonly HashSet<string> Logged = new HashSet<string>();

        internal static readonly List<PlayerController> Previews = new List<PlayerController>();
        internal static readonly Dictionary<Unity.Entities.Entity, PlayerController> Players = new Dictionary<Unity.Entities.Entity, PlayerController>();

        /// <summary>The dye of the weapon <paramref name="pc"/> is holding (0 if none or held-item dyes are off).</summary>
        internal static int HeldDye(PlayerController pc)
        {
            if (pc == null) return 0;
            var held = pc.visuallyEquippedContainedObject;
            return held.objectID != ObjectID.None && held.auxDataIndex != 0 && DyeColor.CanDye(held.objectData) ? DyeOf(held) : 0;
        }

        [HarmonyPostfix]
        public static void Postfix(PlayerController __instance)
        {
            var pc = __instance;
            if (pc == null || pc.vanitySlotsHandler == null || pc.equipmentHandler == null) return;
            Players[pc.entity] = pc;
            try
            {
                if (pc.isLocal && pc.shirtSkin != null && pc.shirtSkin.skin != null && pc.shirtSkin.sr.material != null)
                    Probe(pc.shirtSkin, pc.shirtSkin.sr.material);
                ApplyAll(pc, pc);
                ApplyHeld(pc);
            }
            catch (System.Exception e)
            {
                LogOnce("error:" + e.Message, $"recolor failed: {e}", true);
            }
        }

        /// <summary>Called every frame from IMod.Update: dye the inventory previews like the local player.</summary>
        public static void TickPreviews()
        {
            if (Previews.Count == 0) return;
            var player = Manager.main != null ? Manager.main.player : null;
            if (player == null || player.vanitySlotsHandler == null || player.equipmentHandler == null) return;
            try
            {
                for (int i = Previews.Count - 1; i >= 0; i--)
                {
                    var p = Previews[i];
                    if (p == null) { Previews.RemoveAt(i); continue; }
                    // The previews are "appearance only" copies whose PlayerController component is disabled, so test the object.
                    if (p != player && p.gameObject.activeInHierarchy)
                    {
                        ApplyAll(p, player);
                        LogOnce("preview:" + p.GetInstanceID(), $"dyeing preview {p.name}", false);
                    }
                }
            }
            catch (System.Exception e)
            {
                LogOnce("preview:" + e.Message, $"preview recolor failed: {e}", true);
            }
        }

        /// <summary>Dyes <paramref name="look"/>'s armor layers from <paramref name="owner"/>'s displayed items.</summary>
        private static void ApplyAll(PlayerController look, PlayerController owner)
        {
            var v = owner.vanitySlotsHandler;
            var e = owner.equipmentHandler;
            Apply(look, look.helmSkin, v.helmVanitySlotInventoryHandler, e.helmInventoryHandler);
            Apply(look, look.breastArmorSkin, v.breastVanitySlotInventoryHandler, e.breastInventoryHandler);
            Apply(look, look.pantsArmorSkin, v.pantsVanitySlotInventoryHandler, e.pantsInventoryHandler);
        }

        /// <summary>
        /// The weapon/tool in hand: the game puts the item's sheet on one of these SpriteSheetSkins
        /// (PlayerController.ActivateCarryableItemSpriteAndSkin); only the active one is drawn.
        /// </summary>
        private static void ApplyHeld(PlayerController pc)
        {
            var held = pc.visuallyEquippedContainedObject;
            int dye = DyeSettings.HeldItems && held.objectID != ObjectID.None && held.auxDataIndex != 0 && DyeColor.CanDye(held.objectData) ? DyeOf(held) : 0;
            ApplyHeldLayer(pc.carryableSwingItemSkinSkin, dye);
            ApplyHeldLayer(pc.carryableRangeItemSkinSkin, dye);
            ApplyHeldLayer(pc.carryableShieldItemSkinSkin, dye);
            ApplyHeldLayer(pc.carryableBigSpearItemSkin, dye);
            ApplyHeldLayer(pc.carryableBigSwingItemSkin, dye);
            ApplyHeldLayer(pc.carryableDrillToolSkin, dye);
            ApplyHeldLayer(pc.carryableFishingRodSkin, dye);
            ApplyHeldLayer(pc.instrumentSkin, dye);
        }

        private static void ApplyHeldLayer(SpriteSheetSkin layer, int dye)
        {
            if (layer == null || !layer.gameObject.activeInHierarchy) return;
            ApplyLayer(layer, dye);
        }

        private static void Apply(PlayerController look, SpriteSheetSkin layer, InventoryHandler vanity, InventoryHandler equipped)
        {
            if (layer == null || layer.skin == null) return;
            if (look.isLocal && layer.sr != null && layer.sr.material != null) Probe(layer, layer.sr.material);
            ApplyLayer(layer, DisplayedDye(vanity, equipped));
        }

        internal static void ApplyLayer(SpriteSheetSkin layer, int dye)
        {
            if (layer == null || layer.skin == null) return;
            var sr = layer.sr;
            if (sr == null) return;
            var mat = sr.material;
            if (mat == null) return;

            Texture current = mat.GetTexture(ReplacementTex);
            Texture want = dye == 0 ? layer.skin : DyeTextures.Sheet(layer.skin, dye);
            // With no dye only undo our own texture; anything else (temporary skins etc.) belongs to the game.
            if (want != null && current != want && (dye != 0 || DyeTextures.IsOurs(current)))
                mat.SetTexture(ReplacementTex, want);

            // Glowing parts (Galaxite gear, Burnzooka flames, glowing armor) live in a separate emissive texture
            // on the same material; dye it the same way so the glow matches.
            if (!mat.HasProperty(EmissiveTex)) return;
            Texture emissive = mat.GetTexture(EmissiveTex);
            if (emissive == null) return;
            Texture2D original = DyeTextures.OriginalSheet(emissive);
            if (original == null) return;
            Texture wantEmissive = dye == 0 ? original : DyeTextures.Sheet(original, dye);
            if (wantEmissive != null && wantEmissive != emissive) mat.SetTexture(EmissiveTex, wantEmissive);
        }

        /// <summary>Same choice as PlayerController.UpdateGearCustomization: vanity item if any, else the equipped one.</summary>
        private static int DisplayedDye(InventoryHandler vanity, InventoryHandler equipped)
        {
            ContainedObjectsBuffer item = default;
            if (vanity != null)
            {
                item = vanity.GetContainedObjectData(0);
                if (item.objectID == ObjectID.None && item.amount < 0) return 0; // vanity "hide" marker
            }
            if (item.objectID == ObjectID.None && equipped != null) item = equipped.GetContainedObjectData(0);
            return DyeOf(item);
        }

        /// <summary>The dye stored on an item (0 if none).</summary>
        public static int DyeOf(ContainedObjectsBuffer item)
        {
            if (item.objectID == ObjectID.None || item.auxDataIndex == 0) return 0;
            return InventoryHandler.TryGetExtraInventoryData<MealsEatenCD>(item, out var data) ? data.Value : 0;
        }

        private static void Probe(SpriteSheetSkin layer, Material mat)
        {
            string key = layer.name + "|" + mat.shader.name;
            LogOnce(key, $"layer {layer.name}: shader {mat.shader.name}, _colorReplaceTexture={mat.HasProperty(ColorReplaceTex)}, _ReplacementTex={mat.HasProperty(ReplacementTex)}, skin {layer.skin.name} {layer.skin.width}x{layer.skin.height} {layer.skin.format}", false);
        }

        private static void LogOnce(string key, string message, bool error)
        {
            if (!Logged.Add(key)) return;
            if (error) Debug.LogError($"[{ArmorDyeMod.Name}] {message}");
            else Debug.Log($"[{ArmorDyeMod.Name}] {message}");
        }
    }

    [HarmonyPatch(typeof(CharacterWindowUI), nameof(CharacterWindowUI.SetPlayerController))]
    public static class CharacterPreviewPatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameObject go) => Track(go);

        internal static void Track(GameObject go)
        {
            var pc = go != null ? go.GetComponent<PlayerController>() : null;
            if (pc == null || ArmorRecolor.Previews.Contains(pc)) return;
            ArmorRecolor.Previews.Add(pc);
            Debug.Log($"[{ArmorDyeMod.Name}] tracking preview {pc.name}");
        }
    }

    [HarmonyPatch(typeof(VanityUI), nameof(VanityUI.SetPlayerController))]
    public static class VanityPreviewPatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameObject go) => CharacterPreviewPatch.Track(go);
    }

    /// <summary>
    /// The previews can be set up before mods load (so the SetPlayerController hooks above never see them);
    /// UpdateCharacterCustomization runs every time the window opens, so pick the preview up there too.
    /// </summary>
    [HarmonyPatch(typeof(CharacterWindowUI), nameof(CharacterWindowUI.UpdateCharacterCustomization))]
    public static class CharacterPreviewRefreshPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerController ____playerPreview)
        {
            if (____playerPreview != null) CharacterPreviewPatch.Track(____playerPreview.gameObject);
        }
    }

    [HarmonyPatch(typeof(VanityUI), nameof(VanityUI.UpdateCharacterCustomization))]
    public static class VanityPreviewRefreshPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerController ____playerPreview)
        {
            if (____playerPreview != null) CharacterPreviewPatch.Track(____playerPreview.gameObject);
        }
    }

    /// <summary>
    /// Item icons: every inventory-style slot (inventory, hotbar, equipment, vanity, chests) shows a dyed copy
    /// of its armor icon. Vanilla only re-sets the sprite when the slot's contents change, so this runs after
    /// it every frame and swaps between the original and the dyed copy as needed.
    /// </summary>
    [HarmonyPatch(typeof(InventorySlotUI), nameof(InventorySlotUI.UpdateSlot))]
    public static class SlotIconPatch
    {
        [HarmonyPostfix]
        public static void Postfix(InventorySlotUI __instance)
        {
            var icon = __instance.icon;
            if (icon == null || icon.sprite == null) return;
            if (__instance.GetInventoryHandler() == null) return;
            var item = __instance.GetContainedObjectData();
            Sprite current = icon.sprite;
            Sprite original = DyeTextures.Original(current);
            int dye = 0;
            if (DyeSettings.Icons && item.objectID != ObjectID.None && item.auxDataIndex != 0 && DyeColor.CanDye(item.objectData))
                dye = ArmorRecolor.DyeOf(item);
            Sprite want = dye == 0 ? original : (DyeTextures.Icon(original, dye) ?? original);
            if (want != current) icon.sprite = want;
        }
    }
}
