using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using MasterPet.Handlers;
using MasterPet.Helpers;

namespace MasterPet.UI
{
    public class MasterPetUI : MonoBehaviour
    {
        [SerializeField] private PugText modTitleText;
        [SerializeField] private SpriteRenderer petPreviewRenderer;
        [SerializeField] private ColorSwatch[] colorSwatches;
        [SerializeField] private TalentSlot[] talentSlots;
        [SerializeField] private PugText pointsCounter;
        [SerializeField] private LevelButton levelDownButton;
        [SerializeField] private LevelButton levelUpButton;
        [SerializeField] private PugText levelNumber;
        [SerializeField] private GameObject rightPanel;
        [SerializeField] private GameObject talentSelectorPanel;
        [SerializeField] private TalentSelectorButton[] talentSelectorButtons;
        [SerializeField] private BackButton backButton;
        [SerializeField] private PugText talentSelectorTitle;
        [SerializeField] private ApplyButton applyButton;
        [SerializeField] private GameObject leftPanel;
        [SerializeField] private GameObject decorMiddle;

        // Master Pet Plus: the selector shows every talent, laid out across the whole window.
        private const int SelectorColumns = 10;
        private const float SelectorCellScale = 0.8f;
        private const float SelectorSpacingX = 0.86f;
        private const float SelectorSpacingY = 0.9f;
        private const float SelectorTopY = 1.2f;
        private readonly List<TalentSelectorButton> _selectorButtons = new List<TalentSelectorButton>();
        private List<PetTalent> _selectorTalents;
        private bool _selectorLayoutApplied;

        // Master Pet Plus: on-screen controls help (cloned from the title text at runtime).
        private PugText _hintText;
        private const string MainHint =
            "LEFT-CLICK a talent slot: toggle its point.   RIGHT-CLICK a talent slot: swap in any talent.\n" +
            "+ / - : set pet level.   Colour swatches: pet skin.   Apply or Esc: close.";
        private const string SelectorHint =
            "Click a talent to put it in the highlighted slot. Hover for details. Back returns without changing anything.";

        private Material _petIconMaterial;
        private ObjectID _lastPetObjectID;
        private int _lastAuxDataIndex;
        private int _editingSlotIndex = -1;
        private int _currentLevel = 1;

        private Material[] _swatchMaterials;
        private Texture2D[] _swatchGradientTextures;
        private Texture2D _petPreviewTexture;

        private void Awake()
        {
            AutoAssignReferences();
        }

        private void OnEnable()
        {
            if (_petIconMaterial == null && Manager.ui.mouse != null && Manager.ui.mouse.grabbedItemSR != null)
                _petIconMaterial = Manager.ui.mouse.grabbedItemSR.material;

            if (modTitleText != null)
                modTitleText.Render($"{MasterPetMod.Name} v{MasterPetMod.Version}", true, true);

            if (talentSelectorPanel != null && talentSelectorPanel.activeSelf)
                CloseTalentSelector();

            EnsureHintText();
            ShowHint(MainHint);

            RefreshPetPreview();
            RefreshColorSwatches();
            RefreshLevel();
            RefreshTalents();

            levelDownButton.Select();
        }

        private void Update()
        {
            PlayerController player = Manager.main?.player;
            if (player == null) return;

            InventoryHandler petInventory = player.equipmentHandler?.petInventoryHandler;
            if (petInventory == null) return;

            ContainedObjectsBuffer contained = petInventory.GetContainedObjectData(0);

            if (contained.objectID != _lastPetObjectID || contained.auxDataIndex != _lastAuxDataIndex)
            {
                _lastPetObjectID = contained.objectID;
                _lastAuxDataIndex = contained.auxDataIndex;

                if (talentSelectorPanel != null && talentSelectorPanel.activeSelf)
                    CloseTalentSelector();

                RefreshPetPreview();
                RefreshColorSwatches();
                RefreshLevel();
                RefreshTalents();
            }
        }

