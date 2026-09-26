using System.Globalization;

namespace BuffDurationFloor
{
    /// <summary>
    /// The configured minimum buff duration. The settings menu stores a token like "3m" or
    /// "1m 30s"; the ECS system reads <see cref="FloorSeconds"/> and re-applies whenever
    /// <see cref="Version"/> changes, so keep both cheap to read.
    /// </summary>
    public static class FloorSettings
    {
        public const int StepSeconds = 30;
        public const int MinSeconds = 30;
        public const int MaxSeconds = 600;
        public const int DefaultSeconds = 180;

        /// <summary>Choices offered in Mod Settings: 30s, 1m, 1m 30s ... 10m.</summary>
        public static readonly string[] Ladder = BuildLadder();
        public static readonly string DefaultToken = Token(DefaultSeconds);

        /// <summary>Current floor in seconds. Never below <see cref="MinSeconds"/>.</summary>
        public static float FloorSeconds { get; private set; } = DefaultSeconds;

        /// <summary>Bumped on every effective change; systems compare it to their last-applied value.</summary>
        public static int Version { get; private set; }

        /// <summary>
        /// When false (default) health/mana regeneration-over-time conditions keep their vanilla
        /// duration; the floor applies to every other positive timed buff.
        /// </summary>
        public static bool ExtendHealing { get; private set; }

        public static void SetExtendHealing(bool value)
        {
            if (ExtendHealing == value) return;
            ExtendHealing = value;
            Version++;
        }

        public static void Set(int seconds)
        {
            if (seconds < MinSeconds) seconds = MinSeconds;
            if (seconds > MaxSeconds) seconds = MaxSeconds;
            if (FloorSeconds == seconds) return;
            FloorSeconds = seconds;
            Version++;
        }

        /// <summary>180 → "3m", 90 → "1m 30s", 30 → "30s".</summary>
        public static string Token(int seconds)
        {
            int m = seconds / 60, s = seconds % 60;
            if (m == 0) return s + "s";
            if (s == 0) return m + "m";
            return m + "m " + s + "s";
        }

        /// <summary>Inverse of <see cref="Token"/>; anything unparseable yields the default.</summary>
        public static int Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return DefaultSeconds;
            int total = 0;
            bool any = false;
            foreach (string part in token.Split(' '))
            {
                string p = part.Trim().ToLowerInvariant();
                if (p.Length < 2) continue;
                char unit = p[p.Length - 1];
                if (!int.TryParse(p.Substring(0, p.Length - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) continue;
                if (unit == 'm') { total += n * 60; any = true; }
                else if (unit == 's') { total += n; any = true; }
            }
            return any ? total : DefaultSeconds;
        }

        private static string[] BuildLadder()
        {
            int count = (MaxSeconds - MinSeconds) / StepSeconds + 1;
            var ladder = new string[count];
            for (int i = 0; i < count; i++) ladder[i] = Token(MinSeconds + i * StepSeconds);
            return ladder;
        }
    }
}
