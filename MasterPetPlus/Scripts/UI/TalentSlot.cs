using System.Collections.Generic;
using UnityEngine;
using MasterPet.Helpers;

namespace MasterPet.UI
{
    public class TalentSlot : UIelement
    {
        [SerializeField] private GameObject hoverObject;
        [SerializeField] private GameObject selectedObject;
        [SerializeField] private SpriteRenderer iconRenderer;

        private MasterPetUI _ui;
        private int _slotIndex;
        private PetInfosTable.PetTalentInfo _petTalentInfo;
        private PetType _petType;
        private bool _active;
        private bool _locked;

        private SpriteRenderer _slotBackground;
        private Color _defaultSlotColor = Color.white;
        private Color _defaultIconColor = Color.white;
        private bool _defaultsCaptured;

        public void Setup(MasterPetUI ui, int slotIndex)
        {
            _ui = ui;
            _slotIndex = slotIndex;

            if (_slotBackground == null)
                _slotBackground = GetComponent<SpriteRenderer>();

            if (iconRenderer == null)
            {
                Transform iconChild = transform.Find("Icon");
                if (iconChild != null)
                    iconRenderer = iconChild.GetComponent<SpriteRenderer>();
            }

            if (selectedObject == null)
            {
                Transform selectedChild = transform.Find("Selected");
                if (selectedChild != null)
                    selectedObject = selectedChild.gameObject;
            }

            if (hoverObject == null)
            {
                Transform hoverChild = transform.Find("Hover");
                if (hoverChild != null)
                    hoverObject = hoverChild.gameObject;
            }

            if (!_defaultsCaptured)
            {
                if (_slotBackground != null)
                    _defaultSlotColor = _slotBackground.color;

                if (iconRenderer != null)
                    _defaultIconColor = iconRenderer.color;

                _defaultsCaptured = true;
            }

            SetLocked(_locked);
            SetSelected(false);
        }

        public void SetTalent(PetTalent talent, PetType petType, bool active)
        {
            _petType = petType;
            _active = active && !_locked;

            PetInfosTable petInfosTable = Manager.ui?.petInfosTable;
            if (petInfosTable != null)
                _petTalentInfo = petInfosTable.GetTalent(talent);
            else
                _petTalentInfo = default(PetInfosTable.PetTalentInfo);

            if (iconRenderer != null)
            {
                iconRenderer.sprite = _petTalentInfo.GetIcon(petType);
                Color c = _defaultIconColor;
                c.a = _locked ? 0.35f : _defaultIconColor.a;
                iconRenderer.color = c;
            }

            SetSelected(active);
        }

        public void SetSelected(bool selected)
        {
            _active = selected && !_locked;
            if (selectedObject != null)
                selectedObject.SetActive(selected && !_locked);
        }

        public void SetLocked(bool locked)
        {
            _locked = locked;

            if (_slotBackground != null)
            {
                Color c = _defaultSlotColor;
                c.a = locked ? 0.35f : _defaultSlotColor.a;
                _slotBackground.color = c;
            }

            if (iconRenderer != null)
            {
                Color c = _defaultIconColor;
                c.a = locked ? 0.35f : _defaultIconColor.a;
                iconRenderer.color = c;
            }

            if (locked && selectedObject != null)
                selectedObject.SetActive(false);
        }

        public override void OnSelected()
        {
            if (hoverObject != null)
                hoverObject.SetActive(true);
        }

        public override void OnDeselected(bool playEffect = true)
        {
            if (hoverObject != null)
                hoverObject.SetActive(false);
        }

        public override void OnLeftClicked(bool mod1, bool mod2)
        {
            if (_locked || _ui == null) return;

            MasterPetSounds.PlayButtonClick();
            _ui.OnTalentSlotClicked(_slotIndex);
        }

        public override void OnRightClicked(bool mod1, bool mod2)
        {
            if (_locked || _ui == null) return;

            MasterPetSounds.PlayButtonClick();
            _ui.OpenTalentSelector(_slotIndex);
        }

        public override TextAndFormatFields GetHoverTitle()
        {
            return new TextAndFormatFields
            {
                text = "PetTalents/" + _petTalentInfo.petTalentID.ToString() + _petType.ToString()
            };
        }

        public override List<TextAndFormatFields> GetHoverDescription()
        {
            if (_locked)
            {
                return new List<TextAndFormatFields>
                {
                    new TextAndFormatFields { text = "Locked: raise the pet level (+ button) and put a point in the row above.", color = Color.gray, dontLocalize = true }
                };
            }

            return new List<TextAndFormatFields>
            {
                new TextAndFormatFields { text = "Left-click: toggle point.  Right-click: swap in any talent.", color = Color.gray, dontLocalize = true }
            };
        }

        public override List<TextAndFormatFields> GetHoverStats(bool previewReinforced)
        {
            PlayerController player = Manager.main?.player;
            if (player == null)
                return null;

            InventoryHandler petInventory = player.equipmentHandler?.petInventoryHandler;
            if (petInventory == null)
                return null;

            ContainedObjectsBuffer contained = petInventory.GetContainedObjectData(0);
            if (contained.objectID == ObjectID.None)
                return null;

            ObjectID objectID = contained.objectID;
            float multiplier = 1f;

            if (_petTalentInfo.multiplierOverrides != null)
            {
                foreach (PetInfosTable.PetTalentMultiplierOverride petTalentMultiplierOverride in _petTalentInfo.multiplierOverrides)
                {
                    if (petTalentMultiplierOverride.petId == objectID)
                    {
                        multiplier = petTalentMultiplierOverride.multiplier;
                        break;
                    }
                }
            }

            float buffMultiplier = 1f;
            if (player.activePet != null)
            {
                buffMultiplier += (float)EntityUtility.GetConditionEffectValue(ConditionEffect.BuffsIncrease, player.activePet.entity, base.world) / 100f;
            }

            int baseValue = (_petType == PetType.Buff) ? _petTalentInfo.buffValue : _petTalentInfo.value;
            ConditionData conditionData = new ConditionData
            {
                conditionID = _petTalentInfo.conditionID,
                value = (int)Mathf.Round(baseValue * multiplier * buffMultiplier)
            };

            TextAndFormatFields conditionText = ConditionUI.GetConditionTextAndFormatFields(
                default(ContainedObjectsBuffer),
                conditionData,
                false,
                false,
                false,
                _petType == PetType.Buff
            );

            if (conditionText == null)
                return null;

            conditionText.color = _active ? Color.yellow : Color.gray;

            return new List<TextAndFormatFields> { conditionText };
        }
    }
}