        private void AutoAssignReferences()
        {
            Transform ui = transform.Find("UI");
            if (ui == null) ui = transform;

            if (modTitleText == null)
                modTitleText = ui.Find("Title")?.GetComponent<PugText>();

            if (petPreviewRenderer == null)
            {
                Transform icon = ui.Find("LeftPanel/PetPreview/Icon");
                petPreviewRenderer = icon != null ? icon.GetComponent<SpriteRenderer>() : ui.Find("LeftPanel/PetPreview")?.GetComponent<SpriteRenderer>();
            }

            if (applyButton == null)
                applyButton = ui.Find("LeftPanel/ApplyButton")?.GetComponent<ApplyButton>();

            if (colorSwatches == null || colorSwatches.Length == 0 || colorSwatches[0] == null)
            {
                Transform container = ui.Find("LeftPanel/ColorSwatches");
                if (container != null)
                {
                    var list = new List<ColorSwatch>();
                    foreach (Transform child in container)
                    {
                        if (child.name.StartsWith("ColorSwatch"))
                        {
                            var swatch = child.GetComponent<ColorSwatch>();
                            if (swatch != null) list.Add(swatch);
                        }
                    }
                    colorSwatches = list.ToArray();
                }
            }

            if (talentSlots == null || talentSlots.Length == 0 || talentSlots[0] == null)
            {
                Transform container = ui.Find("RightPanel/TalentGrid");
                if (container != null)
                {
                    var list = new List<TalentSlot>();
                    foreach (Transform child in container)
                    {
                        if (child.name.StartsWith("TalentSlot"))
                        {
                            var slot = child.GetComponent<TalentSlot>();
                            if (slot != null) list.Add(slot);
                        }
                    }
                    list.Sort((a, b) => GetSlotIndex(a.name).CompareTo(GetSlotIndex(b.name)));
                    talentSlots = list.ToArray();
                }
            }

            if (pointsCounter == null)
                pointsCounter = ui.Find("RightPanel/TalentGrid/PointsCounter")?.GetComponent<PugText>();

            if (levelNumber == null)
                levelNumber = ui.Find("LeftPanel/LevelNumber")?.GetComponent<PugText>();

            if (levelDownButton == null)
                levelDownButton = ui.Find("LeftPanel/LevelDownButton")?.GetComponent<LevelButton>();

            if (levelUpButton == null)
                levelUpButton = ui.Find("LeftPanel/LevelUpButton")?.GetComponent<LevelButton>();

            levelDownButton?.Setup(this);
            levelUpButton?.Setup(this);

            if (rightPanel == null)
                rightPanel = ui.Find("RightPanel")?.gameObject;

            if (leftPanel == null)
                leftPanel = ui.Find("LeftPanel")?.gameObject;

            if (decorMiddle == null)
                decorMiddle = ui.Find("DecorMiddle")?.gameObject;

            if (talentSelectorPanel == null)
                talentSelectorPanel = ui.Find("TalentSelectorPanel")?.gameObject;

            if (talentSelectorButtons == null || talentSelectorButtons.Length == 0 || talentSelectorButtons[0] == null)
            {
                Transform container = talentSelectorPanel != null ? talentSelectorPanel.transform.Find("TalentSelectorGrid") : null;
                if (container != null)
                {
                    var list = new List<TalentSelectorButton>();
                    foreach (Transform child in container)
                    {
                        if (child.name.StartsWith("TalentSelectorButton"))
                        {
                            var button = child.GetComponent<TalentSelectorButton>();
                            if (button != null) list.Add(button);
                        }
                    }
                    list.Sort((a, b) => GetSlotIndex(a.name).CompareTo(GetSlotIndex(b.name)));
                    talentSelectorButtons = list.ToArray();
                }
            }

            if (backButton == null)
                backButton = talentSelectorPanel?.transform.Find("TalentSelectorGrid/BackButton")?.GetComponent<BackButton>();

            if (talentSelectorTitle == null)
                talentSelectorTitle = talentSelectorPanel?.transform.Find("Title")?.GetComponent<PugText>();

            applyButton?.Setup(this);
            backButton?.Setup(this);
        }

        private int GetSlotIndex(string name)
        {
            int underscore = name.LastIndexOf('_');
            if (underscore < 0 || underscore >= name.Length - 1) return 0;
            if (int.TryParse(name.Substring(underscore + 1), out int index)) return index;
            return 0;
        }

