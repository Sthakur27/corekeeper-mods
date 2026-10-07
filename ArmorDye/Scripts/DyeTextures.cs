using System.Collections.Generic;
using UnityEngine;

namespace ArmorDye
{
    /// <summary>
    /// Recolored copies of game textures and sprites, cached by source + dye. Game textures are not
    /// CPU-readable, so they are copied through a RenderTexture (Blit + ReadPixels) first.
    /// </summary>
    public static class DyeTextures
    {
        private static readonly Dictionary<long, Texture2D> Sheets = new Dictionary<long, Texture2D>();
        private static readonly Dictionary<long, Sprite> Sprites = new Dictionary<long, Sprite>();
        private static readonly HashSet<Texture> OurSheets = new HashSet<Texture>();
        private static readonly Dictionary<Sprite, Sprite> SpriteOriginal = new Dictionary<Sprite, Sprite>();
        private static readonly HashSet<long> Failed = new HashSet<long>();

        private static long Key(Object o, int dye) => ((long)o.GetInstanceID() << 32) | (uint)dye;

        private static readonly Dictionary<Texture, Texture2D> SheetOriginal = new Dictionary<Texture, Texture2D>();

        public static bool IsOurs(Texture t) => t != null && OurSheets.Contains(t);

        /// <summary>The vanilla texture behind a dyed sheet (itself if it is not one of ours).</summary>
        public static Texture2D OriginalSheet(Texture t) => t != null && SheetOriginal.TryGetValue(t, out var o) ? o : t as Texture2D;

        /// <summary>The vanilla sprite behind <paramref name="s"/> (itself if it is not one of ours).</summary>
        public static Sprite Original(Sprite s) => s != null && SpriteOriginal.TryGetValue(s, out var o) ? o : s;

        /// <summary>A dyed copy of a whole armor sheet (the SpriteSheetSkin texture).</summary>
        public static Texture2D Sheet(Texture2D src, int dye)
        {
            long key = Key(src, dye);
            if (Sheets.TryGetValue(key, out var tex) && tex != null) return tex;
            if (Sheets.Count > 96) ClearSheets();
            tex = Read(src, new RectInt(0, 0, src.width, src.height));
            Dye(tex, dye);
            tex.name = src.name + "_dye_" + dye.ToString("x8");
            Sheets[key] = tex;
            OurSheets.Add(tex);
            SheetOriginal[tex] = src;
            Debug.Log($"[{ArmorDyeMod.Name}] recolored {src.name} ({src.width}x{src.height} {src.format} sRGB={src.isDataSRGB}) -> {DyeColor.Describe(dye)}");
            return tex;
        }

        /// <summary>A dyed copy of an item icon sprite, or null if the sprite can't be copied.</summary>
        public static Sprite Icon(Sprite src, int dye)
        {
            long key = Key(src, dye);
            if (Sprites.TryGetValue(key, out var s) && s != null) return s;
            if (Failed.Contains(key)) return null;
            try
            {
                Rect r = src.packed ? src.textureRect : src.rect;
                var rect = new RectInt(Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.y), Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height));
                if (rect.width <= 0 || rect.height <= 0) throw new System.Exception("empty rect");
                var tex = Read(src.texture, rect);
                Dye(tex, dye);
                tex.name = src.name + "_dye_" + dye.ToString("x8");
                // Keep the pivot where the original has it, relative to the copied (possibly trimmed) region.
                Vector2 offset = src.packed ? src.textureRectOffset : Vector2.zero;
                Vector2 pivot = new Vector2((src.pivot.x - offset.x) / rect.width, (src.pivot.y - offset.y) / rect.height);
                s = Sprite.Create(tex, new Rect(0, 0, rect.width, rect.height), pivot, src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                s.name = tex.name;
                Sprites[key] = s;
                SpriteOriginal[s] = src;
                return s;
            }
            catch (System.Exception e)
            {
                Failed.Add(key);
                Debug.LogWarning($"[{ArmorDyeMod.Name}] could not recolor icon {src.name}: {e.Message}");
                return null;
            }
        }

        private static Texture2D Read(Texture2D src, RectInt rect)
        {
            var rw = src.isDataSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, rw);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var tex = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false, !src.isDataSRGB);
            tex.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            tex.filterMode = src.filterMode;
            tex.wrapMode = src.wrapMode;
            return tex;
        }

        private static void Dye(Texture2D tex, int dye)
        {
            var px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i++) px[i] = DyeColor.Apply(px[i], dye);
            tex.SetPixels32(px);
            tex.Apply(false, false);
        }

        private static void ClearSheets()
        {
            foreach (var t in Sheets.Values) if (t != null) Object.Destroy(t);
            Sheets.Clear();
            OurSheets.Clear();
            SheetOriginal.Clear();
        }
    }
}
