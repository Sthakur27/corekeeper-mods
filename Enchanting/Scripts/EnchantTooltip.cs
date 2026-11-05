using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Adds "Sharpness II" plus what it does to item tooltips: "x1.08 melee damage" for multiplier (Model B)
    /// enchants, otherwise the game's own condition text with the value the stat system applies (item-level-scaled
    /// procs show the value for this item's level).
    /// </summary>
    [HarmonyPatch(typeof(SlotUIBase), nameof(SlotUIBase.GetHoverStats), new[] { typeof(ContainedObjectsBuffer), typeof(bool), typeof(bool) })]
    public static class EnchantTooltip
    {
        private static readonly Color NameColor = new Color(0.78f, 0.55f, 1f);
        private static readonly Color EffectColor = new Color(0.66f, 0.5f, 0.9f);

        public static void Postfix(ContainedObjectsBuffer containedObject, ref List<TextAndFormatFields> __result)
        {
            if (__result == null) return;
            var enchants = EnchantStorage.ReadClient(containedObject);
            if (enchants.Count == 0) return;
            bool unique = false;

            foreach (var e in enchants)
            {
                var def = e.Def;
                if (def == null) continue;
                if (def.Stack == EnchantStack.Max) unique = true;
                // Hover lines are always looked up as localization terms (dontLocalize is ignored here), so the
                // name goes through a pass-through term "{0}" registered in EnchantingMod.EarlyInit.
                __result.Add(new TextAndFormatFields
                {
                    text = EnchantingMod.PlainTerm,
                    formatFields = new[] { $"{def.Name} {Enchants.Roman(e.Level)}" },
                    dontLocalizeFormatFields = true,
                    color = NameColor,
                });
                try
                {
                    if (def.Mode == EnchantMode.Relative)
                    {
                        // Model B: the raw value depends on the player's other bonuses, so show the multiplier.
                        __result.Add(new TextAndFormatFields
                        {
                            text = EnchantingMod.PlainTerm,
                            formatFields = new[] { EnchantMath.MultiplierText(def, e.Level, containedObject.objectID) },
                            dontLocalizeFormatFields = true,
                            color = EffectColor,
                        });
                        continue;
                    }
                    // Flat and item-level-scaled: the game's own condition text with the value actually applied.
                    var effect = ConditionUI.GetConditionTextAndFormatFields(containedObject, new ConditionData
                    {
                        conditionID = def.Condition,
                        value = EnchantMath.Raw(def, e.Level, containedObject.objectData),
                    }, previewReinforced: false, isReinforced: false, previewUpgraded: false);
                    if (effect != null)
                    {
                        effect.color = EffectColor;
                        __result.Add(effect);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[{EnchantingMod.Name}] tooltip text for {def.Condition}: {ex.Message}");
                }
                finally
                {
                    if (def.Note != null)
                        __result.Add(new TextAndFormatFields
                        {
                            text = EnchantingMod.PlainTerm,
                            formatFields = new[] { def.Note },
                            dontLocalizeFormatFields = true,
                            color = EffectColor,
                        });
                }
            }
            if (unique)
                __result.Add(new TextAndFormatFields
                {
                    text = EnchantingMod.PlainTerm,
                    formatFields = new[] { "Doesn't stack: only your highest copy counts" },
                    dontLocalizeFormatFields = true,
                    color = EffectColor,
                });
            if (Enchants.SlotOf(containedObject.objectID) == EnchantSlot.Summon)
                __result.Add(new TextAndFormatFields
                {
                    text = EnchantingMod.PlainTerm,
                    formatFields = new[] { "Works while it is the last tome you held and stays in your inventory" },
                    dontLocalizeFormatFields = true,
                    color = EffectColor,
                });
        }
    }
}
