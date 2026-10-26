using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Profiling;

namespace ModDiagnostics
{
    // Frame phases from the release build's profiler markers, the same ones the game's own
    // BenchmarkRunner reads. Values are the previous frame's time, which matches the interval
    // the recorder samples (IMod.Update runs inside the next frame's script update).
    public sealed class Probes : IDisposable
    {
        private readonly List<string> names = new List<string>();
        private readonly List<ProfilerRecorder> recorders = new List<ProfilerRecorder>();
        private readonly List<double[]> series = new List<double[]>();
        private int count;

        public Probes(int capacity)
        {
            var loop = new ProfilerCategory("PlayerLoop");
            Add("total", loop, "PlayerLoop", capacity);
            Add("scripts", loop, "BehaviourUpdate", capacity);
            Add("lateScripts", loop, "LateBehaviourUpdate", capacity);
            if (Manager.ecs != null && Manager.ecs.ClientWorld != null)
                Add("clientSim", loop, Manager.ecs.ClientWorld.Name + " Unity.Entities.SimulationSystemGroup", capacity);
            if (Manager.ecs != null && Manager.ecs.ServerWorld != null)
                Add("serverSim", loop, Manager.ecs.ServerWorld.Name + " Unity.Entities.SimulationSystemGroup", capacity);
            Add("renderCpu", loop, "UnityEngine.CoreModule.dll!UnityEngine.Rendering::RenderPipelineManager.DoRenderLoop_Internal() [Invoke]", capacity);
            Add("waitForTargetFps", new ProfilerCategory("VSync"), "WaitForTargetFPS", capacity);
        }

        private void Add(string name, ProfilerCategory category, string marker, int capacity)
        {
            try
            {
                var recorder = ProfilerRecorder.StartNew(category, marker);
                if (!recorder.Valid) { recorder.Dispose(); return; }
                names.Add(name); recorders.Add(recorder); series.Add(new double[capacity]);
            }
            catch (Exception) { }
        }

        public int Count => names.Count;

        public void Sample()
        {
            if (count >= (series.Count == 0 ? 0 : series[0].Length)) return;
            for (int i = 0; i < recorders.Count; i++) series[i][count] = recorders[i].LastValue / 1e6;
            count++;
        }

        // Compact "name=ms" list of the last sample, for the hitch log.
        public string Describe()
        {
            if (count == 0) return "";
            var text = new StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0) text.Append(' ');
                text.Append(names[i]).Append('=').Append(series[i][count - 1].ToString("F1", CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        public List<PhaseSummary> Summaries()
        {
            var result = new List<PhaseSummary>();
            for (int i = 0; i < names.Count; i++)
                result.Add(new PhaseSummary { name = names[i], ms = Distribution.From(series[i], count) });
            return result;
        }

        public void Dispose()
        {
            foreach (var recorder in recorders) recorder.Dispose();
            recorders.Clear();
        }
    }
}
