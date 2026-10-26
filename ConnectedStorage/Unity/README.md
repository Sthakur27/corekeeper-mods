# Connected Storage assets

Run `python ConnectedStorage/Unity/build_bundle.py --art` from the repository root. Uses Unity 6000.0.59f2 and the official Mod SDK at `C:\Users\Sid\CoreKeeperModSDK`; `--sdk` overrides the path. Windows and Linux bundles are generated together. `--art` regenerates the original procedural pixel art; no game art is included in the shipped sprites.

The assets contain only vanilla MonoBehaviours, allowing the runtime scripts to handle interactions through Harmony. Object/save keys are `ConnectedStorage_Terminal` and `ConnectedStorage_<Tier>Chest` (stable internal keys) plus `ConnectedStorage_<Tier>Gem`. Mod GUID: `6d6f71a04b8e4a49ba1782ac229bbd51`.

The terminal costs 10 Copper Bars and 20 Wood; the copper unit and each upgrade gem cost 30 bars of their tier. Higher-tier units have no direct crafting recipes. Recipe ingredients are authored on the object; runtime code must add the crafting options to the appropriate stations.

| Tier | Slots |
|---|---:|
| Copper | 20 |
| Tin | 40 |
| Iron | 60 |
| Scarlet | 80 |
| Octarine | 100 |
| Galaxite | 120 |
| Solarite | 140 |
| Pandorium | 160 |

Chests have vanilla InventoryAuthoring with ten columns, zero extra slots, and auto-transfer disabled. Every chest includes DontDestroyOnZeroHealthAuthoring from initial conversion: the server removal system must evacuate it and remove protection after queued transfers finish, allowing vanilla to drop remaining contents. Do not remove that authoring component or add DontDropContainedAuthoring. The terminal has no item inventory; it presents the connected chests through the runtime UI.

Art preview: `Art/preview.png`. Text localization currently uses English fallback for all languages.

After building, run `python ConnectedStorage/Unity/verify_bundle.py` (requires UnityPy) to inspect actual bundle contents, including recipe costs, inventory capacity, removal protection, and absence of custom MonoBehaviours.

The Storage Workbench costs 20 Wood, uses vanilla CraftingBuilding/CraftingAuthoring, and authors all nine crafting recipes. Runtime code inserts its recipe into the empty Wooden Workbench Base slot and retires older tier-bench recipes.

## Runtime art pipeline

`make_art.py` runs `make_unit_art.py` and `make_device_art.py` and creates review sheets. All pixels are authored by these original generators. Native sprite references were inspected in ignored build output for material shading, proportions and directional structure only. The shipped art preserves the existing16-pixel widths, unit/gem16x16 and terminal16x20 sizes, stable file names and object IDs.

Run `python ConnectedStorage/Unity/validate_art.py` before bundling. It checks all102 directional/emissive files for dimensions, hard pixel alpha, glow-mask alignment, and distinct placed-object views. `verify_bundle.py` compares the visible pixels of all102 packed textures to the source images on black and white backgrounds, ignoring Unity transparent-RGB padding. It also retains the prefab/crafting/capacity checks.

The 0.4.3 art pass replaces the trophy-like unit silhouette with a solid top/front housing and inset crystal. Terminal art is a sloped cabinet console; source sizes and names remain stable.

## Art review gate

Use separate creator and critic roles for future visual changes. The critic must inspect actual native sprite references beside candidates at1x and nearest-neighbor enlargement, record concrete accept/revise decisions by asset family, and re-review changed pixels after artists address the findings. Criteria: top/front depth, material clusters, readable silhouettes, coherent directions, tier distinction and original composition. Preserve rejected candidates in ignored build output; only package reviewed candidates. Do not keep adding arbitrary detail to accepted families. Passing this gate is an aesthetic judgment, not proof of in-game appearance or a substitute for user feedback. The0.4.4 pass recorded three reviews; units and terminal were revised after rejection. Native references remain outside shipped art.
