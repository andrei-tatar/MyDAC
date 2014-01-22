using System;
using System.Threading;
using Julia.Interfaces.Drivers;
using Julia.Interfaces.Drawing;

namespace Julia.Ui.Windows
{
    class ValueSetting : MenuWindow
    {
        private readonly Action<ValueSetting> _onValueChanged;
        private readonly int _maxValue;
        private readonly int _minValue;
        private bool _isFocused;
        private int _value;

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (!value) IsFocused = false;
                base.Visible = value;
            }
        }

        public bool IsFocused
        {
            get { return _isFocused; }
            private set
            {
                if (_isFocused == value) return;
                _isFocused = value;
                Refresh();
            }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                value = Math.Min(_maxValue, Math.Max(_minValue, value));
                if (_value == value) return;
                _value = value;
                if (_onValueChanged != null)
                    _onValueChanged(this);
                Refresh();
            }
        }

        public ValueSetting(Action<ValueSetting, IGraphics> refresh, Action<ValueSetting> onValueChanged, int maxValue, int minValue, Orientation orientation = Orientation.Horizontal)
            : base((window, graphics) => refresh((ValueSetting)window, graphics), w => { }, orientation, Timeout.Infinite)
        {
            _onValueChanged = onValueChanged;
            _maxValue = maxValue;
            _minValue = minValue;
        }

        public override bool OnButtonDown()
        {
            IsFocused = !IsFocused;
            return true;
        }

        public override bool OnScroll(int delta)
        {
            if (IsFocused)
            {
                Value += delta;
                return true;
            }

            return base.OnScroll(delta);
        }
    }
}
