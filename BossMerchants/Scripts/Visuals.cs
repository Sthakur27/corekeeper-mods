using System.Collections.Generic;
using HarmonyLib;
using Pug.Sprite;
using UnityEngine;

namespace BossMerchants
{
    /// <summary>
    /// How our merchants and idols look. Their prefabs point at the VANILLA graphics (a merchant / idol of the
    /// same kind, see the builder), so animations, sounds, interaction and pooling are the game's own. When such a
    /// graphic spawns for one of our object IDs, a <see cref="SpriteSwapper"/> is attached that, after the
    /// Animator has run each frame, replaces every vanilla sprite with the same rect cut from our sheet
    /// (merchant sheets keep the vanilla layout exactly, enforced by make_merchant_art.py). On despawn the
    /// swapper puts the vanilla sprites back and switches itself off, so a pooled graphic that is reused by a
    /// vanilla merchant looks vanilla again (AGENTS.md: never leave changed pooled graphics behind).
    ///
    /// Bundle textures, by name:
    ///  - merchants: bossMerchant_&lt;Key&gt;__&lt;vanilla texture name&gt; (one per vanilla sheet replaced)
    ///  - idols:     bossMerchant_&lt;Key&gt;Idol_placed (every sprite on the idol graphic becomes this)
    /// </summary>
    public static class Visuals
    {
        public const string Prefix = "bossMerchant_";
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        private static readonly Dictionary<string, SpriteAssetSkin> Skins = new Dictionary<string, SpriteAssetSkin>();

        public static void OnAssetLoaded(Object obj)
        {
            if (obj is Texture2D tex && tex.name.StartsWith(Prefix)) Textures[tex.name] = tex;
            else if (obj is SpriteAssetSkin skin && skin.name.StartsWith(Prefix)) Skins[skin.name] = skin;
        }

        /// <summary>
        /// SpriteAssetSkin for merchants whose vanilla graphics use SpriteObject + SpriteAsset (the Void Merchant
        /// base): bossMerchant_&lt;Key&gt;_skin, targeting the vanilla SpriteAsset.
        /// </summary>
        public static SpriteAssetSkin SkinFor(Catalog.Merchant m) => Skins.TryGetValue(Prefix + m.Key + "_skin", out var s) ? s : null;

        public static int TextureCount => Textures.Count;

        // vanilla sprite -> our sprite (null = keep vanilla), per theme
        private static readonly Dictionary<(string, Sprite), Sprite> Cache = new Dictionary<(string, Sprite), Sprite>();

        /// <summary>Our replacement for <paramref name="original"/> on a <paramref name="key"/> object, or null.</summary>
        public static Sprite Replacement(Catalog.Merchant m, bool idol, Sprite original)
        {
            if (original == null) return null;
            string key = idol ? m.Key + "Idol" : m.Key;
            if (Cache.TryGetValue((key, original), out var cached)) return cached;
            Sprite made = null;
            var src = original.texture;
            if (idol)
            {
                if (Textures.TryGetValue(Prefix + key + "_placed", out var tex))
                    made = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(original.pivot.x / original.rect.width, original.pivot.y / original.rect.height),
                        original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            else if (src != null && Textures.TryGetValue(Prefix + key + "__" + src.name, out var sheet)
                     && sheet.width == src.width && sheet.height == src.height)
            {
                var r = original.rect;
                made = Sprite.Create(sheet, r, new Vector2(original.pivot.x / r.width, original.pivot.y / r.height),
                    original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            if (made != null) made.name = original.name + "_" + key;
            Cache[(key, original)] = made;
            return made;
        }

        internal static void Attach(EntityMonoBehaviour emb)
        {
            if (emb == null || (Textures.Count == 0 && Skins.Count == 0)) return;
            var id = emb.objectData.objectID;
            if (id == ObjectID.None) return;
            Catalog.Resolve();
            bool idol = false;
            var m = Catalog.ByMerchantId(id);
            if (m == null) { m = Catalog.ByIdolId(id); idol = m != null; }
            if (m == null) return;
            var swapper = emb.GetComponent<SpriteSwapper>();
            if (swapper == null) swapper = emb.gameObject.AddComponent<SpriteSwapper>();
            swapper.Begin(m, idol);
        }

        internal static void Detach(EntityMonoBehaviour emb)
        {
            if (emb == null) return;
            var swapper = emb.GetComponent<SpriteSwapper>();
            if (swapper != null) swapper.End();
        }
    }

    /// <summary>Per-graphic sprite replacement; enabled only while the graphic shows one of our objects.</summary>
    [DefaultExecutionOrder(30000)] // after the game's own LateUpdate work
    public sealed class SpriteSwapper : MonoBehaviour
    {
        private Catalog.Merchant _merchant;
        private bool _idol;
        private SpriteRenderer[] _renderers;
        private SpriteObject[] _spriteObjects;
        private readonly Dictionary<Sprite, Sprite> _back = new Dictionary<Sprite, Sprite>();
        private readonly List<(SpriteObject sprite, DataBlockRef<SpriteAssetSkin> skin)> _skinned =
            new List<(SpriteObject, DataBlockRef<SpriteAssetSkin>)>();

        public void Begin(Catalog.Merchant merchant, bool idol)
        {
            _merchant = merchant;
            _idol = idol;
            if (_renderers == null) _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (_spriteObjects == null) _spriteObjects = GetComponentsInChildren<SpriteObject>(true);
            ApplySkin();
            enabled = _renderers.Length > 0;
            Swap();
        }

        /// <summary>SpriteObject graphics: skin only the sprites whose asset the skin targets (a mismatch logs a hitch).</summary>
        private void ApplySkin()
        {
            _skinned.Clear();
            var skin = _idol ? null : Visuals.SkinFor(_merchant);
            if (skin == null) return;
            foreach (var s in _spriteObjects)
            {
                if (s == null || !s.assetAddress.Equals(skin.targetAssetAddress)) continue;
                _skinned.Add((s, s.skinRef));
                s.skinRef = skin.address;
                s.ApplyVisualChange();
            }
        }

        public void End()
        {
            if (_renderers != null)
                foreach (var r in _renderers)
                    if (r != null && r.sprite != null && _back.TryGetValue(r.sprite, out var original)) r.sprite = original;
            foreach (var (s, old) in _skinned)
            {
                if (s == null) continue;
                s.skinRef = old;
                s.ApplyVisualChange();
            }
            _skinned.Clear();
            enabled = false;
        }

        private void LateUpdate() => Swap();

        private void Swap()
        {
            if (_merchant == null || _renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                var s = r.sprite;
                if (s == null || _back.ContainsKey(s)) continue; // already ours
                var ours = Visuals.Replacement(_merchant, _idol, s);
                if (ours == null) continue;
                _back[ours] = s;
                r.sprite = ours;
            }
        }
    }

    [HarmonyPatch(typeof(EntityMonoBehaviour), "OnSpawn")]
    public static class BossMerchantsSpawnVisualPatch
    {
        [HarmonyPostfix]
        public static void Postfix(EntityMonoBehaviour __instance) => Visuals.Attach(__instance);
    }

    [HarmonyPatch(typeof(EntityMonoBehaviour), "OnDespawn")]
    public static class BossMerchantsDespawnVisualPatch
    {
        [HarmonyPrefix]
        public static void Prefix(EntityMonoBehaviour __instance) => Visuals.Detach(__instance);
    }
}
