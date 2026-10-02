using System;
using System.Collections.Generic;
using CoreLib.Submodule.Command;
using CoreLib.Submodule.Command.Data;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;

namespace PetEditor
{
    /// <summary>
    /// Client UI added to the vanilla PetTalentsWindow:
    ///  - a level row under the reset button: "Level X/10" with - and + buttons (clones of the reset
    ///    button with fresh click events);
    ///  - a talent picker: right-click a talent slot and the 3x3 tree is replaced by a grid of every
    ///    talent in PetInfosTable (clones of a vanilla PetTalentUIElement, so icons and hover text are
    ///    the game's own); left-click one to put it in that slot, right-click to close.
    /// The window's layout (PositionUIElements) is replaced by <see cref="Layout"/>, which is vanilla's
    /// layout plus the level row and the picker size.
    /// </summary>
    public static class PetEditorUI
    {
        private const int PickerColumns = 7;
        private const float PickerScale = 0.8f;

        private static PetTalentsWindow _window;
        private static bool _built;
        private static bool _failed;

        private sealed class Row
        {
            public GameObject root;
            public PugText label;
            public ButtonUIElement dec, inc;
            public string shown;
        }

        private const float ButtonWidth = 0.875f;
        private const float LabelScale = 1f; // the pixel font only renders cleanly at integer scale
        private static Row _levelRow, _colorRow;
        private static float _rowHeight = 1f;

        private static readonly List<PetTalentUIElement> _pickItems = new List<PetTalentUIElement>();
        private static readonly List<PetInfosTable.PetTalentInfo> _pickInfos = new List<PetInfosTable.PetTalentInfo>();
        private static float _pickStep;
        private static Vector3 _pickOrigin;
        private static float _baseWidth = -1f;

        /// <summary>Talent slot being edited, or -1 when the picker is closed.</summary>
        public static int EditingSlot { get; private set; } = -1;

        public static bool IsPickItem(PetTalentUIElement e) => e != null && _pickItems.Contains(e);

        public static void Tick()
        {
            if (_window == null || !_built) return;
            if (!_window.isShowing) { EditingSlot = -1; return; }
            UpdateRows();
        }

        // ---------- layout (replaces PetTalentsWindow.PositionUIElements) ----------

        public static bool Layout(PetTalentsWindow w)
        {
            if (_failed) return false;
            try
            {
                _window = w;
                if (!_built) Build(w);
                if (_baseWidth < 0f) _baseWidth = w.background.size.x;

                float y = w.topEdge.localPosition.y;
                y = UIManager.PositionElementBeneath(w.textInputField.transform, y, w.textInputFieldBackground.size.y, 0.25f);
                y = UIManager.PositionElementBeneath(w.pointsText.transform, y, w.pointsText.dimensions.size.y, 0f);
                float talentsHeight = -w.talentsBotPos.localPosition.y;
                if (EditingSlot >= 0) talentsHeight = PickerHeight();
                y = UIManager.PositionElementBeneath(w.talentsContainer, y, talentsHeight, 0.0625f, moveDownHalfOfHeight: false);
                y = UIManager.PositionElementBeneath(w.resetButtonText.transform, y, w.resetButtonText.dimensions.size.y, 0.0625f);
                y = UIManager.PositionElementBeneath(w.resetButton.transform, y, w.resetButtonBackground.size.y, 0.0625f);
                if (_levelRow != null)
                    y = UIManager.PositionElementBeneath(_levelRow.root.transform, y, _rowHeight, 0.1875f);
                if (_colorRow != null)
                    y = UIManager.PositionElementBeneath(_colorRow.root.transform, y, _rowHeight, 0.0625f);

                float height = w.topEdge.localPosition.y - y + 0.25f;
                float width = EditingSlot >= 0 ? Mathf.Max(_baseWidth, PickerColumns * _pickStep + 0.5f) : _baseWidth;
                w.background.size = new Vector2(width, height);
                float offset = (height % 0.0625f > 0f) ? (0.0625f - height % 0.0625f) : 0f;
                w.background.transform.localPosition = new Vector3(0f, -height / 2f - offset, 0f);
                w.backgroundCollider.size = new Vector3(width, height, 0.1f);
                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogError($"[{PetEditorMod.Name}] Pet window layout failed, editor disabled: {ex}");
                return false;
            }
        }

