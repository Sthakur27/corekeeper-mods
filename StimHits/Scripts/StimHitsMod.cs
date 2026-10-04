using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StimHits
{
    /// <summary>
    /// Replaces vanilla hit sounds with a metallic "ding" in two cases: you damage something
    /// (melee, bows, guns, staffs) and you take damage. Each case has its own sound, volume and
    /// pitch, and "Off" leaves the vanilla sounds untouched. Mechanism in <see cref="Hits"/>.
    /// </summary>
    public sealed class StimHitsMod : IMod
    {
        public const string Name = "StimHits";
        public const string Version = "1.1.0";

        public const string SettingsHint = "Metallic dings replace the take-damage sound when you hit something and when you get hit. Off = vanilla. Custom = your own hit/hurt .mp3/.ogg/.wav in the StimHits sound folder (see README).";

        public static readonly string[] Sounds =
            { "Off", "Stim", "Clang", "Small Clang", "Ding", "Anvil", "Bell", "Shield", "Custom" };

        internal static Setting<string> HitSound;
        internal static Setting<float> HitVolume;
        internal static Setting<float> HitPitch;
        internal static Setting<int> HitRange;
        internal static Setting<bool> HitObjects;
        internal static Setting<string> HurtSound;
        internal static Setting<float> HurtVolume;
        internal static Setting<float> HurtPitch;

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
            SoundBank.Init(info);
            if (info != null && info.Metadata.name == "SidsOverhaul") return; // the overhaul builds the page
            var page = SettingsPages.Create(this, "Stim Hits").Hint(SettingsHint);
            RegisterSettings(page);
            page.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="page"/>.</summary>
        public static void RegisterSettings(SettingsPage page)
        {
            page
                .Choice(out HitSound, "Hit sound", Sounds, "Stim")
                .Slider(out HitVolume, "Hit volume", 0.1f, 1f, 0.8f, 0.1f)
                .Slider(out HitPitch, "Hit pitch", 0.5f, 2f, 1f, 0.05f)
                .Stepper(out HitRange, "Hit range (tiles)", 2, 30, 16)
                .Toggle(out HitObjects, "Ding on objects too", false)
                .Choice(out HurtSound, "Hurt sound", Sounds, "Stim")
                .Slider(out HurtVolume, "Hurt volume", 0.1f, 1f, 0.9f, 0.1f)
                .Slider(out HurtPitch, "Hurt pitch", 0.5f, 2f, 1f, 0.05f);

            // Pick up a newly dropped custom file without restarting.
            HitSound.OnChanged += v => { if (v == "Custom") SoundBank.ReloadCustom(); };
            HurtSound.OnChanged += v => { if (v == "Custom") SoundBank.ReloadCustom(); };
            Debug.Log($"[{Name}] Loaded. Hit: {HitSound.Value}, hurt: {HurtSound.Value}");
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() => SoundBank.Update();
    }
}
