using System;

namespace FuelPower.Core
{
    public enum FuelKind { Wood, Charcoal, Coal }

    // Prototype tuning: energy is measured in seconds of baseline furnace work.
    public static class FuelValues
    {
        public static long Energy(FuelKind kind)
        {
            switch (kind)
            {
                case FuelKind.Wood: return 10;
                case FuelKind.Charcoal: return 20;
                case FuelKind.Coal: return 40;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }

    public enum MachineStatus { Idle, OutputBlocked, NeedsFuel, Running, Completed }

    // Immutable data transfer objects. A game adapter must persist these fields, not Entity IDs.
    public sealed class MachineState
    {
        public readonly decimal RemainingWork;
        public readonly decimal StoredEnergy;
        public readonly FuelKind Fuel;
        public readonly int FuelCount;

        public MachineState(decimal remainingWork, decimal storedEnergy, FuelKind fuel, int fuelCount)
        {
            if (remainingWork < 0 || storedEnergy < 0 || fuelCount < 0)
                throw new ArgumentOutOfRangeException(nameof(remainingWork), "State values cannot be negative.");
            FuelValues.Energy(fuel);
            RemainingWork = remainingWork;
            StoredEnergy = storedEnergy;
            Fuel = fuel;
            FuelCount = fuelCount;
        }
    }

    public sealed class WorkResult
    {
        public readonly MachineState Next;
        public readonly MachineStatus Status;
        public readonly decimal WorkDone;
        public readonly int FuelConsumed;
        public bool Completed => Status == MachineStatus.Completed;

        internal WorkResult(MachineState next, MachineStatus status, decimal workDone, int fuelConsumed)
        {
            Next = next; Status = status; WorkDone = workDone; FuelConsumed = fuelConsumed;
        }
    }

    public static class FuelMachine
    {
        // Pure transition: never removes inventory or emits output itself. The adapter must commit
        // fuel, progress, ingredient/output changes and saved state together, once, on the server.
        // requestedWork = elapsed seconds * speed multiplier, so faster crafting costs more per second.
        public static WorkResult Advance(MachineState state, decimal requestedWork, bool outputAvailable)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (requestedWork < 0) throw new ArgumentOutOfRangeException(nameof(requestedWork));
            if (state.RemainingWork == 0 || requestedWork == 0)
                return new WorkResult(state, MachineStatus.Idle, 0, 0);
            if (!outputAvailable)
                return new WorkResult(state, MachineStatus.OutputBlocked, 0, 0);

            decimal wanted = Math.Min(requestedWork, state.RemainingWork);
            decimal unit = FuelValues.Energy(state.Fuel);
            decimal shortfall = Math.Max(0, wanted - state.StoredEnergy);
            int consumed = (int)Math.Min(state.FuelCount, decimal.Ceiling(shortfall / unit));
            decimal available = state.StoredEnergy + consumed * unit;
            decimal done = Math.Min(wanted, available);
            var next = new MachineState(state.RemainingWork - done, available - done,
                state.Fuel, state.FuelCount - consumed);
            var status = next.RemainingWork == 0 ? MachineStatus.Completed
                : done < wanted ? MachineStatus.NeedsFuel : MachineStatus.Running;
            return new WorkResult(next, status, done, consumed);
        }
    }
}
