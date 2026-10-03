using Unity.Entities;
using UnityEngine;

namespace EnderStash
{
    /// <summary>
    /// The stash is <see cref="Size"/> slots appended to the end of the player prefab's
    /// ContainedObjectsBuffer. The character save stores that whole buffer, so the stash follows the
    /// character into every world. Death only drops the main inventory (slots 10..maxSize of
    /// InventoryBuffer[0]), and crafting / quick stack to nearby chests only look at InventoryBuffer
    /// ranges, so these slots are invisible to everything except our chest view.
    /// </summary>
    public static class StashLayout
    {
        public const int Columns = 10;
        public const int Size = 40;

        /// <summary>First stash slot index in the player's ContainedObjectsBuffer; -1 until the player prefab is seen.</summary>
        public static int Start { get; private set; } = -1;
        public static int End => Start + Size;
        public static bool Ready => Start >= 0;

        public static void OnObjectTypeAdded(Entity entity, GameObject authoringData, EntityManager em)
        {
            if (authoringData == null || !authoringData.TryGetComponent<PlayerAuthoring>(out _)) return;
            if (!em.HasBuffer<ContainedObjectsBuffer>(entity)) return;

            var contained = em.GetBuffer<ContainedObjectsBuffer>(entity);
            int start = contained.Length;
            for (int i = 0; i < Size; i++) contained.Add(default);

            if (Ready && start != Start)
                Debug.LogWarning($"[{EnderStashMod.Name}] Stash start differs between worlds (had {Start}, now {start}). Another mod may be adding player slots later.");
            Start = start;
            Debug.Log($"[{EnderStashMod.Name}] Stash slots {Start}..{End - 1} ({em.World.Name}).");
        }
    }
}
