using System.Globalization;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Util;
using PugMod;
using QuickBuff.Systems;
using Unity.Entities;

namespace QuickBuff
{
    /// <summary>
    /// Server-side handler for "/quickbuff [skipActive:0|1] [skipSeconds]". The key press sends
    /// this through CoreLib's command RPC; it can also be typed in chat. Runs synchronously on the
    /// server main thread (inside CoreLib's CommandCommSystem update) and returns a one-line summary.
    /// </summary>
    public sealed class QuickBuffCommand : IServerCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            bool skipActive = QuickBuffMod.DefaultSkipActive;
            float skipSeconds = QuickBuffMod.DefaultSkipSeconds;

            if (parameters.Length >= 1)
                skipActive = parameters[0].Trim() != "0" && !parameters[0].Trim().Equals("false", System.StringComparison.OrdinalIgnoreCase);
            if (parameters.Length >= 2 && float.TryParse(parameters[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
                skipSeconds = seconds < 0f ? 0f : seconds;

            var world = API.Server.World;
            if (world == null || !world.IsCreated)
                return new CommandOutput("Quick Buff: server world not available.", CommandStatus.Error);

            Entity player = sender.GetPlayerEntity();
            if (player == Entity.Null)
                return new CommandOutput("Quick Buff: no player entity for this connection.", CommandStatus.Error);

            var system = world.GetExistingSystemManaged<QuickBuffServerSystem>() ?? world.GetOrCreateSystemManaged<QuickBuffServerSystem>();
            QuickBuffResult result = system.Consume(player, skipActive, skipSeconds);

            if (result.error != null)
                return new CommandOutput($"Quick Buff: {result.error}", CommandStatus.Warning);
            if (result.onCooldown)
                return new CommandOutput("Quick Buff: too soon, try again.", CommandStatus.Hint);

            if (result.consumed == 0)
            {
                if (result.skippedActive > 0)
                    return new CommandOutput($"Quick Buff: all {result.skippedActive} buff(s) still active.", CommandStatus.Info);
                return new CommandOutput("Quick Buff: no buff food or potions in inventory.", CommandStatus.Info);
            }

            string summary = $"Quick Buff: consumed {result.consumed} item(s)";
            if (result.skippedActive > 0) summary += $", {result.skippedActive} skipped (buff active)";
            return new CommandOutput(summary + ".", CommandStatus.Info);
        }

        public string GetDescription()
        {
            return "Consume one of every buff food and potion in your inventory. Usage: /quickbuff [skipActive 0|1] [skipSeconds]";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "quickbuff", "qb" };
        }
    }
}