        // ---------- talents (after PetTalentsWindow.UpdateTalents) ----------

        public static void AfterUpdateTalents(PetTalentsWindow w)
        {
            if (_failed || !_built) return;
            bool open = EditingSlot >= 0;
            foreach (var e in w.petTalentUIElements)
                if (e != null && e.gameObject.activeSelf == open) e.gameObject.SetActive(!open);

            var pet = PetData(out PetCD petCD);
            PetTalent current = default;
            bool hasCurrent = false;
            if (open && InventoryHandler.TryGetExtraInventoryBuffer(pet, out DynamicBuffer<PetTalentBuffer> buffer) && EditingSlot < buffer.Length)
            {
                current = buffer[EditingSlot].petTalentID;
                hasCurrent = true;
            }
            for (int i = 0; i < _pickItems.Count; i++)
            {
                var item = _pickItems[i];
                if (item.gameObject.activeSelf != open) item.gameObject.SetActive(open);
                if (!open) continue;
                item.UpdateTalent(EditingSlot, _pickInfos[i], petCD);
                item.icon.color = Color.white;
                item.completedBorder.enabled = hasCurrent && _pickInfos[i].petTalentID.Equals(current);
            }
        }

        public static void OpenPicker(int slot)
        {
            EditingSlot = slot;
        }

        public static void ClosePicker()
        {
            EditingSlot = -1;
        }

        public static void Pick(PetTalentUIElement item)
        {
            int idx = _pickItems.IndexOf(item);
            if (idx < 0 || EditingSlot < 0) return;
            Send($"/pet talent {EditingSlot + 1} {(int)_pickInfos[idx].petTalentID}");
            ClosePicker();
        }

        public static void RemovePoint(int slot) => Send($"/pet unpoint {slot + 1}");

        // ---------- level + color rows ----------

        private static void UpdateRows()
        {
            var pet = PetData(out _);
            if (_levelRow != null)
            {
                int level = PetExtensions.GetLevelFromXP(pet.objectData.amount);
                SetLabel(_levelRow, $"Lv {level}/{PetExtensions.maxLevel}");
                _levelRow.dec.canBeClicked = level > 1;
                _levelRow.inc.canBeClicked = level < PetExtensions.maxLevel;
            }
            if (_colorRow != null)
            {
                int count = SkinCount(pet);
                bool any = count > 1;
                SetLabel(_colorRow, any ? $"Color {ShownSkin(pet) + 1}/{count}" : "Color -");
                _colorRow.dec.canBeClicked = any;
                _colorRow.inc.canBeClicked = any;
            }
        }

        private static void SetLabel(Row row, string text)
        {
            if (row.shown == text) return;
            row.shown = text;
            row.label.Render(text, false, true);
        }

        private static void ChangeLevel(int delta)
        {
            var pet = PetData(out _);
            if (pet.objectID == ObjectID.None) return;
            int level = PetExtensions.GetLevelFromXP(pet.objectData.amount) + delta;
            if (level < 1 || level > PetExtensions.maxLevel) return;
            Send($"/pet level {level}");
        }

        private static int SkinCount(ContainedObjectsBuffer pet)
        {
            if (pet.objectID == ObjectID.None || Manager.ui == null || Manager.ui.petInfosTable == null) return 0;
            var info = Manager.ui.petInfosTable.GetPetSkinInfo(pet.objectID);
            return info != null && info.skins != null ? info.skins.Count : 0;
        }

        private static int CurrentSkin(ContainedObjectsBuffer pet)
        {
            return InventoryHandler.TryGetExtraInventoryData(pet, out PetSkinCD skin) ? skin.skinIndex : 0;
        }

        private static int _pendingSkin = -1;
        private static float _pendingUntil;

        /// <summary>Asks the server to change the color (see PetServer.SetColor) and shows the new value right away.</summary>
        private static void ChangeColor(int delta)
        {
            var pet = PetData(out _);
            int count = SkinCount(pet);
            if (count <= 1) return;
            int from = _pendingSkin >= 0 ? _pendingSkin : CurrentSkin(pet);
            int skin = ((from + delta) % count + count) % count;
            _pendingSkin = skin;
            _pendingUntil = Time.unscaledTime + 2f;
            Send($"/pet color {skin + 1}");
        }

