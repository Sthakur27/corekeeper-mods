using System.Linq;
using ModOptions;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FasterMachines
{
    /// <summary>
    /// Per-machine speed multipliers: furnace, smelter kiln, glass smelter, fury forge, cooking pot, table saw,
    /// egg incubator, drills and every other timed machine.
    /// See <see cref="Systems.CraftTimerSpeedSystem"/> (timed machines) and <see cref="Systems.DrillSpeedSystem"/>.
    /// </summary>
    public sealed class FasterMachinesMod : IMod
    {
        public const string Name = "FasterMachines";
        public const string Version = "1.0.1";

        public const string SettingsHint = "How fast each machine works. 1x = vanilla, Instant = one item per tick. "
            + "Applies instantly (an item already in progress speeds up from its next item). In multiplayer the host's values count.";

        // Label, machine, ladder. The labels are also the saved config keys.
        private static readonly (string label, Machine machine, bool drill)[] Rows =
        {
            ("Furnace", Machine.Furnace, false),
            ("Smelter Kiln", Machine.SmelterKiln, false),
            ("Glass Smelter", Machine.GlassSmelter, false),
            ("Fury Forge", Machine.FuryForge, false),
            ("Cooking Pot", Machine.CookingPot, false),
            ("Table Saw", Machine.TableSaw, false),
            ("Egg Incubator (hatching)", Machine.EggIncubator, false),
            ("Drills", Machine.Drill, true),
            ("Other timed machines", Machine.Other, false),
        };

        public void EarlyInit()
        {
            Debug.Log($"[{Name}] v{Version}");
        }

        public void Init()
        {
            if (InOverhaul(this)) return; // the overhaul builds one page per feature
            var section = SettingsPages.Create(this, "Faster Machines").Hint(SettingsHint);
            RegisterSettings(section);
            section.Build();
        }

        /// <summary>Adds this feature's options to <paramref name="section"/>.</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            foreach (var row in Rows)
            {
                var machine = row.machine;
                section.Choice(out Setting<string> s, row.label, row.drill ? MachineSpeeds.DrillLadder : MachineSpeeds.Ladder, MachineSpeeds.DefaultToken);
                MachineSpeeds.Set(machine, MachineSpeeds.Parse(s.Value));
                s.OnChanged += v =>
                {
                    MachineSpeeds.Set(machine, MachineSpeeds.Parse(v));
                    Debug.Log($"[{Name}] {machine}: {MachineSpeeds.Describe(MachineSpeeds.Get(machine))}");
                };
            }
            Debug.Log($"[{Name}] " + string.Join(", ", Rows.Select(r => $"{r.machine} {MachineSpeeds.Describe(MachineSpeeds.Get(r.machine))}")));
        }

        /// <summary>True when this feature is running inside Sid's Overhaul (one combined mod).</summary>
        internal static bool InOverhaul(IMod mod)
        {
            var info = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(mod));
            return info != null && info.Metadata.name == "SidsOverhaul";
        }

        public void Shutdown() { }
        public void ModObjectLoaded(Object obj) { }
        public void Update() { }
    }
}
