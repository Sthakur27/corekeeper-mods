using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Util;
using PugMod;
using QuickBuff.Systems;
using Unity.Entities;

namespace QuickBuff
{
    /// <summary>
    /// Server-side handler for "/quickheal" (sent by the Quick Heal key, default H): drinks the first
    /// healing potion in the main inventory, hotbar first. Does nothing at full health.
    /// </summary>
    public sealed class QuickHealCommand : IServerCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            var world = API.Server.World;
            if (world == null || !world.IsCreated)
                return new CommandOutput("Quick Heal: server world not available.", CommandStatus.Error);

            Entity player = sender.GetPlayerEntity();
            if (player == Entity.Null)
                return new CommandOutput("Quick Heal: no player entity for this connection.", CommandStatus.Error);

            var system = world.GetExistingSystemManaged<QuickBuffServerSystem>() ?? world.GetOrCreateSystemManaged<QuickBuffServerSystem>();
            QuickBuffResult result = system.Consume(player, false, 0f, ConsumeMode.Heal);

            if (result.error != null)
                return new CommandOutput($"Quick Heal: {result.error}", CommandStatus.Warning);
            if (result.onCooldown)
                return new CommandOutput("Quick Heal: too soon, try again.", CommandStatus.Hint);
            if (result.fullHealth)
                return new CommandOutput("Quick Heal: already at full health.", CommandStatus.Hint);
            if (result.consumed == 0)
                return new CommandOutput("Quick Heal: no healing potions in inventory.", CommandStatus.Info);
            return new CommandOutput($"Quick Heal: drank {result.healItem}.", CommandStatus.Info);
        }

        public string GetDescription()
        {
            return "Drink the first healing potion in your inventory. Usage: /quickheal";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "quickheal", "qh" };
        }
    }
}
