using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Adds "Sharpness II" plus the game's own condition text ("+10% melee damage") to item tooltips. Using the
    /// vanilla condition formatter means the shown value is exactly what the stat system will apply.
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

            foreach (var e in enchants)
            {
                var def = e.Def;
                if (def == null) continue;
                __result.Add(new TextAndFormatFields
                {
                    text = $"{def.Name} {Enchants.Roman(e.Level)}",
                    dontLocalize = true,
                    color = NameColor,
                });
                try
                {
                    var effect = ConditionUI.GetConditionTextAndFormatFields(containedObject, new ConditionData
                    {
                        conditionID = def.Condition,
                        value = EnchantUnits.Raw(def, e.Level),
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
            }
        }
    }
}
