namespace MushroomSlayer
{
    public static class ArmorRule
    {
        public static int Threshold = 200;
        public static int Percent = 10;
        public static int Scale(int damage)
        {
            if (damage <= 0) return 0; // A miss/immune hit must not become damage.
            int threshold = System.Math.Max(1, System.Math.Min(10000, Threshold));
            int percent = System.Math.Max(1, System.Math.Min(100, Percent));
            return damage < threshold ? 1 : System.Math.Max(1, (int)((long)damage * percent / 100));
        }

        // Change the queued hit, never HealthCD directly: vanilla still owns damage,
        // death, loot, attribution, multiplayer prediction and combat triggers.
        public static bool Rewrite(ref HealthChange hit, ObjectID target, DamageReductionCD armor)
        {
            if (target != ObjectID.MushroomBrute || armor.maxDamagePerHit != 1 ||
                hit.amount >= 0 || hit.amount == int.MinValue || hit.amount == -int.MaxValue ||
                hit.bypassMaxDamagePerHit)
                return false;

            int damage = -hit.amount;
            if (!hit.bypassDamageReduction)
                damage = (int)System.Math.Min(int.MaxValue, System.Math.Max(0L, (long)damage - armor.reduction));
            hit.amount = -Scale(damage);
            hit.bypassDamageReduction = true;
            hit.bypassMaxDamagePerHit = true;
            // A precomputed kill can reflect unreduced explosion damage. Let vanilla
            // decide against the actual remaining health, including earlier queued hits.
            hit.wasKilled = false;
            return true;
        }
    }
}
