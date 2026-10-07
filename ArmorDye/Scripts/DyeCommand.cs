using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Util;
using PugMod;
using Unity.Entities;

namespace ArmorDye
{
    /// <summary>
    /// "/dye helm|chest|pants|all COLOR" (COLOR = red, #3080ff, shift 120, off) and "/dye info". Runs on the server.
    /// </summary>
    public sealed class DyeCommand : IServerCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            var world = API.Server.World;
            if (world == null || !world.IsCreated)
                return new CommandOutput("Armor Dye: server world not available.", CommandStatus.Error);
            Entity player = sender.GetPlayerEntity();
            if (player == Entity.Null)
                return new CommandOutput("Armor Dye: no player entity for this connection.", CommandStatus.Error);
            if (parameters.Length == 0)
                return new CommandOutput(GetDescription(), CommandStatus.Hint);

            var em = world.EntityManager;
            string sub = parameters[0].ToLowerInvariant();
            if (sub == "info")
                return new CommandOutput("Armor Dye: " + DyeServer.Info(em, player), CommandStatus.Info);

            // "/dye slot N COLOR": N = absolute index in the player's inventory buffer (sent by the dye bucket UI).
            if (sub == "slot")
            {
                if (parameters.Length < 3 || !int.TryParse(parameters[1], out int slot) || !DyeColor.TryParse(parameters, 2, out int slotDye))
                    return new CommandOutput(GetDescription(), CommandStatus.Hint);
                return new CommandOutput("Armor Dye: " + DyeServer.SetDyeAt(em, player, slot, slotDye), CommandStatus.Info);
            }

            if (!DyeColor.TryParse(parameters, 1, out int dye))
                return new CommandOutput(GetDescription(), CommandStatus.Hint);

            string result;
            switch (sub)
            {
                case "helm": case "head": case "helmet":
                    result = DyeServer.SetDye(em, player, ArmorPiece.Helm, dye);
                    break;
                case "chest": case "breast": case "body":
                    result = DyeServer.SetDye(em, player, ArmorPiece.Chest, dye);
                    break;
                case "pants": case "legs":
                    result = DyeServer.SetDye(em, player, ArmorPiece.Pants, dye);
                    break;
                case "hand": case "held": case "weapon": case "tool":
                    result = DyeServer.SetHeld(em, player, dye);
                    break;
                case "all":
                    result = DyeServer.SetDye(em, player, ArmorPiece.Helm, dye) + " "
                           + DyeServer.SetDye(em, player, ArmorPiece.Chest, dye) + " "
                           + DyeServer.SetDye(em, player, ArmorPiece.Pants, dye);
                    break;
                default:
                    return new CommandOutput(GetDescription(), CommandStatus.Hint);
            }
            return new CommandOutput("Armor Dye: " + result, CommandStatus.Info);
        }

        public string GetDescription()
        {
            return "Dye your displayed armor or the item in your hand. /dye <helm|chest|pants|all|hand> <red|blue|...|#rrggbb|shift <degrees>|off> | /dye info";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "dye", "armordye" };
        }
    }
}
