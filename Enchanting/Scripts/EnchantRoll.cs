using System.Collections.Generic;

namespace Enchanting
{
    /// <summary>Roll rules and costs (SPEC.md "Roll rules" / "Cost per roll"). Tunables are static for now.</summary>
    public static class EnchantRoll
    {
        public static int MaxEnchants = 3;
        public static int GoldPerStep = 5;
        public static int CoinsPerStep = 250;
        // Chance (percent) that a NEW enchant lands at level I / II / III.
        public static int[] NewLevelWeights = { 60, 30, 10 };

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
        public static string Roll(List<EnchantEntry> current, EnchantSlot slot, ObjectID item = ObjectID.None)
        {
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
                current[victim] = new EnchantEntry { Id = pick.Id, Level = RollNewLevel(pick) };
                return $"{old.Def?.Name} {Enchants.Roman(old.Level)} was replaced by {pick.Name} {Enchants.Roman(current[victim].Level)}.";
            }

            if (candidates.Count == 0) return "Every enchant on this item is already maxed.";
            var chosen = candidates[Rng.Next(candidates.Count)];
            int at = IndexOf(current, chosen.Id);
            if (at >= 0)
            {
                var e = current[at];
                e.Level++;
                current[at] = e;
                return $"{chosen.Name} rose to {Enchants.Roman(e.Level)}.";
            }
            var added = new EnchantEntry { Id = chosen.Id, Level = RollNewLevel(chosen) };
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
            current[at] = e;
            return $"{e.Def?.Name} rose to {Enchants.Roman(e.Level)}.";
        }

        private static int RollNewLevel(EnchantDef def)
        {
            int total = 0;
            int max = System.Math.Min(def.MaxLevel, NewLevelWeights.Length);
            for (int i = 0; i < max; i++) total += NewLevelWeights[i];
            int r = Rng.Next(total);
            for (int i = 0; i < max; i++)
            {
                if (r < NewLevelWeights[i]) return i + 1;
                r -= NewLevelWeights[i];
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
