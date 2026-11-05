using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Util;
using PugMod;
using Unity.Entities;

namespace Enchanting
{
    /// <summary>
    /// Debug/admin command, runs on the server. Target = the held item unless the last word is helm, chest, pants,
    /// ring1, ring2, necklace or offhand.
    ///   /enchant                      show the target's enchants
    ///   /enchant list                 all enchants (keys, slots, values)
    ///   /enchant set &lt;key&gt; &lt;lvl&gt;     add or change one enchant (lvl 0 removes it)
    ///   /enchant remove &lt;key&gt;         remove one enchant
    ///   /enchant clear                remove all enchants
    ///   /enchant roll [n]             n free random rolls (default 1)
    ///   /enchant cost                 what the next table roll would cost
    /// </summary>
    public sealed class EnchantCommand : IServerCommandHandler
    {
        private enum Target { Held, Helm, Chest, Pants, Ring1, Ring2, Necklace, Offhand }

        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            var world = API.Server.World;
            if (world == null || !world.IsCreated) return Error("server world not available.");
            Entity player = sender.GetPlayerEntity();
            if (player == Entity.Null) return Error("no player entity for this connection.");
            var em = world.EntityManager;

            var args = parameters.ToList();
            Target target = Target.Held;
            if (args.Count > 0 && TryTarget(args[args.Count - 1], out var t))
            {
                target = t;
                args.RemoveAt(args.Count - 1);
            }
            string sub = args.Count > 0 ? args[0].ToLowerInvariant() : "show";

