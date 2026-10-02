using Unity.Entities;
using UnityEngine;

namespace LoadoutSharing
{
    /// <summary>Vanity kinds in VanitySlotsCD order.</summary>
    public enum VanityKind { Helm = 0, Breast = 1, Pants = 2 }

    /// <summary>
    /// Per-loadout vanity. Vanilla has ONE set of vanity slots (helm / breast / pants, indices in
    /// VanitySlotsCD) that every loadout shares. We append three more slots for every other loadout to the
    /// player prefab, AFTER every other mod's slots (this handler is subscribed in Init, after all
    /// EarlyInit subscriptions such as Five Loadouts), so slots that existing saves already use keep their
    /// indices. Loadout 1 keeps the vanilla vanity slots. An empty vanity slot means "show this loadout's
    /// real armor", exactly like vanilla; vanity does not waterfall.
    /// </summary>
    public static class VanityLayout
    {
        public const int KindCount = 3;

        private static readonly int[,] _slots = new int[SlotLayout.MaxPresetCount, KindCount];

        public static bool Ready { get; private set; }

        /// <summary>Loadouts that have their own vanity slots (= SlotLayout.PresetCount at prefab time).</summary>
        public static int Count { get; private set; }

        /// <summary>The vanity slot of <paramref name="kind"/> for <paramref name="preset"/> (loadout 1's for unknown presets).</summary>
        public static int Slot(int preset, VanityKind kind)
        {
            if (preset < 0 || preset >= Count) preset = 0;
            return _slots[preset, (int)kind];
        }

        public static void OnObjectTypeAdded(Entity entity, GameObject authoringData, EntityManager em)
        {
            if (authoringData == null || !authoringData.TryGetComponent<PlayerAuthoring>(out _)) return;
            if (!em.HasComponent<VanitySlotsCD>(entity) || !em.HasBuffer<ContainedObjectsBuffer>(entity)) return;

            var vanilla = em.GetComponentData<VanitySlotsCD>(entity);
            var contained = em.GetBuffer<ContainedObjectsBuffer>(entity);
            int count = SlotLayout.PresetCount;

            _slots[0, (int)VanityKind.Helm] = vanilla.helmVanitySlotIndex;
            _slots[0, (int)VanityKind.Breast] = vanilla.breastVanitySlotIndex;
            _slots[0, (int)VanityKind.Pants] = vanilla.pantsVanitySlotIndex;
            for (int p = 1; p < count; p++)
            {
                for (int k = 0; k < KindCount; k++)
                {
                    _slots[p, k] = contained.Length;
                    contained.Add(default);
                    SlotLayout.ReserveIndex(_slots[p, k]);
                }
            }
            Count = count;
            Ready = true;
            Debug.Log($"[{LoadoutSharingMod.Name}] Vanity layout: {count} loadouts, loadout 1 = {_slots[0, 0]}/{_slots[0, 1]}/{_slots[0, 2]}, " +
                      $"last = {_slots[count - 1, 0]}/{_slots[count - 1, 1]}/{_slots[count - 1, 2]} ({em.World.Name})");
        }
    }
}
