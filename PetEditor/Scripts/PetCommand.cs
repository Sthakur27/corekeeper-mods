using System;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Util;
using PugMod;
using Unity.Entities;

namespace PetEditor
{
    /// <summary>
    /// "/pet level N", "/pet talent SLOT TALENT", "/pet unpoint SLOT" (slots 1-9, TALENT = name or number).
    /// The pet window buttons send these; they can also be typed in chat. Runs on the server.
    /// </summary>
    public sealed class PetCommand : IServerCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            var world = API.Server.World;
            if (world == null || !world.IsCreated)
                return new CommandOutput("Pet Editor: server world not available.", CommandStatus.Error);
            Entity player = sender.GetPlayerEntity();
            if (player == Entity.Null)
                return new CommandOutput("Pet Editor: no player entity for this connection.", CommandStatus.Error);
            if (parameters.Length == 0)
                return new CommandOutput(GetDescription(), CommandStatus.Hint);

            var em = world.EntityManager;
            string sub = parameters[0].ToLowerInvariant();
            string result;
            if (sub == "level" && parameters.Length >= 2 && int.TryParse(parameters[1], out int level))
                result = PetServer.SetLevel(em, player, level);
            else if (sub == "talent" && parameters.Length >= 3 && int.TryParse(parameters[1], out int slot) && TryParseTalent(parameters[2], out PetTalent talent))
                result = PetServer.SetTalent(em, player, slot - 1, talent);
            else if (sub == "color" && parameters.Length >= 2 && int.TryParse(parameters[1], out int color))
                result = PetServer.SetColor(em, player, color - 1);
            else if (sub == "unpoint" && parameters.Length >= 2 && int.TryParse(parameters[1], out int slot2))
                result = PetServer.RemovePoint(em, player, slot2 - 1);
            else
                return new CommandOutput(GetDescription(), CommandStatus.Hint);
            return new CommandOutput("Pet Editor: " + result, CommandStatus.Info);
        }

        private static bool TryParseTalent(string s, out PetTalent talent)
        {
            if (int.TryParse(s, out int n) && Enum.IsDefined(typeof(PetTalent), n))
            {
                talent = (PetTalent)n;
                return true;
            }
            return Enum.TryParse(s, true, out talent) && Enum.IsDefined(typeof(PetTalent), talent);
        }

        public string GetDescription()
        {
            return "Edit your equipped pet. /pet level <1-10> | /pet talent <slot 1-9> <talent name or id> | /pet color <n> | /pet unpoint <slot 1-9>";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "pet", "peteditor" };
        }
    }
}
