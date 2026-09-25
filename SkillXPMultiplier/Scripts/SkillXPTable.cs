using System;
using System.Globalization;

namespace SkillXPMultiplier
{
    /// <summary>
    /// The per-skill multiplier table. Settings store a token like "10x"; the ECS system reads
    /// the parsed float from <see cref="Get"/> every frame, so keep that path allocation-free.
    /// </summary>
    public static class SkillXPTable
    {
        /// <summary>Matches the game's SkillID enum order (0..11). NUM_SKILLS is excluded.</summary>
        public static readonly string[] SkillNames =
        {
            "Mining", "Running", "Melee", "Vitality", "Crafting", "Range",
            "Gardening", "Fishing", "Cooking", "Magic", "Summoning", "Explosives"
        };

        public const int SkillCount = 12;

        /// <summary>Choices offered in the Mod Settings menu, in cycle order. 0x freezes a skill.</summary>
        public static readonly string[] Ladder =
        {
            "0x", "0.25x", "0.5x", "0.75x", "1x", "1.5x", "2x", "3x", "4x", "5x", "6x", "8x",
            "10x", "12x", "15x", "20x", "25x", "30x", "40x", "50x", "75x", "100x"
        };

        public const string DefaultToken = "1x";

        private static readonly float[] _values = new float[SkillCount];

        static SkillXPTable()
        {
            for (int i = 0; i < SkillCount; i++) _values[i] = Parse(DefaultToken);
        }

        /// <summary>Current multiplier for a skill index; 1 for anything out of range.</summary>
        public static float Get(int skillIndex)
        {
            return skillIndex >= 0 && skillIndex < SkillCount ? _values[skillIndex] : 1f;
        }

        public static void Set(int skillIndex, float value)
        {
            if (skillIndex >= 0 && skillIndex < SkillCount) _values[skillIndex] = value;
        }

        /// <summary>"10x" → 10, "0.5x" → 0.5. Unparseable input falls back to 1.</summary>
        public static float Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1f;
            string s = token.Trim().TrimEnd('x', 'X');
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v >= 0f ? v : 1f;
        }

        /// <summary>The ladder token whose value is closest to the requested multiplier.</summary>
        public static string Snap(float wanted)
        {
            string best = Ladder[0];
            float bestDist = float.MaxValue;
            foreach (string token in Ladder)
            {
                float d = Math.Abs(Parse(token) - wanted);
                if (d < bestDist) { bestDist = d; best = token; }
            }
            return best;
        }

        /// <summary>Skill index for a name or alias ("range"/"ranged", "summon", "mine"...); -1 if unknown.</summary>
        public static int FindSkill(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            string n = name.Trim().ToLowerInvariant();
            switch (n)
            {
                case "ranged": n = "range"; break;
                case "explosive": n = "explosives"; break;
                case "minion": case "minions": case "summon": n = "summoning"; break;
                case "run": n = "running"; break;
                case "mine": n = "mining"; break;
                case "cook": n = "cooking"; break;
                case "fish": n = "fishing"; break;
                case "garden": n = "gardening"; break;
                case "craft": n = "crafting"; break;
                case "hp": case "health": n = "vitality"; break;
            }
            for (int i = 0; i < SkillCount; i++)
                if (SkillNames[i].ToLowerInvariant() == n) return i;
            return -1;
        }
    }
}
