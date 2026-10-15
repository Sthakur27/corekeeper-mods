using System;

// Minimal data-only test doubles. Production builds reference the actual game types.
public enum ObjectID { MushroomBrute, Other }
public struct DamageReductionCD { public int maxDamagePerHit, reduction; }
public struct HealthChange
{
    public int amount;
    public bool bypassMaxDamagePerHit, bypassDamageReduction, wasKilled;
}

public static class RuleTests
{
    private static int checks;
    private static void Check(bool value, string label)
    {
        checks++;
        if (!value) throw new Exception(label);
    }
    private static HealthChange Hit(int amount) => new HealthChange { amount = amount };
    public static void Main()
    {
        var gray = new DamageReductionCD { maxDamagePerHit = 1 };
        var red = new DamageReductionCD();
        int[] inputs = { 1, 50, 199, 200, 209, 999, 2000 };
        int[] outputs = { 1, 1, 1, 20, 20, 99, 200 };
        for (int i = 0; i < inputs.Length; i++)
        {
            var hit = Hit(-inputs[i]);
            Check(MushroomSlayer.ArmorRule.Rewrite(ref hit, ObjectID.MushroomBrute, gray), "gray handled");
            Check(hit.amount == -outputs[i] && hit.bypassMaxDamagePerHit && hit.bypassDamageReduction, "threshold/rounding");
            Check(!MushroomSlayer.ArmorRule.Rewrite(ref hit, ObjectID.MushroomBrute, gray), "never scale twice");
        }
        foreach (int amount in new[] { 0, 100, -int.MaxValue, int.MinValue })
        {
            var hit = Hit(amount);
            Check(!MushroomSlayer.ArmorRule.Rewrite(ref hit, ObjectID.MushroomBrute, gray) && hit.amount == amount, "zero/healing/creative/overflow");
        }
        var redHit = Hit(-2000);
        Check(!MushroomSlayer.ArmorRule.Rewrite(ref redHit, ObjectID.MushroomBrute, red) && redHit.amount == -2000, "red unchanged");
        var other = Hit(-2000);
        Check(!MushroomSlayer.ArmorRule.Rewrite(ref other, ObjectID.Other, gray) && other.amount == -2000, "other enemies unchanged");
        var bypass = Hit(-2000); bypass.bypassMaxDamagePerHit = true;
        Check(!MushroomSlayer.ArmorRule.Rewrite(ref bypass, ObjectID.MushroomBrute, gray) && bypass.amount == -2000, "explicit bypass unchanged");
        var reduced = Hit(-210);
        var flat = new DamageReductionCD { maxDamagePerHit = 1, reduction = 11 };
        MushroomSlayer.ArmorRule.Rewrite(ref reduced, ObjectID.MushroomBrute, flat);
        Check(reduced.amount == -1, "threshold after flat reduction");
        var immune = Hit(-10);
        MushroomSlayer.ArmorRule.Rewrite(ref immune, ObjectID.MushroomBrute, flat);
        Check(immune.amount == 0, "zero damage stays zero");
        var alreadyReduced = Hit(-200); alreadyReduced.bypassDamageReduction = true;
        MushroomSlayer.ArmorRule.Rewrite(ref alreadyReduced, ObjectID.MushroomBrute, flat);
        Check(alreadyReduced.amount == -20, "flat reduction not applied twice");
        int health = 810;
        int[] remaining = { 610, 410, 210, 10, -190 };
        for (int i = 0; i < 5; i++)
        {
            var hit = Hit(-2000); hit.wasKilled = true;
            MushroomSlayer.ArmorRule.Rewrite(ref hit, ObjectID.MushroomBrute, gray);
            Check(!hit.wasKilled, "discard premature kill flag");
            health += hit.amount;
            Check(health == remaining[i], "five-hit hard kill");
        }
        var transition = Hit(-2000);
        MushroomSlayer.ArmorRule.Rewrite(ref transition, ObjectID.MushroomBrute, red);
        Check(transition.amount == -2000, "red after gray unchanged");
        foreach (int damage in new[] { 1200, 2400, 8100 })
        {
            // Projectile helper now queues uncapped, already-flat-reduced damage.
            var arrow = Hit(-damage);
            arrow.bypassDamageReduction = true;
            arrow.wasKilled = true;
            Check(MushroomSlayer.ArmorRule.Rewrite(ref arrow, ObjectID.MushroomBrute, gray), "projectile normalized");
            Check(arrow.amount == -damage / 10 && !arrow.wasKilled, "projectile/crit damage and premature kill");
            Check(!MushroomSlayer.ArmorRule.Rewrite(ref arrow, ObjectID.MushroomBrute, gray), "health pass does not scale arrow twice");
        }
        MushroomSlayer.ArmorRule.Threshold = 500;
        MushroomSlayer.ArmorRule.Percent = 25;
        Check(MushroomSlayer.ArmorRule.Scale(499) == 1, "custom threshold");
        Check(MushroomSlayer.ArmorRule.Scale(500) == 125, "custom percentage");
        Check(MushroomSlayer.ArmorRule.Scale(1200) == 300, "live setting applies");
        MushroomSlayer.ArmorRule.Threshold = 1;
        MushroomSlayer.ArmorRule.Percent = 1;
        Check(MushroomSlayer.ArmorRule.Scale(1) == 1, "positive damage minimum");
        MushroomSlayer.ArmorRule.Percent = 100;
        Check(MushroomSlayer.ArmorRule.Scale(int.MaxValue) == int.MaxValue, "percentage overflow safety");
        MushroomSlayer.ArmorRule.Threshold = 200;
        MushroomSlayer.ArmorRule.Percent = 10;
        Check(MushroomSlayer.ArmorRule.Scale(1200) == 120, "defaults restored");
        Console.WriteLine(checks + " armor rule checks passed.");
    }
}
