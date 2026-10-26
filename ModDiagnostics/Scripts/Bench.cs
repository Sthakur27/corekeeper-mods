using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PlayerState;
using PugMod;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModDiagnostics
{
    // Unattended benchmark, driven by release/performance/bench_runner.py.
    // If ModDiagnostics/bench.txt exists at startup: start the given world and character from the
    // title screen, replay a recorded route in NoClip (the same way the game's own ExploreWorld
    // benchmark moves the player), capture with the recorder, write ModDiagnostics/bench-status.txt
    // and quit. Without the control file nothing here runs.
    public static class Bench
    {
        private const string ControlPath = "ModDiagnostics/bench.txt";
        private const string StatusPath = "ModDiagnostics/bench-status.txt";
        private const string RouteFolder = "ModDiagnostics/routes";
        private enum Phase { Off, WaitTitle, Loading, Settle, Run, Quit }

        private static Phase phase;
        private static bool checkedControl;
        private static Dictionary<string, string> config;
        private static List<Vector4> route; // x, y, z, seconds since route start
        private static double phaseAt, stableSince = -1, runAt;
        private static bool recording, captureStarted;
        private static string recordingName;
        private static readonly List<Vector4> recorded = new List<Vector4>();
        private static double recordStart, recordLast;

        public static bool Recording => recording;

        public static void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (!checkedControl) { checkedControl = true; TryStartFromControl(now); }
            if (recording) SampleRoute(now);
            if (phase == Phase.Off) return;
            try { Step(now); }
            catch (Exception e) { Finish("error", e.Message); } // GetType().Name is reflection: blocked by the loader
        }

        private static void TryStartFromControl(double now)
        {
            try
            {
                if (!API.ConfigFilesystem.FileExists(ControlPath)) return;
                config = ParseKeyValues(Encoding.UTF8.GetString(API.ConfigFilesystem.Read(ControlPath)));
                route = LoadRoute(Get("route", "base"));
                if (route == null || route.Count < 2) { Finish("error", "route not found or too short: " + Get("route", "base")); return; }
                Recorder.BenchInfo = "route=" + Get("route", "base") + " world=" + Get("world", "?") + " character=" + Get("character", "?") + " config=" + Get("config", "");
                phase = Phase.WaitTitle; phaseAt = now;
                WriteStatus("waiting-title", "");
            }
            catch (Exception e) { Finish("error", "control file: " + e.Message); }
        }

        private static void Step(double now)
        {
            switch (phase)
            {
                case Phase.WaitTitle:
                    if (now - phaseAt > 180) { Finish("error", "title screen never became ready"); return; }
                    bool ready = SceneManager.GetActiveScene().name == "Title" && Manager.load != null && !Manager.load.IsLoading()
                        && Manager.saves != null && Manager.menu != null && !Manager.menu.isStartingGame;
                    if (!ready) { stableSince = -1; return; }
                    if (stableSince < 0) stableSince = now;
                    if (now - stableSince < 3) return;
                    SaveSlotPlayOption.StartGameFromActivity(GetInt("world", 0), GetInt("character", 0));
                    phase = Phase.Loading; phaseAt = now;
                    WriteStatus("loading", "");
                    return;
                case Phase.Loading:
                    if (now - phaseAt > 300) { Finish("error", "world did not finish loading"); return; }
                    var player = Manager.main == null ? null : Manager.main.player;
                    if (player == null || Manager.load.IsLoading() || SceneManager.GetActiveScene().name != "Main") return;
                    // NoClip and no input, exactly like BenchmarkRunner.ExploreWorld.Setup.
                    player.playerCommandSystem.SetPlayerState(player.entity, PlayerStateEnum.NoClip, locked: true);
                    Manager.input.DisableInput();
                    Place(player, 0);
                    phase = Phase.Settle; phaseAt = now;
                    WriteStatus("settling", "");
                    return;
                case Phase.Settle:
                    Place(Manager.main.player, 0);
                    if (now - phaseAt < GetInt("settle", 20)) return;
                    Recorder.Start(Get("label", "bench"), GetInt("seconds", 60));
                    if (!Recorder.Active) { Finish("error", "recorder did not start: " + Recorder.LastReport); return; }
                    captureStarted = true;
                    phase = Phase.Run; runAt = now;
                    WriteStatus("running", "");
                    return;
                case Phase.Run:
                    if (Recorder.Active) { Place(Manager.main.player, now - runAt); return; }
                    Finish(string.IsNullOrEmpty(Recorder.LastReportPath) ? "error" : "done", Recorder.LastReport);
                    return;
                case Phase.Quit:
                    if (now - phaseAt > 2 && GetInt("quit", 1) == 1) Application.Quit();
                    return;
            }
        }

        // Moves along the route at the recorded pace, back and forth so the player never jumps.
        private static void Place(PlayerController player, double seconds)
        {
            if (player == null) return;
            double length = route[route.Count - 1].w;
            double t = length <= 0 ? 0 : seconds % (2 * length);
            if (t > length) t = 2 * length - t;
            int i = 1;
            while (i < route.Count - 1 && route[i].w < t) i++;
            Vector4 a = route[i - 1], b = route[i];
            float u = b.w > a.w ? (float)((t - a.w) / (b.w - a.w)) : 0f;
            var p = Vector4.Lerp(a, b, Mathf.Clamp01(u));
            player.SetPlayerPosition(new float3(p.x, p.y, p.z));
        }

        private static void Finish(string state, string detail)
        {
            if (captureStarted && Recorder.Active) Recorder.Stop("bench-" + state);
            try
            {
                var player = Manager.main == null ? null : Manager.main.player;
                if (player != null) player.playerCommandSystem.UnlockCurrentState(player.entity);
                if (Manager.input != null) Manager.input.EnableInput();
            }
            catch (Exception) { }
            WriteStatus(state, detail);
            Debug.Log("[ModDiagnostics] Bench " + state + ": " + detail);
            phase = Phase.Quit; phaseAt = Time.realtimeSinceStartupAsDouble;
        }

        private static void WriteStatus(string state, string detail)
        {
            try
            {
                API.ConfigFilesystem.CreateDirectory("ModDiagnostics");
                string text = "state=" + state + "\nreport=" + Recorder.LastReportPath + "\nlabel=" + Get("label", "")
                    + "\ndetail=" + (detail ?? "").Replace('\n', ' ') + "\n";
                API.ConfigFilesystem.Write(StatusPath, Encoding.UTF8.GetBytes(text));
            }
            catch (Exception) { }
        }

        // /modperf route record <name>: walk the loop normally, then /modperf route stop.
        public static string StartRecording(string name)
        {
            if (recording) return "Already recording route " + recordingName + ".";
            if (Manager.main == null || Manager.main.player == null) return "Load a world first.";
            recordingName = SafeName(name); recorded.Clear();
            recordStart = recordLast = Time.realtimeSinceStartupAsDouble; recording = true;
            Add(Manager.main.player.WorldPosition, 0);
            return "Recording route " + recordingName + ". Walk your loop, then /modperf route stop.";
        }

        public static string StopRecording()
        {
            if (!recording) return "No route is being recorded.";
            recording = false;
            if (recorded.Count < 2) return "Route too short; nothing saved.";
            var text = new StringBuilder("# seconds x y z\n");
            foreach (var p in recorded)
                text.Append(p.w.ToString("F3", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(p.x.ToString("F3", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(p.y.ToString("F3", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(p.z.ToString("F3", CultureInfo.InvariantCulture)).Append('\n');
            API.ConfigFilesystem.CreateDirectory(RouteFolder);
            string path = RouteFolder + "/" + recordingName + ".txt";
            API.ConfigFilesystem.Write(path, Encoding.UTF8.GetBytes(text.ToString()));
            return "Saved " + path + " (" + recorded.Count + " points, " + recorded[recorded.Count - 1].w.ToString("F0", CultureInfo.InvariantCulture) + " s).";
        }

        private static void SampleRoute(double now)
        {
            if (now - recordLast < 0.1) return;
            var player = Manager.main == null ? null : Manager.main.player;
            if (player == null) return;
            recordLast = now;
            Add(player.WorldPosition, now - recordStart);
        }

        private static void Add(Vector3 position, double seconds) => recorded.Add(new Vector4(position.x, position.y, position.z, (float)seconds));

        private static List<Vector4> LoadRoute(string name)
        {
            string path = RouteFolder + "/" + SafeName(name) + ".txt";
            if (!API.ConfigFilesystem.FileExists(path)) return null;
            var points = new List<Vector4>();
            foreach (string line in Encoding.UTF8.GetString(API.ConfigFilesystem.Read(path)).Split('\n'))
            {
                if (line.StartsWith("#")) continue;
                var parts = line.Trim().Split(' ');
                if (parts.Length != 4) continue;
                points.Add(new Vector4(Parse(parts[1]), Parse(parts[2]), Parse(parts[3]), Parse(parts[0])));
            }
            return points;
        }

        private static float Parse(string text) => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static Dictionary<string, string> ParseKeyValues(string text)
        {
            var result = new Dictionary<string, string>();
            foreach (string line in text.Split('\n'))
            {
                int eq = line.IndexOf('=');
                if (eq > 0) result[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            return result;
        }

        private static string Get(string key, string fallback) => config != null && config.TryGetValue(key, out var v) ? v : fallback;
        private static int GetInt(string key, int fallback) => int.TryParse(Get(key, ""), out int v) ? v : fallback;

        private static string SafeName(string name)
        {
            var result = new StringBuilder();
            foreach (char c in name ?? "")
                if (result.Length < 32 && (char.IsLetterOrDigit(c) || c == '-' || c == '_')) result.Append(c);
            return result.Length == 0 ? "route" : result.ToString();
        }
    }
}
