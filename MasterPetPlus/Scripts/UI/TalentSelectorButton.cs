using System.Collections.Generic;
using UnityEngine;
using MasterPet.Helpers;

namespace MasterPet.UI
{
    public class TalentSelectorButton : UIelement
    {
        [SerializeField] private GameObject hoverObject;
        [SerializeField] private SpriteRenderer iconRenderer;

        private MasterPetUI _ui;
        private int _talentIndex;
        private PetTalent _petTalent;
        private PetType _petType;

        public void Setup(MasterPetUI ui, int talentIndex, PetTalent petTalent, PetType petType)
        {
            _ui = ui;
            _talentIndex = talentIndex;
            _petTalent = petTalent;
            _petType = petType;

            if (iconRenderer == null)
            {
                Transform iconChild = transform.Find("Icon");
                if (iconChild != null)
                    iconRenderer = iconChild.GetComponent<SpriteRenderer>();
            }

            if (hoverObject == null)
            {
                Transform hoverChild = transform.Find("Hover");
                if (hoverChild != null)
                    hoverObject = hoverChild.gameObject;
            }

            if (iconRenderer != null)
            {
                PetInfosTable petInfosTable = Manager.ui?.petInfosTable;
                if (petInfosTable != null)
                {
                    PetInfosTable.PetTalentInfo talentInfo = petInfosTable.GetTalent(petTalent);
                    iconRenderer.sprite = talentInfo.GetIcon(petType);
                }
                else
                {
                    iconRenderer.sprite = null;
                }
            }
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
            if (_ui != null)
            {
                MasterPetSounds.PlayButtonClick();
                _ui.OnTalentSelectorButtonClicked(_talentIndex);
            }
        }

        public override TextAndFormatFields GetHoverTitle()
        {
            return new TextAndFormatFields
            {
                text = "PetTalents/" + _petTalent.ToString() + _petType.ToString()
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
            PetInfosTable petInfosTable = Manager.ui?.petInfosTable;
            if (petInfosTable == null)
                return null;

            PetInfosTable.PetTalentInfo talentInfo = petInfosTable.GetTalent(_petTalent);

            float multiplier = 1f;
            if (talentInfo.multiplierOverrides != null)
            {
                foreach (PetInfosTable.PetTalentMultiplierOverride petTalentMultiplierOverride in talentInfo.multiplierOverrides)
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

            int baseValue = (_petType == PetType.Buff) ? talentInfo.buffValue : talentInfo.value;

            ConditionData conditionData = new ConditionData
            {
                conditionID = talentInfo.conditionID,
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

            conditionText.color = Color.yellow;

            return new List<TextAndFormatFields> { conditionText };
        }
    }
}