        /// <summary>The requested color while it is on its way to the server, otherwise the pet's real color.</summary>
        private static int ShownSkin(ContainedObjectsBuffer pet)
        {
            int real = CurrentSkin(pet);
            if (_pendingSkin >= 0 && (real == _pendingSkin || Time.unscaledTime > _pendingUntil)) _pendingSkin = -1;
            return _pendingSkin >= 0 ? _pendingSkin : real;
        }

        // ---------- build ----------

        private static void Build(PetTalentsWindow w)
        {
            _built = true;
            _rowHeight = w.resetButtonBackground.size.y;
            _levelRow = BuildRow(w, "PetEditorLevelRow", "-", "+", () => ChangeLevel(-1), () => ChangeLevel(+1));
            _colorRow = BuildRow(w, "PetEditorColorRow", "<", ">", () => ChangeColor(-1), () => ChangeColor(+1));
            BuildPicker(w);
            Debug.Log($"[{PetEditorMod.Name}] Pet window extended: level + color rows, {_pickItems.Count} talents in the picker.");
        }

        /// <summary>
        /// A row "label   [dec] [inc]". The label is a clone of the points text with localization off (so
        /// our text is not looked up as a translation term) and the buttons are narrow clones of the reset button.
        /// </summary>
        private static Row BuildRow(PetTalentsWindow w, string name, string decLabel, string incLabel, UnityAction dec, UnityAction inc)
        {
            var row = new Row();
            row.root = new GameObject(name);
            row.root.transform.SetParent(w.resetButton.transform.parent, false);
            row.root.layer = w.resetButton.gameObject.layer;

            float half = w.background.size.x / 2f;
            float incX = half - 0.375f - ButtonWidth / 2f;
            float decX = -incX;

            row.label = UnityEngine.Object.Instantiate(w.pointsText.gameObject, row.root.transform).GetComponent<PugText>();
            PrepareText(row.label);
            row.label.transform.localPosition = Vector3.zero;
            row.label.transform.localScale = row.label.transform.localScale * LabelScale;
            SetOpaque(row.label.gameObject);

            row.dec = CloneButton(w, row.root.transform, decLabel, dec, new Vector3(decX, 0f, 0f));
            row.inc = CloneButton(w, row.root.transform, incLabel, inc, new Vector3(incX, 0f, 0f));
            return row;
        }

        private static ButtonUIElement CloneButton(PetTalentsWindow w, Transform parent, string label, UnityAction onClick, Vector3 pos)
        {
            Transform src = w.resetButton.transform;
            var go = UnityEngine.Object.Instantiate(src.gameObject, parent);
            go.name = "PetEditorButton" + label;
            go.transform.localPosition = pos;
            var button = go.GetComponent<ButtonUIElement>();
            button.onLeftClick = new UnityEvent();
            button.onLeftClick.AddListener(onClick);
            button.onRightClick = new UnityEvent();
            button.canBeClicked = true;

            // Narrow the background and the click area.
            var bg = FindByPath(src, w.resetButtonBackground != null ? w.resetButtonBackground.transform : null, go.transform);
            var bgSR = bg != null ? bg.GetComponent<SpriteRenderer>() : null;
            if (bgSR != null) bgSR.size = new Vector2(ButtonWidth, bgSR.size.y);
            var col = go.GetComponent<BoxCollider>();
            if (col != null) col.size = new Vector3(ButtonWidth, col.size.y, col.size.z);

            // The reset button shows a coin and its price; hide the coin and reuse the price text as the label.
            var coin = FindByPath(src, w.resetButtonCoinSR != null ? w.resetButtonCoinSR.transform : null, go.transform);
            if (coin != null) coin.gameObject.SetActive(false);
            var price = FindByPath(src, w.resetButtonCoinText != null ? w.resetButtonCoinText.transform : null, go.transform);
            var text = price != null ? price.GetComponent<PugText>() : null;
            SetOpaque(go);
            if (text != null)
            {
                PrepareText(text);
                text.transform.localPosition = new Vector3(0f, text.transform.localPosition.y, text.transform.localPosition.z);
                text.Render(label, false, true);
            }
            return button;
        }

