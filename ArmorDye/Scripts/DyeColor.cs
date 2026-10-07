using System.Globalization;
using UnityEngine;

namespace ArmorDye
{
    /// <summary>
    /// The dye value stored in an armor item's MealsEatenCD (see SPEC.md). 0 = no dye; the top byte is
    /// the mode: 1 = colorize to the RGB in the low 24 bits, 2 = hue shift by the degrees in the low 9 bits.
    /// </summary>
    public static class DyeColor
    {
        public const int ModeColorize = 1;
        public const int ModeHueShift = 2;

        public static int Mode(int dye) => (dye >> 24) & 0xFF;

        /// <summary>
        /// Items that can carry a dye: armor (has an armor look) and single, non-stacking items with durability
        /// (weapons, tools, fishing rods). Stackables are excluded: per-item data would stop them stacking.
        /// </summary>
        public static bool CanDye(ObjectDataCD o)
        {
            if (o.objectID == ObjectID.None) return false;
            if (PugDatabase.HasComponent<EquipmentSkinCD>(o)) return true;
            var info = PugDatabase.GetObjectInfo(o.objectID, o.variation);
            return info != null && !info.isStackable && PugDatabase.HasComponent<DurabilityCD>(o);
        }

        public static int Colorize(Color32 c) => (ModeColorize << 24) | (c.r << 16) | (c.g << 8) | c.b;

        public static int HueShift(int degrees) => (ModeHueShift << 24) | (((degrees % 360) + 360) % 360);

        /// <summary>Parses "red", "#3080ff", "3080ff", "shift 120" or "off" into a dye value.</summary>
        public static bool TryParse(string[] args, int start, out int dye)
        {
            dye = 0;
            if (start >= args.Length) return false;
            string a = args[start].ToLowerInvariant();
            if (a == "off" || a == "none" || a == "clear" || a == "reset") return true;
            if (a == "shift" || a == "hue")
            {
                if (start + 1 < args.Length && int.TryParse(args[start + 1], out int deg))
                {
                    dye = HueShift(deg);
                    return true;
                }
                return false;
            }
            if (TryNamed(a, out Color32 named))
            {
                dye = Colorize(named);
                return true;
            }
            string hex = a.TrimStart('#');
            if (hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
            {
                dye = (ModeColorize << 24) | rgb;
                return true;
            }
            return false;
        }

        private static bool TryNamed(string name, out Color32 c)
        {
            switch (name)
            {
                case "red": c = new Color32(220, 40, 40, 255); return true;
                case "orange": c = new Color32(240, 130, 30, 255); return true;
                case "gold": case "yellow": c = new Color32(240, 200, 50, 255); return true;
                case "green": c = new Color32(60, 190, 70, 255); return true;
                case "teal": case "cyan": c = new Color32(40, 190, 200, 255); return true;
                case "blue": c = new Color32(50, 100, 230, 255); return true;
                case "purple": c = new Color32(150, 70, 220, 255); return true;
                case "pink": c = new Color32(240, 110, 190, 255); return true;
                case "white": c = new Color32(235, 235, 235, 255); return true;
                case "black": c = new Color32(60, 60, 70, 255); return true;
                default: c = default; return false;
            }
        }

        public static string Describe(int dye)
        {
            switch (Mode(dye))
            {
                case 0: return "no dye";
                case ModeColorize: return "#" + (dye & 0xFFFFFF).ToString("x6");
                case ModeHueShift: return $"hue shift {dye & 0x1FF}";
                default: return $"unknown dye 0x{dye:x8}";
            }
        }

        /// <summary>Recolors one pixel. Fully transparent pixels and unknown modes are left alone.</summary>
        public static Color32 Apply(Color32 p, int dye)
        {
            if (p.a == 0) return p;
            int mode = Mode(dye);
            if (mode == ModeColorize)
            {
                // Photoshop-style colorize: take hue/saturation from the dye, keep the pixel's lightness so
                // the sprite's shading and outlines survive.
                float l = (0.299f * p.r + 0.587f * p.g + 0.114f * p.b) / 255f;
                float tr = ((dye >> 16) & 0xFF) / 255f, tg = ((dye >> 8) & 0xFF) / 255f, tb = (dye & 0xFF) / 255f;
                return new Color32(Lighten(tr, l), Lighten(tg, l), Lighten(tb, l), p.a);
            }
            if (mode == ModeHueShift)
            {
                Color.RGBToHSV(new Color32(p.r, p.g, p.b, 255), out float h, out float s, out float v);
                h = (h + (dye & 0x1FF) / 360f) % 1f;
                Color32 o = Color.HSVToRGB(h, s, v);
                o.a = p.a;
                return o;
            }
            return p;
        }

        private static byte Lighten(float target, float l)
        {
            float v = l < 0.5f ? target * 2f * l : target + (1f - target) * (2f * l - 1f);
            return (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
        }
    }
}
