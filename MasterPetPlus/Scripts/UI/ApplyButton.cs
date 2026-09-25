using UnityEngine;
using MasterPet.Helpers;

namespace MasterPet.UI
{
    public class ApplyButton : UIelement
    {
        [SerializeField] private GameObject hoverObject;

        private MasterPetUI _ui;

        public void Setup(MasterPetUI ui)
        {
            _ui = ui;

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
            if (_ui != null)
            {
                MasterPetSounds.PlayClose();
                _ui.OnApplyClicked();
            }
        }
    }
}