        private void RefreshPetPreview()
        {
            if (petPreviewRenderer == null) return;
            if (!PetDataHandler.TryGetEquippedPet(Manager.main?.player, out ContainedObjectsBuffer contained)) return;

            ObjectInfo objectInfo = PugDatabase.GetObjectInfo(contained.objectID, contained.variation);
            if (objectInfo == null) return;

            petPreviewRenderer.sprite = objectInfo.icon ?? objectInfo.smallIcon;

            if (_petIconMaterial != null)
            {
                petPreviewRenderer.material = new Material(_petIconMaterial);
                petPreviewRenderer.material.DisableKeyword("USE_GRADIENT_MAP");
                petPreviewRenderer.material.SetTexture("_GradientMap", null);
            }

            Manager.ui.ApplyAnyIconGradientMap(contained, petPreviewRenderer);
        }

        private void RefreshColorSwatches()
        {
            if (colorSwatches == null || colorSwatches.Length == 0) return;
            if (!PetDataHandler.TryGetEquippedPet(Manager.main?.player, out ContainedObjectsBuffer contained)) return;

            PetInfosTable petInfosTable = Manager.ui?.petInfosTable;
            if (petInfosTable == null) return;

            PetInfosTable.PetSkinInfo skinInfo = petInfosTable.GetPetSkinInfo(contained.objectID);
            ObjectInfo objectInfo = PugDatabase.GetObjectInfo(contained.objectID, contained.variation);

            bool useLargeIcon = UIHelper.NeedsLargeIcon(contained.objectID);
            float targetSize = UIHelper.GetPetTargetSize(contained.objectID);

            Sprite petSprite = objectInfo == null ? null :
                (useLargeIcon ? objectInfo.icon ?? objectInfo.smallIcon : objectInfo.smallIcon ?? objectInfo.icon);

            PetSkinCD currentSkin;
            if (!PetDataHandler.TryGetPetSkin(contained, out currentSkin))
                currentSkin = default;

            if (skinInfo == null || skinInfo.skins == null || skinInfo.skins.Count == 0)
            {
                for (int i = 0; i < colorSwatches.Length; i++)
                {
                    if (colorSwatches[i] == null) continue;

                    if (i == 0)
                    {
                        colorSwatches[i].gameObject.SetActive(true);
                        colorSwatches[i].Setup(this, i);
                        colorSwatches[i].SetSelected(true);

                        SpriteRenderer sr = colorSwatches[i].IconRenderer;
                        if (sr != null && petSprite != null)
                        {
                            UIHelper.FitSpriteToSize(sr, petSprite, targetSize);

                            if (_petIconMaterial != null)
                            {
                                sr.material = new Material(_petIconMaterial);
                                sr.material.DisableKeyword("USE_GRADIENT_MAP");
                                sr.material.SetTexture("_GradientMap", null);
                            }
                        }

                        Transform selectedChild = colorSwatches[i].transform.Find("Selected");
                        if (selectedChild != null)
                            selectedChild.gameObject.SetActive(true);
                    }
                    else
                    {
                        colorSwatches[i].gameObject.SetActive(false);
                    }
                }
                return;
            }

            if (_swatchMaterials == null || _swatchMaterials.Length != colorSwatches.Length)
                _swatchMaterials = new Material[colorSwatches.Length];

            if (_swatchGradientTextures == null || _swatchGradientTextures.Length != colorSwatches.Length)
                _swatchGradientTextures = new Texture2D[colorSwatches.Length];

            for (int i = 0; i < colorSwatches.Length; i++)
            {
                if (colorSwatches[i] == null) continue;
                if (i >= skinInfo.skins.Count) { colorSwatches[i].gameObject.SetActive(false); continue; }

                colorSwatches[i].gameObject.SetActive(true);
                colorSwatches[i].Setup(this, i);
                colorSwatches[i].SetSelected(i == currentSkin.skinIndex);

                SpriteRenderer sr = colorSwatches[i].IconRenderer;
                if (sr == null || petSprite == null) continue;

                UIHelper.FitSpriteToSize(sr, petSprite, targetSize);

                GradientMapDataBlock gradient = skinInfo.skins[i].primaryGradientMap;
                if (gradient == null || !gradient.hasData || _petIconMaterial == null) continue;

                if (_swatchMaterials[i] == null)
                {
                    _swatchMaterials[i] = new Material(_petIconMaterial);
                    _swatchMaterials[i].DisableKeyword("USE_GRADIENT_MAP");
                    _swatchMaterials[i].SetTexture("_GradientMap", null);
                }

                sr.material = _swatchMaterials[i];
                sr.material.DisableKeyword("USE_GRADIENT_MAP");
                sr.material.SetTexture("_GradientMap", null);

                if (_swatchGradientTextures[i] == null || _swatchGradientTextures[i].width != gradient.textureWidth)
                {
                    if (_swatchGradientTextures[i] != null) Destroy(_swatchGradientTextures[i]);
                    _swatchGradientTextures[i] = new Texture2D(gradient.textureWidth, 1, TextureFormat.ARGB32, false);
                }

                UIHelper.FillGradientTexture(_swatchGradientTextures[i], gradient);
                sr.material.EnableKeyword("USE_GRADIENT_MAP");
                sr.material.SetTexture("_GradientMap", _swatchGradientTextures[i]);

                Transform selectedChild = colorSwatches[i].transform.Find("Selected");
                if (selectedChild != null)
                    selectedChild.gameObject.SetActive(i == currentSkin.skinIndex);
            }
        }

