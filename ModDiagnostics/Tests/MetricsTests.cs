using System;
using ModDiagnostics;

internal static class MetricsTests
{
    private static int tests;
    private static void Check(bool condition, string reason)
    {
        tests++;
        if (!condition) throw new Exception(reason);
    }
    private static bool Near(double a, double b) => Math.Abs(a - b) < 0.000001;
    private static void Main()
    {
        var empty = FrameSummary.From(Array.Empty<double>(), 0);
        Check(empty.count == 0 && empty.maxMs == 0, "Empty capture must stay empty.");
        var samples = new[] { 100.0, 10.0, 20.0, 30.0, 9999.0 };
        var s = FrameSummary.From(samples, 4);
        Check(s.count == 4 && Near(s.meanMs, 40) && Near(s.medianMs, 25), "Partial buffer statistics.");
        Check(s.p95Ms == 100 && s.p99Ms == 100 && s.maxMs == 100, "Nearest-rank percentiles.");
        Check(samples[0] == 100 && samples[4] == 9999, "Summary cannot mutate capture buffer.");
        s = FrameSummary.From(new[] { 1.0, 9.0, 2.0 }, 3);
        Check(s.medianMs == 2, "Odd median.");
        var hundred = new double[100];
        for (int i = 0; i < 100; i++) hundred[i] = i + 1;
        s = FrameSummary.From(hundred, hundred.Length);
        Check(s.p95Ms == 95 && s.p99Ms == 99, "Percentile ranks at larger sample size.");
        s = FrameSummary.From(new[] { 1000.0 / 60, 1000.0 / 30, 50, 100, 101 }, 5);
        Check(s.over16_67ms == 4 && s.over33_33ms == 3 && s.over50ms == 2 && s.over100ms == 1,
            "Thresholds are strict greater-than, including exact boundaries.");
        bool threw = false;
        try { FrameSummary.From(new double[1], 2); } catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, "Reject out-of-buffer summary counts.");

        var m = new Measurements(2, 1);
        Check(m.AddFrame(double.NaN) && m.AddFrame(double.PositiveInfinity) && m.AddFrame(0) && m.AddFrame(-1),
            "Invalid samples must not end recording.");
        Check(m.Count == 0 && m.IgnoredFrames == 4, "Invalid samples excluded from counts.");
        Check(m.AddFrame(10) && m.AddFrame(20) && !m.AddFrame(30) && m.Count == 2, "Sample capacity is bounded.");
        var scope = m.Get("one");
        Check(object.ReferenceEquals(scope, m.Get("one")), "Existing scopes reuse storage.");
        Check(m.Get("overflow") == null && m.DroppedScopes == 1 && m.Scopes.Count == 1, "Scope capacity is bounded.");
        scope.Add(4, 32); scope.Add(-5, -100); scope.Add(9, 8);
        Check(scope.calls == 3 && scope.inclusiveMs == 13 && scope.maxMs == 9 && scope.allocatedBytes == 40,
            "Scope totals clamp invalid negative deltas and preserve peak.");

        var gate = new FrameWindow();
        double ms;
        Check(!gate.TrySample(1, false, out ms) && !gate.TrySample(4, true, out ms), "No warmup-to-first-frame bridge.");
        Check(gate.TrySample(4.02, true, out ms) && Near(ms, 20), "Actual eligible frame spacing.");
        Check(!gate.TrySample(5, false, out ms) && !gate.TrySample(50, true, out ms), "Focus/menu gap excluded.");
        Check(gate.TrySample(50.1, true, out ms) && Near(ms, 100), "Resume after one discarded interval.");
        Check(!gate.TrySample(49, true, out ms), "Backward clock cannot produce negative samples.");
        Check(!gate.TrySample(double.NaN, true, out ms) && !gate.TrySample(51, true, out ms), "Invalid clock resets frame history.");
        Check(gate.TrySample(52, true, out ms) && Near(ms, 1000), "Large real stutters are retained, not delta-time-clamped.");
        var h = new Measurements(4, 4, 1);
        var outer = h.Get("outer"); var inner = h.Get("inner");
        h.AddFrameScope(inner, 2); h.AddFrameScope(outer, 5); h.AddFrameScope(inner, 1);
        double topMs;
        var top = h.EndFrame(out topMs);
        Check(object.ReferenceEquals(top, outer) && Near(topMs, 5), "Slowest single scope wins; nested scopes are not summed.");
        Check(h.EndFrame(out topMs) == null && topMs == 0 && inner.frameMs == 0, "Frame scope window resets.");
        h.AddHitch(1.5, 80, true, 2048, outer, 5);
        h.AddHitch(2.5, 40, false, 0, null, 0);
        Check(h.Hitches.Count == 1 && h.DroppedHitches == 1, "Hitch log is bounded.");
        Check(h.Hitches[0].gc && h.Hitches[0].topScope == "outer" && outer.hitchTop == 1 && Near(outer.hitchTopMs, 5),
            "Hitch keeps GC flag and slowest scope.");
        h.AddTiming(4, 0); h.AddTiming(double.NaN, 6); h.AddTiming(-1, double.PositiveInfinity);
        Check(h.CpuCount == 1 && h.GpuCount == 1 && h.CpuMain[0] == 4 && h.Gpu[0] == 6, "Invalid CPU/GPU timings are skipped.");
        Check(h.AddFrame(10, 3.5) && h.AllocatedKb[0] == 3.5 && h.AddFrame(10, -1) && h.AllocatedKb[1] == 0,
            "Per-frame allocation stored with its frame and clamped.");
        var d = Distribution.From(new[] { 1.0, 2.0, 3.0 }, 3);
        Check(d.count == 3 && Near(d.mean, 2) && d.median == 2 && d.max == 3, "Distribution summary.");
        Console.WriteLine(tests + " metrics and sampling checks passed.");
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        var report = new CaptureReport { label = "quote\" slash\\ newline\n snow\u2603", sampledSeconds = 60.25,
            frames = FrameSummary.From(new[] { 5.0, 10.0, 20.0 }, 3) };
        report.mods.Add(new ModStamp { name = "example", configFingerprint = "1234" });
        report.scopes.Add(new ScopeSummary { name = "Update", calls = 3, inclusiveMs = 1.25, allocatedBytes = 99 });
        report.markers.Add(new Marker { seconds = 2.5, label = "marker" });
        report.repeatedLogs.Add(new LogCount { level = "Warning", message = "tab\tcontrol\u0001", count = 2 });
        report.featureSwitches.Add("Feature: example = true");
        report.hitches.Add(new Hitch { seconds = 1.5, ms = 80, gc = true, allocatedBytes = 2048, topScope = "Update", topScopeMs = 5 });
        report.hitchesWithGc = 1;
        report.cpuMainMs = Distribution.From(new[] { 4.0 }, 1);
        Console.WriteLine(ReportJson.Serialize(report));
    }
}
