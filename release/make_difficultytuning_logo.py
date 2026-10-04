"""1280x720 mod.io listing image for Difficulty Tuning: two settings panels side by side, HARD (a horned
skull enemy, red/orange) and NORMAL (a slime, green/blue), each with damage/health/speed/boss sliders.
python make_difficultytuning_logo.py"""
import os, sys, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from make_art import W, H, font, sprite, cave_bg, text_shadow, P

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "difficultytuning_logo.png")

PD = dict(P, **{
    'b': (236, 228, 208),  # bone
    'B': (170, 156, 136),  # bone shade
    'h': (120, 60, 50),    # horn
    'H': (78, 38, 34),     # horn shade
    'e': (255, 110, 50),   # glowing eye
    'E': (255, 210, 120),  # eye core
    'l': (120, 220, 130),  # slime
    'L': (60, 160, 100),   # slime shade
    'm': (190, 250, 190),  # slime highlight
    'd': (24, 60, 40),     # slime eye
    'w': (250, 250, 240),
})

# horned skull enemy
SKULL = [
    "kk..................kk",
    "khk................khk",
    "khhk..............khhk",
    ".khhk....kkkk....khhk.",
    ".kHhhk.kkbbbbkk.khhHk.",
    "..kHhhkbbbbbbbbkhhHk..",
    "...kHkbbwbbbbbbbkHk...",
    "....kbbwbbbbbbbbbk....",
    "...kbbbbbbbbbbbbbbk...",
    "...kbbkkkbbbbkkkbbk...",
    "...kbkeEekbbkeEekbk...",
    "...kbkeeekbbkeeekbk...",
    "...kbbkkkbbbbkkkbbk...",
    "...kBbbbbbkkbbbbbBk...",
    "....kBbbbbkkbbbbBk....",
    ".....kBbbbbbbbbBk.....",
    "......kbkbkbkbkbk.....",
    "......kBbBbBbBbBk.....",
    ".......kkkkkkkkk......",
]

# slime
SLIME = [
    "........kkkkkk........",
    "......kkmmllllkk......",
    ".....kmmwmlllllLk.....",
    "....kmwmlllllllllk....",
    "...kmmllllllllllllk...",
    "...kmllllllllllllLk...",
    "..kmllkkllllllkklLLk..",
    "..klllkwdlllllkwdlLk..",
    "..klllkddlllllkddlLk..",
    ".klllllllllllllllllLk.",
    ".kllllllkllllkllllLLk.",
    ".klllllllkkkkllllLLLk.",
    "kLlllllllllllllllLLLLk",
    "kLLlllllllllllllLLLLLk",
    ".kLLLLLLLLLLLLLLLLLLk.",
    "..kkkkkkkkkkkkkkkkkk..",
]


def glow(img, cx, cy, rad, col, peak=110):
    g = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(g)
    for r in range(rad, 0, -6):
        a = int(peak * (1 - r / rad) ** 1.4)
        gd.ellipse([cx - r, cy - r * 0.8, cx + r, cy + r * 0.8], fill=col + (a,))
    return Image.alpha_composite(img, g)


def panel(img, x0, x1, y0, y1, label, accent, fill, spr, rows):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([x0 + 6, y0 + 7, x1 + 6, y1 + 7], radius=18, fill=(6, 4, 10))
    d.rounded_rectangle([x0, y0, x1, y1], radius=18, fill=fill, outline=accent, width=5)
    # header: sprite + label
    s = sprite(spr, PD, 6)
    img.paste(s, (x0 + 26, y0 + 18), s)
    d = ImageDraw.Draw(img)
    text_shadow(d, (x0 + 190, y0 + 34), label, font(56), accent, anchor="lt")
    text_shadow(d, (x0 + 192, y0 + 98), "WORLDS", font(22), (220, 214, 226), anchor="lt")
    d.line([(x0 + 22, y0 + 140), (x1 - 22, y0 + 140)], fill=accent, width=2)
    # slider rows
    ry = y0 + 166
    tx0, tx1 = x0 + 160, x1 - 120
    for name, val in rows:
        d.text((x0 + 26, ry), name, font=font(24), fill=(232, 226, 236), anchor="lm")
        d.rounded_rectangle([tx0, ry - 7, tx1, ry + 7], radius=7, fill=(20, 16, 26), outline=(90, 84, 104), width=2)
        frac = max(0.0, min(1.0, (val - 0.5) / 1.5))   # 0.5x .. 2x
        kx = int(tx0 + (tx1 - tx0) * frac)
        d.rounded_rectangle([tx0 + 2, ry - 5, max(tx0 + 8, kx), ry + 5], radius=5, fill=accent)
        # vanilla tick at 1x
        vx = int(tx0 + (tx1 - tx0) * (0.5 / 1.5))
        d.rectangle([vx - 1, ry - 13, vx + 1, ry + 13], fill=(200, 196, 210))
        d.rectangle([kx - 9, ry - 15, kx + 9, ry + 15], fill=(28, 22, 30))
        d.rectangle([kx - 6, ry - 12, kx + 6, ry + 12], fill=(245, 240, 250))
        txt = ("%g" % val) + "x"
        hot = abs(val - 1.0) > 1e-6
        text_shadow(d, (x1 - 26, ry), txt, font(28), accent if hot else (226, 222, 232), anchor="rm")
        ry += 62
    return img


def main():
    img = cave_bg(seed=88, top=(18, 14, 28), bottom=(40, 28, 46)).convert("RGBA")
    img = glow(img, 330, 380, 330, (255, 90, 40), peak=70)
    img = glow(img, 950, 380, 330, (60, 200, 170), peak=60)

    # a few embers on the hard side, droplets on the normal side
    d = ImageDraw.Draw(img)
    rnd = random.Random(5)
    for _ in range(24):
        x, y = rnd.randint(30, 600), rnd.randint(160, 600)
        s = rnd.choice([4, 6, 8])
        d.rectangle([x, y, x + s, y + s], fill=(255, rnd.randint(100, 170), 60, 160))
    for _ in range(24):
        x, y = rnd.randint(680, 1250), rnd.randint(160, 600)
        s = rnd.choice([4, 6, 8])
        d.rectangle([x, y, x + s, y + s], fill=(110, 220, rnd.randint(170, 230), 150))

    py0, py1 = 168, 592
    hard_rows = [("Damage", 1.5), ("Health", 1.5), ("Speed", 1.0), ("Bosses", 1.0)]
    norm_rows = [("Damage", 1.0), ("Health", 1.0), ("Speed", 1.0), ("Bosses", 1.0)]
    img = panel(img, 70, 610, py0, py1, "HARD", (255, 130, 70), (46, 22, 26, 235), SKULL, hard_rows)
    img = panel(img, 670, 1210, py0, py1, "NORMAL", (110, 220, 170), (18, 38, 40, 235), SLIME, norm_rows)

    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 22), "DIFFICULTY TUNING", font(84), (240, 232, 250))
    text_shadow(d, (W // 2, 112), "Tune enemies separately for hard mode and normal worlds.", font(28), (214, 206, 226))

    # caption box
    bx0, bx1 = 250, W - 250
    d.rounded_rectangle([bx0, 614, bx1, 698], radius=16, fill=(14, 10, 22, 230), outline=(200, 170, 140), width=3)
    text_shadow(d, (W // 2, 626), "Regular enemies and bosses, per world type.", font(25), (255, 220, 190))
    text_shadow(d, (W // 2, 660), "1x = vanilla.", font(25), (190, 236, 210))

    img.convert("RGB").save(OUT, optimize=True)
    print(OUT)


if __name__ == "__main__":
    main()
