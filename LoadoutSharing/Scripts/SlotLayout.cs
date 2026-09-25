using Unity.Entities;
using UnityEngine;

namespace LoadoutSharing
{
    /// <summary>The equipment slots this mod handles.</summary>
    public enum SlotKind
    {
        Helm,
        Necklace,
        Breast,
        Pants,
        Ring1,
        Ring2,
        OffHand,
        Bag,
        Lantern,
        Pet
    }

    /// <summary>
    /// Fixed slot layout plus the fallback rule.
    ///
    /// Vanilla gives every loadout its own helm/necklace/chest/pants/rings/off-hand slot but makes
    /// all three loadouts point at ONE bag, ONE lantern and ONE pet slot. We append six extra slots
    /// to the player prefab (loadout 2 bag/lantern/pet, loadout 3 bag/lantern/pet) in exactly the
    /// order Loadout Plus uses, so saves migrated by that mod carry straight over. Now every loadout
    /// has a private slot for every kind.
    ///
    /// Fallback: loadout 1 is the base. For loadouts 2 and 3, a slot uses its own item if it holds
    /// one, otherwise it falls through to loadout 1's item. The indices are deterministic, so the
    /// client and server agree without any network sync.
    /// </summary>
    public static class SlotLayout
    {
        public const int KindCount = 10;
        public const int VanillaPresetCount = 3;
        /// <summary>Upper bound for presets another mod may register (Five Loadouts uses 5).</summary>
        public const int MaxPresetCount = 8;
        /// <summary>Number of presets with a known private-slot table. 3 unless another mod registers more.</summary>
        public static int PresetCount { get; private set; } = VanillaPresetCount;

        public static bool Ready { get; private set; }

        /// <summary>Private slot index per [preset, kind], captured from the player prefab.</summary>
        private static readonly int[,] _private = new int[MaxPresetCount, KindCount];

        /// <summary>Highest slot index we rely on; the contained-objects buffer must be longer than this.</summary>
        public static int MaxIndex { get; private set; } = -1;

        /// <summary>The slot this preset owns for the kind.</summary>
        public static int PrivateSlot(int preset, SlotKind kind) => _private[preset, (int)kind];

        /// <summary>
        /// Lets another mod (Five Loadouts) add presets beyond the vanilla three. Called at prefab
        /// time in every world with that preset's own slot indices; the fallback rule, UI sync and
        /// pet/bag sync then cover it like presets 2 and 3.
        /// </summary>
        public static void RegisterPreset(int preset, EquipmentCD e, int petSlot)
        {
            if (preset < VanillaPresetCount || preset >= MaxPresetCount) return;
            _private[preset, (int)SlotKind.Helm] = e.helmSlotIndex;
            _private[preset, (int)SlotKind.Necklace] = e.necklaceSlotIndex;
            _private[preset, (int)SlotKind.Breast] = e.breastSlotIndex;
            _private[preset, (int)SlotKind.Pants] = e.pantsSlotIndex;
            _private[preset, (int)SlotKind.Ring1] = e.ring1SlotIndex;
            _private[preset, (int)SlotKind.Ring2] = e.ring2SlotIndex;
            _private[preset, (int)SlotKind.OffHand] = e.offHandIndex;
            _private[preset, (int)SlotKind.Bag] = e.bagIndex;
            _private[preset, (int)SlotKind.Lantern] = e.lanternIndex;
            _private[preset, (int)SlotKind.Pet] = petSlot;
            for (int k = 0; k < KindCount; k++)
                if (_private[preset, k] > MaxIndex) MaxIndex = _private[preset, k];
            if (preset + 1 > PresetCount) PresetCount = preset + 1;
            Debug.Log($"[{LoadoutSharingMod.Name}] Registered preset {preset + 1}: helm={e.helmSlotIndex} bag={e.bagIndex} lantern={e.lanternIndex} pet={petSlot}; presets={PresetCount}");
        }

        private static bool HasItem(DynamicBuffer<ContainedObjectsBuffer> contained, int slot)
        {
            return slot >= 0 && slot < contained.Length && contained[slot].objectID != ObjectID.None;
        }

        /// <summary>True when the preset's own slot is empty and loadout 1's slot has an item to fall through to.</summary>
        public static bool IsInherited(int preset, SlotKind kind, DynamicBuffer<ContainedObjectsBuffer> contained)
        {
            if (preset <= 0) return false;
            int own = _private[preset, (int)kind];
            int baseSlot = _private[0, (int)kind];
            if (own < 0 || baseSlot < 0 || own == baseSlot) return false;
            return !HasItem(contained, own) && HasItem(contained, baseSlot);
        }

        /// <summary>The slot this preset actually uses for the kind, honouring fallback.</summary>
        public static int EffectiveSlot(int preset, SlotKind kind, DynamicBuffer<ContainedObjectsBuffer> contained)
        {
            return IsInherited(preset, kind, contained)
                ? _private[0, (int)kind]
                : _private[preset, (int)kind];
        }

