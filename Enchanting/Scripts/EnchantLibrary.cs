using System.Collections.Generic;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// The table's "library" (Minecraft's bookshelves): placed figurines (any enemy or boss figurine, vanilla
    /// ObjectIDs named *Trophy) within <see cref="Radius"/> tiles of the Enchanting Table raise the chance that a new
    /// enchant lands at a high level (EnchantRoll.WeightsFor, capped at EnchantRoll.MaxFigurines).
    ///
    /// Cost: counted on the server only when the roll button is pressed (one pass over placed objects), never per
    /// frame. Dropped figurines lying on the floor (PickUpItemCD) don't count.
    /// </summary>
    public static class EnchantLibrary
    {
        public const float Radius = 5f;

        private static HashSet<ObjectID> _figurines;

        /// <summary>Every vanilla figurine ObjectID (enum names ending in "Trophy"), built once.</summary>
        public static bool IsFigurine(ObjectID id)
        {
            if (_figurines == null)
            {
                _figurines = new HashSet<ObjectID>();
                // Plain cast + ToString (already used elsewhere in the mod) instead of Enum.GetValues, to stay clear of
                // anything the mod loader's sandbox might reject. Vanilla IDs are below 32768; runs once (~ms).
                for (int i = 1; i < 32768; i++)
                {
                    var o = (ObjectID)i;
                    if (o.ToString().EndsWith("Trophy")) _figurines.Add(o);
                }
            }
            return _figurines.Contains(id);
        }

        /// <summary>Placed figurines within Radius of the table (not capped).</summary>
        public static int CountFigurines(EntityManager em, Entity table)
        {
            if (table == Entity.Null || !em.Exists(table) || !em.HasComponent<LocalTransform>(table)) return 0;
            return CountFigurines(em, em.GetComponentData<LocalTransform>(table).Position.xz);
        }

        /// <summary>Placed figurines within Radius of a world position (x, z), in any world.</summary>
        public static int CountFigurines(EntityManager em, float2 at)
        {
            int count = 0;
            using (var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ObjectDataCD>(), ComponentType.ReadOnly<LocalTransform>() },
                None = new[] { ComponentType.ReadOnly<PickUpItemCD>() },
            }))
            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                foreach (var e in entities)
                {
                    if (!IsFigurine(em.GetComponentData<ObjectDataCD>(e).objectID)) continue;
                    if (math.distance(em.GetComponentData<LocalTransform>(e).Position.xz, at) <= Radius) count++;
                }
            }
            return count;
        }

        // Client readout for the table window: recounted at most once a second while the window asks for it.
        private static float _clientAt = -10f;
        private static int _clientCount;
        private static Vector3 _clientPos;
        public const float ClientRefresh = 1f;

        /// <summary>Client: figurines the open table sees (cached ~1 s; same rule as the server count).</summary>
        public static int ClientCount(CraftingBuilding building)
        {
            if (building == null || !building.entityExist) return 0;
            var pos = building.WorldPosition;
            if (Time.unscaledTime - _clientAt < ClientRefresh && pos == _clientPos) return _clientCount;
            _clientAt = Time.unscaledTime;
            _clientPos = pos;
            var em = building.world.EntityManager;
            _clientCount = em.HasComponent<LocalTransform>(building.entity)
                ? CountFigurines(em, em.GetComponentData<LocalTransform>(building.entity).Position.xz)
                : CountFigurines(em, new float2(pos.x, pos.z));
            return _clientCount;
        }
    }

    /// <summary>
    /// Hover text of the table's roll button: how many figurines the table sees and the odds for a new enchant,
    /// plus a hint to place more. ButtonUIElement owns the description; only UpgradeForgeButtons on our table change.
    /// </summary>
    [HarmonyPatch(typeof(ButtonUIElement), nameof(ButtonUIElement.GetHoverDescription))]
    public static class EnchantTableFigurineHover
    {
        private static readonly Color Info = new Color(0.78f, 0.55f, 1f);
        private static readonly Color Hint = new Color(0.66f, 0.62f, 0.75f);

        public static void Postfix(ButtonUIElement __instance, ref List<TextAndFormatFields> __result)
        {
            if (!(__instance is UpgradeForgeButton) || EnchantRemoveButton.IsRemove((UpgradeForgeButton)__instance)) return;
            if (!EnchantTableUI.IsTableOpen(out var building)) return;
            int n = System.Math.Min(EnchantLibrary.ClientCount(building), EnchantRoll.MaxFigurines);
            if (__result == null) __result = new List<TextAndFormatFields>();
            __result.Add(Line($"Figurines nearby: {n}/{EnchantRoll.MaxFigurines}", Info));
            __result.Add(Line($"New enchant odds: {EnchantRoll.OddsText(n)}", Info));
            if (n < EnchantRoll.MaxFigurines)
                __result.Add(Line($"Place figurines within {(int)EnchantLibrary.Radius} tiles to raise the odds", Hint));
        }

        internal static TextAndFormatFields Line(string text, Color color) => new TextAndFormatFields
        {
            text = EnchantingMod.PlainTerm,
            formatFields = new[] { text },
            dontLocalizeFormatFields = true,
            color = color,
        };
    }
}