        private bool HasPointsInRow(DynamicBuffer<PetTalentBuffer> talents, int a, int b, int c)
        {
            if (talents.Length <= c) return false;
            return talents[a].points != 0 || talents[b].points != 0 || talents[c].points != 0;
        }

        private void ClearSlot(PlayerController player, DynamicBuffer<PetTalentBuffer> talents, int index)
        {
            if (talents[index].points == 0) return;

            PetTalentBuffer updated = talents[index];
            updated.points = 0;
            talents[index] = updated;
            PetActionHandler.TrySetPetTalentPoints(player, index, 0);
        }

        private void ClearAllTalentsOnLevelDown(int previousLevel, int newLevel)
        {
            if (newLevel >= previousLevel) return;

            PlayerController player = Manager.main?.player;
            if (player == null) return;
            if (!PetDataHandler.TryGetEquippedPet(player, out ContainedObjectsBuffer contained)) return;
            if (!PetDataHandler.TryGetPetTalents(contained, out DynamicBuffer<PetTalentBuffer> talents)) return;

            for (int i = 0; i < talents.Length; i++)
                ClearSlot(player, talents, i);
        }

        private void RefreshTalents()
        {
            if (talentSlots == null || talentSlots.Length == 0) return;
            if (!PetDataHandler.TryGetEquippedPet(Manager.main?.player, out ContainedObjectsBuffer contained)) return;

            PetCD petCD;
            if (!PetDataHandler.TryGetPetCD(contained, out petCD)) petCD = default;

            if (!PetDataHandler.TryGetPetTalents(contained, out DynamicBuffer<PetTalentBuffer> talents)) return;

            int petLevel = _currentLevel;

            bool row1HasPoints = HasPointsInRow(talents, 0, 1, 2);
            bool row2HasPoints = HasPointsInRow(talents, 3, 4, 5);

            bool row1Unlocked = petLevel >= 2;
            bool row2Unlocked = petLevel >= 4 && row1HasPoints;
            bool row3Unlocked = petLevel >= 6 && row2HasPoints;

            bool[] unlocked = new bool[talentSlots.Length];
            unlocked[0] = row1Unlocked;
            unlocked[1] = row1Unlocked;
            unlocked[2] = row1Unlocked;
            unlocked[3] = row2Unlocked;
            unlocked[4] = row2Unlocked;
            unlocked[5] = row2Unlocked;
            unlocked[6] = row3Unlocked;
            unlocked[7] = row3Unlocked;
            unlocked[8] = row3Unlocked;

            int maxPoints = 0;
            if (petLevel >= 2) maxPoints = 1;
            if (petLevel >= 4) maxPoints = 2;
            if (petLevel >= 6) maxPoints = 3;
            if (petLevel >= 8) maxPoints = 4;
            if (petLevel >= 10) maxPoints = 5;

            int spent = 0;
            for (int i = 0; i < talents.Length && i < unlocked.Length; i++)
            {
                if (unlocked[i] && talents[i].points > 0)
                    spent++;
            }
            int available = maxPoints - spent;

            if (pointsCounter != null)
                pointsCounter.Render($"Points: {available}", true, true);

            for (int i = 0; i < talentSlots.Length; i++)
            {
                if (talentSlots[i] == null) continue;
                if (i >= talents.Length) { talentSlots[i].gameObject.SetActive(false); continue; }

                talentSlots[i].gameObject.SetActive(true);
                talentSlots[i].Setup(this, i);
                talentSlots[i].SetLocked(!unlocked[i]);

                PetTalent talent = talents[i].petTalentID;
                bool active = talents[i].points != 0 && unlocked[i];
                talentSlots[i].SetTalent(talent, petCD.petType, active);
            }
        }

