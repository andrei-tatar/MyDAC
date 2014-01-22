using Julia.Interfaces.Drivers;
using Julia.Interfaces.Drawing;

namespace Julia.Ui
{
    class OffWindow : Window
    {
        private readonly IScreen _screen;

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (base.Visible == value) return;

                _screen.On = !value;
                base.Visible = value;
            }
        }

        public OffWindow(IScreen screen)
        {
            _screen = screen;
        }

        public override bool OnButtonDown()
        {
            Parent.SwitchWindowBack(false);
            return true;
        }

        public override bool OnScroll(int delta)
        {
            Parent.SwitchWindowBack(false);
            return true;
        }

        public override void Refresh(IGraphics graphics)
        {
            graphics.Clear();
        }
    }
}
