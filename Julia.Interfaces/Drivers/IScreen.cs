using System;
using Julia.Interfaces.Drawing;

namespace Julia.Interfaces.Drivers
{
    public interface IScreen : IGraphics, IDisposable
    {
        int MaxBrightness { get; }
        int MinBrightness { get; }
        int Brightness { get; set; }
        bool On { get; set; }

        void Flush();
    }
}