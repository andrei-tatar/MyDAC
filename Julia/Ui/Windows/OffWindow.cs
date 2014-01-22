using Julia.Interfaces.Drivers;
using Julia.Interfaces.Drawing;

namespace Julia.Ui.Windows
{
    class OffWindow : BaseWindow
    {
        private readonly IScreen _screen;

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
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
            Program.Instance.WindowManager.SwitchWindowBack();
            return true;
        }

        public override bool OnScroll(int delta)
        {
            Program.Instance.WindowManager.SwitchWindowBack();
            return true;
        }

        public override void Refresh(IGraphics graphics)
        {
            graphics.Clear();
        }
    }
}
