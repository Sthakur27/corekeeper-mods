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
    /// Server-side handler for "/quickfood [skipActive 0|1] [skipSeconds] [cookedOnly 0|1] [skipFish 0|1]"
    /// (sent by the Quick Food key, default F): Quick Buff for food only. Eats one of every buff food
    /// (potions and Caveling Coffee excluded), skipping foods whose buffs are all still active, and
    /// optionally raw food and fish dishes. Hunger is ignored.
    /// </summary>
    public sealed class QuickFoodCommand : IServerCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            bool skipActive = parameters.Length >= 1 ? Flag(parameters[0]) : QuickBuffMod.DefaultSkipActive;
            float skipSeconds = QuickBuffMod.DefaultSkipSeconds;
            if (parameters.Length >= 2 && float.TryParse(parameters[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
                skipSeconds = seconds < 0f ? 0f : seconds;
            bool cookedOnly = parameters.Length >= 3 ? Flag(parameters[2]) : QuickBuffMod.DefaultFoodCookedOnly;
            bool skipFish = parameters.Length >= 4 ? Flag(parameters[3]) : QuickBuffMod.DefaultFoodSkipFish;

            var world = API.Server.World;
            if (world == null || !world.IsCreated)
                return new CommandOutput("Quick Food: server world not available.", CommandStatus.Error);

            Entity player = sender.GetPlayerEntity();
            if (player == Entity.Null)
                return new CommandOutput("Quick Food: no player entity for this connection.", CommandStatus.Error);

            var system = world.GetExistingSystemManaged<QuickBuffServerSystem>() ?? world.GetOrCreateSystemManaged<QuickBuffServerSystem>();
            QuickBuffResult result = system.Consume(player, skipActive, skipSeconds, ConsumeMode.Food, cookedOnly, skipFish);

            if (result.error != null)
                return new CommandOutput($"Quick Food: {result.error}", CommandStatus.Warning);
            if (result.onCooldown)
                return new CommandOutput("Quick Food: too soon, try again.", CommandStatus.Hint);
            if (result.consumed == 0)
            {
                if (result.skippedActive > 0)
                    return new CommandOutput($"Quick Food: all {result.skippedActive} food buff(s) still active.", CommandStatus.Info);
                return new CommandOutput("Quick Food: no buff food in inventory.", CommandStatus.Info);
            }
            string summary = $"Quick Food: ate {result.consumed} item(s)";
            if (result.skippedActive > 0) summary += $", {result.skippedActive} skipped (buff active)";
            return new CommandOutput(summary + ".", CommandStatus.Info);
        }

        private static bool Flag(string value)
        {
            value = value.Trim();
            return value != "0" && !value.Equals("false", System.StringComparison.OrdinalIgnoreCase);
        }

        public string GetDescription()
        {
            return "Eat one of every buff food in your inventory (no potions, no Caveling Coffee). Usage: /quickfood [skipActive 0|1] [skipSeconds] [cookedOnly 0|1] [skipFish 0|1]";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "quickfood", "qf" };
        }
    }
}
