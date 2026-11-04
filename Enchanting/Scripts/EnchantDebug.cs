using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Test instrumentation, off by default ("/enchant debug on"). Groups measurements by what was used:
    /// "BattleAxe [Sharpness III]" vs "BattleAxe [none]", so two otherwise equal items can be compared.
    /// - Hits: every damage number the local player caused (melee or own projectile), crits counted apart.
    /// - Durability: every drop in the held item's durability (server side).
    /// "/enchant report" prints averages per item, "/enchant reset" clears them. Lines also go to Player.log.
    /// Static state is shared by client and server only in single player / on the host, which is where it is
    /// meant to be used.
    /// </summary>
    public static class EnchantDebug
    {
        public static bool On;
        // Appended to the item label by the combat test, so its first and last plain runs report separately.
        public static string RunTag = "";

        private sealed class Stat
        {
            public int Hits, Crits, Min = int.MaxValue, Max;
            public long Sum, CritSum;
            public int Uses, DurabilityLost;
            public int Burning, Poisoned, Stunned, Slowed; // hits after which the target had that status
            public int Healed;
        }

        // Ranged slows land as MovementSpeedDecrease (damage code), slime slows as SlowedBySlime: both count as slowed.
        internal static readonly ConditionID[] Statuses = { ConditionID.Burning, ConditionID.Poisoned, ConditionID.Stunned, ConditionID.SlowedBySlime, ConditionID.MovementSpeedDecrease };

        /// <summary>HP the player regained during a combat-test run (lifesteal shows up here).</summary>
        internal static void AddHeal(string key, int amount)
        {
            if (amount > 0) Get(key).Healed += amount;
        }

        private static readonly Dictionary<string, Stat> Stats = new Dictionary<string, Stat>();

        private static Stat Get(string key)
        {
            if (!Stats.TryGetValue(key, out var s)) Stats[key] = s = new Stat();
            return s;
        }

        public static string Label(ObjectID id, List<EnchantEntry> enchants)
        {
            return $"{id} [{(enchants.Count == 0 ? "none" : EnchantStorage.Describe(enchants))}]";
        }

        // ------------------------------------------------------------------ hits (client)

        internal static void OnDamageNumber(EffectEventCD e, bool crit)
        {
            var player = Manager.main.player;
            if (player == null || !player.entityExist) return;
            var world = player.world;
            Entity attacker = e.entity2;
            bool ours = attacker == player.entity;
            if (!ours && attacker != Entity.Null && EntityUtility.EntityExists(attacker, world)
                && EntityUtility.HasComponentData<ProjectileCD>(attacker, world)
                && EntityUtility.TryGetComponentData<OwnerReferenceCD>(attacker, world, out var owner))
                ours = owner.owner == player.entity;
            if (!ours) return;

            if (!EntityUtility.TryGetComponentData<EquippedObjectCD>(player.entity, world, out var equipped)) return;
            // Read the slot itself: it carries the item's aux index (enchants).
            var items = world.EntityManager.GetBuffer<ContainedObjectsBuffer>(player.entity, true);
            int slot = equipped.equippedSlotIndex;
            var held = slot >= 0 && slot < items.Length ? items[slot] : equipped.containedObject;
            string key = Label(held.objectID, EnchantStorage.ReadClient(held)) + RunTag;
            var s = Get(key);
            if (crit)
            {
                s.Crits++;
                s.CritSum += e.value1;
            }
            else
            {
                s.Hits++;
                s.Sum += e.value1;
                if (e.value1 < s.Min) s.Min = e.value1;
                if (e.value1 > s.Max) s.Max = e.value1;
            }
            // On-hit procs: which statuses the target carries at this hit (ConditionsBuffer is replicated).
            string statuses = "";
            if (EntityUtility.EntityExists(e.entity, world) && world.EntityManager.HasBuffer<ConditionsBuffer>(e.entity))
            {
                var conditions = world.EntityManager.GetBuffer<ConditionsBuffer>(e.entity, true);
                foreach (var status in Statuses)
                {
                    bool has = false;
                    for (int i = 0; i < conditions.Length && !has; i++) has = conditions[i].condition.conditionData.conditionID == status;
                    if (!has) continue;
                    statuses += " " + status;
                    if (status == ConditionID.Burning) s.Burning++;
                    else if (status == ConditionID.Poisoned) s.Poisoned++;
                    else if (status == ConditionID.Stunned) s.Stunned++;
                    else if (!statuses.Contains(" slowed")) { s.Slowed++; statuses += " slowed"; continue; }
                    else continue;
                }
            }
            Debug.Log($"[{EnchantingMod.Name}] hit {(crit ? "CRIT " : "")}{e.value1} with {key}{statuses}");
        }

        // ------------------------------------------------------------------ durability (server)

        private static readonly Dictionary<Entity, (ObjectID id, int aux, int amount)> LastHeld =
            new Dictionary<Entity, (ObjectID, int, int)>();

        internal static void TrackDurability(EntityManager em, Entity player, ContainedObjectsBuffer held, InventoryAuxDataSystemDataCD aux)
        {
            if (!On) return;
            if (held.objectID == ObjectID.None || !PugDatabase.HasComponent<DurabilityCD>(held.objectData))
            {
                LastHeld.Remove(player);
                return;
            }
            if (LastHeld.TryGetValue(player, out var last) && last.id == held.objectID && last.aux == held.auxDataIndex
                && held.amount < last.amount)
            {
                int lost = last.amount - held.amount;
                string key = Label(held.objectID, EnchantStorage.Read(em, aux, held));
                var s = Get(key);
                s.Uses++;
                s.DurabilityLost += lost;
                Debug.Log($"[{EnchantingMod.Name}] durability -{lost} ({held.amount} left) on {key}");
            }
            LastHeld[player] = (held.objectID, held.auxDataIndex, held.amount);
        }

        // ------------------------------------------------------------------ report

        public static string Report()
        {
            if (Stats.Count == 0) return On ? "no hits or durability loss recorded yet." : "debug is off; use /enchant debug on.";
            var sb = new StringBuilder();
            foreach (var kv in Stats)
            {
                var s = kv.Value;
                sb.Append(kv.Key).Append(": ");
                if (s.Hits > 0) sb.Append($"{s.Hits} hits avg {s.Sum / (double)s.Hits:0.0} (min {s.Min}, max {s.Max})");
                if (s.Crits > 0) sb.Append($", {s.Crits} crits avg {s.CritSum / (double)s.Crits:0.0}");
                int all = s.Hits + s.Crits;
                if (all > 0)
                {
                    sb.Append($", crit rate {100.0 * s.Crits / all:0}%");
                    if (s.Burning > 0) sb.Append($", burning {100.0 * s.Burning / all:0}%");
                    if (s.Poisoned > 0) sb.Append($", poisoned {100.0 * s.Poisoned / all:0}%");
                    if (s.Stunned > 0) sb.Append($", stunned {100.0 * s.Stunned / all:0}%");
                    if (s.Slowed > 0) sb.Append($", slowed {100.0 * s.Slowed / all:0}%");
                }
                if (s.Healed > 0) sb.Append($"; healed {s.Healed} hp");
                if (s.Uses > 0) sb.Append($"; durability {s.DurabilityLost} lost over {s.Uses} drops");
                sb.Append(" | ");
            }
            string text = sb.ToString();
            Debug.Log($"[{EnchantingMod.Name}] report: {text}");
            return text;
        }

        public static void Reset()
        {
            Stats.Clear();
            RunTag = "";
            LastHeld.Clear();
        }
    }

    // A void prefix always runs, even when another mod's prefix skips the original.
    [HarmonyPatch(typeof(EffectEventExtensions), "PlayEffect")]
    public static class EnchantDebugHitPatch
    {
        public static void Prefix(EffectEventCD effectEvent)
        {
            if (!EnchantDebug.On) return;
            try
            {
                if (effectEvent.effectID == EffectID.WhiteDamageNumber) EnchantDebug.OnDamageNumber(effectEvent, false);
                else if (effectEvent.effectID == EffectID.CritNumber) EnchantDebug.OnDamageNumber(effectEvent, true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{EnchantingMod.Name}] debug hit: {e.Message}");
            }
        }
    }
}
