using System.Collections.Generic;

namespace Enchanting
{
    /// <summary>Roll rules and costs (SPEC.md "Roll rules" / "Cost per roll"). Tunables are static for now.</summary>
    public static class EnchantRoll
    {
        public static int MaxEnchants = 3;
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
        /// Rolls one enchant onto <paramref name="current"/> (modified in place). Returns a player-facing line, or
        /// null when nothing can be rolled for this item type.
        /// </summary>
        public static string Roll(List<EnchantEntry> current, EnchantSlot slot, ObjectID item = ObjectID.None, int figurines = 0)
        {
            var weights = WeightsFor(figurines);
            var pool = Enchants.PoolFor(slot, item);
            if (pool.Count == 0) return null;

            // Upgrade targets: anything in the pool not already at max level.
            var candidates = new List<EnchantDef>();
            foreach (var d in pool)
            {
                int idx = IndexOf(current, d.Id);
                if (idx < 0 || current[idx].Level < d.MaxLevel) candidates.Add(d);
            }

            if (current.Count >= MaxEnchants)
            {
                // Full: replace one random enchant with a different, fresh one.
                int victim = Rng.Next(current.Count);
                var fresh = new List<EnchantDef>();
                foreach (var d in pool)
                    if (IndexOf(current, d.Id) < 0) fresh.Add(d);
                if (fresh.Count == 0) return LevelUpExisting(current, candidates);
                var pick = fresh[Rng.Next(fresh.Count)];
                var old = current[victim];
                current.RemoveAt(victim);
                var fresh1 = new EnchantEntry { Id = pick.Id, Level = RollNewLevel(pick, weights) };
                current.Add(fresh1); // newest goes on top of the stack
                return $"{old.Def?.Name} {Enchants.Roman(old.Level)} was replaced by {pick.Name} {Enchants.Roman(fresh1.Level)}.";
            }

            if (candidates.Count == 0) return "Every enchant on this item is already maxed.";
            var chosen = candidates[Rng.Next(candidates.Count)];
            int at = IndexOf(current, chosen.Id);
            if (at >= 0)
            {
                var e = current[at];
                e.Level++;
                MoveToTop(current, at, e);
                return $"{chosen.Name} rose to {Enchants.Roman(e.Level)}.";
            }
            var added = new EnchantEntry { Id = chosen.Id, Level = RollNewLevel(chosen, weights) };
            current.Add(added);
            return $"Gained {chosen.Name} {Enchants.Roman(added.Level)}.";
        }

        private static string LevelUpExisting(List<EnchantEntry> current, List<EnchantDef> candidates)
        {
            var upgradable = new List<int>();
            for (int i = 0; i < current.Count; i++)
                if (candidates.Exists(d => d.Id == current[i].Id)) upgradable.Add(i);
            if (upgradable.Count == 0) return "Every enchant on this item is already maxed.";
            int at = upgradable[Rng.Next(upgradable.Count)];
            var e = current[at];
            e.Level++;
            MoveToTop(current, at, e);
            return $"{e.Def?.Name} rose to {Enchants.Roman(e.Level)}.";
        }

        // Stack order: the list is oldest → newest; whatever a roll touched becomes the top (last) entry, which the
        // table's remove button pops and the tooltip shows first.
        private static void MoveToTop(List<EnchantEntry> current, int at, EnchantEntry e)
        {
            current.RemoveAt(at);
            current.Add(e);
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