        private void RefreshLevel()
        {
            if (levelNumber == null) return;
            if (!PetDataHandler.TryGetEquippedPet(Manager.main?.player, out ContainedObjectsBuffer contained)) return;

            _currentLevel = PetDataHandler.GetPetLevel(contained);
            levelNumber.Render($"Level: {_currentLevel}", true, true);
        }

        public void OnLevelUpClicked() => ChangePetLevel(1);
        public void OnLevelDownClicked() => ChangePetLevel(-1);

        private void ChangePetLevel(int delta)
        {
            int targetLevel = Mathf.Clamp(_currentLevel + delta, 1, PetExtensions.maxLevel);
            if (targetLevel == _currentLevel) return;

            int targetXP = PetExtensions.GetXPFromLevel(targetLevel);
            if (!PetActionHandler.TrySetPetXP(Manager.main?.player, targetXP)) return;

            int previousLevel = _currentLevel;
            _currentLevel = targetLevel;
            levelNumber?.Render($"Level: {_currentLevel}", true, true);

            ClearAllTalentsOnLevelDown(previousLevel, _currentLevel);
            RefreshTalents();
        }

        public void OnTalentSlotClicked(int slotIndex)
        {
            if (!PetDataHandler.TryGetEquippedPet(Manager.main?.player, out ContainedObjectsBuffer contained)) return;
            if (!PetDataHandler.TryGetPetTalents(contained, out DynamicBuffer<PetTalentBuffer> talents)) return;
            if (slotIndex < 0 || slotIndex >= talents.Length) return;

            int currentPoints = talents[slotIndex].points;
            int newPoints = currentPoints != 0 ? 0 : 1;

            PetActionHandler.TrySetPetTalentPoints(Manager.main?.player, slotIndex, newPoints);

            PetTalentBuffer updated = talents[slotIndex];
            updated.points = newPoints;
            talents[slotIndex] = updated;

            RefreshTalents();
        }

        public void OpenTalentSelector(int slotIndex)
        {
            _editingSlotIndex = slotIndex;
            PopulateTalentSelector();
            if (rightPanel != null) rightPanel.SetActive(false);
            if (leftPanel != null) leftPanel.SetActive(false);
            if (decorMiddle != null) decorMiddle.SetActive(false);
            if (talentSelectorPanel != null) talentSelectorPanel.SetActive(true);

            ShowHint(SelectorHint);

            if (backButton != null)
                backButton.Select();
        }

        public void CloseTalentSelector()
        {
            if (rightPanel != null) rightPanel.SetActive(true);
            if (leftPanel != null) leftPanel.SetActive(true);
            if (decorMiddle != null) decorMiddle.SetActive(true);
            if (talentSelectorPanel != null) talentSelectorPanel.SetActive(false);

            ShowHint(MainHint);

            if (_editingSlotIndex >= 0 && _editingSlotIndex < talentSlots.Length)
            {
                TalentSlot slot = talentSlots[_editingSlotIndex];
                if (slot != null)
                    slot.Select();
            }

            _editingSlotIndex = -1;
        }

