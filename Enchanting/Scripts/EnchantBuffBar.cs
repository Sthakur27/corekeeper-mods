using System.Collections.Generic;
using HarmonyLib;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Client HUD (2026-11-13): the buff bar above the health bar draws one icon per ConditionsBuffer entry, capped at
    /// 18. With many enchant/food/potion entries it fills up with look-alike icons and hides the rest. This replaces
    /// ConditionsContainerUI.UpdateConditions with the same drawing, but entries that share a condition ID, or a
    /// description (ConditionInfo.useSameDescAsId: different IDs the game words identically), become one icon showing the sum (what the stat system applies); a timed buff keeps the longest time
    /// left (a permanent entry wins). Stack-type conditions (GetStacks &gt; 0) stay one icon per entry, as in vanilla.
    ///
    /// Cost: one pass over the player's buffer per frame, same as vanilla, plus a small dictionary.
    /// Fallback: any exception logs once and hands the bar back to vanilla for the rest of the session.
    /// </summary>
    [HarmonyPatch(typeof(ConditionsContainerUI), "UpdateConditions")]
    public static class EnchantBuffBar
    {
        private const int MaxIcons = 18; // ConditionsContainerUI.MAX_CONDITIONS (18 prefabs built in Awake)

        private struct Group
        {
            public Condition Shown;   // representative entry with the merged value
            public int Sources;
            public bool Stack;
            public float Remaining;   // seconds; PositiveInfinity for permanent entries
        }

        private static readonly List<Group> _groups = new List<Group>();
        private static readonly Dictionary<ConditionID, int> _index = new Dictionary<ConditionID, int>();
        private static bool _broken;
        private static int _dumps;            // diagnostic Player.log dumps written (capped)
        private static int _lastSignature;
        private const int MaxDumps = 30;

        /// <summary>How many buffer entries the icon for this condition merges (hover text); 0/1 = not merged.</summary>
        public static readonly Dictionary<ConditionID, int> Sources = new Dictionary<ConditionID, int>();

        /// <summary>Every condition ID merged into the icon showing this ID (hover total sums all of them).</summary>
        public static readonly Dictionary<ConditionID, List<ConditionID>> Members = new Dictionary<ConditionID, List<ConditionID>>();
        private static readonly List<List<ConditionID>> _memberLists = new List<List<ConditionID>>();

        public static bool Prefix(List<ConditionUI> ___conditions, ref int ___activeAmountOfConditions)
        {
            if (_broken) return true;
            try
            {
                Draw(___conditions, ref ___activeAmountOfConditions);
                return false;
            }
            catch (System.Exception e)
            {
                _broken = true;
                Sources.Clear();
                Debug.LogWarning($"[{EnchantingMod.Name}] buff bar merge disabled, using the vanilla bar: {e}");
                return true;
            }
        }

        private static void Draw(List<ConditionUI> icons, ref int active)
        {
            PlayerController player = Manager.main.player;
            if (player == null || !player.entityExist) return;
            NetworkTick tick = EntityUtility.GetCurrentTickOnClient(player.entity, player.world, out var fraction);
            uint rate = (uint)PlatformConfiguration.Instance.SessionConfiguration.SimulationTickRate;
            var buffer = EntityUtility.GetConditions(player.entity, player.world);
            var table = Manager.ui.conditionsIconsTable;

            _groups.Clear();
            _index.Clear();
            for (int i = 0; i < buffer.Length; i++)
            {
                Condition c = buffer.ElementAt(i).condition;
                var id = c.conditionData.conditionID;
                var info = table.GetConditionInfo(id);
                if (info.icon == null) continue;
                bool stack = ConditionExtensions.GetStacks(id, c.conditionData.value) > 0;
                float remaining = c.conditionData.duration > 0f && !float.IsPositiveInfinity(c.conditionData.duration)
                    ? NetworkTimeUtilities.TimeToFutureTickInSeconds(tick, fraction, c.removeTick, rate)
                    : float.PositiveInfinity;
                var key = info.useSameDescAsId != ConditionID.None ? info.useSameDescAsId : id;
                if (stack || !_index.TryGetValue(key, out int at))
                {
                    if (!stack) _index[key] = _groups.Count;
                    var members = MemberList(_groups.Count);
                    members.Add(id);
                    _groups.Add(new Group { Shown = c, Sources = 1, Stack = stack, Remaining = remaining });
                    continue;
                }
                var g = _groups[at];
                if (!_memberLists[at].Contains(id)) _memberLists[at].Add(id);
                // The stat system adds every entry up (SummarizeConditionsSystem.AddConditionToSummarizedBuffers sums
                // regardless of isAdditiveWithSelf, which only decides how AddOrRefreshCondition merges new buffs).
                int merged = g.Shown.conditionData.value + c.conditionData.value;
                if (remaining > g.Remaining) { g.Shown = c; g.Remaining = remaining; }
                g.Shown.conditionData.value = merged;
                g.Sources++;
                _groups[at] = g;
            }

            // Same quota as vanilla: stack-type icons first claim their slots, the rest fill what's left.
            int stacks = 0;
            foreach (var g in _groups) if (g.Stack) stacks++;
            stacks = Mathf.Min(stacks, MaxIcons);
            int others = Mathf.Min(_groups.Count - stacks, MaxIcons - stacks);

            Dump(buffer, table);

            Sources.Clear();
            Members.Clear();
            int slot = 0, usedStacks = 0, usedOthers = 0;
            for (int gi = 0; gi < _groups.Count; gi++)
            {
                var g = _groups[gi];
                if (slot >= MaxIcons || slot >= icons.Count) break;
                if (g.Stack) { if (usedStacks >= stacks) continue; usedStacks++; }
                else
                {
                    if (usedOthers >= others) continue;
                    usedOthers++;
                    Sources[g.Shown.conditionData.conditionID] = g.Sources;
                    Members[g.Shown.conditionData.conditionID] = _memberLists[gi];
                }
                Show(icons, slot++, g.Shown, tick, fraction, rate);
            }

            if (slot < MaxIcons && slot < icons.Count && Manager.saves.IsCreativeModeCharacter()
                && EntityUtility.IsComponentEnabled<GodModeCD>(player.entity, player.world))
            {
                Show(icons, slot++, new Condition { conditionData = new ConditionData { conditionID = ConditionID.GodMode } }, tick, fraction, rate);
            }

            active = slot;
            for (int k = slot; k < icons.Count; k++)
                if (icons[k].gameObject.activeSelf) icons[k].gameObject.SetActive(false);
        }

        private static List<ConditionID> MemberList(int i)
        {
            while (_memberLists.Count <= i) _memberLists.Add(new List<ConditionID>());
            _memberLists[i].Clear();
            return _memberLists[i];
        }

        // Diagnostic (2026-11-13, Sid saw look-alike icons stay separate): whenever the buff list changes shape, write
        // what it holds and how it merged to Player.log, at most MaxDumps times a session.
        private static void Dump(DynamicBuffer<ConditionsBuffer> buffer, ConditionsTable table)
        {
            if (_dumps >= MaxDumps) return;
            int sig = buffer.Length * 7919 + _groups.Count;
            for (int i = 0; i < buffer.Length; i++) sig = sig * 31 + (int)buffer[i].condition.conditionData.conditionID;
            if (_dumps > 0 && sig == _lastSignature) return;
            _lastSignature = sig;
            _dumps++;
            var parts = new List<string>();
            for (int i = 0; i < buffer.Length; i++)
            {
                var d = buffer[i].condition.conditionData;
                var info = table.GetConditionInfo(d.conditionID);
                parts.Add($"{d.conditionID}={d.value} dur={d.duration} mult={d.valueMultiplier}"
                    + (info.icon == null ? " noicon" : "")
                    + (info.useSameDescAsId != ConditionID.None ? $" desc={info.useSameDescAsId}" : "")
                    + (info.isAdditiveWithSelf ? "" : " nonadd"));
            }
            Debug.Log($"[{EnchantingMod.Name}] buff bar ({_dumps}/{MaxDumps}): {buffer.Length} entries -> {_groups.Count} icons: {string.Join("; ", parts)}");
        }

        private static void Show(List<ConditionUI> icons, int slot, Condition c, NetworkTick tick, float fraction, uint rate)
        {
            icons[slot].UpdateCondition(c, tick, fraction, rate);
            if (!icons[slot].gameObject.activeSelf) icons[slot].gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hover text of a buff icon: where the value comes from, one line per source, then the stat's real total.
    /// Sources: each enchant on the held item, worn gear and accessories (name, level, item); timed buff entries
    /// (food, potions); other buff-list entries; the gear's own stats (GivesConditionsWhenEquipped); and whatever is
    /// left over (skills, set bonuses, reinforcement) as one line. Enchant multipliers are stored as plain amounts,
    /// so the total is the effective value the game uses.
    /// </summary>
    [HarmonyPatch(typeof(ConditionUI), nameof(ConditionUI.GetHoverStats))]
    public static class EnchantBuffBarHover
    {
        private static readonly Color Info = new Color(0.78f, 0.55f, 1f);
        private static readonly Color Source = new Color(0.66f, 0.62f, 0.75f);
        private static readonly List<ConditionID> _single = new List<ConditionID>(1);

        public static void Postfix(Condition ___activeCondition, ref List<TextAndFormatFields> __result)
        {
            var id = ___activeCondition.conditionData.conditionID;
            if (id == ConditionID.None || __result == null) return;
            try
            {
                Add(id, ___activeCondition.conditionData.value, __result);
            }
            catch (System.Exception e)
            {
                if (EnchantDebug.On) Debug.LogWarning($"[{EnchantingMod.Name}] buff hover: {e.Message}");
            }
        }

        private static void Add(ConditionID id, int shown, List<TextAndFormatFields> result)
        {
            var player = Manager.main.player;
            if (player == null || !player.entityExist) return;
            var em = player.world.EntityManager;
            if (!EnchantBuffBar.Members.TryGetValue(id, out var members) || members.Count == 0)
            {
                _single.Clear();
                _single.Add(id);
                members = _single;
            }

            var lines = new List<TextAndFormatFields>();
            int listed = 0;

            // 1. The buff list itself (what the icon sums).
            var buffer = EntityUtility.GetConditions(player.entity, player.world);
            NetworkTick tick = EntityUtility.GetCurrentTickOnClient(player.entity, player.world, out var fraction);
            uint rate = (uint)PlatformConfiguration.Instance.SessionConfiguration.SimulationTickRate;
            int ours = 0;
            bool hasOurs = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                var c = buffer[i].condition;
                if (!members.Contains(c.conditionData.conditionID)) continue;
                if (EnchantEffectSystem.IsOurs(c)) { ours += c.conditionData.value; hasOurs = true; continue; }
                var d = c.conditionData;
                bool timed = d.duration > 0f && !float.IsPositiveInfinity(d.duration);
                string label = timed ? "food or potion" : float.IsPositiveInfinity(d.duration) ? "permanent buff" : "effect";
                if (timed) d.duration = Mathf.Max(1f, NetworkTimeUtilities.TimeToFutureTickInSeconds(tick, fraction, c.removeTick, rate));
                lines.Add(SourceLine(d, label));
                listed += c.conditionData.value;
            }

            // 2. Enchants (one marked buff entry per stat, split back into the items that grant it).
            if (hasOurs)
            {
                int before = lines.Count;
                AddEnchantSources(em, player.entity, members, lines);
                if (lines.Count == before) lines.Add(SourceLine(new ConditionData { conditionID = id, value = ours }, "enchantments"));
                listed += ours;
            }

            // 3. The gear's own stats (not on the buff list; part of the game's total).
            int gear = AddGearSources(em, player.entity, members, lines);

            // 4. Total from the game's stat summary, and whatever no line above explains.
            int total = 0;
            bool hasTotal = em.HasBuffer<SummarizedConditionsBuffer>(player.entity);
            if (hasTotal)
            {
                var sum = em.GetBuffer<SummarizedConditionsBuffer>(player.entity, true);
                foreach (var m in members) if ((int)m < sum.Length) total += sum[(int)m].value;
                int other = total - listed - gear;
                if (other != 0) lines.Add(SourceLine(new ConditionData { conditionID = id, value = other }, "skills, set bonuses, reinforcement"));
            }

            if (lines.Count < 2 && (!hasTotal || total == shown)) return; // a single source: the icon line says it all
            result.Add(EnchantTableFigurineHover.Line("From:", Source));
            result.AddRange(lines);
            if (hasTotal && total != 0)
            {
                var t = ConditionUI.GetConditionTextAndFormatFields(default, new ConditionData { conditionID = id, value = total },
                    previewReinforced: false, isReinforced: false, previewUpgraded: false);
                t.color = Info;
                t.additionalText = "(total)";
                t.additionalTextColor = Info;
                result.Add(t);
            }
        }

        private static TextAndFormatFields SourceLine(ConditionData d, string label)
        {
            var line = ConditionUI.GetConditionTextAndFormatFields(default, d, previewReinforced: false, isReinforced: false, previewUpgraded: false);
            line.color = Source;
            line.additionalText = "(" + label + ")";
            line.additionalTextColor = Source;
            return line;
        }

        private static List<int> Slots(EntityManager em, Entity player, out int held)
        {
            var slots = new List<int>();
            held = -1;
            if (em.HasComponent<EquippedObjectCD>(player))
            {
                held = em.GetComponentData<EquippedObjectCD>(player).equippedSlotIndex;
                slots.Add(held);
            }
            if (em.HasComponent<EquipmentCD>(player))
            {
                var eq = em.GetComponentData<EquipmentCD>(player);
                slots.Add(eq.helmSlotIndex);
                slots.Add(eq.breastSlotIndex);
                slots.Add(eq.pantsSlotIndex);
                slots.Add(eq.ring1SlotIndex);
                slots.Add(eq.ring2SlotIndex);
                slots.Add(eq.necklaceSlotIndex);
                slots.Add(eq.offHandIndex);
                slots.Add(eq.bagIndex);     // same slot list SummarizeConditionsSystem reads
                slots.Add(eq.lanternIndex);
            }
            return slots;
        }

        private static void AddEnchantSources(EntityManager em, Entity player, List<ConditionID> members, List<TextAndFormatFields> lines)
        {
            if (!em.HasBuffer<ContainedObjectsBuffer>(player)) return;
            var items = em.GetBuffer<ContainedObjectsBuffer>(player, true);
            var slots = Slots(em, player, out int held);
            var heldId = held >= 0 && held < items.Length ? items[held].objectID : ObjectID.None;
            foreach (int slot in slots)
            {
                if (slot < 0 || slot >= items.Length || items[slot].objectID == ObjectID.None) continue;
                var item = items[slot];
                foreach (var e in EnchantStorage.ReadClient(item))
                {
                    var def = e.Def;
                    if (def == null || def.OnMinions || def.FollowsLight) continue;
                    var cond = def.Mode == EnchantMode.Relative
                        ? EnchantMath.WriteCondition(def, EnchantMath.BucketWeapon(def, item.objectID, heldId))
                        : def.Condition;
                    if (!members.Contains(cond)) continue;
                    string label = def.Name + " " + Enchants.Roman(e.Level) + ", " + ItemName(item);
                    if (def.Mode == EnchantMode.Relative)
                    {
                        lines.Add(new TextAndFormatFields
                        {
                            text = EnchantingMod.PlainTerm,
                            formatFields = new[] { EnchantMath.MultiplierText(def, e.Level, item.objectID) },
                            dontLocalizeFormatFields = true,
                            color = Source,
                            additionalText = "(" + label + ")",
                            additionalTextColor = Source,
                        });
                    }
                    else
                    {
                        lines.Add(SourceLine(new ConditionData { conditionID = cond, value = EnchantMath.Raw(def, e.Level, item.objectData) }, label));
                    }
                }
            }
        }

        private static int AddGearSources(EntityManager em, Entity player, List<ConditionID> members, List<TextAndFormatFields> lines)
        {
            if (!em.HasBuffer<ContainedObjectsBuffer>(player)) return 0;
            var items = em.GetBuffer<ContainedObjectsBuffer>(player, true);
            int total = 0;
            foreach (int slot in Slots(em, player, out _))
            {
                if (slot < 0 || slot >= items.Length || items[slot].objectID == ObjectID.None) continue;
                var item = items[slot];
                foreach (var d in ConditionUIExtensions.GetConditionsOnEquip(item.objectData))
                {
                    if (!members.Contains(d.conditionID) || d.value == 0) continue;
                    lines.Add(SourceLine(d, ItemName(item)));
                    total += d.value;
                }
            }
            return total;
        }

        private static string ItemName(ContainedObjectsBuffer item)
        {
            try
            {
                var tf = PlayerController.GetObjectName(item, localize: true);
                if (tf != null && !string.IsNullOrEmpty(tf.text))
                    return tf.dontLocalize ? tf.text : PugText.ProcessText(tf.text, tf.formatFields, true, false);
            }
            catch (System.Exception) { }
            return item.objectID.ToString();
        }
    }
}
