using System;
using System.Collections.Generic;

namespace ModDiagnostics
{
    [Serializable] public sealed class ModStamp { public string name, configFingerprint, configReadError; }
    [Serializable] public sealed class Marker { public double seconds; public string label; }
    [Serializable] public sealed class LogCount { public string level, message; public long count; }
    [Serializable] public sealed class CaptureReport
    {
        public int schema = 1;
        public string recorderVersion = "0.2.0", buildIdentity, label, startedUtc, reason;
        public string gameVersion, unityVersion, cpu, gpu;
        public int width, height, vSyncCount, targetFrameRate, excludedFrames, droppedScopes;
        public int gc0, gc1, gc2, gcObservationFrames, hitchesWithGc, droppedHitches;
        public long managedHeapStart, managedHeapEnd, logMessages, warnings, errors, otherLogPatterns;
        public double durationSeconds, sampledSeconds, warmupSeconds = 3, instrumentationBookkeepingMs;
        public bool sampleCapacityReached, frameTimingAvailable;
        public FrameSummary frames;
        public Distribution cpuMainMs, gpuMs, allocatedKbPerFrame;
        public List<Hitch> hitches = new List<Hitch>();
        public List<ModStamp> mods = new List<ModStamp>();
        public List<string> featureSwitches = new List<string>();
        public List<ScopeSummary> scopes = new List<ScopeSummary>();
        public List<Marker> markers = new List<Marker>();
        public List<LogCount> repeatedLogs = new List<LogCount>();
        public string limitations = "Frame spacing, not CPU/GPU execution time. Scopes are inclusive main-thread elapsed time, including waits; do not sum nested scopes. GC counts and heap size are process-wide and include excluded/warmup intervals. Allocation bytes are current-thread scoped allocations, not retained memory. Source/config fingerprints are identity checks, not performance measurements. Profiling itself has cost; compare recorder-only and instrumented runs. Hitches are frames over 33.33 ms; gc means a collection was observed in that frame interval; topScope is the single slowest instrumented scope in the frame and is empty without instrumentation. cpuMainMs/gpuMs come from Unity FrameTimingManager (latest completed frame, may lag a few frames; absent when frameTimingAvailable is false). allocatedKbPerFrame is main-thread managed allocation between samples.";
    }

}
