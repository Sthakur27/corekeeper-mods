using System.Globalization;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using Unity.Entities;

namespace SkillXPMultiplier
{
    /// <summary>
    /// Chat command mirror of the Mod Settings section.
    ///   /xp                 show every skill's multiplier
    ///   /xp mining 20       set one skill (snapped to the nearest menu choice)
    ///   /xp all 5           set every skill
    /// It runs on the server and changes the host's settings, so only the host can set values.
    /// </summary>
    public sealed class SkillXPCommand : IServerCommandHandler
    {
        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            bool host = MultiplayerSync.IsHostConnection(sender);
            string mode = host ? "" : MultiplayerSync.LocalEachPlayer
                ? " (host's values; the host lets each player pick their own in Settings > Mod Options)"
                : " (the host decides for everyone)";

            if (parameters.Length == 0)
                return "XP multipliers: " + SkillXPMultiplierMod.Describe() + mode;

            // Runs on the server, so it changes the host's settings: only the host may do that.
            if (!host)
                return MultiplayerSync.LocalEachPlayer
                    ? "Set your own multipliers in Settings > Mod Options > " + SkillXPMultiplierMod.Title + "."
                    : "Only the host can change XP multipliers on this world.";

            if (parameters.Length != 2)
                return "Usage: /xp <skill|all> <multiplier>   e.g. /xp mining 20, /xp all 1";

            string target = parameters[0];
            string valueText = parameters[1].Trim().TrimEnd('x', 'X');
            if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float wanted) || wanted < 0f)
                return "Multiplier must be a number from 0 to 100 (0 freezes the skill).";
            if (wanted > 100f) wanted = 100f;

            if (target.Trim().ToLowerInvariant() == "all")
            {
                string token = null;
                for (int i = 0; i < SkillXPTable.SkillCount; i++)
                    token = SkillXPMultiplierMod.SetSkill(i, wanted);
                return $"All skills set to {token}.";
            }

            int skill = SkillXPTable.FindSkill(target);
            if (skill < 0)
                return $"Unknown skill '{target}'. Skills: {string.Join(", ", SkillXPTable.SkillNames)}, all";

            string used = SkillXPMultiplierMod.SetSkill(skill, wanted);
            string note = SkillXPTable.Parse(used) != wanted ? $" (nearest available to {wanted:0.##}x)" : "";
            return $"{SkillXPTable.SkillNames[skill]} XP multiplier set to {used}{note}.";
        }

        public string GetDescription()
        {
            return "Per-skill XP multiplier. Usage: /xp | /xp <skill> <0-100> | /xp all <0-100>";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "xp" };
        }
    }
}
