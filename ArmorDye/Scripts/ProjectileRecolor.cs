using System.Collections.Generic;
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
        private static bool _logged;

        [HarmonyPostfix]
        public static void Postfix(EntityMonoBehaviour __instance)
        {
            try
            {
                if (__instance == null || __instance is PlayerController) return;
                int dye = ShooterDye(__instance);
                var tag = __instance.GetComponent<DyedProjectile>();
                if (dye == 0 && tag == null) return;
                if (tag == null) tag = __instance.gameObject.AddComponent<DyedProjectile>();
                tag.SetDye(dye);
                if (dye != 0 && !_logged)
                {
                    _logged = true;
                    Debug.Log($"[{ArmorDyeMod.Name}] dyeing projectile {__instance.name}: {DyeColor.Describe(dye)}");
                }
            }
            catch (System.Exception e)
            {
                if (!_logged) Debug.LogError($"[{ArmorDyeMod.Name}] projectile dye failed: {e}");
                _logged = true;
            }
        }

        private static int ShooterDye(EntityMonoBehaviour emb)
        {
            Entity e = emb.entity;
            var world = emb.world;
            if (e == Entity.Null || world == null || !world.IsCreated) return 0;
            var em = world.EntityManager;
            if (!em.Exists(e) || !em.HasComponent<ProjectileCD>(e) || !em.HasComponent<OwnerReferenceCD>(e)) return 0;
            Entity owner = em.GetComponentData<OwnerReferenceCD>(e).owner;
            if (owner == Entity.Null) return 0;
            PlayerController pc = null;
            if (Manager.memory != null && Manager.memory.TryGetEntityMono(owner, out PlayerController found)) pc = found;
            if (pc == null) ArmorRecolor.Players.TryGetValue(owner, out pc);
            return ArmorRecolor.HeldDye(pc);
        }
    }

    /// <summary>
    /// Keeps one projectile's visuals dyed. Runs in LateUpdate (after the Animator has picked this frame's
    /// sprite): sprites get dyed copies, SpriteSheetSkins get dyed sheets, particle systems and trails are
    /// tinted (they have no texture of their own to recolor). Dye 0 (or the setting off) restores vanilla.
    /// </summary>
    public class DyedProjectile : MonoBehaviour
    {
        private int _dye;
        private int _appliedTint = -1;
        private SpriteRenderer[] _sprites;
        private SpriteSheetSkin[] _skins;
        private ParticleSystem[] _particles;
        private TrailRenderer[] _trails;
        private readonly Dictionary<ParticleSystem, ParticleSystem.MinMaxGradient> _particleColors = new Dictionary<ParticleSystem, ParticleSystem.MinMaxGradient>();
        private readonly Dictionary<TrailRenderer, Gradient> _trailColors = new Dictionary<TrailRenderer, Gradient>();

        public void SetDye(int dye)
        {
            _dye = dye;
            if (_sprites == null)
            {
                _sprites = GetComponentsInChildren<SpriteRenderer>(true);
                _skins = GetComponentsInChildren<SpriteSheetSkin>(true);
                _particles = GetComponentsInChildren<ParticleSystem>(true);
                _trails = GetComponentsInChildren<TrailRenderer>(true);
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
            }
            foreach (var skin in _skins) ArmorRecolor.ApplyLayer(skin, dye);
            if (_appliedTint != dye) Tint(dye);
            if (dye == 0 && _appliedTint == 0) enabled = false; // vanilla again; SetDye re-enables on reuse
        }

        private void Tint(int dye)
        {
            _appliedTint = dye;
            foreach (var ps in _particles)
            {
                if (ps == null) continue;
                var main = ps.main;
                if (!_particleColors.TryGetValue(ps, out var original)) _particleColors[ps] = original = main.startColor;
                main.startColor = dye == 0 ? original : TintGradient(original, dye);
            }
            foreach (var tr in _trails)
            {
                if (tr == null) continue;
                if (!_trailColors.TryGetValue(tr, out var original)) _trailColors[tr] = original = tr.colorGradient;
                tr.colorGradient = dye == 0 ? original : Tint(original, dye);
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
