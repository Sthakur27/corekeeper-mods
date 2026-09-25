using UnityEngine;
using MasterPet.Helpers;

namespace MasterPet.UI
{
    public class ColorSwatch : UIelement
    {
        [SerializeField] private GameObject hoverObject;
        [SerializeField] private SpriteRenderer iconRenderer;

        private MasterPetUI _ui;
        private int _skinIndex;
        private bool _isSelected;

        public SpriteRenderer IconRenderer
        {
            get
            {
                if (iconRenderer == null)
                {
                    Transform iconTransform = transform.Find("Icon");
                    if (iconTransform != null)
                        iconRenderer = iconTransform.GetComponent<SpriteRenderer>();
                }
                return iconRenderer;
            }
        }

        public void Setup(MasterPetUI ui, int skinIndex)
        {
            _ui = ui;
            _skinIndex = skinIndex;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (_isSelected && hoverObject != null)
                hoverObject.SetActive(false);
        }

        public override void OnSelected()
        {
            if (!_isSelected && hoverObject != null)
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
                _ui.OnColorSwatchClicked(_skinIndex);
            }
        }
    }
}