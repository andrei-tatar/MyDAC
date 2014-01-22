using System;

namespace Julia.Interfaces.Drivers
{
    public enum RemoteButton
    {
        Sleep = 0x50,
        Power = 0xB8,
        Presets = 0x20,
        VolumeUp = 0x70,
        Previous = 0x28,
        PlayPause = 0x18,
        Next = 0x68,
        VolumeDown = 0x30,
        Shuffle = 0x38,
        Mute = 0xB0,
        Repeat = 0x78,
        Minus = 0x08,
        Mode = 0x60,
        Plus = 0x48,
        Equalizer = 0x10,
        Snooze = 0xA0,
        BButton = 0x90,
        Answer = 0x00,
        Reject = 0x40,
    }

    public delegate void ButtonPressedEventhandler(RemoteButton button);

    public interface IRemoteInterface : IDisposable
    {
        event ButtonPressedEventhandler OnButtonPressed;
        event ButtonPressedEventhandler OnButtonDown;
        event ButtonPressedEventhandler OnButtonUp;
    }
}