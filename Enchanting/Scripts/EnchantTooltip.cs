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
        private static readonly Color InactiveColor = new Color(0.5f, 0.5f, 0.5f);

        public static void Postfix(ContainedObjectsBuffer containedObject, bool previewReinforced, bool previewUpgraded,
            ref List<TextAndFormatFields> __result)
        {
            if (__result == null) return;
            if (EnchantRecipe.TableId != ObjectID.None && containedObject.objectID == EnchantRecipe.TableId)
            {
                // The table item: tell players about the figurine library before they place it.
                __result.Add(EnchantTableFigurineHover.Line(
                    $"Place up to {EnchantRoll.MaxFigurines} figurines within {(int)EnchantLibrary.Radius} tiles for better enchantments", EffectColor));
                return;
            }
            var enchants = EnchantStorage.ReadClient(containedObject);
            if (enchants.Count == 0) return;
            // The forge's reinforce/upgrade preview computes its own numbers; leave those alone.
            if (!previewReinforced && !previewUpgraded) ApplyToWeaponLines(containedObject, enchants, __result);

            // Newest first: the top line is the enchant the table's remove button takes off (stored last).
            int first = EnchantRoll.FirstActive(enchants.Count, Enchants.SlotOf(containedObject.objectID));
            for (int n = enchants.Count - 1; n >= 0; n--)
            {
                var e = enchants[n];
                var def = e.Def;
                if (def == null) continue;
                if (n < first)
                {
                    // Extra enchant from before the 1-per-item limit on armor/accessories: kept, but does nothing.
                    __result.Add(new TextAndFormatFields
                    {
                        text = EnchantingMod.PlainTerm,
                        formatFields = new[] { $"{def.Name} {Enchants.Roman(e.Level)} (inactive)" },
                        dontLocalizeFormatFields = true,
                        color = InactiveColor,
                    });
                    continue;
                }
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
            if (first > 0)
                __result.Add(new TextAndFormatFields
                {
                    text = EnchantingMod.PlainTerm,
                    formatFields = new[] { "Armor and accessories use only their top enchant. Remove the others at the table." },
                    dontLocalizeFormatFields = true,
                    color = InactiveColor,
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

        // Vanilla tooltip line keys (SlotUIBase): weapon damage ("meleeWeapon"/"rangeWeapon", the damage range text
        // in formatFields[0]) and attacks per second ("WeaponSpeed").
        private const string MeleeDamageKey = "meleeWeapon", RangeDamageKey = "rangeWeapon", SpeedKey = "WeaponSpeed";

        /// <summary>
        /// The weapon's own damage and attack-speed lines include this item's multiplier enchants (Sharpness, Power,
        /// Arcane Power; Swiftblade, Quick Draw), like reinforcement does, with the multiplier shown after the number.
        /// Same base as vanilla (WeaponDamageCD at the item's level, +15% when reinforced; CooldownCD).
        /// </summary>
        private static void ApplyToWeaponLines(ContainedObjectsBuffer item, List<EnchantEntry> enchants, List<TextAndFormatFields> lines)
        {
            int dmgPct = 0, speedPct = 0;
            foreach (var e in enchants)
            {
                var def = e.Def;
                if (def == null || def.Mode != EnchantMode.Relative) continue;
                int pct = def.Values[Mathf.Clamp(e.Level, 1, def.MaxLevel) - 1];
                if (def.Bucket == RelBucket.Damage) dmgPct += pct;
                else if (def.Bucket == RelBucket.AttackSpeed) speedPct += pct;
            }
            if (dmgPct == 0 && speedPct == 0) return;
            try
            {
                var data = item.objectData;
                foreach (var line in lines)
                {
                    if (line == null) continue;
                    if (dmgPct != 0 && (line.text == MeleeDamageKey || line.text == RangeDamageKey)
                        && PugDatabase.TryGetComponent<HasWeaponDamageCD>(data, out var w))
                    {
                        var world = Manager.ecs.ClientWorld;
                        int dmg = EntityUtility.GetComponentData<WeaponDamageCD>(EntityUtility.GetLevelEntity(data), world).damage;
                        if (PugDatabase.HasComponent<DurabilityCD>(data) && PugDatabase.GetComponent<DurabilityCD>(data).IsReinforced(item.amount))
                            dmg += (int)Unity.Mathematics.math.round(dmg * 0.15f);
                        int enchanted = Mathf.RoundToInt(dmg * (1f + dmgPct / 100f));
                        line.formatFields = new[]
                        {
                            PugText.ProcessText(w.isMagic ? "magicWeaponDamage" : "physicalWeaponDamage",
                                StatsUIUtility.GetDamageRangeFormatFields(enchanted), shouldLocalize: true, shouldLocalizeFormatFields: false),
                        };
                        line.additionalText = Factor(dmgPct);
                        line.additionalTextColor = NameColor;
                    }
                    else if (speedPct != 0 && line.text == SpeedKey)
                    {
                        float cd = PugDatabase.HasComponent<RangeWeaponCD>(data) ? 0.6f : 0.4f;
                        if (PugDatabase.HasComponent<CooldownCD>(data)) cd = PugDatabase.GetComponent<CooldownCD>(data).cooldown;
                        if (cd <= 0f) continue;
                        float rate = 1f / cd * (1f + speedPct / 100f);
                        line.formatFields = new[] { rate.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) };
                        line.additionalText = Factor(speedPct);
                        line.additionalTextColor = NameColor;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[{EnchantingMod.Name}] weapon tooltip numbers: {ex.Message}");
            }
        }

        private static string Factor(int pct) =>
            "(x" + (1f + pct / 100f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " enchanted)";
    }
}
