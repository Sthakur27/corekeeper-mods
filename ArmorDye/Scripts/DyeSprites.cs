using UnityEngine;

namespace ArmorDye
{
    /// <summary>Original 16x16 pixel art for the dye bucket, drawn from string grids (16 px per unit like item icons).</summary>
    public static class DyeSprites
    {
        // O outline, B body, L light body, H handle, P paint (tinted), D paint drip (tinted)
        private static readonly string[] BucketRows =
        {
            "................",
            "....HHHHHHHH....",
            "...H........H...",
            "..H..........H..",
            "..OOOOOOOOOOOO..",
            ".OPPPPPPPPPPPPO.",
            ".OLPPPPPPPPPPBO.",
            "..OLBBBDBBBBBO..",
            "..OLBBBDBBBBBO..",
            "..OLBBBBBBBBBO..",
            "...OLBBBBBBBO...",
            "...OLBBBBBBBO...",
            "...OLBBBBBBBO...",
            "....OOOOOOOO....",
            "................",
            "................",
        };

        private static Sprite _bucket, _paint, _rainbow, _cross;

        public static Sprite Bucket() => _bucket ?? (_bucket = Make(BucketRows, c =>
            c == 'O' ? new Color32(40, 30, 30, 255) :
            c == 'B' ? new Color32(150, 150, 160, 255) :
            c == 'L' ? new Color32(200, 200, 210, 255) :
            c == 'H' ? new Color32(90, 80, 80, 255) : new Color32(0, 0, 0, 0)));

        /// <summary>Paint surface + drip in white, tinted with the picked color.</summary>
        public static Sprite Paint() => _paint ?? (_paint = Make(BucketRows, c =>
            c == 'P' || c == 'D' ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0)));

        /// <summary>Rainbow paint for the hue-shift entries.</summary>
        public static Sprite Rainbow() => _rainbow ?? (_rainbow = Make(BucketRows, (c, x, y) =>
            c == 'P' || c == 'D' ? (Color32)Color.HSVToRGB(x / 16f, 0.8f, 1f) : new Color32(0, 0, 0, 0)));

        /// <summary>A small X for "remove dye".</summary>
        public static Sprite Cross()
        {
            if (_cross != null) return _cross;
            var rows = new string[16];
            for (int y = 0; y < 16; y++)
            {
                var chars = new char[16];
                for (int x = 0; x < 16; x++)
                {
                    bool on = y >= 4 && y <= 12 && x >= 4 && x <= 12 && (x - 4 == y - 4 || x - 4 == 12 - y);
                    chars[x] = on ? 'X' : '.';
                }
                rows[y] = new string(chars);
            }
            return _cross = Make(rows, c => c == 'X' ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0));
        }

        private static Sprite _pixel, _swFill, _swFrame, _swRainbow, _swRing;

        /// <summary>A 1x1 white sprite, 1 unit wide (scale it for panels).</summary>
        public static Sprite Pixel() => _pixel ?? (_pixel = Square(1, 1f, (x, y) => new Color32(255, 255, 255, 255)));

        /// <summary>Swatch interior (12x12 white, tinted with the color).</summary>
        public static Sprite SwatchFill() => _swFill ?? (_swFill = Square(14, 16f, (x, y) =>
            x >= 1 && x <= 12 && y >= 1 && y <= 12 ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0)));

        /// <summary>Dark 1px outline around the swatch.</summary>
        public static Sprite SwatchFrame() => _swFrame ?? (_swFrame = Square(14, 16f, (x, y) =>
            x == 0 || x == 13 || y == 0 || y == 13 ? new Color32(60, 40, 25, 255) : new Color32(0, 0, 0, 0)));

        /// <summary>Rainbow swatch for the hue-shift entries.</summary>
        public static Sprite SwatchRainbow() => _swRainbow ?? (_swRainbow = Square(14, 16f, (x, y) =>
            x >= 1 && x <= 12 && y >= 1 && y <= 12 ? (Color32)Color.HSVToRGB((x - 1) / 12f, 0.75f, 1f) : new Color32(0, 0, 0, 0)));

        /// <summary>White ring around the picked swatch.</summary>
        public static Sprite SwatchRing() => _swRing ?? (_swRing = Square(16, 16f, (x, y) =>
            x == 0 || x == 15 || y == 0 || y == 15 ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0)));

        private static Sprite Square(int size, float ppu, System.Func<int, int, Color32> color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = color(x, y);
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
        }

        private static Sprite Make(string[] rows, System.Func<char, Color32> color) => Make(rows, (c, x, y) => color(c));

        private static Sprite Make(string[] rows, System.Func<char, int, int, Color32> color)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = color(rows[h - 1 - y][x], x, y);
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
