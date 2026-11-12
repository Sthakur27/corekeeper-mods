# Review notes from the first two designs (apply before you report)

The orchestrator reviewed decor and explosives against vanilla. Both concepts passed; both missed the native
style the same way. Check your work against every point:

1. **No noise/dither speckle.** Vanilla uses deliberate clusters (2-6 px shapes) that describe form: highlight
   blob top-left, crack/fold lines, glow pockets. Random single-pixel speckle reads as fake texture. Remove it.
2. **Palette like vanilla.** Sample ramps from the actual game sprites for your materials. Use 4-5 steps per
   material with hue shift: shadows cooler (toward blue/indigo/maroon), highlights warmer. Don't push saturation
   above the vanilla merchants'; compare saturation/value side by side with Fisher_merchant / Slime_merchant.
3. **Form shading, not flat fills.** Rim light from the top-left, core shadow, occlusion where parts overlap,
   darker undersides, metal specular dots. A body that is one flat colour with an outline is a miss.
4. **Outlines.** Dark and hue-tinted, lighter or broken on lit edges, never one uniform dark line all around.
5. **Scale.** Fill the cell about as much as the vanilla merchant on the same rig does, on the same ground line.
   Small characters read as props next to vanilla merchants.
6. **Idols.** They should read at 1x as a clean iconic object beside Mysterious Idol / Slime Oil / Void Idol:
   same cluster style, same saturation, no speckle.

In your preview, put your idle frame and idol beside vanilla at 1x and 6x, and only report once they would
pass as shipped game art.