            if (sub == "list") return Info(ListAll());
            if (sub == "stats") return Info(Stats(em, player));
            if (sub == "selftest")
            {
                if (args.Count > 1 && args[1].ToLowerInvariant() == "status") return Info(EnchantSelfTestSystem.Status());
                if (!IsAdmin(em, player)) return Error("only admins can run the selftest.");
                return Info(EnchantSelfTestSystem.Start(em, player));
            }
            if (sub == "combattest")
            {
                if (args.Count > 1 && args[1].ToLowerInvariant() == "status") return Info(EnchantCombatTestSystem.Status());
                if (!IsAdmin(em, player)) return Error("only admins can run the combat test.");
                float seconds = 20f;
                if (args.Count > 1) float.TryParse(args[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
                return Info(EnchantCombatTestSystem.Start(em, player, seconds <= 0 ? 20f : seconds));
            }
            if (sub == "debug")
            {
                EnchantDebug.On = args.Count < 2 || args[1].ToLowerInvariant() != "off";
                return Info(EnchantDebug.On
                    ? "debug on: hits and durability loss are recorded per item. Hit things, then /enchant report."
                    : "debug off.");
            }
            if (sub == "report") return Info(EnchantDebug.Report() + " Stats now: " + Stats(em, player));
            if (sub == "reset")
            {
                EnchantDebug.Reset();
                return Info("debug measurements cleared.");
            }
            // Sent by the Enchanting Table button (EnchantTableUI): /enchant table <x> <z>
            if (sub == "table")
            {
                if (args.Count < 3 || !int.TryParse(args[1], out int x) || !int.TryParse(args[2], out int z)) return Hint();
                return Info(EnchantTableServer.Roll(em, player, x, z));
            }
            // Sent by the table's remove button (EnchantRemoveButton): /enchant tableclear <x> <z>
            if (sub == "tableclear")
            {
                if (args.Count < 3 || !int.TryParse(args[1], out int x) || !int.TryParse(args[2], out int z)) return Hint();
                return Info(EnchantTableServer.Clear(em, player, x, z));
            }

            if (!TryGetSlot(em, player, target, out int slot, out string err)) return Error(err);
            if (!EnchantStorage.TryGetAux(em, out var aux)) return Error("inventory aux data not ready.");
            var item = em.GetBuffer<ContainedObjectsBuffer>(player)[slot];
            var kind = Enchants.SlotOf(item.objectID);
            if (kind == EnchantSlot.None) return Error($"{item.objectID} can't be enchanted.");
            var current = EnchantStorage.Read(em, aux, item);

            if (sub == "show" || sub == "info")
                return Info($"{item.objectID}: {EnchantStorage.Describe(current)}. Pool: {string.Join(", ", Enchants.PoolFor(kind).Select(d => d.Key))}");
            if (sub == "cost")
            {
                EnchantRoll.Cost(current.Count, out int gold, out int coins);
                return Info($"next roll on {item.objectID}: {gold} Gold Bars + {coins} Ancient Coins.");
            }

            if (!IsAdmin(em, player)) return Error("only admins can change enchants with commands.");

            string message;
            switch (sub)
            {
                case "set":
                {
                    if (args.Count < 3 || !int.TryParse(args[2], out int level)) return Hint();
                    var def = Enchants.Find(args[1]);
                    if (def == null) return Error($"unknown enchant '{args[1]}'. Try /enchant list.");
                    if ((def.Slots & kind) == 0) message = $"note: {def.Name} is not in the {kind} pool, set anyway. ";
                    else message = "";
                    int idx = EnchantRoll.IndexOf(current, def.Id);
                    if (level <= 0)
                    {
                        if (idx >= 0) current.RemoveAt(idx);
                    }
                    else
                    {
                        level = System.Math.Min(level, def.MaxLevel);
                        var e = new EnchantEntry { Id = def.Id, Level = level };
                        if (idx >= 0) current[idx] = e;
                        else current.Add(e);
                    }
                    break;
                }
                case "remove":
                {
                    if (args.Count < 2) return Hint();
                    var def = Enchants.Find(args[1]);
                    if (def == null) return Error($"unknown enchant '{args[1]}'.");
                    int idx = EnchantRoll.IndexOf(current, def.Id);
                    if (idx >= 0) current.RemoveAt(idx);
                    message = "";
                    break;
                }
                case "clear":
                    current.Clear();
                    message = "";
                    break;
                case "roll":
                {
                    int n = 1;
                    if (args.Count > 1 && int.TryParse(args[1], out int parsed)) n = System.Math.Max(1, System.Math.Min(parsed, 50));
                    var lines = new List<string>();
                    for (int i = 0; i < n; i++) lines.Add(EnchantRoll.Roll(current, kind) ?? "nothing to roll.");
                    message = string.Join(" ", lines) + " ";
                    break;
                }
                default:
                    return Hint();
            }

            string writeErr = EnchantStorage.Write(em, player, slot, current);
            if (writeErr != null) return Error(writeErr);
            return Info($"{message}{item.objectID}: {EnchantStorage.Describe(current)}.");
        }

        private static bool IsAdmin(EntityManager em, Entity player)
        {
            return em.HasComponent<PlayerGhost>(player) && em.GetComponentData<PlayerGhost>(player).adminPrivileges >= 1;
        }

        /// <summary>
        /// What the stat system actually has: our ConditionsBuffer entries (raw values) and the player's summed
        /// value for each of those conditions (all sources: gear, skills, food, enchants).
        /// </summary>
        private static string Stats(EntityManager em, Entity player)
        {
            if (!em.HasBuffer<ConditionsBuffer>(player)) return "no conditions on the player.";
            var conditions = em.GetBuffer<ConditionsBuffer>(player, true);
            bool hasSum = em.HasBuffer<SummarizedConditionsBuffer>(player);
            var parts = new List<string>();
            for (int i = 0; i < conditions.Length; i++)
            {
                var c = conditions[i].condition;
                if (!EnchantEffectSystem.IsOurs(c)) continue;
                var id = c.conditionData.conditionID;
                string total = "?";
                if (hasSum)
                {
                    var sum = em.GetBuffer<SummarizedConditionsBuffer>(player, true);
                    if ((int)id < sum.Length) total = sum[(int)id].value.ToString();
                }
                parts.Add($"{id} enchant raw {c.conditionData.value}, total raw {total}");
            }
            string text = parts.Count == 0 ? "no enchant bonuses active (hold or wear an enchanted item)." : string.Join("; ", parts);
            UnityEngine.Debug.Log($"[{EnchantingMod.Name}] stats: {text}");
            return text;
        }

        private static string ListAll()
        {
            return string.Join("; ", Enchants.All.Select(d => $"{d.Key} [{d.Slots}] {d.Condition} {Describe(d)}{(d.Stack == EnchantStack.Max ? " (highest copy counts)" : "")}"));
        }

        // Flat: displayed values; Relative: multiplier per level; ItemLevel: percent of the vanilla value at the item's level.
        private static string Describe(EnchantDef d)
        {
            switch (d.Mode)
            {
                case EnchantMode.Relative: return string.Join("/", d.Values.Select(v => "x" + (1 + v / 100f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
                case EnchantMode.ItemLevel: return string.Join("/", d.Values.Select(v => v + "%")) + " of vanilla at item level";
                default: return string.Join("/", d.Values);
            }
        }

        private static bool TryTarget(string word, out Target target)
        {
            switch (word.ToLowerInvariant())
            {
                case "held": case "hand": target = Target.Held; return true;
                case "helm": case "head": case "helmet": target = Target.Helm; return true;
                case "chest": case "body": case "breast": target = Target.Chest; return true;
                case "pants": case "legs": target = Target.Pants; return true;
                case "ring1": case "ring": target = Target.Ring1; return true;
                case "ring2": target = Target.Ring2; return true;
                case "necklace": case "neck": case "amulet": target = Target.Necklace; return true;
                case "offhand": case "shield": target = Target.Offhand; return true;
                default: target = Target.Held; return false;
            }
        }

        private static bool TryGetSlot(EntityManager em, Entity player, Target target, out int slot, out string error)
        {
            slot = -1;
            error = null;
            if (!em.HasBuffer<ContainedObjectsBuffer>(player) || !em.HasComponent<EquipmentCD>(player) || !em.HasComponent<EquippedObjectCD>(player))
            {
                error = "no player found.";
                return false;
            }
            var equipment = em.GetComponentData<EquipmentCD>(player);
            switch (target)
            {
                case Target.Helm: slot = equipment.helmSlotIndex; break;
                case Target.Chest: slot = equipment.breastSlotIndex; break;
                case Target.Pants: slot = equipment.pantsSlotIndex; break;
                case Target.Ring1: slot = equipment.ring1SlotIndex; break;
                case Target.Ring2: slot = equipment.ring2SlotIndex; break;
                case Target.Necklace: slot = equipment.necklaceSlotIndex; break;
                case Target.Offhand: slot = equipment.offHandIndex; break;
                default: slot = em.GetComponentData<EquippedObjectCD>(player).equippedSlotIndex; break;
            }
            var items = em.GetBuffer<ContainedObjectsBuffer>(player);
            if (slot < 0 || slot >= items.Length || items[slot].objectID == ObjectID.None)
            {
                error = target == Target.Held ? "hold the item you want to enchant." : $"nothing equipped in {target.ToString().ToLowerInvariant()}.";
                return false;
            }
            return true;
        }

        private static CommandOutput Info(string text) => new CommandOutput("Enchanting: " + text, CommandStatus.Info);
        private static CommandOutput Error(string text) => new CommandOutput("Enchanting: " + text, CommandStatus.Error);
        private CommandOutput Hint() => new CommandOutput(GetDescription(), CommandStatus.Hint);

        public string GetDescription()
        {
            return "/enchant [list|stats|selftest [status]|combattest [seconds|status]|debug [off]|report|reset|set <key> <lvl>|remove <key>|clear|roll [n]|cost] [held|helm|chest|pants|ring1|ring2|necklace|offhand]";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "enchant", "ench" };
        }
    }
}
