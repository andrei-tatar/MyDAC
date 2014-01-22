using System;

namespace Julia.Interfaces.Drivers
{
    public interface IMouse : IDisposable
    {
        event MouseButtonEventHandler OnMouseDown;
        event MouseButtonEventHandler OnMouseUp;
        event MouseWheelEventHandler OnMouseWheel;
    }

    public delegate void MouseButtonEventHandler(MouseButton button);
    public delegate void MouseWheelEventHandler(int wheelDelta);
}