using System;
using System.Threading;
using Julia.Interfaces.Drivers;
using Julia.Interfaces.Drawing;

namespace Julia.Ui.Windows
{
    class ToggleSetting : MenuWindow
    {
        private readonly Action<ToggleSetting> _onCheckedChanged;
        private bool _isFocused, _isChecked;

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

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                if (_onCheckedChanged != null)
                    _onCheckedChanged(this);
                Refresh();
            }
        }

        public ToggleSetting(Action<ToggleSetting, IGraphics> refresh, Action<ToggleSetting> onCheckedChanged, Orientation orientation = Orientation.Horizontal)
            : base((window, graphics) => refresh((ToggleSetting)window, graphics), w => { }, orientation, Timeout.Infinite)
        {
            _onCheckedChanged = onCheckedChanged;
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
                IsChecked = delta > 0;
                return true;
            }

            return base.OnScroll(delta);
        }
    }
}
