using System;
using System.Collections.Generic;

namespace ModDiagnostics
{
    // Frame spacing must not bridge warmup, menus, focus loss, or a clock reset.
    public sealed class FrameWindow
    {
        private double previous;
        private bool previousEligible;
        public bool TrySample(double now, bool eligible, out double milliseconds)
        {
            milliseconds = 0;
            bool valid = !double.IsNaN(now) && !double.IsInfinity(now);
            bool accept = valid && eligible && previousEligible && now > previous;
            if (accept) milliseconds = (now - previous) * 1000;
            previous = now;
            previousEligible = valid && eligible;
            return accept;
        }
    }

    [Serializable]
    public sealed class FrameSummary
    {
        public int count, over16_67ms, over33_33ms, over50ms, over100ms;
        public double meanMs, medianMs, p95Ms, p99Ms, maxMs;
        public static FrameSummary From(double[] samples, int count)
        {
            if (count < 0 || count > samples.Length) throw new ArgumentOutOfRangeException(nameof(count));
            var result = new FrameSummary { count = count };
            if (count == 0) return result;
            var sorted = new double[count];
            Array.Copy(samples, sorted, count);
            Array.Sort(sorted);
            for (int i = 0; i < count; i++)
            {
                double value = sorted[i];
                result.meanMs += value;
                if (value > 1000.0 / 60) result.over16_67ms++;
                if (value > 1000.0 / 30) result.over33_33ms++;
                if (value > 50) result.over50ms++;
                if (value > 100) result.over100ms++;
            }
            result.meanMs /= count;
            result.medianMs = count % 2 == 0 ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2 : sorted[count / 2];
            result.p95Ms = sorted[(int)Math.Ceiling(count * 0.95) - 1];
            result.p99Ms = sorted[(int)Math.Ceiling(count * 0.99) - 1];
            result.maxMs = sorted[count - 1];
            return result;
        }
    }

    // Plain percentile summary for non-frame series (CPU/GPU time, per-frame allocation).
    [Serializable]
    public sealed class Distribution
    {
        public int count;
        public double mean, median, p95, p99, max;
        public static Distribution From(double[] samples, int count)
        {
            if (samples == null || count < 0 || count > samples.Length) throw new ArgumentOutOfRangeException(nameof(count));
            var frames = FrameSummary.From(samples, count);
            return new Distribution { count = frames.count, mean = frames.meanMs, median = frames.medianMs,
                p95 = frames.p95Ms, p99 = frames.p99Ms, max = frames.maxMs };
        }
    }

    // One slow frame. seconds counts from the end of warmup. topScope is the single
    // instrumented scope with the most inclusive time in that frame (empty when uninstrumented).
    [Serializable]
    public sealed class Hitch
    {
        public double seconds, ms, topScopeMs;
        public bool gc;
        public long allocatedBytes;
        public string topScope, phases;
    }

    [Serializable]
    public sealed class ScopeSummary
    {
        public string name;
        public long calls, allocatedBytes, examined, changed, hitchTop;
        public double inclusiveMs, maxMs, hitchTopMs;
        [NonSerialized] public double frameMs;
        public void Add(double milliseconds, long bytes)
        {
            calls++;
            inclusiveMs += Math.Max(0, milliseconds);
            maxMs = Math.Max(maxMs, milliseconds);
            allocatedBytes += Math.Max(0, bytes);
        }
    }

    // Fixed bounds avoid a diagnostic run creating unbounded allocation of its own.
    public sealed class Measurements
    {
        public const double HitchMs = 1000.0 / 30;
        public readonly double[] Frames, CpuMain, Gpu, AllocatedKb;
        public int Count, IgnoredFrames, DroppedScopes, CpuCount, GpuCount, DroppedHitches;
        public readonly Dictionary<string, ScopeSummary> Scopes = new Dictionary<string, ScopeSummary>();
        public readonly List<Hitch> Hitches = new List<Hitch>();
        private readonly List<ScopeSummary> touched = new List<ScopeSummary>();
        private readonly int scopeLimit, hitchLimit;
        public Measurements(int frames = 120000, int scopes = 512, int hitches = 200)
        {
            if (frames < 1 || scopes < 1 || hitches < 0) throw new ArgumentOutOfRangeException();
            Frames = new double[frames]; CpuMain = new double[frames]; Gpu = new double[frames]; AllocatedKb = new double[frames];
            scopeLimit = scopes; hitchLimit = hitches;
            touched.Capacity = scopes;
        }
        // Scope time inside the current frame; nested scopes are tracked separately, never summed.
        public void AddFrameScope(ScopeSummary scope, double milliseconds)
        {
            if (scope.frameMs == 0) touched.Add(scope);
            scope.frameMs += Math.Max(milliseconds, 1e-9);
        }
        // Closes the per-frame scope window. Call once per sampled or ignored frame.
        public ScopeSummary EndFrame(out double topMs)
        {
            ScopeSummary top = null; topMs = 0;
            foreach (var scope in touched)
            {
                if (scope.frameMs > topMs) { topMs = scope.frameMs; top = scope; }
                scope.frameMs = 0;
            }
            touched.Clear();
            return top;
        }
        public void AddHitch(double seconds, double milliseconds, bool gc, long allocatedBytes, ScopeSummary top, double topMs, string phases = "")
        {
            if (top != null) { top.hitchTop++; top.hitchTopMs += topMs; }
            if (Hitches.Count == hitchLimit) { DroppedHitches++; return; }
            Hitches.Add(new Hitch { seconds = seconds, ms = milliseconds, gc = gc, allocatedBytes = allocatedBytes,
                topScope = top == null ? "" : top.name, topScopeMs = topMs, phases = phases });
        }
        public void AddTiming(double cpuMainMs, double gpuMs)
        {
            if (cpuMainMs > 0 && !double.IsInfinity(cpuMainMs) && CpuCount < CpuMain.Length) CpuMain[CpuCount++] = cpuMainMs;
            if (gpuMs > 0 && !double.IsInfinity(gpuMs) && GpuCount < Gpu.Length) Gpu[GpuCount++] = gpuMs;
        }
        public bool AddFrame(double milliseconds, double allocatedKb = 0)
        {
            if (double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds <= 0)
            { IgnoredFrames++; return true; }
            if (Count == Frames.Length) return false;
            AllocatedKb[Count] = Math.Max(0, allocatedKb);
            Frames[Count++] = milliseconds;
            return true;
        }
        public ScopeSummary Get(string name)
        {
            if (Scopes.TryGetValue(name, out var result)) return result;
            if (Scopes.Count == scopeLimit) { DroppedScopes++; return null; }
            result = new ScopeSummary { name = name };
            Scopes.Add(name, result);
            return result;
        }
    }
}
