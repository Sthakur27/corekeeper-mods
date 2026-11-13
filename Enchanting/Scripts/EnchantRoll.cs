using System.Collections.Generic;

namespace Enchanting
{
    /// <summary>Roll rules and costs (SPEC.md "Roll rules" / "Cost per roll"). Tunables are static for now.</summary>
    public static class EnchantRoll
    {
        /// <summary>Enchant slots on weapons, tools and summoning tomes (2026-11-13: armor and accessories hold 1).</summary>
        public const int MaxEnchants = 3;
        public const int GearEnchants = 1;
        public static int GoldPerStep = 5;
        public static int CoinsPerStep = 250;
        // Chance (percent) that a NEW enchant lands at level I / II / III, with no figurines near the table...
        public static int[] NewLevelWeights = { 60, 30, 10 };
        // ...and with the full set of figurines (MaxFigurines or more). In between it slides linearly.
        public static int[] FullLibraryWeights = { 0, 40, 60 };
        public const int MaxFigurines = 9;

        /// <summary>Level I / II / III weights for <paramref name="figurines"/> placed near the table.</summary>
        public static int[] WeightsFor(int figurines)
        {
            int n = System.Math.Max(0, System.Math.Min(figurines, MaxFigurines));
            var w = new int[NewLevelWeights.Length];
            for (int i = 0; i < w.Length; i++)
                w[i] = NewLevelWeights[i] + (FullLibraryWeights[i] - NewLevelWeights[i]) * n / MaxFigurines;
            return w;
        }

        /// <summary>"I 33% / II 37% / III 30%" for chat.</summary>
        public static string OddsText(int figurines)
        {
            var w = WeightsFor(figurines);
            int total = 0;
            foreach (int x in w) total += x;
            var parts = new List<string>();
            for (int i = 0; i < w.Length; i++) parts.Add($"{Enchants.Roman(i + 1)} {(total > 0 ? w[i] * 100 / total : 0)}%");
            return string.Join(" / ", parts);
        }

        private static readonly System.Random Rng = new System.Random();

        public static void Cost(int enchantsOnItem, out int gold, out int coins)
        {
            int step = 1 + System.Math.Min(enchantsOnItem, MaxEnchants - 1);
            gold = GoldPerStep * step;
            coins = CoinsPerStep * step;
        }

        /// <summary>
        /// How many enchants an item of this kind holds (Sid, 2026-11-13): weapons, tools and tomes 3; armor,
        /// rings, necklaces and offhands 1, since stacked defence was too strong.
        /// </summary>
        public static int MaxFor(EnchantSlot slot) =>
            Enchants.IsHeldSlot(slot) || slot == EnchantSlot.Summon ? MaxEnchants : GearEnchants;

        /// <summary>
        /// Index of the first enchant that works: only the newest <see cref="MaxFor"/> count. Older armor and
        /// accessories may still carry extras from before the limit; those show as inactive until removed.
        /// </summary>
        public static int FirstActive(int count, EnchantSlot slot) => System.Math.Max(0, count - MaxFor(slot));

        /// <summary>Why the table won't roll on this item (full), or null when there is room.</summary>
        public static string FullReason(List<EnchantEntry> current, EnchantSlot slot)
        {
            int max = MaxFor(slot);
            if (current.Count < max) return null;
            string what = max == 1 ? "Armor and accessories hold 1 enchant" : $"This item holds {max} enchants";
            return $"{what}. Remove one at the table to roll again.";
        }

        /// <summary>
        /// Rolls one NEW enchant onto <paramref name="current"/> (modified in place): a random enchant from the
        /// item's pool that it doesn't have yet, at level I/II/III by the figurine odds. There is no levelling up
        /// (Sid, 2026-11-13): to get a better roll, remove the enchant and roll again. Returns a player-facing line,
        /// or null when nothing can be rolled for this item type.
        /// </summary>
        public static string Roll(List<EnchantEntry> current, EnchantSlot slot, ObjectID item = ObjectID.None, int figurines = 0)
        {
            var pool = Enchants.PoolFor(slot, item);
            if (pool.Count == 0) return null;
            string full = FullReason(current, slot);
            if (full != null) return full;

            var fresh = new List<EnchantDef>();
            foreach (var d in pool)
                if (IndexOf(current, d.Id) < 0) fresh.Add(d);
            if (fresh.Count == 0) return "No other enchant can go on this item.";
            var pick = fresh[Rng.Next(fresh.Count)];
            var added = new EnchantEntry { Id = pick.Id, Level = RollNewLevel(pick, WeightsFor(figurines)) };
            current.Add(added); // newest goes on top of the stack (remove pops it, tooltip lists it first)
            return $"Gained {pick.Name} {Enchants.Roman(added.Level)}.";
        }

        private static int RollNewLevel(EnchantDef def, int[] weights)
        {
            int total = 0;
            int max = System.Math.Min(def.MaxLevel, weights.Length);
            for (int i = 0; i < max; i++) total += weights[i];
            if (total <= 0) return max; // e.g. full library on a 2-level enchant with weight only on III
            int r = Rng.Next(total);
            for (int i = 0; i < max; i++)
            {
                if (r < weights[i]) return i + 1;
                r -= weights[i];
            }
            return 1;
        }

        public static int IndexOf(List<EnchantEntry> list, int id)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Id == id) return i;
            return -1;
        }
    }
}
