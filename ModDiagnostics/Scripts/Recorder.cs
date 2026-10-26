using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using PugMod;
using UnityEngine;

namespace ModDiagnostics
{
    public static class Recorder
    {
        // Diagnostic staging replaces this with a source/target fingerprint.
        public const string BuildIdentity = "recorder-only";
        public static bool Active { get; private set; }
        public static string Label => report == null ? "" : report.label;
        public static string LastReport { get; private set; } = "No capture yet.";
        private static Measurements data;
        private static CaptureReport report;
        private static double started, deadline, readyAt, bookkeeping;
        private static int generation, mainThread, gc0, gc1, gc2, lastGc;
        private static long lastAllocated;
        private static readonly FrameTiming[] timings = new FrameTiming[1];
        private static bool measuring;
        private static FrameWindow frameWindow;
        private static readonly object logLock = new object();
        private static readonly Dictionary<string, LogCount> logs = new Dictionary<string, LogCount>();
        private static string pendingJson, pendingPath;

        public static string Start(string label, int seconds)
        {
            if (Active) return "Already recording. Stop the current capture first.";
            if (seconds < 5 || seconds > 300) return "Choose 5-300 seconds.";
            if (pendingJson != null) return "Export the pending capture with /modperf export before starting another.";
            mainThread = Thread.CurrentThread.ManagedThreadId;
            data = new Measurements();
            report = new CaptureReport {
                buildIdentity = BuildIdentity, label = SafeLabel(label), startedUtc = DateTime.UtcNow.ToString("o"),
                gameVersion = Application.version, unityVersion = Application.unityVersion,
                cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName,
                width = Screen.width, height = Screen.height, vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = Application.targetFrameRate
            };
            // Snapshot metadata before recording, never once per frame. No raw config
            // contents or private sound-file paths are exported.
            foreach (var mod in API.ModLoader.LoadedMods)
            {
                var stamp = new ModStamp { name = mod.Metadata.name, configFingerprint = "absent" };
                try
                {
                    string path = stamp.name + "/config.cfg";
                    if (API.ConfigFilesystem.FileExists(path))
                    {
                        var bytes = API.ConfigFilesystem.Read(path);
                        uint hash = 2166136261;
                        foreach (byte b in bytes) hash = unchecked((hash ^ b) * 16777619);
                        stamp.configFingerprint = hash.ToString("x8");
                        if (stamp.name == "SidsOverhaul")
                            foreach (string line in Encoding.UTF8.GetString(bytes).Split('\n'))
                                if (line.TrimStart().StartsWith("Feature: ")) report.featureSwitches.Add(line.Trim());
                    }
                }
                catch (Exception) { stamp.configReadError = "config-read-failed"; }
                report.mods.Add(stamp);
            }
            report.mods.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            gc0 = GC.CollectionCount(0); gc1 = GC.CollectionCount(1); gc2 = GC.CollectionCount(2);
            lastGc = gc0 + gc1 + gc2;
            report.managedHeapStart = GC.GetTotalMemory(false);
            // Needs the player's frame timing stats; without them only frame spacing is reported.
            report.frameTimingAvailable = FrameTimingManager.IsFeatureEnabled();
            lastAllocated = GC.GetAllocatedBytesForCurrentThread();
            bookkeeping = 0; measuring = false; frameWindow = new FrameWindow();
            lock (logLock) logs.Clear();
            generation++;
            started = Time.realtimeSinceStartupAsDouble;
            readyAt = started + report.warmupSeconds;
            deadline = readyAt + seconds;
            Active = true;
            Application.logMessageReceivedThreaded += OnLog;
            return "Recording " + report.label + " after 3s warmup for " + seconds + "s. Close chat and follow the benchmark route.";
        }

        public static void SampleFrame()
        {
            if (!Active) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now >= deadline) { Stop("duration"); return; }
            bool eligible = Eligible(now);
            int currentGc = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
            bool gc = currentGc != lastGc;
            if (gc && eligible) report.gcObservationFrames++;
            lastGc = currentGc;
            long allocatedNow = GC.GetAllocatedBytesForCurrentThread();
            long allocated = allocatedNow - lastAllocated;
            lastAllocated = allocatedNow;
            // Scopes recorded since the previous sample belong to the interval measured now.
            var top = data.EndFrame(out double topMs);
            if (report.frameTimingAvailable) FrameTimingManager.CaptureFrameTimings();
            measuring = frameWindow.TrySample(now, eligible, out double milliseconds);
            if (!measuring) { data.IgnoredFrames++; return; }
            if (report.frameTimingAvailable && FrameTimingManager.GetLatestTimings(1, timings) > 0)
                data.AddTiming(timings[0].cpuMainThreadFrameTime, timings[0].gpuFrameTime);
            if (milliseconds > Measurements.HitchMs)
            {
                data.AddHitch(now - readyAt, milliseconds, gc, allocated, top, topMs);
                if (gc) report.hitchesWithGc++;
            }
            if (!data.AddFrame(milliseconds, allocated / 1024.0))
            { report.sampleCapacityReached = true; Stop("sample-capacity"); }
        }

        private static bool Eligible(double now) => now >= readyAt && now < deadline && Application.isFocused
            && Manager.main?.player != null && (Manager.menu == null || !Manager.menu.IsAnyMenuActive());