        /// <summary>
        /// A cloned PugText still holds the source's letter sprites (as cloned children); free them so our
        /// text does not render on top of the old one, and turn off translation lookup for our plain text.
        /// </summary>
        private static void PrepareText(PugText text)
        {
            text.Clear(false, true);
            // Anything still under the clone is a leftover copy of the source's letters; drop it.
            for (int i = text.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(text.transform.GetChild(i).gameObject);
            text.localize = false;
            text.formatFields = new string[0];
        }

        /// <summary>The reset button may be greyed out when it is cloned; make the clone fully visible.</summary>
        private static void SetOpaque(GameObject go)
        {
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var c = sr.color;
                if (c.a < 1f) sr.color = new Color(c.r, c.g, c.b, 1f);
            }
            foreach (var t in go.GetComponentsInChildren<PugText>(true))
                t.SetTempColor(Color.white);
        }

        /// <summary>Finds in <paramref name="cloneRoot"/> the object at the same path <paramref name="target"/> has under <paramref name="srcRoot"/>.</summary>
        private static Transform FindByPath(Transform srcRoot, Transform target, Transform cloneRoot)
        {
            if (target == null) return null;
            var path = new List<int>();
            for (Transform t = target; t != null && t != srcRoot; t = t.parent) path.Add(t.GetSiblingIndex());
            Transform c = cloneRoot;
            for (int i = path.Count - 1; i >= 0; i--)
            {
                if (path[i] >= c.childCount) return null;
                c = c.GetChild(path[i]);
            }
            return c;
        }

        private static void BuildPicker(PetTalentsWindow w)
        {
            var table = Manager.ui != null ? Manager.ui.petInfosTable : null;
            if (table == null || table.petTalents == null || w.petTalentUIElements.Count < 4) return;
            var template = w.petTalentUIElements[0];
            Vector3 p0 = template.transform.localPosition;
            float dx = Mathf.Abs(w.petTalentUIElements[1].transform.localPosition.x - p0.x);
            if (dx < 0.01f) dx = 1.25f;
            _pickStep = dx * PickerScale;
            _pickOrigin = new Vector3(-(PickerColumns - 1) * _pickStep / 2f, p0.y, p0.z);

            for (int i = 0; i < table.petTalents.Count; i++)
            {
                var go = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
                go.name = "PetEditorPick_" + table.petTalents[i].petTalentID;
                int r = i / PickerColumns, c = i % PickerColumns;
                go.transform.localPosition = _pickOrigin + new Vector3(c * _pickStep, -r * _pickStep, 0f);
                go.transform.localScale = template.transform.localScale * PickerScale;
                go.SetActive(false);
                _pickItems.Add(go.GetComponent<PetTalentUIElement>());
                _pickInfos.Add(table.petTalents[i]);
            }
        }

        private static float PickerHeight()
        {
            int rows = (_pickItems.Count + PickerColumns - 1) / PickerColumns;
            float top = _window != null && _window.petTalentUIElements.Count > 0 ? _window.petTalentUIElements[0].transform.localPosition.y : 0f;
            // talents container height = distance from its top to the lowest row, plus half a cell.
            return -(top - (rows - 1) * _pickStep) + _pickStep * 0.6f;
        }

        // ---------- helpers ----------

        private static ContainedObjectsBuffer PetData(out PetCD petCD)
        {
            petCD = default;
            var player = Manager.main != null ? Manager.main.player : null;
            if (player == null) return default;
            var pet = player.equipmentHandler.petInventoryHandler.GetContainedObjectData(0);
            if (pet.objectID != ObjectID.None && PugDatabase.HasComponent<PetCD>(pet.objectData))
                petCD = PugDatabase.GetComponent<PetCD>(pet.objectData);
            return pet;
        }

        private static void Send(string command)
        {
            var comm = CommandModule.ClientCommSystem;
            if (comm == null)
            {
                Debug.LogWarning($"[{PetEditorMod.Name}] Command channel not ready.");
                return;
            }
            comm.SendCommand(command, CommandFlags.None);
        }
    }
}
