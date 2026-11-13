using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
            float2 at = em.GetComponentData<LocalTransform>(table).Position.xz;
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
    }
}
