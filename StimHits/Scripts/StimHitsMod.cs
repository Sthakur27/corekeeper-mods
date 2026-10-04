using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StimHits
{
    /// <summary>
    /// Replaces vanilla hit sounds with metallic dings when you damage something (melee, bows,
    /// guns, staffs) and when you take damage, and plays a coin sound when a creature you hit dies.
    /// Each slot has its own sound, volume and pitch; "Off" leaves the vanilla sounds untouched. Mechanism in <see cref="Hits"/>.
    /// </summary>
    public sealed class StimHitsMod : IMod
    {
        public const string Name = "StimHits";
        public const string Version = "1.4.1";

        public const string SettingsHint = "Metallic dings replace the hit sounds when you hit something and when you get hit, plus a coin sound when something you hit dies. Off = vanilla. Every sound file in your StimHits/Sounds folder is listed by name (see README); Auto = your hit/hurt/kill files if present, else the built-in sound.";

        /// <summary>The three settings of one sound slot.</summary>
        public sealed class SoundSlot
        {
            public Setting<string> Sound;
            public Setting<float> Volume;
            public Setting<float> Pitch;
        }

        private static readonly SoundSlot HitSlot = new SoundSlot();
        private static readonly SoundSlot HurtSlot = new SoundSlot();
        private static readonly SoundSlot KillSlot = new SoundSlot();
        internal static Setting<int> HitRange;
        internal static Setting<bool> HitObjects;

        internal static Setting<string> HitSound => HitSlot.Sound;
        internal static Setting<string> HurtSound => HurtSlot.Sound;
        internal static Setting<string> KillSound => KillSlot.Sound;

        public static SoundSlot Slot(SoundBank.Kind kind)
            => kind == SoundBank.Kind.Hit ? HitSlot : kind == SoundBank.Kind.Hurt ? HurtSlot : KillSlot;

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
            var sounds = SoundBank.Choices(); // built-ins + every file in the local sounds folder
            page
                .Choice(out HitSlot.Sound, "Hit sound", sounds, "Auto")
                .Slider(out HitSlot.Volume, "Hit volume", 0.1f, 1f, 0.8f, 0.1f)
                .Slider(out HitSlot.Pitch, "Hit pitch", 0.5f, 2f, 1f, 0.05f)
                .Choice(out KillSlot.Sound, "Kill sound", sounds, "Auto")
                .Slider(out KillSlot.Volume, "Kill volume", 0.1f, 1f, 0.9f, 0.1f)
                .Slider(out KillSlot.Pitch, "Kill pitch", 0.5f, 2f, 1f, 0.05f)
                .Choice(out HurtSlot.Sound, "Hurt sound", sounds, "Auto")
                .Slider(out HurtSlot.Volume, "Hurt volume", 0.1f, 1f, 0.9f, 0.1f)
                .Slider(out HurtSlot.Pitch, "Hurt pitch", 0.5f, 2f, 1f, 0.05f)
                .Stepper(out HitRange, "Hit range (tiles)", 2, 30, 16)
                .Toggle(out HitObjects, "Ding on objects too", false);

            // Load the picked file right away so the next hit already uses it.
            foreach (var slot in new[] { HitSlot, HurtSlot, KillSlot })
            {
                SoundBank.Prepare(slot.Sound.Value);
                slot.Sound.OnChanged += SoundBank.Prepare;
            }
            Debug.Log($"[{Name}] Loaded. Hit: {HitSound.Value}, kill: {KillSound.Value}, hurt: {HurtSound.Value}");
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() => SoundBank.Update();
    }
}