        private void EnsureHintText()
        {
            if (_hintText != null || modTitleText == null) return;

            Transform ui = transform.Find("UI");
            if (ui == null) ui = transform;

            GameObject clone = Instantiate(modTitleText.gameObject, ui);
            clone.name = "HintText";
            clone.transform.localPosition = new Vector3(0f, -2.72f, modTitleText.transform.localPosition.z);
            clone.transform.localScale = Vector3.one;

            SpriteRenderer bar = clone.GetComponent<SpriteRenderer>();
            if (bar != null) bar.enabled = false;

            _hintText = clone.GetComponent<PugText>();
            if (_hintText != null)
            {
                _hintText.localize = false;
                _hintText.maxWidth = 8.6f;
            }
        }

        private void ShowHint(string text)
        {
            if (_hintText == null) return;
            _hintText.Render(text, true, true);
        }

        private void ApplySelectorLayout()
        {
            if (_selectorLayoutApplied || talentSelectorPanel == null) return;

            // Move the selector panel to the window centre; the grid now spans the whole window.
            Vector3 panelPos = talentSelectorPanel.transform.localPosition;
            talentSelectorPanel.transform.localPosition = new Vector3(0f, panelPos.y, panelPos.z);

            Transform grid = talentSelectorPanel.transform.Find("TalentSelectorGrid");
            if (grid != null)
                grid.localPosition = new Vector3(0f, 0f, grid.localPosition.z);

            // The decorative divider lines were positioned for the old 3x3 layout.
            Transform decor = talentSelectorPanel.transform.Find("Decor");
            if (decor != null) decor.gameObject.SetActive(false);
            Transform decor2 = talentSelectorPanel.transform.Find("Decor2");
            if (decor2 != null) decor2.gameObject.SetActive(false);

            _selectorLayoutApplied = true;
        }

        private static Vector3 GetSelectorCellPosition(int cellIndex, float z)
        {
            int col = cellIndex % SelectorColumns;
            int row = cellIndex / SelectorColumns;
            float x0 = -(SelectorColumns - 1) * SelectorSpacingX * 0.5f;
            return new Vector3(x0 + col * SelectorSpacingX, SelectorTopY - row * SelectorSpacingY, z);
        }

        private void EnsureSelectorButtons(int count)
        {
            if (_selectorButtons.Count == 0 && talentSelectorButtons != null)
            {
                foreach (TalentSelectorButton existing in talentSelectorButtons)
                {
                    if (existing != null)
                        _selectorButtons.Add(existing);
                }
            }

            if (_selectorButtons.Count == 0) return;

            TalentSelectorButton template = _selectorButtons[0];
            while (_selectorButtons.Count < count)
            {
                GameObject clone = Instantiate(template.gameObject, template.transform.parent);
                clone.name = "TalentSelectorButton_" + _selectorButtons.Count;
                TalentSelectorButton button = clone.GetComponent<TalentSelectorButton>();
                if (button == null) { Destroy(clone); break; }
                _selectorButtons.Add(button);
            }
        }

        private void PopulateTalentSelector()
        {
            if (!PetDataHandler.TryGetEquippedPet(Manager.main?.player, out ContainedObjectsBuffer contained)) return;

            // Master Pet Plus: offer every talent in the game, not only the pet's own roll pool.
            if (!PetActionHandler.TryGetAllPetTalents(out _selectorTalents))
            {
                if (!PetActionHandler.TryGetPetTalentPool(Manager.main?.player, out _selectorTalents)) return;
            }

            PetCD petCD;
            if (!PetDataHandler.TryGetPetCD(contained, out petCD)) petCD = default;

            ApplySelectorLayout();
            EnsureSelectorButtons(_selectorTalents.Count);

            if (talentSelectorTitle != null)
            {
                talentSelectorTitle.localize = false;
                talentSelectorTitle.Render($"Slot {_editingSlotIndex + 1}: choose any talent", true, true);
            }

            // Cell 0 is the back button, talents fill the cells after it.
            if (backButton != null)
            {
                backButton.transform.localPosition = GetSelectorCellPosition(0, backButton.transform.localPosition.z);
                backButton.transform.localScale = new Vector3(SelectorCellScale, SelectorCellScale, 1f);
            }

            for (int i = 0; i < _selectorButtons.Count; i++)
            {
                TalentSelectorButton button = _selectorButtons[i];
                if (button == null) continue;

                if (i >= _selectorTalents.Count) { button.gameObject.SetActive(false); continue; }

                button.transform.localPosition = GetSelectorCellPosition(i + 1, button.transform.localPosition.z);
                button.transform.localScale = new Vector3(SelectorCellScale, SelectorCellScale, 1f);
                button.gameObject.SetActive(true);
                button.Setup(this, i, _selectorTalents[i], petCD.petType);
            }
        }

