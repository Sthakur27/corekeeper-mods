using System;

namespace FuelPower.Core
{
    public enum WallOrigin { Unknown, Natural, PlayerPlaced }
    public enum BreakSource { PlayerMining, Drill, Explosion, Other }

    public static class CoalDrops
    {
        // Eligibility is supplied by a verified server event adapter. Unknown provenance fails closed.
        // Call once per completed destruction, never once per hit or per observing client.
        public static bool FromWall(WallOrigin origin, BreakSource source, bool eligibleWall,
            decimal chance, decimal roll, bool allowDrills = false, bool allowExplosions = false)
        {
            Validate(chance, roll);
            if (origin != WallOrigin.Natural || !eligibleWall) return false;
            bool allowed = source == BreakSource.PlayerMining
                || (source == BreakSource.Drill && allowDrills)
                || (source == BreakSource.Explosion && allowExplosions);
            return allowed && roll < chance;
        }

        // The adapter must check an explicit enemy allowlist and process one authoritative death event.
        public static bool FromMob(bool eligibleEnemy, decimal chance, decimal roll)
        {
            Validate(chance, roll);
            return eligibleEnemy && roll < chance;
        }

        private static void Validate(decimal chance, decimal roll)
        {
            if (chance < 0 || chance > 1) throw new ArgumentOutOfRangeException(nameof(chance));
            if (roll < 0 || roll >= 1) throw new ArgumentOutOfRangeException(nameof(roll));
        }
    }
}
