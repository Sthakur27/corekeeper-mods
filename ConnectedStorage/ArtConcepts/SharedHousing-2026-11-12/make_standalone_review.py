"""Compare shipped units with the standalone lid and clamp study."""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from make_fused_review import TIERS, housing, original

HERE = Path(__file__).resolve().parent


def main():
    board = Image.new('RGB', (1120, 385), '#252b33')
    draw = ImageDraw.Draw(board)
    regular = 'C:/Windows/Fonts/segoeui.ttf'
    font = ImageFont.truetype(regular, 16)
    small = ImageFont.truetype(regular, 14)
    title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 24)
    draw.text((24, 16), 'Runic Storage | Individual housings', font=title, fill='#f2dfba')
    draw.text((24, 52), 'Shipped sprites above. Gem-tinted lids and matching clamps below.',
              font=font, fill='#e1e6eb')
    for i, tier in enumerate(TIERS):
        x = 24 + i * 120
        shipped = original(tier)
        candidate = housing(tier, revised=True)
        assert shipped.getchannel('A').tobytes() == candidate.getchannel('A').tobytes()
        for y, sprite in ((114, shipped), (232, candidate)):
            enlarged = sprite.resize((96, 96), Image.Resampling.NEAREST)
            board.paste(enlarged, (x, y), enlarged)
        draw.text((x, 84), tier, font=font, fill='#e1e6eb')
        board.paste(candidate, (x, 345), candidate)
        draw.text((x + 22, 343), '1×', font=small, fill='#bdc9d2')
    board.save(HERE / 'standalone-review.png')


if __name__ == '__main__':
    main()
