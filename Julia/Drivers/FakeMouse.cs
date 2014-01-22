using System.Windows.Forms;
using Julia.Interfaces.Drivers;

namespace Julia.Drivers
{
    class FakeMouse : IMouse
    {
        public event MouseButtonEventHandler OnMouseDown;
        public event MouseButtonEventHandler OnMouseUp;
        public event MouseWheelEventHandler OnMouseWheel;

        public FakeMouse(Control control)
        {
            control.MouseDown +=
                (s, e) =>
                {
                    if (OnMouseDown == null) return;
                    var button = MouseButton.None;
                    switch (e.Button)
                    {
                        case MouseButtons.Left:
                            button = MouseButton.Left;
                            break;
                        case MouseButtons.Middle:
                            button = MouseButton.Middle;
                            break;
                        case MouseButtons.Right:
                            button = MouseButton.Right;
                            break;
                        case MouseButtons.None:
                            return;
                    }
                    OnMouseDown(button);
                };
            control.MouseUp +=
                (s, e) =>
                {
                    if (OnMouseUp == null) return;
                    var button = MouseButton.None;
                    switch (e.Button)
                    {
                        case MouseButtons.Left:
                            button = MouseButton.Left;
                            break;
                        case MouseButtons.Middle:
                            button = MouseButton.Middle;
                            break;
                        case MouseButtons.Right:
                            button = MouseButton.Right;
                            break;
                        case MouseButtons.None:
                            return;
                    }
                    OnMouseUp(button);
                };
            control.MouseWheel +=
                (s, e) =>
                {
                    if (OnMouseWheel == null) return;
                    OnMouseWheel(e.Delta / 120);
                };
        }

        public void Dispose()
        {
        }
    }
}
