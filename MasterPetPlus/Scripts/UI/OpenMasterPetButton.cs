using UnityEngine;
using MasterPet.Helpers;

namespace MasterPet.UI
{
    public class OpenMasterPetButton : UIelement
    {
        [SerializeField] private GameObject hoverObject;

        public void Setup()
        {
            if (hoverObject == null)
            {
                Transform hoverChild = transform.Find("Hover");
                if (hoverChild != null)
                    hoverObject = hoverChild.gameObject;
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
            if (MasterPetMod.UIInstance == null)
                return;

            if (!MasterPetMod.UIInstance.activeSelf)
            {
                if (Manager.ui != null)
                    Manager.ui.TryHideAllInventoryAndCraftingUI(true);

                MasterPetSounds.PlayOpen();
                MasterPetMod.UIInstance.SetActive(true);
            }
        }
    }
}