        /// <summary>Rewrites the index fields of an EquipmentCD for the given preset (pouches untouched).</summary>
        public static bool ApplyTo(ref EquipmentCD e, int preset, DynamicBuffer<ContainedObjectsBuffer> contained)
        {
            bool changed = false;
            changed |= Set(ref e.helmSlotIndex, EffectiveSlot(preset, SlotKind.Helm, contained));
            changed |= Set(ref e.necklaceSlotIndex, EffectiveSlot(preset, SlotKind.Necklace, contained));
            changed |= Set(ref e.breastSlotIndex, EffectiveSlot(preset, SlotKind.Breast, contained));
            changed |= Set(ref e.pantsSlotIndex, EffectiveSlot(preset, SlotKind.Pants, contained));
            changed |= Set(ref e.ring1SlotIndex, EffectiveSlot(preset, SlotKind.Ring1, contained));
            changed |= Set(ref e.ring2SlotIndex, EffectiveSlot(preset, SlotKind.Ring2, contained));
            changed |= Set(ref e.offHandIndex, EffectiveSlot(preset, SlotKind.OffHand, contained));
            changed |= Set(ref e.bagIndex, EffectiveSlot(preset, SlotKind.Bag, contained));
            changed |= Set(ref e.lanternIndex, EffectiveSlot(preset, SlotKind.Lantern, contained));
            return changed;
        }

        private static bool Set(ref int field, int value)
        {
            if (value < 0 || field == value) return false;
            field = value;
            return true;
        }

        /// <summary>
        /// Runs once per world for every object type the game registers. For the player prefab it
        /// appends our six slots, rewires presets 2 and 3, and records the private slot table.
        /// Both the client and the server world run this on the same prefab, so the indices match.
        /// </summary>
        public static void OnObjectTypeAdded(Entity entity, GameObject authoringData, EntityManager em)
        {
            if (authoringData == null || !authoringData.TryGetComponent<PlayerAuthoring>(out _)) return;
            if (!em.HasBuffer<EquipmentPresetsBuffer>(entity)) return;
            if (!em.HasBuffer<ContainedObjectsBuffer>(entity)) return;

            var presets = em.GetBuffer<EquipmentPresetsBuffer>(entity);
            if (presets.Length < VanillaPresetCount) return;

            var contained = em.GetBuffer<ContainedObjectsBuffer>(entity);

            int p1Pet = em.HasComponent<PetOwnerCD>(entity) ? em.GetComponentData<PetOwnerCD>(entity).SlotIndex : -1;

            // Same allocation order as Loadout Plus, on purpose.
            int p2Bag = contained.Length; contained.Add(default);
            int p2Lantern = contained.Length; contained.Add(default);
            int p2Pet = contained.Length; contained.Add(default);
            int p3Bag = contained.Length; contained.Add(default);
            int p3Lantern = contained.Length; contained.Add(default);
            int p3Pet = contained.Length; contained.Add(default);

            var e1 = presets[1].equipment;
            e1.bagIndex = p2Bag;
            e1.lanternIndex = p2Lantern;
            presets[1] = new EquipmentPresetsBuffer { equipment = e1 };

            var e2 = presets[2].equipment;
            e2.bagIndex = p3Bag;
            e2.lanternIndex = p3Lantern;
            presets[2] = new EquipmentPresetsBuffer { equipment = e2 };

            int prevBag2 = _private[1, (int)SlotKind.Bag];
            for (int p = 0; p < VanillaPresetCount; p++)
            {
                var e = presets[p].equipment;
                _private[p, (int)SlotKind.Helm] = e.helmSlotIndex;
                _private[p, (int)SlotKind.Necklace] = e.necklaceSlotIndex;
                _private[p, (int)SlotKind.Breast] = e.breastSlotIndex;
                _private[p, (int)SlotKind.Pants] = e.pantsSlotIndex;
                _private[p, (int)SlotKind.Ring1] = e.ring1SlotIndex;
                _private[p, (int)SlotKind.Ring2] = e.ring2SlotIndex;
                _private[p, (int)SlotKind.OffHand] = e.offHandIndex;
                _private[p, (int)SlotKind.Bag] = e.bagIndex;
                _private[p, (int)SlotKind.Lantern] = e.lanternIndex;
            }
            _private[0, (int)SlotKind.Pet] = p1Pet;
            _private[1, (int)SlotKind.Pet] = p2Pet;
            _private[2, (int)SlotKind.Pet] = p3Pet;
            if (p3Pet > MaxIndex) MaxIndex = p3Pet;

            if (Ready && prevBag2 != p2Bag)
            {
                Debug.LogWarning($"[{LoadoutSharingMod.Name}] Player prefab layout differs between worlds " +
                                 $"(had bag2={prevBag2}, now {p2Bag}). Another mod may be reordering slots.");
            }
            Ready = true;

            Debug.Log($"[{LoadoutSharingMod.Name}] Player layout: pet={p1Pet}/{p2Pet}/{p3Pet} " +
                      $"bag2={p2Bag} lantern2={p2Lantern} bag3={p3Bag} lantern3={p3Lantern} " +
                      $"helm={_private[0, 0]}/{_private[1, 0]}/{_private[2, 0]}");
        }
    }
}
