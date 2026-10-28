using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Util;
using PugMod;
using QuickBuff.Systems;
using Unity.Entities;

namespace QuickBuff
{
    /// <summary>
    /// Server-side handler for "/quickmana" (sent by the Quick Mana key, default J): drinks the first
    /// mana potion in the main inventory, hotbar first. Does nothing at full mana.
    /// </summary>
    public sealed class QuickManaCommand : IServerCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            var world = API.Server.World;
            if (world == null || !world.IsCreated)
                return new CommandOutput("Quick Mana: server world not available.", CommandStatus.Error);

            Entity player = sender.GetPlayerEntity();
            if (player == Entity.Null)
                return new CommandOutput("Quick Mana: no player entity for this connection.", CommandStatus.Error);

            var system = world.GetExistingSystemManaged<QuickBuffServerSystem>() ?? world.GetOrCreateSystemManaged<QuickBuffServerSystem>();
            QuickBuffResult result = system.Consume(player, false, 0f, ConsumeMode.Mana);

            if (result.error != null)
                return new CommandOutput($"Quick Mana: {result.error}", CommandStatus.Warning);
            if (result.onCooldown)
                return new CommandOutput("Quick Mana: too soon, try again.", CommandStatus.Hint);
            if (result.fullMana)
                return new CommandOutput("Quick Mana: mana is already full.", CommandStatus.Hint);
            if (result.consumed == 0)
                return new CommandOutput("Quick Mana: no mana potions in inventory.", CommandStatus.Info);
            return new CommandOutput($"Quick Mana: drank {result.healItem}.", CommandStatus.Info);
        }

        public string GetDescription()
        {
            return "Drink the first mana potion in your inventory. Usage: /quickmana";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "quickmana", "qm" };
        }
    }
}
