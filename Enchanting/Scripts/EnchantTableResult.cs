using System.Collections.Generic;
using UnityEngine;

namespace Enchanting
{
    /// <summary>
    /// Shows what a roll gave right in the table window (2026-11-13: the chat reply is hidden behind the open
    /// inventory). Client only: watches the enchants of the item in the table slot and, when they change while the
    /// window is open, writes "Rolled: Resilience III: -8% damage taken" (or "Removed: ...") on a text line under
    /// the button row. The same text is added to the roll buttons' hover as "Last roll".
    ///
    /// Cost: one non-allocating hash of the slot item's enchant buffer per frame while the table window is open;
    /// the enchant list is read and the text rendered only when that hash changes.
    /// </summary>
    public static class EnchantTableResult
    {
        /// <summary>Latest result text for the open table (null until something changes).</summary>
        public static string Last { get; private set; }

        private static PugText _label;
        private static Transform _parent;
        private static ObjectID _item;
        private static int _sig;
        private static readonly List<EnchantEntry> _before = new List<EnchantEntry>();
        private static bool _failed;

        private static readonly Color Gained = new Color(0.85f, 0.7f, 1f);
        private static readonly Color Lost = new Color(1f, 0.55f, 0.5f);

        public static bool Owns(Transform t) => _label != null && t.IsChildOf(_label.transform);

        /// <summary>Window opened on the table: forget the previous result, remember the slot item as it is.</summary>
        internal static void Reset(Transform parent)
        {
            _parent = parent;
            Last = null;
            Show(null, Gained);
            Snapshot(EnchantTableItemPatch.Item);
        }

        internal static void Hide()
        {
            Last = null;
            if (_label != null) _label.gameObject.SetActive(false);
        }

        /// <summary>Every frame while the table window is open (UpgradeForgeUI.LateUpdate postfix).</summary>
        internal static void Tick()
        {
            if (_failed) return;
            try
            {
                var item = EnchantTableItemPatch.Item;
                int sig = EnchantStorage.SignatureClient(item);
                if (item.objectID == _item && sig == _sig) return;
                if (item.objectID != _item)
                {
                    // Another item went into the slot: nothing was rolled yet.
                    Last = null;
                    Show(null, Gained);
                    Snapshot(item);
                    return;
                }
                var now = EnchantStorage.ReadClient(item);
                string text = Describe(_before, now, item, out bool gained);
                Snapshot(item, now, sig);
                if (text == null) return;
                Last = text;
                Show(text, gained ? Gained : Lost);
            }
            catch (System.Exception e)
            {
                _failed = true;
                Debug.LogWarning($"[{EnchantingMod.Name}] table result text disabled: {e.Message}");
            }
        }

        private static void Snapshot(ContainedObjectsBuffer item) =>
            Snapshot(item, EnchantStorage.ReadClient(item), EnchantStorage.SignatureClient(item));

        private static void Snapshot(ContainedObjectsBuffer item, List<EnchantEntry> list, int sig)
        {
            _item = item.objectID;
            _sig = sig;
            _before.Clear();
            _before.AddRange(list);
        }

        // New entries (id + level not there before) are what was rolled; otherwise name what disappeared.
        private static string Describe(List<EnchantEntry> before, List<EnchantEntry> now, ContainedObjectsBuffer item, out bool gained)
        {
            gained = false;
            var parts = new List<string>();
            foreach (var e in now)
                if (!Contains(before, e) && e.Def != null) parts.Add($"{e.Def.Name} {Enchants.Roman(e.Level)}: {Effect(e, item)}");
            if (parts.Count > 0)
            {
                gained = true;
                return "Rolled " + string.Join(", ", parts);
            }
            foreach (var e in before)
                if (!Contains(now, e) && e.Def != null) parts.Add($"{e.Def.Name} {Enchants.Roman(e.Level)}");
            return parts.Count > 0 ? "Removed " + string.Join(", ", parts) : null;
        }

        private static bool Contains(List<EnchantEntry> list, EnchantEntry e)
        {
            foreach (var x in list) if (x.Id == e.Id && x.Level == e.Level) return true;
            return false;
        }

        /// <summary>The enchant's effect as the tooltip words it ("-8% damage taken", "x1.12 melee damage").</summary>
        internal static string Effect(EnchantEntry e, ContainedObjectsBuffer item)
        {
            var def = e.Def;
            if (def.Mode == EnchantMode.Relative) return EnchantMath.MultiplierText(def, e.Level, item.objectID);
            if (def.Note != null && def.Condition == ConditionID.None) return def.Note;
            var tf = ConditionUI.GetConditionTextAndFormatFields(item, new ConditionData
            {
                conditionID = def.Condition,
                value = EnchantMath.Raw(def, e.Level, item.objectData),
            }, previewReinforced: false, isReinforced: false, previewUpgraded: false);
            if (tf == null) return def.Name;
            return tf.dontLocalize ? tf.text : PugText.ProcessText(tf.text, tf.formatFields, shouldLocalize: true, shouldLocalizeFormatFields: false);
        }

        private static void Show(string text, Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (_label != null) _label.gameObject.SetActive(false);
                return;
            }
            if (_label == null && !Build()) return;
            _label.gameObject.SetActive(true);
            _label.transform.localPosition = EnchantRemoveButton.ButtonCentre
                - new Vector3(0f, EnchantRemoveButton.ButtonHeight * 0.85f, 0f);
            _label.maxWidth = EnchantRemoveButton.RowHalfWidth * 2f;
            _label.SetTempColor(color);
            _label.Render(text, false, true, true);
        }

        // A copy of the health number's PugText (same approach as the Stats HUD), parented to the button row.
        private static bool Build()
        {
            if (_parent == null) return false;
            var bar = Manager.ui != null ? Manager.ui.playerHealthBarUI : null;
            if (bar == null || bar.healthAmountText == null) return false;
            var text = Object.Instantiate(bar.healthAmountText.gameObject, _parent, false).GetComponent<PugText>();
            text.name = "EnchantingTableResult";
            text.gameObject.layer = _parent.gameObject.layer;
            text.Clear(false, true);
            for (int i = text.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(text.transform.GetChild(i).gameObject);
            text.localize = false;
            text.formatFields = new string[0];
            text.style.horizontalAlignment = PugTextStyle.HorizontalAlignment.center;
            text.style.maskInteraction = SpriteMaskInteraction.None;
            text.DisableAutoInactivationOnStart();
            text.freeResourcesOnDisable = false;
            text.overrideMaterial = null;
            // Draw above the table panel: highest sorting order among the panel's renderers + 10.
            int order = 0;
            foreach (var sr in _parent.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr != null) order = Mathf.Max(order, sr.sortingOrder);
            text.SetOrderInLayer(order + 10);
            _label = text;
            Debug.Log($"[{EnchantingMod.Name}] table result text added (order {order + 10}).");
            return true;
        }
    }
}
