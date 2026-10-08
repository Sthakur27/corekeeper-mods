using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace ArmorDye
{
    /// <summary>
    /// Projectiles take the dye of the weapon that fired them. Every visual object goes through
    /// EntityMonoBehaviour.Spawn (also when a pooled one is reused); for projectiles (ProjectileCD) whose
    /// owner (OwnerReferenceCD, replicated) is a player, we read that player's held item and attach a
    /// <see cref="DyedProjectile"/> that keeps the visuals dyed. "The weapon that fired it" = the item in hand
    /// when the projectile appears, which is right for normal shots.
    /// </summary>
    [HarmonyPatch(typeof(EntityMonoBehaviour), nameof(EntityMonoBehaviour.Spawn))]
    public static class ProjectileSpawnPatch
    {
        private static readonly HashSet<string> Logged = new HashSet<string>();

        [HarmonyPostfix]
        public static void Postfix(EntityMonoBehaviour __instance)
        {
            try
            {
                if (__instance == null || __instance is PlayerController) return;
                if (!TryGetShooter(__instance, out PlayerController pc, out bool isProjectile)) return;
                int dye = isProjectile ? ArmorRecolor.HeldDye(pc) : 0;
                var tag = __instance.GetComponent<DyedProjectile>();
                Describe(__instance, isProjectile, dye);
                if (dye == 0 && tag == null) return;
                if (tag == null) tag = __instance.gameObject.AddComponent<DyedProjectile>();
                tag.SetDye(dye);
            }
            catch (System.Exception e)
            {
                if (Logged.Add("error:" + e.Message)) Debug.LogError($"[{ArmorDyeMod.Name}] projectile dye failed: {e}");
            }
        }

        /// <summary>True for objects owned by a player; <paramref name="isProjectile"/> says whether it is a shot.</summary>
        private static bool TryGetShooter(EntityMonoBehaviour emb, out PlayerController pc, out bool isProjectile)
        {
            pc = null;
            isProjectile = false;
            Entity e = emb.entity;
            var world = emb.world;
            if (e == Entity.Null || world == null || !world.IsCreated) return false;
            var em = world.EntityManager;
            if (!em.Exists(e) || !em.HasComponent<OwnerReferenceCD>(e)) return false;
            Entity owner = em.GetComponentData<OwnerReferenceCD>(e).owner;
            if (owner == Entity.Null) return false;
            if (Manager.memory != null && Manager.memory.TryGetEntityMono(owner, out PlayerController found)) pc = found;
            if (pc == null) ArmorRecolor.Players.TryGetValue(owner, out pc);
            if (pc == null) return false;
            isProjectile = em.HasComponent<ProjectileCD>(e);
            return true;
        }

        /// <summary>Once per object type: what it is made of, so undyed visuals can be traced.</summary>
        private static void Describe(EntityMonoBehaviour emb, bool isProjectile, int dye)
        {
            if (!Logged.Add(emb.name)) return;
            var sb = new StringBuilder();
            foreach (var sr in emb.GetComponentsInChildren<SpriteRenderer>(true))
                sb.Append($"\n  sprite {sr.gameObject.name}: {(sr.sprite != null ? sr.sprite.name : "-")}, emissive={(sr.sharedMaterial != null && sr.sharedMaterial.HasProperty("_EmissiveTex") && sr.sharedMaterial.GetTexture("_EmissiveTex") != null)}");
            foreach (var ps in emb.GetComponentsInChildren<ParticleSystem>(true))
                sb.Append($"\n  particles {ps.gameObject.name}: start {ps.main.startColor.mode}, overLifetime={ps.colorOverLifetime.enabled}");
            foreach (var c in emb.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform || c is SpriteRenderer || c is ParticleSystem || c is ParticleSystemRenderer) continue;
                sb.Append($"\n  component {c}");
            }
            Debug.Log($"[{ArmorDyeMod.Name}] player-owned object {emb.name}: projectile={isProjectile}, dye {DyeColor.Describe(dye)}{sb}");
        }
    }

    /// <summary>
    /// Keeps one projectile's visuals dyed. Runs in LateUpdate (after the Animator has picked this frame's
    /// sprite): sprites get dyed copies (and their glow textures dyed sheets), SpriteSheetSkins get dyed
    /// sheets, particle systems, trails, lines and lights are tinted (no texture of their own to recolor).
    /// Dye 0 (or the setting off) restores vanilla.
    /// </summary>
    public class DyedProjectile : MonoBehaviour
    {
        private static readonly int EmissiveTex = Shader.PropertyToID("_EmissiveTex");

        private int _dye;
        private int _appliedTint = -1;
        private SpriteRenderer[] _sprites;
        private SpriteSheetSkin[] _skins;
        private ParticleSystem[] _particles;
        private TrailRenderer[] _trails;
        private LineRenderer[] _lines;
        private Light[] _lights;
        private readonly Dictionary<ParticleSystem, ParticleSystem.MinMaxGradient> _startColors = new Dictionary<ParticleSystem, ParticleSystem.MinMaxGradient>();
        private readonly Dictionary<ParticleSystem, ParticleSystem.MinMaxGradient> _lifetimeColors = new Dictionary<ParticleSystem, ParticleSystem.MinMaxGradient>();
        private readonly Dictionary<Object, Gradient> _gradients = new Dictionary<Object, Gradient>();
        private readonly Dictionary<Light, Color> _lightColors = new Dictionary<Light, Color>();

        public void SetDye(int dye)
        {
            _dye = dye;
            if (_sprites == null)
            {
                _sprites = GetComponentsInChildren<SpriteRenderer>(true);
                _skins = GetComponentsInChildren<SpriteSheetSkin>(true);
                _particles = GetComponentsInChildren<ParticleSystem>(true);
                _trails = GetComponentsInChildren<TrailRenderer>(true);
                _lines = GetComponentsInChildren<LineRenderer>(true);
                _lights = GetComponentsInChildren<Light>(true);
            }
            enabled = true;
            LateUpdate();
        }

        private void LateUpdate()
        {
            int dye = DyeSettings.Projectiles ? _dye : 0;
            foreach (var sr in _sprites)
            {
                if (sr == null || sr.sprite == null) continue;
                Sprite original = DyeTextures.Original(sr.sprite);
                Sprite want = dye == 0 ? original : (DyeTextures.Icon(original, dye) ?? original);
                if (want != sr.sprite) sr.sprite = want;
                DyeEmissive(sr, dye);
            }
            foreach (var skin in _skins) ArmorRecolor.ApplyLayer(skin, dye);
            if (_appliedTint != dye) Tint(dye);
            if (dye == 0 && _appliedTint == 0) enabled = false; // vanilla again; SetDye re-enables on reuse
        }

        /// <summary>
        /// A sprite's glow texture is sampled with the sprite's own UVs, so it gets a dyed copy of the whole
        /// texture (only touched when the shared material actually has one, so plain sprites keep sharing).
        /// </summary>
        private static void DyeEmissive(SpriteRenderer sr, int dye)
        {
            var shared = sr.sharedMaterial;
            if (shared == null || !shared.HasProperty(EmissiveTex)) return;
            Texture current = shared.GetTexture(EmissiveTex);
            if (current == null) return;
            Texture2D original = DyeTextures.OriginalSheet(current);
            if (original == null) return;
            Texture want = dye == 0 ? original : DyeTextures.Sheet(original, dye);
            if (want != null && want != current) sr.material.SetTexture(EmissiveTex, want);
        }

        private void Tint(int dye)
        {
            _appliedTint = dye;
            foreach (var ps in _particles)
            {
                if (ps == null) continue;
                var main = ps.main;
                if (!_startColors.TryGetValue(ps, out var start)) _startColors[ps] = start = main.startColor;
                main.startColor = dye == 0 ? start : TintGradient(start, dye);
                var col = ps.colorOverLifetime;
                if (col.enabled)
                {
                    if (!_lifetimeColors.TryGetValue(ps, out var life)) _lifetimeColors[ps] = life = col.color;
                    col.color = dye == 0 ? life : TintGradient(life, dye);
                }
            }
            foreach (var tr in _trails)
            {
                if (tr == null) continue;
                if (!_gradients.TryGetValue(tr, out var g)) _gradients[tr] = g = tr.colorGradient;
                tr.colorGradient = dye == 0 ? g : Tint(g, dye);
            }
            foreach (var lr in _lines)
            {
                if (lr == null) continue;
                if (!_gradients.TryGetValue(lr, out var g)) _gradients[lr] = g = lr.colorGradient;
                lr.colorGradient = dye == 0 ? g : Tint(g, dye);
            }
            foreach (var light in _lights)
            {
                if (light == null) continue;
                if (!_lightColors.TryGetValue(light, out var c)) _lightColors[light] = c = light.color;
                light.color = dye == 0 ? c : Tint(c, dye);
            }
        }

        private static Color Tint(Color c, int dye)
        {
            Color32 o = DyeColor.Apply((Color32)c, dye);
            return new Color(o.r / 255f, o.g / 255f, o.b / 255f, c.a);
        }

        private static Gradient Tint(Gradient g, int dye)
        {
            if (g == null) return null;
            var keys = g.colorKeys;
            for (int i = 0; i < keys.Length; i++) keys[i].color = Tint(keys[i].color, dye);
            var result = new Gradient { mode = g.mode };
            result.SetKeys(keys, g.alphaKeys);
            return result;
        }

        private static ParticleSystem.MinMaxGradient TintGradient(ParticleSystem.MinMaxGradient m, int dye)
        {
            switch (m.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(Tint(m.color, dye));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(Tint(m.colorMin, dye), Tint(m.colorMax, dye));
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(Tint(m.gradient, dye));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(Tint(m.gradientMin, dye), Tint(m.gradientMax, dye));
                default:
                    return m;
            }
        }
    }
}
