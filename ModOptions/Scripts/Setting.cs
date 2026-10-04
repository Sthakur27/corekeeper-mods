using System;
using System.Globalization;
using CoreLib.Data.Configuration;

namespace ModOptions
{
    /// <summary>One option on a settings page, as the menu sees it (label, value text, step left/right).</summary>
    public abstract class SettingBase
    {
        public string Label { get; }

        protected SettingBase(string label)
        {
            Label = label;
        }

        /// <summary>Text shown in the value column.</summary>
        public abstract string DisplayValue { get; }

        /// <summary>Moves the value one step (+1 = next / larger, -1 = previous / smaller).</summary>
        public abstract void Step(int direction);

        /// <summary>Sets the mod's default value.</summary>
        public abstract void ResetToDefault();
    }

    /// <summary>
    /// A setting stored in the owning mod's CoreLib config file. Read <see cref="Value"/> any time;
    /// <see cref="OnChanged"/> fires whenever the value changes (menu or code).
    /// </summary>
    public abstract class Setting<T> : SettingBase
    {
        private readonly ConfigEntry<T> _entry;

        public T Default { get; }

        public event Action<T> OnChanged;

        protected Setting(string label, ConfigEntry<T> entry, T defaultValue) : base(label)
        {
            _entry = entry;
            Default = defaultValue;
        }

        public T Value
        {
            get => _entry.Value;
            set
            {
                T clean = Sanitize(value);
                if (Equals(_entry.Value, clean)) return;
                _entry.Value = clean;
                OnChanged?.Invoke(clean);
            }
        }

        public override void ResetToDefault() => Value = Default;

        /// <summary>Brings a stored or assigned value into range (called on load and on every set).</summary>
        protected abstract T Sanitize(T value);

        /// <summary>Fixes a value loaded from disk that is out of range (e.g. a choice list that changed).</summary>
        internal void SanitizeStored()
        {
            T clean = Sanitize(_entry.Value);
            if (!Equals(_entry.Value, clean)) _entry.Value = clean;
        }
    }

    internal sealed class ToggleSetting : Setting<bool>
    {
        public ToggleSetting(string label, ConfigEntry<bool> entry, bool def) : base(label, entry, def) { }

        public override string DisplayValue => Value ? "On" : "Off";

        public override void Step(int direction) => Value = !Value;

        protected override bool Sanitize(bool value) => value;
    }

    internal sealed class ChoiceSetting : Setting<string>
    {
        private readonly string[] _values;

        public ChoiceSetting(string label, ConfigEntry<string> entry, string[] values, string def) : base(label, entry, def)
        {
            _values = values;
        }

        public override string DisplayValue => Value;

        public override void Step(int direction)
        {
            int i = Array.IndexOf(_values, Value);
            if (i < 0) i = Math.Max(0, Array.IndexOf(_values, Default));
            i = ((i + direction) % _values.Length + _values.Length) % _values.Length; // wraps around
            Value = _values[i];
        }

        protected override string Sanitize(string value) => Array.IndexOf(_values, value) >= 0 ? value : Default;
    }

    internal sealed class StepperSetting : Setting<int>
    {
        private readonly int _min, _max, _step;

        public StepperSetting(string label, ConfigEntry<int> entry, int min, int max, int def, int step) : base(label, entry, def)
        {
            _min = min;
            _max = max;
            _step = Math.Max(1, step);
        }

        public override string DisplayValue => Value.ToString(CultureInfo.InvariantCulture);

        public override void Step(int direction) => Value = Value + direction * _step;

        protected override int Sanitize(int value) => Math.Max(_min, Math.Min(_max, value));
    }

    internal sealed class SliderSetting : Setting<float>
    {
        private readonly float _min, _max, _step;

        public SliderSetting(string label, ConfigEntry<float> entry, float min, float max, float def, float step) : base(label, entry, def)
        {
            _min = min;
            _max = max;
            _step = step > 0f ? step : (max - min) / 20f;
        }

        public override string DisplayValue => Value.ToString("0.##", CultureInfo.InvariantCulture);

        public override void Step(int direction) => Value = Value + direction * _step;

        protected override float Sanitize(float value)
        {
            float v = Math.Max(_min, Math.Min(_max, value));
            return (float)Math.Round(v, 4);
        }
    }
}
