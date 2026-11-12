# Perspective: Core Keeper is 2.5D, seen from above at an angle (Sid's #1 note)

Sid's verdict on every design so far: the concepts are fine, but they are drawn head-on, flat front view.
Core Keeper's sprites, items and NPCs alike, are drawn as if the camera looks DOWN at about a 3/4 angle.
Redraw your merchant and idol in that view. Study the vanilla art for exactly this before touching pixels:
Slime Oil (top of the lid is a bright oval), Pile of Chum (crate seen mostly from above, contents visible
inside), the trophies and Ra-Akar urn (round bases drawn as ovals, top surfaces catch light), and the four
vanilla merchants (top of head/hood/hat visible, hat brims as ovals, the top of the backpack visible).

## Rules

- **Top planes are visible and the brightest.** Any surface facing up (lids, rims, hat tops, shoulders,
  plinth tops, the top of a box) is shown as a foreshortened oval or parallelogram and gets the lightest ramp.
  Vertical faces facing the viewer are mid-tone, and darken toward the bottom.
- **Round things are ovals, not circles or rectangles.** Cylinders (jars, pots, bells, bases) have an oval
  top and a curved oval bottom edge. Boxes show their top face as well as the front.
- **Ground contact.** Objects and characters sit on the floor with a darker underside and a small contact
  shadow. The base or plinth reads as a slab seen from above.
- **Characters are seen slightly from above.** You see the top of the head or hat and the back/top of any
  pack. The face sits lower on the head than in a front view. The body is squat and foreshortened, feet are
  small, and limbs or held items tilt so you also see their tops. Hat brims are wide ovals around the head.
- **Light comes from above (top-left).** Shade like the vanilla sprites: top light, darker undersides,
  occlusion below overhangs. Don't shade it as front-lit with a rim on the side.
- Keep everything else: the rig/cell constraints from designer_brief.md, the style_review_notes.md points
  (no speckle, vanilla ramps, tinted outlines, vanilla scale), and your approved concept, name and stock.

## Self-check (required)

In your preview, put your idle frame next to the vanilla merchants and your idol next to Slime Oil, Pile of
Chum and the Mysterious Idol at 1x and 8x. Ask: "if this sat in the game, would its camera angle match?" If
any top plane is missing or it reads as a front view, keep going.
