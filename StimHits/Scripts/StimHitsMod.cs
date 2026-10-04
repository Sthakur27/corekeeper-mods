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
        public const string Version = "1.2.0";

        public const string SettingsHint = "Metallic dings replace the hit sounds when you hit something and when you get hit, plus a coin sound when something you hit dies. Off = vanilla. Auto = your own hit/hurt/kill .mp3 from the StimHits folder if present (see README), else the built-in sound.";

        public static readonly string[] Sounds =
        {
            "Off", "Auto", "Ting", "Clang", "Coin",
            "Game Clang", "Game Small Clang", "Game Ding", "Game Anvil", "Game Bell", "Game Shield",
        };

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
            page
                .Choice(out HitSlot.Sound, "Hit sound", Sounds, "Auto")
                .Slider(out HitSlot.Volume, "Hit volume", 0.1f, 1f, 0.8f, 0.1f)
                .Slider(out HitSlot.Pitch, "Hit pitch", 0.5f, 2f, 1f, 0.05f)
                .Choice(out KillSlot.Sound, "Kill sound", Sounds, "Auto")
                .Slider(out KillSlot.Volume, "Kill volume", 0.1f, 1f, 0.9f, 0.1f)
                .Slider(out KillSlot.Pitch, "Kill pitch", 0.5f, 2f, 1f, 0.05f)
                .Choice(out HurtSlot.Sound, "Hurt sound", Sounds, "Auto")
                .Slider(out HurtSlot.Volume, "Hurt volume", 0.1f, 1f, 0.9f, 0.1f)
                .Slider(out HurtSlot.Pitch, "Hurt pitch", 0.5f, 2f, 1f, 0.05f)
                .Stepper(out HitRange, "Hit range (tiles)", 2, 30, 16)
                .Toggle(out HitObjects, "Ding on objects too", false);

            // Pick up a newly dropped local file without restarting.
            foreach (var slot in new[] { HitSlot, HurtSlot, KillSlot })
                slot.Sound.OnChanged += v => { if (v == "Auto") SoundBank.ReloadLocal(); };
            Debug.Log($"[{Name}] Loaded. Hit: {HitSound.Value}, kill: {KillSound.Value}, hurt: {HurtSound.Value}");
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() => SoundBank.Update();
    }
}
