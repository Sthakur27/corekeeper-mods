// Compile-check stand-ins for source-only dependencies (CoreLib, ModSettingsMenu). Real API names.
using System;
using Unity.Entities;

namespace ModSettingsMenu.Settings
{
    public enum SliderDisplay { Steps, Number, Percent }
    public sealed class SettingHandle<T> { public T Value { get; set; } public event Action<T> OnChanged; }
    public sealed class SectionBuilder
    {
        public SectionBuilder Hint(string text) => this;
        public SectionBuilder Toggle(out SettingHandle<bool> handle, string key, bool def) { handle = new SettingHandle<bool>(); return this; }
        public SectionBuilder Slider(out SettingHandle<float> handle, string key, float min, float max, float def, float step, SliderDisplay display = SliderDisplay.Steps) { handle = new SettingHandle<float>(); return this; }
        public SectionBuilder Choice<T>(out SettingHandle<T> handle, string key, T[] values, T def) { handle = new SettingHandle<T>(); return this; }
        public SectionBuilder Stepper(out SettingHandle<int> handle, string key, int min, int max, int def) { handle = new SettingHandle<int>(); return this; }
        public void Build() { }
    }
    public static class ModSettings { public static SectionBuilder Section(PugMod.IMod consumer) => new SectionBuilder(); }
}

namespace CoreLib
{
    public class CompileLogger { public void LogWarning(object message) {} public void LogError(object message) {} }
    public static class CoreLibMod { public static CompileLogger log = new CompileLogger(); public static void LoadSubmodule(params Type[] moduleTypes) { } }
}

namespace CoreLib.Submodule.ControlMapping
{
    public class ControlMappingModule
    {
        public static Action rewiredStart;
        public static void AddKeyboardBind(string keyBindName = "", Rewired.KeyboardKeyCode defaultKeyCode = Rewired.KeyboardKeyCode.None,
            Rewired.ModifierKey modifier = Rewired.ModifierKey.None, Rewired.ModifierKey modifier2 = Rewired.ModifierKey.None,
            Rewired.ModifierKey modifier3 = Rewired.ModifierKey.None, int categoryId = -1) { }
        public static int AddNewCategory(string categoryName) => 0;
    }
}

namespace CoreLib.Submodule.Command.Data
{
    public enum CommandStatus : byte { None, Info, Hint, Warning, Error }
    [Flags] public enum CommandFlags : byte { None = 0, UserWantsHints = 1, SentFromQuantumConsole = 2 }
    public struct CommandOutput
    {
        public string feedback; public CommandStatus status;
        public CommandOutput(string feedback) { this.feedback = feedback; status = CommandStatus.Info; }
        public CommandOutput(string feedback, CommandStatus status) { this.feedback = feedback; this.status = status; }
        public static implicit operator CommandOutput(string d) => new CommandOutput(d);
    }
}

namespace CoreLib.Submodule.Command.Interface
{
    public interface ICommandInfo { string GetDescription(); string[] GetTriggerNames(); }
    public interface IServerCommandHandler : ICommandInfo { Data.CommandOutput Execute(string[] parameters, Entity sender); }
}

namespace CoreLib.Submodule.Command.System
{
    public partial class CommandCommSystem : PugSimulationSystemBase
    {
        public void SendCommand(string message, Data.CommandFlags commandFlags) { } public void SendResponse(string message, Data.CommandStatus status, Data.CommandFlags commandFlags = Data.CommandFlags.None, Entity targetConnection = default) {}
        protected override void OnUpdate() { }
    }
}

namespace CoreLib.Submodule.Command
{
    public class CommandModule
    {
        public static System.CommandCommSystem ClientCommSystem => null;
        public static void AddCommands(long modId, string modName) { }
    }
}

namespace CoreLib.Submodule.Command.Util
{
    public static class CommandUtil { public static Entity GetPlayerEntity(this Entity sender) => Entity.Null; }
}

