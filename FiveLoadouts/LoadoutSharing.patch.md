# LoadoutSharing changes needed for Five Loadouts

Minimal edits to `C:\Users\Sid\CoreKeeperMods\LoadoutSharing` so its fallback rule covers
presets 0..4 when Five Loadouts is installed. Nothing changes for players without Five Loadouts:
`PresetCount` stays 3 until something registers more presets.

Why it is needed: LoadoutSharing sizes its private-slot table to 3 presets and clamps the
active preset to `PresetCount - 1` in `HandlerSync.TryGetContext`. With loadout 4 or 5 active
that clamp makes the equipment UI show loadout 3's slots while `EquipmentCD` uses loadout 4's,
and `LoadoutLayoutSystem` never applies fallback to presets 3 and 4.

Order of operations: Five Loadouts declares LoadoutSharing as an optional dependency, so the
loader runs LoadoutSharing's `EarlyInit` (and therefore its `OnObjectTypeAdded` handler) first.
LoadoutSharing appends its six slots and captures presets 0..2 as today; Five Loadouts then
appends its 20 slots and calls `SlotLayout.RegisterPreset(3, ...)` and `RegisterPreset(4, ...)`
(looked up by name with `AccessTools`, so no compile-time reference either way).

Validated: a copy of LoadoutSharing with exactly these edits compiles with 0 errors against the
game assemblies (offline csc), and Five Loadouts compiles independently.

## 1. `Scripts/SlotLayout.cs`

### 1a. Replace the constant with a growable count

Old:
```csharp
        public const int KindCount = 10;
        public const int PresetCount = 3;
```
New:
```csharp
        public const int KindCount = 10;
        public const int VanillaPresetCount = 3;
        /// <summary>Upper bound for presets another mod may register (Five Loadouts uses 5).</summary>
        public const int MaxPresetCount = 8;
        /// <summary>Number of presets with a known private-slot table. 3 unless another mod registers more.</summary>
        public static int PresetCount { get; private set; } = VanillaPresetCount;
```

### 1b. Size the table by the upper bound

Old:
```csharp
        private static readonly int[,] _private = new int[PresetCount, KindCount];
```
New:
```csharp
        private static readonly int[,] _private = new int[MaxPresetCount, KindCount];
```

### 1c. Keep `OnObjectTypeAdded` on the vanilla three (two edits)

Old:
```csharp
            if (presets.Length < PresetCount) return;
```
New:
```csharp
            if (presets.Length < VanillaPresetCount) return;
```

Old:
```csharp
            for (int p = 0; p < PresetCount; p++)
            {
                var e = presets[p].equipment;
```
New:
```csharp
            for (int p = 0; p < VanillaPresetCount; p++)
            {
                var e = presets[p].equipment;
```

Old:
```csharp
            MaxIndex = p3Pet;
```
New:
```csharp
            if (p3Pet > MaxIndex) MaxIndex = p3Pet;
```
(`MaxIndex` must only grow: the handler fires once per world and Five Loadouts may already
have registered higher indices from the previous world.)

### 1d. Add the registration API (anywhere inside `SlotLayout`, e.g. after `PrivateSlot`)

```csharp
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
```

## 2. `Scripts/Systems/LoadoutLayoutSystem.cs`

Old:
```csharp
                var presets = EntityManager.GetBuffer<EquipmentPresetsBuffer>(entity);
                if (presets.Length < SlotLayout.PresetCount) continue;
```
New:
```csharp
                var presets = EntityManager.GetBuffer<EquipmentPresetsBuffer>(entity);
                if (presets.Length < SlotLayout.VanillaPresetCount) continue;
                int presetCount = System.Math.Min(SlotLayout.PresetCount, presets.Length);
```

Old:
```csharp
                for (int p = 0; p < SlotLayout.PresetCount; p++)
                {
                    var e = presets[p].equipment;
```
New:
```csharp
                for (int p = 0; p < presetCount; p++)
                {
                    var e = presets[p].equipment;
```

## 3. No change needed

- `HandlerSync.TryGetContext` already uses `SlotLayout.PresetCount - 1`; it now reads the
  property, so the clamp becomes 0..4 automatically.
- `LoadoutPresetSyncSystem` reads the active preset from `ActiveEquipmentPresetCD` and calls
  `SlotLayout.EffectiveSlot(preset, ...)`, which indexes the (now larger) table. Values 3 and 4
  are valid once registered; before registration `_private[3, k]` is 0, but the system only
  runs after `SlotLayout.Ready`, and registration happens in the same prefab pass.
- The Harmony patches and `UIPatches` are preset-agnostic.

## 4. Optional

- Bump `LoadoutSharingMod.Version` to `"2.1.0"` and mention in `README.md`: "Works with Five
  Loadouts: loadouts 4 and 5 fall back to loadout 1 the same way."
- After both are installed, look for these Player.log lines in order:
  `[LoadoutSharing] Player layout: ...`, `[LoadoutSharing] Registered preset 4 ...`,
  `[LoadoutSharing] Registered preset 5 ...`, `[FiveLoadouts] Player layout: ... privateExtras=True`.
