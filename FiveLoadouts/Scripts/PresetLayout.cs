using Unity.Entities;
using UnityEngine;

namespace FiveLoadouts
{
    /// <summary>
    /// Slot allocation for loadouts 4 and 5, done once per world on the player prefab.
    ///
    /// For each extra preset we append, in this fixed order, to ContainedObjectsBuffer:
    ///   helm, necklace, chest, pants, ring1, ring2, off-hand, bag, lantern, pet   (10 slots)
    /// and add an EquipmentPresetsBuffer entry copied from preset 1 (so the four pouch indices
    /// stay shared, exactly like vanilla) with the gear indices rewired to the new slots.
    ///
    /// The bag, lantern and pet slots are ALWAYS allocated so the layout (and therefore every
    /// saved index) is the same whether or not LoadoutSharing is installed. They are only
    /// wired in when LoadoutSharing's patched API is present; otherwise presets 4 and 5 keep
    /// pointing at the vanilla shared bag/lantern slot and the vanilla PetOwnerCD slot, which
    /// is exactly how vanilla presets 2 and 3 behave.
    /// </summary>
    public static class PresetLayout
    {
        public const int VanillaPresetCount = 3;
        public const int PresetCount = 5;
        public const int SlotsPerExtraPreset = 10;

        public static bool Ready { get; private set; }

        /// <summary>Highest slot index we rely on; the contained-objects buffer must be longer than this.</summary>
        public static int MaxIndex { get; private set; } = -1;

        /// <summary>Whether the private bag/lantern/pet slots are wired in (LoadoutSharing bridge available).</summary>
        public static bool PrivateExtras { get; private set; }

        private static readonly EquipmentCD[] _presetTable = new EquipmentCD[PresetCount];
        private static readonly int[] _petSlot = new int[PresetCount];

        /// <summary>The prefab-time EquipmentCD for an extra preset (3 or 4).</summary>
        public static EquipmentCD PresetEquipment(int preset) => _presetTable[preset];

        /// <summary>The private pet slot for an extra preset (3 or 4), or -1.</summary>
        public static int PetSlot(int preset) => _petSlot[preset];

        public static void OnObjectTypeAdded(Entity entity, GameObject authoringData, EntityManager em)
        {
            try
            {
                Apply(entity, authoringData, em);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{FiveLoadoutsMod.Name}] Prefab setup failed: {ex}");
            }
        }

        private static void Apply(Entity entity, GameObject authoringData, EntityManager em)
        {
            if (authoringData == null || !authoringData.TryGetComponent<PlayerAuthoring>(out _)) return;
            if (!em.HasBuffer<EquipmentPresetsBuffer>(entity)) return;
            if (!em.HasBuffer<ContainedObjectsBuffer>(entity)) return;

            var presets = em.GetBuffer<EquipmentPresetsBuffer>(entity);
            if (presets.Length < VanillaPresetCount)
            {
                Debug.LogWarning($"[{FiveLoadoutsMod.Name}] Player prefab has {presets.Length} presets, expected {VanillaPresetCount}; skipping.");
                return;
            }
            if (presets.Length >= PresetCount) return; // already extended (defensive)

            bool sharingRan = presets[1].equipment.bagIndex != presets[0].equipment.bagIndex;
            bool privateExtras = LoadoutSharingBridge.CanRegister;
            var baseEquipment = presets[0].equipment;

            int prevMax = MaxIndex;
            int firstNew = em.GetBuffer<ContainedObjectsBuffer>(entity).Length;

            for (int p = presets.Length; p < PresetCount; p++)
            {
                var contained = em.GetBuffer<ContainedObjectsBuffer>(entity);
                var e = baseEquipment; // pouch indices and (shared mode) bag/lantern come from preset 1
                e.helmSlotIndex = Add(contained);
                e.necklaceSlotIndex = Add(contained);
                e.breastSlotIndex = Add(contained);
                e.pantsSlotIndex = Add(contained);
                e.ring1SlotIndex = Add(contained);
                e.ring2SlotIndex = Add(contained);
                e.offHandIndex = Add(contained);
                int bag = Add(contained);
                int lantern = Add(contained);
                int pet = Add(contained);
                if (privateExtras)
                {
                    e.bagIndex = bag;
                    e.lanternIndex = lantern;
                }

                // Re-fetch: adding to one buffer never moves another, but be explicit.
                presets = em.GetBuffer<EquipmentPresetsBuffer>(entity);
                presets.Add(new EquipmentPresetsBuffer { equipment = e });

                _presetTable[p] = e;
                _petSlot[p] = pet;
                MaxIndex = pet;

                if (privateExtras)
                    LoadoutSharingBridge.RegisterPreset(p, e, pet);
            }

            if (Ready && prevMax != MaxIndex)
            {
                Debug.LogWarning($"[{FiveLoadoutsMod.Name}] Player prefab layout differs between worlds " +
                                 $"(max index was {prevMax}, now {MaxIndex}). Another mod may be reordering slots.");
            }
            PrivateExtras = privateExtras;
            Ready = true;

            Debug.Log($"[{FiveLoadoutsMod.Name}] Player layout: presets={PresetCount} firstNewSlot={firstNew} maxIndex={MaxIndex} " +
                      $"helm4={_presetTable[3].helmSlotIndex} helm5={_presetTable[4].helmSlotIndex} " +
                      $"pet4={_petSlot[3]} pet5={_petSlot[4]} loadoutSharingRanFirst={sharingRan} privateExtras={privateExtras}");
        }

        private static int Add(DynamicBuffer<ContainedObjectsBuffer> contained)
        {
            int index = contained.Length;
            contained.Add(default);
            return index;
        }
    }
}
