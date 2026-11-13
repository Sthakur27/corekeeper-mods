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
    /// Hover text of a buff icon: the header shows the stat's real total (what the game applies), then one line per
    /// source, "source: effect". Sources follow SummarizeConditionsSystem: buff-list entries (our enchants split back
    /// into item + enchant; other entries named by what they are, with time left), worn and held gear (own stats,
    /// +reinforcement, broken items skipped), skills, skill talents, affixes, souls, and one remainder line (set
    /// bonuses, pet). Built when the hover asks and cached per icon for <see cref="CacheSeconds"/>, never per frame
    /// on its own.
    /// </summary>
    [HarmonyPatch(typeof(ConditionUI), nameof(ConditionUI.GetHoverStats))]
    public static class EnchantBuffBarHover
    {
        private static readonly Color Info = new Color(0.78f, 0.55f, 1f);
        private static readonly Color Source = new Color(0.66f, 0.62f, 0.75f);
        private const float CacheSeconds = 0.5f;

        private class Cached
        {
            public float At;
            public int Shown;
            public int Total;
            public bool HasTotal;
            public readonly List<TextAndFormatFields> Lines = new List<TextAndFormatFields>();
        }

        private static readonly Dictionary<ConditionID, Cached> _cache = new Dictionary<ConditionID, Cached>();
        private static readonly List<ConditionID> _single = new List<ConditionID>(1);

        public static void Postfix(Condition ___activeCondition, ref List<TextAndFormatFields> __result)
        {
            var id = ___activeCondition.conditionData.conditionID;
            if (id == ConditionID.None || __result == null) return;
            try
            {
                if (!_cache.TryGetValue(id, out var c)) _cache[id] = c = new Cached { At = -10f };
                int shown = ___activeCondition.conditionData.value;
                if (Time.unscaledTime - c.At > CacheSeconds || c.Shown != shown)
                {
                    c.At = Time.unscaledTime;
                    c.Shown = shown;
                    c.Lines.Clear();
                    Build(id, c);
                }
                if (c.HasTotal && c.Total != shown && __result.Count > 0)
                {
                    // Header = what the game applies (gear, skills etc. included), not just the buff-list sum.
                    var d = ___activeCondition.conditionData;
                    d.value = c.Total;
                    var header = ConditionUI.GetConditionTextAndFormatFields(default, d, previewReinforced: false, isReinforced: false, previewUpgraded: false);
                    header.additionalText = "(total)";
                    header.additionalTextColor = Info;
                    __result[0] = header;
                }
                if (c.Lines.Count > 1 || (c.HasTotal && c.Total != shown)) __result.AddRange(c.Lines);
            }
            catch (System.Exception e)
            {
                if (EnchantDebug.On) Debug.LogWarning($"[{EnchantingMod.Name}] buff hover: {e.Message}");
            }
        }

        private static void Build(ConditionID id, Cached cache)
        {
            var lines = cache.Lines;
            var player = Manager.main.player;
            if (player == null || !player.entityExist) return;
            var em = player.world.EntityManager;
            var entity = player.entity;
            if (!EnchantBuffBar.Members.TryGetValue(id, out var members) || members.Count == 0)
            {
                _single.Clear();
                _single.Add(id);
                members = _single;
            }
            int listed = 0;

            // 1. The buff list.
            var buffer = EntityUtility.GetConditions(entity, player.world);
            NetworkTick tick = EntityUtility.GetCurrentTickOnClient(entity, player.world, out var fraction);
            uint rate = (uint)PlatformConfiguration.Instance.SessionConfiguration.SimulationTickRate;
            int ours = 0;
            bool hasOurs = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                var c = buffer[i].condition;
                var d = c.conditionData;
                if (!members.Contains(d.conditionID)) continue;
                listed += d.value;
                if (EnchantEffectSystem.IsOurs(c)) { ours += d.value; hasOurs = true; continue; }
                bool timed = d.duration > 0f && !float.IsPositiveInfinity(d.duration);
                float left = timed ? NetworkTimeUtilities.TimeToFutureTickInSeconds(tick, fraction, c.removeTick, rate) : 0f;
                lines.Add(SourceLine(BuffLabel(d.conditionID, d.duration, left), d.conditionID, d.value));
            }
            if (hasOurs)
            {
                int before = lines.Count;
                AddEnchantSources(em, entity, members, lines);
                if (lines.Count == before) lines.Add(SourceLine("Enchantments", id, ours));
            }

            // 2. Gear (not on the buff list).
            listed += AddGearSources(em, entity, members, lines);

            // 3. Skills, talents, affixes, souls.
            if (em.HasBuffer<SkillConditionsBuffer>(entity))
            {
                var b = em.GetBuffer<SkillConditionsBuffer>(entity, true);
                int v = 0;
                for (int i = 0; i < b.Length; i++) if (members.Contains(b[i].conditionData.conditionID)) v += b[i].conditionData.value;
                listed += AddIf(lines, "Skills", id, v);
            }
            if (em.HasBuffer<SkillTalentConditionsBuffer>(entity))
            {
                var b = em.GetBuffer<SkillTalentConditionsBuffer>(entity, true);
                int v = 0;
                for (int i = 0; i < b.Length; i++) if (members.Contains(b[i].conditionData.conditionID)) v += b[i].conditionData.value;
                listed += AddIf(lines, "Skill talents", id, v);
            }
            if (em.HasBuffer<ActiveAffixConditionsBuffer>(entity))
            {
                var b = em.GetBuffer<ActiveAffixConditionsBuffer>(entity, true);
                int v = 0;
                for (int i = 0; i < b.Length; i++) if (members.Contains(b[i].conditionData.conditionID)) v += b[i].conditionData.value;
                listed += AddIf(lines, "Item affixes", id, v);
            }
            if (em.HasBuffer<SoulsConditionsBuffer>(entity) && em.HasComponent<CollectedAndEnabledSoulsMask>(entity))
            {
                var b = em.GetBuffer<SoulsConditionsBuffer>(entity, true);
                var mask = em.GetComponentData<CollectedAndEnabledSoulsMask>(entity);
                int v = 0;
                for (int i = 0; i < b.Length; i++)
                    if (mask.HasSoulEnabled(b[i].soulID) && members.Contains(b[i].conditionData.conditionID)) v += b[i].conditionData.value;
                listed += AddIf(lines, "Souls", id, v);
            }

            // 4. The game's total; what's left is computed inside the summary only (armor set bonuses, pet talents).
            if (em.HasBuffer<SummarizedConditionsBuffer>(entity))
            {
                var sum = em.GetBuffer<SummarizedConditionsBuffer>(entity, true);
                int total = 0;
                foreach (var m in members) if ((int)m < sum.Length) total += sum[(int)m].value;
                cache.Total = total;
                cache.HasTotal = true;
                AddIf(lines, "Armor set bonus, pet or other", id, total - listed);
            }
        }

        private static int AddIf(List<TextAndFormatFields> lines, string label, ConditionID id, int value)
        {
            if (value == 0) return 0;
            lines.Add(SourceLine(label, id, value));
            return value;
        }

        /// <summary>One plain line "label: +3% reduced damage taken from bosses" (label first, effect text intact).</summary>
        private static TextAndFormatFields SourceLine(string label, ConditionID id, int value)
        {
            var tf = ConditionUI.GetConditionTextAndFormatFields(default, new ConditionData { conditionID = id, value = value },
                previewReinforced: false, isReinforced: false, previewUpgraded: false);
            string effect = tf.dontLocalize ? tf.text : PugText.ProcessText(tf.text, tf.formatFields, shouldLocalize: true, shouldLocalizeFormatFields: false);
            return EnchantTableFigurineHover.Line(label + ": " + effect, Source);
        }

        private static TextAndFormatFields PlainSource(string label, string effect) =>
            EnchantTableFigurineHover.Line(label + ": " + effect, Source);

        /// <summary>Name for a buff-list entry that isn't ours: known situational buffs by name, else by duration.</summary>
        private static string BuffLabel(ConditionID id, float duration, float left)
        {
            string name;
            switch (id)
            {
                case ConditionID.DamageIncreaseAtMaxHealth: name = "At full health"; break;
                case ConditionID.DamageIncreaseFromRunning: name = "While running"; break;
                default:
                    string raw = id.ToString();
                    if (raw.StartsWith("WellFed")) name = "Well fed";
                    else if (raw.StartsWith("Aura")) name = "Aura from a nearby ally or item";
                    else if (float.IsPositiveInfinity(duration)) name = "Permanent buff (" + Humanize(raw) + ")";
                    else if (duration >= 60f) name = "Food or potion";
                    else name = Humanize(raw);
                    break;
            }
            if (left > 0f)
            {
                int s = Mathf.CeilToInt(left);
                name += s >= 60 ? $" ({s / 60}:{s % 60:00} left)" : $" ({s} s left)";
            }
            return name;
        }

        private static string Humanize(string raw)
        {
            var sb = new System.Text.StringBuilder(raw.Length + 8);
            for (int i = 0; i < raw.Length; i++)
            {
                char ch = raw[i];
                if (i > 0 && char.IsUpper(ch) && !char.IsUpper(raw[i - 1])) sb.Append(' ');
                sb.Append(i == 0 ? ch : char.ToLowerInvariant(ch));
            }
            return sb.ToString();
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
                    string label = def.Name + " " + Enchants.Roman(e.Level) + " (" + ItemName(item) + ")";
                    if (def.Mode == EnchantMode.Relative)
                        lines.Add(PlainSource(label, EnchantMath.MultiplierText(def, e.Level, item.objectID)));
                    else
                        lines.Add(SourceLine(label, cond, EnchantMath.Raw(def, e.Level, item.objectData)));
                }
            }
        }

        private static int AddGearSources(EntityManager em, Entity player, List<ConditionID> members, List<TextAndFormatFields> lines)
        {
            if (!em.HasBuffer<ContainedObjectsBuffer>(player)) return 0;
            var items = em.GetBuffer<ContainedObjectsBuffer>(player, true);
            var table = Manager.ui.conditionsIconsTable;
            int total = 0;
            foreach (int slot in Slots(em, player, out _))
            {
                if (slot < 0 || slot >= items.Length || items[slot].objectID == ObjectID.None) continue;
                var item = items[slot];
                bool hasDurability = PugDatabase.TryGetComponent<DurabilityCD>(item.objectID, out var durability);
                if (hasDurability && item.amount <= 0) continue; // broken gear gives nothing
                bool reinforced = hasDurability && durability.IsReinforced(item.amount);
                foreach (var d in ConditionUIExtensions.GetConditionsOnEquip(item.objectData))
                {
                    if (!members.Contains(d.conditionID) || d.value == 0) continue;
                    int value = d.value;
                    var info = table.GetConditionInfo(d.conditionID);
                    if (reinforced && !info.isUnique && info.effect != ConditionEffect.MaxMinions)
                        value += ReinforceBonus(d.value, info.isNegative);
                    lines.Add(SourceLine(ItemName(item) + (reinforced ? ", reinforced" : ""), d.conditionID, value));
                    total += value;
                }
            }
            return total;
        }

        // SummarizeConditionsSystem's reinforcement bonus: +15% of the value, at least 1 point.
        private static int ReinforceBonus(int value, bool isNegative)
        {
            if (value < 0)
            {
                int b = Mathf.RoundToInt(Mathf.Min(-1f, value * 0.14999998f));
                return isNegative ? -b : b;
            }
            return Mathf.RoundToInt(Mathf.Max(1f, value * 0.14999998f));
        }

        private static string ItemName(ContainedObjectsBuffer item)
        {
            try
            {
                // localize: true returns the finished, translated name in .text (PlayerController.GetObjectName).
                var tf = PlayerController.GetObjectName(item, localize: true);
                if (tf != null && !string.IsNullOrEmpty(tf.text)) return tf.text;
            }
            catch (System.Exception) { }
            return item.objectID.ToString();
        }
    }
}
