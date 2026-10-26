using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ModDiagnostics
{
    // Explicit schema: runtime-loaded types can lose nested fields in Unity JsonUtility.
    // No reflection or engine serializer; work happens only after recording stops.
    public static class ReportJson
    {
        private static string Value(string value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("\"");
            foreach (char c in value)
            {
                if (c == '\\') text.Append("\\\\");
                else if (c == '"') text.Append("\\\"");
                else if (c < 32 || c >= 127) text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else text.Append(c);
            }
            return text.Append('"').ToString();
        }
        private static string Value(long value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Value(double value) => double.IsNaN(value) || double.IsInfinity(value) ? "null" : value.ToString("R", CultureInfo.InvariantCulture);
        private static string Value(bool value) => value ? "true" : "false";
        private static string List<T>(IEnumerable<T> values, Func<T, string> serialize)
        {
            var text = new StringBuilder("["); bool first = true;
            foreach (var value in values)
            {
                if (!first) text.Append(',');
                text.Append(serialize(value)); first = false;
            }
            return text.Append(']').ToString();
        }
        public static string Serialize(CaptureReport value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("{");
            text.Append("\"schema\":").Append(Value(value.schema));
            text.Append(",\"recorderVersion\":").Append(Value(value.recorderVersion));
            text.Append(",\"buildIdentity\":").Append(Value(value.buildIdentity));
            text.Append(",\"label\":").Append(Value(value.label));
            text.Append(",\"startedUtc\":").Append(Value(value.startedUtc));
            text.Append(",\"reason\":").Append(Value(value.reason));
            text.Append(",\"gameVersion\":").Append(Value(value.gameVersion));
            text.Append(",\"unityVersion\":").Append(Value(value.unityVersion));
            text.Append(",\"cpu\":").Append(Value(value.cpu));
            text.Append(",\"gpu\":").Append(Value(value.gpu));
            text.Append(",\"width\":").Append(Value(value.width));
            text.Append(",\"height\":").Append(Value(value.height));
            text.Append(",\"vSyncCount\":").Append(Value(value.vSyncCount));
            text.Append(",\"targetFrameRate\":").Append(Value(value.targetFrameRate));
            text.Append(",\"excludedFrames\":").Append(Value(value.excludedFrames));
            text.Append(",\"droppedScopes\":").Append(Value(value.droppedScopes));
            text.Append(",\"gc0\":").Append(Value(value.gc0));
            text.Append(",\"gc1\":").Append(Value(value.gc1));
            text.Append(",\"gc2\":").Append(Value(value.gc2));
            text.Append(",\"gcObservationFrames\":").Append(Value(value.gcObservationFrames));
            text.Append(",\"hitchesWithGc\":").Append(Value(value.hitchesWithGc));
            text.Append(",\"droppedHitches\":").Append(Value(value.droppedHitches));
            text.Append(",\"frameTimingAvailable\":").Append(Value(value.frameTimingAvailable));
            text.Append(",\"managedHeapStart\":").Append(Value(value.managedHeapStart));
            text.Append(",\"managedHeapEnd\":").Append(Value(value.managedHeapEnd));
            text.Append(",\"logMessages\":").Append(Value(value.logMessages));
            text.Append(",\"warnings\":").Append(Value(value.warnings));
            text.Append(",\"errors\":").Append(Value(value.errors));
            text.Append(",\"otherLogPatterns\":").Append(Value(value.otherLogPatterns));
            text.Append(",\"durationSeconds\":").Append(Value(value.durationSeconds));
            text.Append(",\"sampledSeconds\":").Append(Value(value.sampledSeconds));
            text.Append(",\"warmupSeconds\":").Append(Value(value.warmupSeconds));
            text.Append(",\"instrumentationBookkeepingMs\":").Append(Value(value.instrumentationBookkeepingMs));
            text.Append(",\"sampleCapacityReached\":").Append(Value(value.sampleCapacityReached));
            text.Append(",\"frames\":").Append(Serialize(value.frames));
            text.Append(",\"cpuMainMs\":").Append(Serialize(value.cpuMainMs));
            text.Append(",\"gpuMs\":").Append(Serialize(value.gpuMs));
            text.Append(",\"allocatedKbPerFrame\":").Append(Serialize(value.allocatedKbPerFrame));
            text.Append(",\"hitches\":").Append(List(value.hitches, Serialize));
            text.Append(",\"phases\":").Append(List(value.phases, Serialize));
            text.Append(",\"bench\":").Append(Value(value.bench));
            text.Append(",\"mods\":").Append(List(value.mods, Serialize));
            text.Append(",\"featureSwitches\":").Append(List(value.featureSwitches, Value));
            text.Append(",\"scopes\":").Append(List(value.scopes, Serialize));
            text.Append(",\"markers\":").Append(List(value.markers, Serialize));
            text.Append(",\"repeatedLogs\":").Append(List(value.repeatedLogs, Serialize));
            text.Append(",\"limitations\":").Append(Value(value.limitations));
            return text.Append('}').ToString();
        }
        public static string Serialize(FrameSummary value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("{");
            text.Append("\"count\":").Append(Value(value.count));
            text.Append(",\"over16_67ms\":").Append(Value(value.over16_67ms));
            text.Append(",\"over33_33ms\":").Append(Value(value.over33_33ms));
            text.Append(",\"over50ms\":").Append(Value(value.over50ms));
            text.Append(",\"over100ms\":").Append(Value(value.over100ms));
            text.Append(",\"meanMs\":").Append(Value(value.meanMs));
            text.Append(",\"medianMs\":").Append(Value(value.medianMs));
            text.Append(",\"p95Ms\":").Append(Value(value.p95Ms));
            text.Append(",\"p99Ms\":").Append(Value(value.p99Ms));
            text.Append(",\"maxMs\":").Append(Value(value.maxMs));
            return text.Append('}').ToString();
        }
        public static string Serialize(ScopeSummary value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("{");
            text.Append("\"name\":").Append(Value(value.name));
            text.Append(",\"calls\":").Append(Value(value.calls));
            text.Append(",\"allocatedBytes\":").Append(Value(value.allocatedBytes));
            text.Append(",\"examined\":").Append(Value(value.examined));
            text.Append(",\"changed\":").Append(Value(value.changed));
            text.Append(",\"inclusiveMs\":").Append(Value(value.inclusiveMs));
            text.Append(",\"maxMs\":").Append(Value(value.maxMs));
            text.Append(",\"hitchTop\":").Append(Value(value.hitchTop));
            text.Append(",\"hitchTopMs\":").Append(Value(value.hitchTopMs));
            return text.Append('}').ToString();
        }
        public static string Serialize(Distribution value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("{");
            text.Append("\"count\":").Append(Value(value.count));
            text.Append(",\"mean\":").Append(Value(value.mean));
            text.Append(",\"median\":").Append(Value(value.median));
            text.Append(",\"p95\":").Append(Value(value.p95));
            text.Append(",\"p99\":").Append(Value(value.p99));
            text.Append(",\"max\":").Append(Value(value.max));
            return text.Append('}').ToString();
        }
        public static string Serialize(Hitch value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("{");
            text.Append("\"seconds\":").Append(Value(value.seconds));
            text.Append(",\"ms\":").Append(Value(value.ms));
            text.Append(",\"gc\":").Append(Value(value.gc));
            text.Append(",\"allocatedBytes\":").Append(Value(value.allocatedBytes));
            text.Append(",\"topScope\":").Append(Value(value.topScope));
            text.Append(",\"topScopeMs\":").Append(Value(value.topScopeMs));
            text.Append(",\"phases\":").Append(Value(value.phases));
            return text.Append('}').ToString();
        }
        public static string Serialize(PhaseSummary value)
        {
            if (value == null) return "null";
            return "{\"name\":" + Value(value.name) + ",\"ms\":" + Serialize(value.ms) + "}";
        }
        public static string Serialize(ModStamp value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("{");
            text.Append("\"name\":").Append(Value(value.name));
            text.Append(",\"configFingerprint\":").Append(Value(value.configFingerprint));
            text.Append(",\"configReadError\":").Append(Value(value.configReadError));
            return text.Append('}').ToString();
        }
        public static string Serialize(Marker value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("{");
            text.Append("\"seconds\":").Append(Value(value.seconds));
            text.Append(",\"label\":").Append(Value(value.label));
            return text.Append('}').ToString();
        }
        public static string Serialize(LogCount value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("{");
            text.Append("\"level\":").Append(Value(value.level));
            text.Append(",\"message\":").Append(Value(value.message));
            text.Append(",\"count\":").Append(Value(value.count));
            return text.Append('}').ToString();
        }
    }
}
