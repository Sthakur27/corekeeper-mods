using UnityEngine;
using MasterPet.Helpers;

namespace MasterPet.UI
{
    public class LevelButton : UIelement
    {
        public enum ButtonType
        {
            Down,
            Up
        }

        [SerializeField] private ButtonType buttonType;
        [SerializeField] private GameObject hoverObject;
        [SerializeField] private PugText labelText;

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

            if (labelText == null)
            {
                Transform labelChild = transform.Find("Label");
                if (labelChild != null)
                    labelText = labelChild.GetComponent<PugText>();
            }

            if (labelText != null)
            {
                labelText.localize = false;
                labelText.Render(buttonType == ButtonType.Down ? "-" : "+", true, true);
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
            if (_ui == null) return;

            MasterPetSounds.PlayButtonClick();

            if (buttonType == ButtonType.Down)
                _ui.OnLevelDownClicked();
            else
                _ui.OnLevelUpClicked();
        }
    }
}