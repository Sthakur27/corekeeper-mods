# More NPCs: approved visual direction — 2026-11-12

**Implementation approved later in this chat:** after reviewing all eight pairs, Sid said “amazing. lets build it”. This authorizes importing these designs into runtime art and building the mod. The review-only restrictions below describe the earlier concept phase, not the subsequent authorized build. Keep source concepts and backups for comparison.

Sid called the Old Hemlock concept “MUCH better” and requested that this style be recorded and applied by sub-agents to every NPC and matching item. This approves the design direction, not replacement of existing runtime assets. His earlier instruction remains: do not replace without confirmation.

## Primary visual reference

`../ArtConcepts/OldHemlock-2026-11-12/concept-v1.png` is the approved style anchor. Its exact built-in image_gen prompt is saved beside it in `prompt.txt`. Inspect the image, not just this description, before designing.

Use extracted native merchant and idol art as the perspective, scale and material reference: `build/bossmerchants/reference`, `build/bossmerchants/idol_reference`, and theme-specific `build/bossmerchants/ref_<key>` under the repository root. `Unity/Art/previews/final_lineup.png` provides a convenient existing-art/native comparison. Its mod drawings are prior designs, not the newly approved direction.

## What to preserve across the set

- Elevated three-quarter view: visible top of hood/hat/head, shoulders, pack and item lids; face sits low beneath the brow. Compact foreshortened bodies and small feet. Avoid eye-level portraits and flat side profiles.
- Strong silhouette and large deliberate pixel clusters. The character reads first, then the profession and one or two signature props. Avoid tiny decorative clutter and random speckle.
- Layered forms: clear occlusion beneath brims, around satchels, between limbs and robes. Upper-left/top-facing planes catch warm highlights; undersides use cool, hue-tinted shadows.
- Restrained material palettes, about 3–4 principal tones per material. Earthy cloth, aged leather/wood and small brighter magical accents. Keep each NPC's own boss/theme palette; do not turn everyone into a purple/green Hemlock.
- Dark colored edges integrated with each material, softened/broken on lit sides. No uniform black contour, broad bloom, dramatic rim light or smooth gradients.
- Readable middle values against cave darkness. Reserve the brightest pixels for small glass glints, eyes or localized magic.
- Items share the same camera and lighting. Round tops read as foreshortened ovals; show thickness and a dark contact edge. Design toward a simple 16×16 icon, with distinctive silhouette at small size.
- Preserve established NPC names, species, personalities, professions and idol identities from `docs/designs/<key>.json`. Create new drawings, not recolors of vanilla sprites.

## Honest limits of the approved concept

The generated Hemlock board is a visual concept, not a validated game sprite. Its pixels have some softness and its mask remains somewhat side-on. Improve these where possible while keeping the approved appeal. Do not claim perfect pixel grids, exact palette counts, animation compatibility, true 1× scale, or in-game validation from a generated concept board.

## Review deliverables and scope

For each of the eight NPC/idol pairs, save a separately named concept board and exact prompt under `ArtConcepts/StylePass-2026-11-12/<key>/`. A consistent charcoal board should show the named NPC prominently, a smaller readable rendition, and its named summoning idol. The small rendition is a readability illustration, not a certified native-scale render. Hemlock's existing approved board may be reused without another redesign.

Use built-in image_gen and the imagegen skill for generated raster artwork. Reference the approved Hemlock image and inspected native/current theme images. Keep generated content clearly labeled as a concept. Review the result for silhouette, overhead angle, thematic identity and consistency with Hemlock.

Do not modify production sheets, generators, manifests, bundles, installed mods or existing uncommitted work. Do not install, publish, launch or terminate the game. Animation sheets, exact native-resolution pixel cleanup and integration follow only after Sid approves replacement.

## Pair assignments

| Key | NPC | Item |
| --- | --- | --- |
| blocks | Grout | Slick Cornerstone |
| decor | Ottoline Tidewick | Omoroth Shell Lamp |
| explosives | Fizzwick, the Molten Bombardier | Igneous Fuse Core |
| potions | Old Hemlock | Ivy Culture Jar |
| eggs | Ankhra the Clutch-Keeper | Brood Egg Idol |
| hydras | Tridra the Haggler | Triune Idol |
| farmanimals | Wren Hayfeather | Skyherd Bell |
| seeds | Tilly the Seedgrub | Brood Seedpot |
