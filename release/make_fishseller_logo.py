"""Fish Seller listing image (1280x720): a merchant buy window full of fish, scrolled partway down,
with a scrollbar, a Starlight Nautilus slot and a Shiny Larva Meat slot.
python make_fishseller_logo.py  ->  fishseller_logo.png"""
import colorsys
import random
from PIL import ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow

FISH = [
    "......OO......",
    "....OOLLO...OO",
    "..OOLLMMMO.OMO",
    ".OLLMMMMMMOMMO",
    "OLEMMMMMMMMMMO",
    "OLMMMMMMDDMMMO",
    ".ODDMMMMDDOMMO",
    "..OODDDDDO.ODO",
    "....OOOOO...OO",
]

NAUTILUS = [
    "....OOOOOO....",
    "..OOHHLLLLOO..",
    ".OHLLMMMMLLLO.",
    "OHLMMOOOOMMLLO",
    "OLMMOLLLLOMMLO",
    "OLMOLMMOOLOMLO",
    "OLMOLMOELOOMLO",
    "OLMMOOLLOMMMLO",
    ".OLMMMMMMMMDO.",
    "..OODDDDDDOO..",
    "....OOOOOO....",
]

MEAT = [
    "....OOOOO.....",
    "..OOLHLLLOO...",
    ".OLHLLMMMLLO..",
    "OLLMMMMMMMMLO.",
    "OLMMMDMMMDMMLO",
    "OLMMMMMMMMMMDO",
    ".OMMDMMMDMMDO.",
    "..OODDDDDDOO..",
    "....OOOOOO....",
]


def shades(rgb):
    h, l, s = colorsys.rgb_to_hls(*[c / 255 for c in rgb])

    def at(lv, sv=s):
        r, g, b = colorsys.hls_to_rgb(h, max(0, min(1, lv)), max(0, min(1, sv)))
        return (int(r * 255), int(g * 255), int(b * 255))
    return {"O": (24, 18, 26), "L": at(l + 0.2), "M": rgb, "D": at(l - 0.17), "H": at(l + 0.33, s * 0.6), "E": (250, 250, 250)}


def slot(d, x, y, s, hi=False):
    d.rectangle([x, y, x + s, y + s], fill=(24, 16, 12))
    d.rectangle([x + 4, y + 4, x + s - 4, y + s - 4], fill=(70, 48, 34) if not hi else (110, 80, 44),
                outline=(150, 110, 70) if not hi else (250, 214, 120), width=3)


def main():
    img = cave_bg(seed=11, top=(14, 22, 34), bottom=(22, 44, 58)).convert("RGBA")
    d = ImageDraw.Draw(img)
    rnd = random.Random(5)

    cols, rows, s, gap = 8, 3, 96, 10
    gw = cols * s + (cols - 1) * gap
    gx, gy = (W - gw) // 2 + 30, 205
    gh = rows * s + (rows - 1) * gap
    # window: wood frame
    d.rectangle([gx - 34, gy - 34, gx + gw + 34, gy + gh + 34], fill=(30, 20, 14))
    d.rectangle([gx - 26, gy - 26, gx + gw + 26, gy + gh + 26], fill=(112, 78, 48), outline=(170, 124, 78), width=4)

    hues = [0.0, 0.05, 0.1, 0.14, 0.33, 0.45, 0.52, 0.6, 0.7, 0.8, 0.9, 0.95]
    k = 0
    for r in range(rows):
        for c in range(cols):
            x, y = gx + c * (s + gap), gy + r * (s + gap)
            special = (r, c) == (1, 5)
            meat = (r, c) == (2, 2)
            slot(d, x, y, s, hi=special)
            if special:
                spr = sprite(NAUTILUS, shades((236, 186, 64)), 5)
            elif meat:
                spr = sprite(MEAT, shades((246, 200, 90)), 5)
            else:
                hcol = hues[k % len(hues)]
                k += 1
                rr, gg, bb = colorsys.hls_to_rgb(hcol, 0.55, 0.65 + rnd.random() * 0.25)
                spr = sprite(FISH, shades((int(rr * 255), int(gg * 255), int(bb * 255))), 5)
            lift = 12 if special else 0
            img.alpha_composite(spr, (x + (s - spr.width) // 2, y + (s - spr.height) // 2 - lift))

    # scrollbar left of the window: thumb 3/7 of the track, scrolled down a bit
    tx = gx - 80
    d.rectangle([tx, gy - 10, tx + 18, gy + gh + 10], fill=(10, 10, 14))
    track = gh + 20
    th = int(track * 3 / 7)
    ty = gy - 10 + int((track - th) * 0.4)
    d.rectangle([tx + 2, ty, tx + 16, ty + th], fill=(242, 216, 150))
    # wheel arrows
    for dy, up in ((gy - 52, True), (gy + gh + 30, False)):
        pts = [(tx + 9, dy), (tx - 6, dy + 18), (tx + 24, dy + 18)] if up else [(tx - 6, dy), (tx + 24, dy), (tx + 9, dy + 18)]
        d.polygon(pts, fill=(242, 216, 150))

    # price tag on the nautilus slot
    nx, ny = gx + 5 * (s + gap), gy + 1 * (s + gap)
    tag = font(24)
    d.rectangle([nx + 14, ny + s - 32, nx + s - 14, ny + s - 7], fill=(24, 16, 12))
    text_shadow(d, (nx + s // 2, ny + s - 31), "500", tag, (255, 220, 110))

    text_shadow(d, (W // 2, 48), "FISH SELLER", font(110), (255, 236, 200))
    text_shadow(d, (W // 2, 600), "Every fish for sale, plus Shiny Larva Meat", font(38), (214, 230, 240))
    text_shadow(d, (W // 2, 650), "Scroll any merchant's wares with the mouse wheel", font(30), (170, 200, 220))
    img.convert("RGB").save("fishseller_logo.png")
    print("wrote fishseller_logo.png")


if __name__ == "__main__":
    main()
