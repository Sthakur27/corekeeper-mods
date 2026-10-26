using System;
using System.Linq;
using CoreLib;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using PugMod;

namespace ModDiagnostics
{
    public sealed class DiagnosticsMod : IMod
    {
        public void EarlyInit()
        {
            CoreLibMod.LoadSubmodule(typeof(CommandModule));
            var owner = API.ModLoader.LoadedMods.FirstOrDefault(m => m.Handlers.Contains(this));
            if (owner != null) CommandModule.AddCommands(owner.ModId, "Mod Diagnostics");
        }
        public void Init() { }
        public void Update() { if (Recorder.Active) Recorder.SampleFrame(); }
        public void Shutdown() { if (Recorder.Active) Recorder.Stop("shutdown"); }
        public void ModObjectLoaded(UnityEngine.Object obj) { }
    }

    public sealed class PerfCommand : IClientCommandHandler
    {
        public string[] GetTriggerNames() => new[] { "modperf" };
        public string GetDescription() => "Local diagnostics: /modperf start label [seconds 5-300], stop, status, export, or mark label.";
        public CommandOutput Execute(string[] args)
        {
            if (args.Length == 0 || args[0] == "status")
                return new CommandOutput(Recorder.Active ? "Recording: " + Recorder.Label : "Recorder is off. " + Recorder.LastReport);
            if (args[0] == "stop") return new CommandOutput(Recorder.Stop("manual"));
            if (args[0] == "export") return new CommandOutput(Recorder.Export());
            if (args[0] == "mark" && args.Length == 2) { Recorder.Mark(args[1]); return new CommandOutput("Marker added if recording."); }
            if (args[0] == "start" && (args.Length == 2 || args.Length == 3))
            {
                int seconds = 60;
                if (args.Length == 3 && !int.TryParse(args[2], out seconds)) return new CommandOutput("Seconds must be an integer.");
                if (seconds < 5 || seconds > 300) return new CommandOutput("Choose 5-300 seconds.");
                try { return new CommandOutput(Recorder.Start(args[1], seconds)); }
                catch (Exception e) { return new CommandOutput("Recorder could not start: " + e.Message, CommandStatus.Error); }
            }
            return new CommandOutput(GetDescription());
        }
    }
}
