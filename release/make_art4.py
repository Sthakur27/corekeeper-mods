"""Listing image for Mod Options (1280x720). python make_art4.py"""
from PIL import Image, ImageDraw
from make_art import W, H, font, cave_bg, text_shadow

OUT = r"C:\Users\Sid\CoreKeeperMods\release"


def modoptions():
    img = cave_bg(seed=61, top=(14, 22, 40), bottom=(26, 40, 66)).convert("RGBA")
    d = ImageDraw.Draw(img)
    text_shadow(d, (W // 2, 34), "MOD OPTIONS", font(92), (240, 248, 255))
    text_shadow(d, (W // 2, 140), "A settings menu that stays tidy: pick a mod, see only its options.", font(26), (190, 210, 235))

    border, panel = (120, 200, 230), (8, 12, 22)
    # left: the mod list
    lx, ly, lw, lh = 90, 210, 440, 440
    d.rectangle([lx, ly, lx + lw, ly + lh], fill=panel, outline=border, width=4)
    text_shadow(d, (lx + lw // 2, ly + 18), "Mod Options", font(30), (255, 255, 255))
    mods = ["Auto Replant", "Faster Mushrooms", "Hard Mode Tuning", "Potion Seller", "Quick Buff", "Skill XP"]
    for i, m in enumerate(mods):
        y = ly + 80 + i * 56
        col = (120, 220, 255) if i == 2 else (175, 180, 190)
        text_shadow(d, (lx + lw // 2, y), m, font(28), col)
    # arrow between panels
    d.polygon([(560, 420), (600, 400), (600, 440)], fill=(120, 200, 230))
    d.rectangle([540, 412, 562, 428], fill=(120, 200, 230))
    d.polygon([(610, 400), (650, 420), (610, 440)], fill=(120, 200, 230))
    d.rectangle([600, 412, 612, 428], fill=(120, 200, 230))

    # right: one page with values
    rx, ry, rw, rh = 680, 210, 520, 440
    d.rectangle([rx, ry, rx + rw, ry + rh], fill=panel, outline=border, width=4)
    text_shadow(d, (rx + rw // 2, ry + 18), "Hard Mode Tuning", font(30), (255, 255, 255))
    rows = [("Enemy damage", "1.25x"), ("Enemy health", "1.5x"), ("Move speed", "1.2x"), ("Boss damage", "1x")]
    for i, (k, v) in enumerate(rows):
        y = ry + 90 + i * 62
        sel = i == 2
        d.text((rx + 250, y), k, font=font(26), fill=(165, 200, 220) if sel else (175, 180, 190), anchor="ra")
        d.text((rx + 290, y), f"<  {v}  >", font=font(26), fill=(60, 150, 255) if sel else (175, 180, 190))
    text_shadow(d, (rx + rw // 2, ry + rh - 70), "Reset to defaults", font(26), (200, 205, 215))
    text_shadow(d, (W // 2, 672), "Works next to Mod Settings Menu. Keyboard, mouse and controller.", font(22), (160, 180, 205))
    img.convert("RGB").save(OUT + r"\modoptions_logo.png")
    print("modoptions_logo.png")


if __name__ == "__main__":
    modoptions()
