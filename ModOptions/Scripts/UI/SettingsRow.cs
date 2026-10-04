using System;
using UnityEngine;

namespace ModOptions.UI
{
    /// <summary>
    /// A row in our menus. Either a value row (label + value, left/right or click changes it) or a
    /// button row (label only, click runs <see cref="OnClick"/>). Added at runtime to rows cloned from
    /// the vanilla gameplay settings screen, replacing their vanilla option component.
    /// </summary>
    public class SettingsRow : RadicalMenuOption
    {
        public SettingBase Setting;
        public Action OnClick;
        public string LabelString = "";

        /// <summary>When set, the first click only arms the row (label = <see cref="ConfirmLabel"/>); a second click within a few seconds runs <see cref="OnClick"/>.</summary>
        public string ConfirmLabel;
        private const float ConfirmSeconds = 3f;
        private float _armedUntil = -1f;

        /// <summary>Description text: shown, never selected.</summary>
        public bool IsInfo;

        public override bool IsSelectionEnabled(bool visualOnly = false) => !IsInfo && base.IsSelectionEnabled(visualOnly);

        public override void OnParentMenuActivation()
        {
            base.OnParentMenuActivation();
            Refresh();
        }

        private const float ClickGuardSeconds = 0.4f;

        private static readonly Color InfoColor = new Color(0.72f, 0.78f, 0.84f, 1f);

        public void Refresh()
        {
            if (IsInfo && labelText != null)
            {
                labelText.SetFont(TextManager.FontFace.thinSmall);
                labelText.Render(LabelString);
                labelText.SetTempColor(InfoColor);
                return;
            }
            if (labelText != null) labelText.Render(LabelString);
            if (valueText != null && Setting != null) valueText.Render("<  " + Setting.DisplayValue + "  >");
        }

        public override void OnActivated()
        {
            base.OnActivated();
            if (Setting != null) Change(1);
            else Click();
        }

        private void Click()
        {
            if (ConfirmLabel == null)
            {
                OnClick?.Invoke();
                return;
            }
            if (Time.unscaledTime > _armedUntil)
            {
                _armedUntil = Time.unscaledTime + ConfirmSeconds;
                if (labelText != null) labelText.Render(ConfirmLabel);
                return;
            }
            _armedUntil = -1f;
            OnClick?.Invoke();
            Refresh();
        }

        protected override void Update()
        {
            base.Update();
            if (_armedUntil > 0f && Time.unscaledTime > _armedUntil)
            {
                _armedUntil = -1f;
                Refresh(); // confirmation timed out: back to the normal label
            }
        }

        public override void OnCleanupAndReset()
        {
            base.OnCleanupAndReset();
            _armedUntil = -1f;
        }

        public override bool OnSkimLeft()
        {
            if (Setting == null) return false;
            Change(-1);
            return true;
        }

        public override bool OnSkimRight()
        {
            if (Setting == null) return false;
            Change(1);
            return true;
        }

        /// <summary>
        /// Mouse: clicking the left half of the value ("&lt;") lowers it, anywhere else raises it
        /// (right-click is Back in the game's menus, so it cannot be used for "previous").
        /// </summary>
        public override void OnLeftClicked(bool mod1, bool mod2)
        {
            // The click that opened this menu can land on the row now under the cursor; ignore it.
            if (Time.unscaledTime - SettingsMenu.OpenedAt < ClickGuardSeconds) return;
            if (Setting == null)
            {
                base.OnLeftClicked(mod1, mod2);
                return;
            }
            int direction = 1;
            if (valueText != null && Manager.ui != null && Manager.ui.mouse != null)
            {
                Vector3 local = valueText.transform.InverseTransformPoint(Manager.ui.mouse.GetMouseUIViewPosition());
                if (local.x < valueText.dimensions.center.x) direction = -1;
            }
            Change(direction);
        }

        private void Change(int direction)
        {
            Setting.Step(direction);
            Refresh();
        }
    }

    /// <summary>The "Mod Options" entry in the game's Options menu.</summary>
    public class OpenSettingsButton : RadicalMenuOption
    {
        public override void OnParentMenuActivation()
        {
            base.OnParentMenuActivation();
            if (labelText != null) labelText.Render(SettingsMenu.ButtonLabel);
        }

        public override void OnActivated()
        {
            base.OnActivated();
            SettingsMenu.OpenList();
        }
    }
}
