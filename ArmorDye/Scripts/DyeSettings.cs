using ModOptions;

namespace ArmorDye
{
    /// <summary>Armor Dye options (Mod Options page). All are client-side: they only change what you see.</summary>
    public static class DyeSettings
    {
        public const string SettingsHint = "Hover an armor piece, weapon or tool in your inventory and press P (rebind under Controls > Armor Dye) to pick a dye. These options only change what you see.";

        private static Setting<bool> _held, _projectiles, _icons;

        public static bool HeldItems => _held?.Value ?? true;
        public static bool Projectiles => _projectiles?.Value ?? true;
        public static bool Icons => _icons?.Value ?? true;

        /// <summary>Adds the options to <paramref name="section"/> (the standalone page or the overhaul's).</summary>
        public static void RegisterSettings(SettingsPage section)
        {
            section
                .Toggle(out _held, "Dye weapons and tools in hand", true)
                .Toggle(out _projectiles, "Dye projectiles", true)
                .Toggle(out _icons, "Dye item icons", true);
        }
    }
}
