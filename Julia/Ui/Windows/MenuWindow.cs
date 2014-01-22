using System;
using Julia.Interfaces.Drawing;

namespace Julia.Ui.Windows
{
    enum Orientation
    {
        Horizontal, Vertical
    }

    class MenuWindow : BaseWindow
    {
        private readonly Action<MenuWindow, IGraphics> _refresh;
        private readonly Action<MenuWindow> _onSelect;

        private Window _next;

        public Window Previous { get; private set; }
        public Window Next
        {
            get { return _next; }
            set
            {
                var next = _next as MenuWindow;
                if (next != null) next.Previous = null;

                _next = value;
                next = _next as MenuWindow;
                if (next != null) next.Previous = this;
            }
        }
        public object Tag { get; set; }

        private readonly Orientation _orientation;
        private readonly int _autoSwitchBackTimeout;
        private int _offTime;

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (value) _offTime = 0;
                base.Visible = value;
            }
        }

        public MenuWindow(Action<MenuWindow, IGraphics> refresh, Action<MenuWindow> onSelect, Orientation orientation = Orientation.Horizontal, int autoSwitchBackTimeout = 3000)
        {
            _refresh = refresh;
            _onSelect = onSelect;
            _orientation = orientation;
            _autoSwitchBackTimeout = autoSwitchBackTimeout;
        }

        public override void Refresh(IGraphics graphics)
        {
            _refresh(this, graphics);
            base.Refresh(graphics);
        }

        public override bool OnScroll(int delta)
        {
            _offTime = 0;
            if (delta < 0)
            {
                if (Previous != null)
                    Program.Instance.WindowManager.SwitchWindow(Previous, false, _orientation == Orientation.Horizontal ? SlideDirection.Right : SlideDirection.Bottom);
            }
            else
                if (Next != null)
                    Program.Instance.WindowManager.SwitchWindow(Next, false, _orientation == Orientation.Horizontal ? SlideDirection.Left : SlideDirection.Top);
            return true;
        }

        public override void OnTick(int deltaTimeMs)
        {
            if (_autoSwitchBackTimeout == -1) return;

            _offTime += deltaTimeMs;
            if (_offTime >= _autoSwitchBackTimeout)
                Program.Instance.WindowManager.SwitchWindowBack();
        }

        public override bool OnButtonDown()
        {
            _onSelect(this);
            return true;
        }
    }
}