        public struct Scope : IDisposable
        {
            private ScopeSummary summary;
            private readonly double start;
            private readonly long allocated;
            private readonly int session;
            internal Scope(ScopeSummary summary)
            {
                this.summary = summary; session = generation;
                allocated = GC.GetAllocatedBytesForCurrentThread();
                start = Time.realtimeSinceStartupAsDouble;
            }
            public void Dispose()
            {
                if (summary == null) return;
                var target = summary; summary = null;
                if (!Active || generation != session || Thread.CurrentThread.ManagedThreadId != mainThread) return;
                double end = Time.realtimeSinceStartupAsDouble;
                if (!Eligible(end)) return;
                long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                double milliseconds = (end - start) * 1000;
                target.Add(milliseconds, bytes);
                data.AddFrameScope(target, milliseconds);
                bookkeeping += (Time.realtimeSinceStartupAsDouble - end) * 1000;
            }
        }
        public static Scope Measure(string name)
        {
            if (!Active || !measuring || Thread.CurrentThread.ManagedThreadId != mainThread) return default;
            double before = Time.realtimeSinceStartupAsDouble;
            if (!Eligible(before)) return default;
            var summary = data.Get(name);
            bookkeeping += (Time.realtimeSinceStartupAsDouble - before) * 1000;
            return summary == null ? default : new Scope(summary);
        }
        // Optional manual instrumentation at an existing loop: count its actual work,
        // never add a separate query just to count entities.
        public static void Work(string name, long examined, long changed)
        {
            if (!Active || !measuring || Thread.CurrentThread.ManagedThreadId != mainThread) return;
            if (!Eligible(Time.realtimeSinceStartupAsDouble)) return;
            var summary = data.Get(name);
            if (summary == null) return;
            summary.examined += Math.Max(0, examined); summary.changed += Math.Max(0, changed);
        }
        public static void Mark(string label)
        {
            if (Active && report.markers.Count < 128)
                report.markers.Add(new Marker { seconds = Time.realtimeSinceStartupAsDouble - started, label = SafeLabel(label) });
        }
        private static void OnLog(string message, string stack, LogType type)
        {
            if (!Active) return;
            lock (logLock)
            {
                if (!Active) return;
                report.logMessages++;
                if (type == LogType.Warning) report.warnings++;
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors++;
                // Count all messages, retain only bounded warning/error patterns.
                if (type == LogType.Log) return;
                string key = type + ":" + (message == null ? "" : message.Substring(0, Math.Min(message.Length, 160)));
                if (logs.TryGetValue(key, out var count)) { count.count++; return; }
                if (logs.Count == 64) { report.otherLogPatterns++; return; }
                logs.Add(key, new LogCount { level = type.ToString(), message = key, count = 1 });
            }
        }
        public static string Stop(string reason)
        {
            if (!Active) return "Recorder is off. " + LastReport;
            Active = false; measuring = false;
            Application.logMessageReceivedThreaded -= OnLog;
            report.durationSeconds = Time.realtimeSinceStartupAsDouble - started;
            report.reason = reason;
            report.gc0 = GC.CollectionCount(0) - gc0; report.gc1 = GC.CollectionCount(1) - gc1; report.gc2 = GC.CollectionCount(2) - gc2;
            report.managedHeapEnd = GC.GetTotalMemory(false);
            report.frames = FrameSummary.From(data.Frames, data.Count);
            report.allocatedKbPerFrame = Distribution.From(data.AllocatedKb, data.Count);
            if (data.CpuCount > 0) report.cpuMainMs = Distribution.From(data.CpuMain, data.CpuCount);
            if (data.GpuCount > 0) report.gpuMs = Distribution.From(data.Gpu, data.GpuCount);
            report.hitches.AddRange(data.Hitches);
            report.droppedHitches = data.DroppedHitches;
            report.sampledSeconds = report.frames.meanMs * report.frames.count / 1000;
            report.excludedFrames = data.IgnoredFrames; report.droppedScopes = data.DroppedScopes;
            report.instrumentationBookkeepingMs = bookkeeping;
            report.scopes.AddRange(data.Scopes.Values);
            report.scopes.Sort((a, b) => b.inclusiveMs.CompareTo(a.inclusiveMs));
            lock (logLock) report.repeatedLogs.AddRange(logs.Values);
            pendingPath = "ModDiagnostics/reports/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + report.label + ".json";
            pendingJson = ReportJson.Serialize(report);
            data = null;
            string result = Export();
            Debug.Log("[ModDiagnostics] " + result);
            return result;
        }
        public static string Export()
        {
            if (pendingJson == null) return LastReport;
            try
            {
                API.ConfigFilesystem.CreateDirectory("ModDiagnostics/reports");
                API.ConfigFilesystem.Write(pendingPath, Encoding.UTF8.GetBytes(pendingJson));
                LastReport = "Saved " + pendingPath + " (" + report.frames.count + " frames, p95 " + report.frames.p95Ms.ToString("F2") + "ms).";
                pendingJson = null;
                return LastReport;
            }
            catch (Exception e)
            {
                LastReport = "Export failed: " + e.Message + ". Capture retained; retry /modperf export.";
                return LastReport;
            }
        }
        private static string SafeLabel(string label)
        {
            var result = new StringBuilder();
            foreach (char c in label ?? "capture")
                if (result.Length < 48 && ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_')) result.Append(c);
            return result.Length == 0 ? "capture" : result.ToString();
        }
    }
}