        public void OnTalentSelectorButtonClicked(int talentIndex)
        {
            if (_editingSlotIndex < 0 || _editingSlotIndex >= talentSlots.Length) return;
            if (!PetDataHandler.TryGetEquippedPet(Manager.main?.player, out ContainedObjectsBuffer contained)) return;
            if (_selectorTalents == null || talentIndex < 0 || talentIndex >= _selectorTalents.Count) return;

            PetTalent selectedTalent = _selectorTalents[talentIndex];
            PetActionHandler.TrySetPetTalentID(Manager.main?.player, _editingSlotIndex, selectedTalent);

            if (PetDataHandler.TryGetPetTalents(contained, out DynamicBuffer<PetTalentBuffer> talents) &&
                _editingSlotIndex >= 0 && _editingSlotIndex < talents.Length)
            {
                PetTalentBuffer updated = talents[_editingSlotIndex];
                updated.petTalentID = selectedTalent;
                talents[_editingSlotIndex] = updated;
            }

            RefreshTalents();
            CloseTalentSelector();
        }

        public void OnBackButtonClicked() => CloseTalentSelector();
        public void OnApplyClicked() => gameObject.SetActive(false);

        public void OnColorSwatchClicked(int skinIndex)
        {
            if (!PetActionHandler.TrySetPetSkin(Manager.main?.player, skinIndex)) return;
            if (!PetDataHandler.TryGetEquippedPet(Manager.main?.player, out ContainedObjectsBuffer contained)) return;

            PetInfosTable petInfosTable = Manager.ui?.petInfosTable;
            if (petInfosTable != null && petPreviewRenderer != null)
            {
                PetInfosTable.PetSkinInfo skinInfo = petInfosTable.GetPetSkinInfo(contained.objectID);
                if (skinInfo != null && skinInfo.skins.Count > skinIndex)
                {
                    GradientMapDataBlock gradient = skinInfo.skins[skinIndex].primaryGradientMap;
                    if (gradient != null && gradient.hasData)
                    {
                        if (_petIconMaterial != null)
                        {
                            petPreviewRenderer.material = new Material(_petIconMaterial);
                            petPreviewRenderer.material.DisableKeyword("USE_GRADIENT_MAP");
                            petPreviewRenderer.material.SetTexture("_GradientMap", null);
                        }

                        if (_petPreviewTexture == null || _petPreviewTexture.width != gradient.textureWidth)
                        {
                            if (_petPreviewTexture != null) Destroy(_petPreviewTexture);
                            _petPreviewTexture = new Texture2D(gradient.textureWidth, 1, TextureFormat.ARGB32, false);
                        }

                        UIHelper.FillGradientTexture(_petPreviewTexture, gradient);
                        petPreviewRenderer.material.EnableKeyword("USE_GRADIENT_MAP");
                        petPreviewRenderer.material.SetTexture("_GradientMap", _petPreviewTexture);
                    }
                }
            }

            if (colorSwatches == null) return;
            for (int i = 0; i < colorSwatches.Length; i++)
            {
                if (colorSwatches[i] == null) continue;
                if (colorSwatches[i].gameObject.activeSelf)
                {
                    bool isSelected = (i == skinIndex);
                    colorSwatches[i].SetSelected(isSelected);
                    Transform selectedChild = colorSwatches[i].transform.Find("Selected");
                    if (selectedChild != null) selectedChild.gameObject.SetActive(isSelected);
                }
            }
        }
    }
}