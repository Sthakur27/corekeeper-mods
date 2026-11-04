using System;
using FuelPower.Core;

internal static class Program
{
    private static int checks;

    private static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }

    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (ArgumentException) { checks++; return; }
        throw new Exception(message);
    }

    private static MachineState State(decimal work = 25, decimal energy = 0,
        FuelKind fuel = FuelKind.Wood, int count = 0) => new MachineState(work, energy, fuel, count);

    private static void FuelTests()
    {
        var empty = FuelMachine.Advance(State(), 5, true);
        Check(empty.Status == MachineStatus.NeedsFuel && empty.WorkDone == 0
            && empty.Next.RemainingWork == 25, "An empty furnace must not progress.");

        var initial = State(count: 2);
        var blocked = FuelMachine.Advance(initial, 5, false);
        Check(ReferenceEquals(blocked.Next, initial) && blocked.FuelConsumed == 0
            && blocked.Status == MachineStatus.OutputBlocked, "Blocked output must not burn fuel.");
        Check(FuelMachine.Advance(State(work: 0, count: 2), 100, true).FuelConsumed == 0,
            "Idle furnaces must not burn fuel.");
        Check(FuelMachine.Advance(initial, 0, true).FuelConsumed == 0, "Zero elapsed work must not burn fuel.");

        var first = FuelMachine.Advance(initial, 6, true);
        Check(first.FuelConsumed == 1 && first.Next.StoredEnergy == 4
            && first.Next.RemainingWork == 19 && first.Next.FuelCount == 1,
            "Opening one wood unit must preserve unused burn credit.");
        Check(initial.FuelCount == 2 && initial.RemainingWork == 25, "Planning must not mutate live state.");
        var second = FuelMachine.Advance(first.Next, 4, true);
        Check(second.FuelConsumed == 0 && second.Next.StoredEnergy == 0,
            "Use existing burn credit before opening more fuel.");

        var exhausted = FuelMachine.Advance(State(count: 1), 25, true);
        Check(exhausted.Status == MachineStatus.NeedsFuel && exhausted.WorkDone == 10
            && exhausted.Next.RemainingWork == 15 && !exhausted.Completed,
            "Fuel exhaustion must stop precisely without completing output.");
        var refueled = new MachineState(exhausted.Next.RemainingWork, exhausted.Next.StoredEnergy, FuelKind.Coal, 1);
        var complete = FuelMachine.Advance(refueled, 100, true);
        Check(complete.Completed && complete.WorkDone == 15 && complete.Next.StoredEnergy == 25,
            "Refueling must resume preserved work and carry surplus energy forward.");
        Check(!FuelMachine.Advance(complete.Next, 100, true).Completed,
            "Completed state must not emit completion again.");

        var snapshot = new MachineState(first.Next.RemainingWork, first.Next.StoredEnergy,
            first.Next.Fuel, first.Next.FuelCount);
        var resumed = FuelMachine.Advance(snapshot, 19, true);
        var uninterrupted = FuelMachine.Advance(first.Next, 19, true);
        Check(resumed.Next.RemainingWork == uninterrupted.Next.RemainingWork
            && resumed.Next.StoredEnergy == uninterrupted.Next.StoredEnergy
            && resumed.Next.FuelCount == uninterrupted.Next.FuelCount,
            "Reconstructing a snapshot must preserve the transition result (not a game-save test).");

        foreach (FuelKind fuel in Enum.GetValues(typeof(FuelKind)))
        for (int count = 0; count < 5; count++)
        {
            // Verify energy conservation and tick-size independence, including fractional speed/work.
            var start = State(work: 37, energy: 2.25m, fuel: fuel, count: count);
            var bulk = FuelMachine.Advance(start, 50, true);
            var ticked = start;
            decimal totalWork = 0;
            int totalFuel = 0;
            for (int tick = 0; tick < 200; tick++)
            {
                var step = FuelMachine.Advance(ticked, 0.25m, true);
                totalWork += step.WorkDone;
                totalFuel += step.FuelConsumed;
                ticked = step.Next;
            }
            Check(ticked.RemainingWork == bulk.Next.RemainingWork
                && ticked.StoredEnergy == bulk.Next.StoredEnergy && ticked.FuelCount == bulk.Next.FuelCount,
                "Fuel accounting must be independent of tick size.");
            Check(start.StoredEnergy + totalFuel * FuelValues.Energy(fuel) == totalWork + ticked.StoredEnergy,
                "Energy must be conserved.");
        }

        var normal = FuelMachine.Advance(State(work: 100, count: 10), 5, true);
        var fast = FuelMachine.Advance(State(work: 100, count: 10), 25, true);
        Check(fast.WorkDone == normal.WorkDone * 5 && fast.FuelConsumed > normal.FuelConsumed,
            "Five times the work must require five times the energy.");
        Reject(() => FuelMachine.Advance(initial, -1, true), "Negative elapsed work accepted.");
        Reject(() => State(energy: -1), "Negative stored energy accepted.");
        Reject(() => State(count: -1), "Negative fuel count accepted.");
        Reject(() => State(fuel: (FuelKind)999), "Unknown fuel accepted.");
    }

    private static void CoalTests()
    {
        Check(CoalDrops.FromWall(WallOrigin.Natural, BreakSource.PlayerMining, true, .05m, .049m),
            "Eligible natural wall should pass a successful roll.");
        Check(!CoalDrops.FromWall(WallOrigin.Natural, BreakSource.PlayerMining, true, .05m, .05m),
            "Chance threshold must be exclusive.");
        foreach (WallOrigin origin in Enum.GetValues(typeof(WallOrigin)))
        foreach (BreakSource source in Enum.GetValues(typeof(BreakSource)))
            Check(CoalDrops.FromWall(origin, source, true, 1, 0)
                == (origin == WallOrigin.Natural && source == BreakSource.PlayerMining),
                "Default policy must reject unknown/placed walls and automated destruction.");
        Check(!CoalDrops.FromWall(WallOrigin.Natural, BreakSource.PlayerMining, false, 1, 0),
            "Ineligible wall types must not drop coal.");
        Check(CoalDrops.FromWall(WallOrigin.Natural, BreakSource.Drill, true, 1, 0, allowDrills: true),
            "Drill drops must require explicit opt-in.");
        Check(!CoalDrops.FromWall(WallOrigin.PlayerPlaced, BreakSource.Explosion, true, 1, 0, allowExplosions: true),
            "Explosion opt-in must not allow placed wall farming.");
        Check(!CoalDrops.FromMob(false, 1, 0) && CoalDrops.FromMob(true, 1, .99m)
            && !CoalDrops.FromMob(true, 0, 0), "Mob allowlist and chance must both apply.");
        int drops = 0;
        for (int i = 0; i < 10000; i++)
            if (CoalDrops.FromWall(WallOrigin.Natural, BreakSource.PlayerMining, true, .05m, i / 10000m)) drops++;
        Check(drops == 500, "Five percent should accept exactly five percent of uniform test samples.");
        Reject(() => CoalDrops.FromMob(true, 1.01m, 0), "Invalid chance accepted.");
        Reject(() => CoalDrops.FromMob(true, .05m, 1), "Invalid random sample accepted.");
    }

    private static void Demo()
    {
        Console.WriteLine("Demo: 25 work recipe, 1 wood (10 energy); refuel with 1 coal (40 energy). Values are provisional.");
        var state = State(count: 1);
        for (int tick = 1; tick <= 7; tick++)
        {
            if (tick == 4) state = new MachineState(state.RemainingWork, state.StoredEnergy, FuelKind.Coal, 1);
            var result = FuelMachine.Advance(state, 5, tick != 5);
            state = result.Next;
            Console.WriteLine($"step={tick} status={result.Status} workLeft={state.RemainingWork} energy={state.StoredEnergy} fuel={state.FuelCount} consumed={result.FuelConsumed}");
        }
    }

    private static void Main(string[] args)
    {
        FuelTests();
        CoalTests();
        Console.WriteLine($"PASS: {checks} assertions against the actual C# core. No game integration tested.");
        if (Array.IndexOf(args, "--demo") >= 0) Demo();
    }
}
