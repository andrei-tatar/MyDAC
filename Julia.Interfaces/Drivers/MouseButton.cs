using System;

namespace Julia.Interfaces.Drivers
{
    [Flags]
    public enum MouseButton
    {
        None = 0,
        Left = 0x110,
        Right = 0x111,
        Middle = 0x112,
    }
}
