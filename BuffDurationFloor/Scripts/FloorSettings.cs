using System.Globalization;

namespace BuffDurationFloor
{
    /// <summary>
    /// The two configured minimum durations: one for ordinary buffs, one for healing/regen
    /// conditions. The settings menu stores tokens like "3:00", "45 s" or "Off"; the ECS system
    /// reads <see cref="BuffFloorSeconds"/> / <see cref="HealingFloorSeconds"/> and re-applies
    /// whenever <see cref="Version"/> changes, so keep everything cheap to read.
    /// A floor of 0 means "Off": that group is left at vanilla.
    /// </summary>
    public static class FloorSettings
    {
        public const string OffToken = "Off";

        /// <summary>Choices offered in Mod Settings, shared by both floors.</summary>
        public static readonly string[] Ladder =
        {
            OffToken, "30 s", "45 s", "1:00", "1:30", "2:00", "3:00", "5:00", "10:00"
        };

        public const int DefaultBuffSeconds = 180;
        public const int DefaultHealingSeconds = 0;

        public static readonly string DefaultBuffToken = Token(DefaultBuffSeconds);
        public static readonly string DefaultHealingToken = Token(DefaultHealingSeconds);

        /// <summary>Floor for non-healing buffs in seconds; 0 = Off.</summary>
        public static float BuffFloorSeconds { get; private set; } = DefaultBuffSeconds;

        /// <summary>Floor for heal-over-time / mana-regen conditions in seconds; 0 = Off.</summary>
        public static float HealingFloorSeconds { get; private set; } = DefaultHealingSeconds;

        /// <summary>Bumped on every effective change; systems compare it to their last-applied value.</summary>
        public static int Version { get; private set; }

        public static void SetBuffFloor(int seconds)
        {
            seconds = Clamp(seconds);
            if (BuffFloorSeconds == seconds) return;
            BuffFloorSeconds = seconds;
            Version++;
        }

        public static void SetHealingFloor(int seconds)
        {
            seconds = Clamp(seconds);
            if (HealingFloorSeconds == seconds) return;
            HealingFloorSeconds = seconds;
            Version++;
        }

        private static int Clamp(int seconds)
        {
            if (seconds < 0) seconds = 0;
            if (seconds > 600) seconds = 600;
            return seconds;
        }

        /// <summary>0 → "Off", 30 → "30 s", 90 → "1:30", 600 → "10:00".</summary>
        public static string Token(int seconds)
        {
            if (seconds <= 0) return OffToken;
            if (seconds < 60) return seconds + " s";
            int m = seconds / 60, s = seconds % 60;
            return m + ":" + s.ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Inverse of <see cref="Token"/>; anything unparseable yields <paramref name="fallback"/>.
        /// Also accepts the pre-1.2 tokens ("3m", "1m 30s") so an old config still maps correctly.
        /// </summary>
        public static int Parse(string token, int fallback)
        {
            if (string.IsNullOrEmpty(token)) return fallback;
            string t = token.Trim();
            if (t.Equals(OffToken, System.StringComparison.OrdinalIgnoreCase)) return 0;

            int colon = t.IndexOf(':');
            if (colon > 0)
            {
                if (int.TryParse(t.Substring(0, colon), NumberStyles.Integer, CultureInfo.InvariantCulture, out int m)
                    && int.TryParse(t.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int s))
                    return m * 60 + s;
                return fallback;
            }

            int total = 0;
            bool any = false;
            string compact = t.Replace(" s", "s").Replace(" m", "m");
            foreach (string part in compact.Split(' '))
            {
                string p = part.Trim().ToLowerInvariant();
                if (p.Length < 2) continue;
                char unit = p[p.Length - 1];
                if (!int.TryParse(p.Substring(0, p.Length - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) continue;
                if (unit == 'm') { total += n * 60; any = true; }
                else if (unit == 's') { total += n; any = true; }
            }
            return any ? total : fallback;
        }
    }
}
