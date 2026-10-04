"""1280x720 listing image for Stim Hits: a sword striking a slime with a bright impact burst,
sound-wave arcs and "TING!", a gold coin popping off for the kill sound. python make_stimhits_logo.py"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow, P

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "stimhits_logo.png")

PS = dict(P, **{
    'm': (110, 200, 120),  # slime
    'M': (60, 140, 80),    # slime shade
    'l': (180, 240, 180),  # slime highlight
    'e': (20, 30, 24),     # eyes
})

SWORD = [
    "..............kk",
    ".............kwk",
    "............kwsk",
    "...........kwsk.",
    "..........kwsk..",
    ".........kwsk...",
    "........kwsk....",
    ".......kwsk.....",
    "..kk..kwsk......",
    "..kgkkwsk.......",
    "...kgksk........",
    "....kgk.........",
    "...kbkgk........",
    "..kbk.kgk.......",
    ".kbk...kk.......",
    ".kk.............",
]

SLIME = [
    ".....kkkkkk.....",
    "...kkmmmmmmkk...",
    "..kmmllmmmmmmk..",
    ".kmmllmmmmmmmmk.",
    ".kmmmmmmmmmmmmk.",
    "kmmmekmmmmekmmmk",
    "kmmmeemmmmeemmmk",
    "kmmmmmmmmmmmmmMk",
    "kMmmmmmmmmmmmMMk",
    ".kMMMMMMMMMMMMk.",
    "..kkkkkkkkkkkk..",
]

COIN = [
    "..kkkk..",
    ".kggggk.",
    "kgyggGGk",
    "kgyggGGk",
    "kgggGGGk",
    "kggGGGGk",
    ".kGGGGk.",
    "..kkkk..",
]


def burst(d, cx, cy, r_in, r_out, n, col, width):
    for i in range(n):
        a = i * 2 * math.pi / n + 0.2
        d.line([(cx + r_in * math.cos(a), cy + r_in * math.sin(a)),
                (cx + r_out * math.cos(a), cy + r_out * math.sin(a))], fill=col, width=width)


def main():
    img = cave_bg(seed=77, top=(18, 16, 34), bottom=(36, 26, 58)).convert("RGBA")
    d = ImageDraw.Draw(img)
    rnd = random.Random(3)

    # ground
    d.rectangle([0, 600, W, H], fill=(46, 36, 52))
    d.rectangle([0, 600, W, 608], fill=(78, 62, 86))

    # slime and the impact point on it
    slime = sprite(SLIME, PS, 16)
    sx, sy = 560, 600 - slime.height
    img.alpha_composite(slime, (sx, sy))
    cx, cy = sx + 30, sy + 40

    # impact: white flash, star burst, metallic sparks
    d.ellipse([cx - 60, cy - 60, cx + 60, cy + 60], fill=(255, 250, 220, 120))
    d.ellipse([cx - 30, cy - 30, cx + 30, cy + 30], fill=(255, 255, 255, 230))
    burst(d, cx, cy, 70, 150, 12, (255, 236, 150), 8)
    burst(d, cx, cy, 160, 200, 12, (200, 214, 236), 5)
    for _ in range(26):
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(90, 230)
        x, y = cx + r * math.cos(a), cy + r * math.sin(a)
        s = rnd.choice([6, 8, 10])
        d.rectangle([x, y, x + s, y + s], fill=rnd.choice([(255, 240, 160), (220, 230, 250), (255, 200, 90)]))

    # sword swinging in from the left, tip at the impact
    sword = sprite(SWORD, PS, 13)
    img.alpha_composite(sword, (cx - sword.width + 30, cy - 30))

    # sound waves to the right of the hit
    for i, rad in enumerate((200, 250, 300)):
        d.arc([cx - rad, cy - rad, cx + rad, cy + rad], -40, 40, fill=(140, 220, 255, 230 - i * 60), width=10)

    # coin popping off (kill sound)
    coin = sprite(COIN, PS, 12)
    img.alpha_composite(coin, (1110, 420))
    d.line([(1040, 500), (1100, 470)], fill=(255, 220, 120), width=6)

    f_big, f_mid, f_small = font(110), font(44), font(34)
    text_shadow(d, (1100, 300), "TING!", font(84), (255, 236, 150), anchor="mm")
    text_shadow(d, (W // 2, 40), "STIM HITS", f_big, (240, 240, 250))
    text_shadow(d, (W // 2, 160), "Metallic hit, kill & hurt sounds", f_mid, (150, 220, 255))
    text_shadow(d, (W // 2, 655), "Use your own sounds  |  Mod Options settings", f_small, (220, 200, 160))

    img.convert("RGB").save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
