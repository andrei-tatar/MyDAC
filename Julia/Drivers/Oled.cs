using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Julia.Interfaces.Drawing;
using Julia.Interfaces.Drivers;

namespace Julia.Drivers
{
    class Oled : Graphics, IScreen
    {
        private readonly HardwareService _hwService;
        private int _brightness;
        private bool _on;

        public int MaxBrightness { get { return 0x5C; } }
        public int MinBrightness { get { return 0x00; } }

        public int Brightness
        {
            get { return _brightness; }
            set
            {
                value = Math.Min(MaxBrightness, Math.Max(MinBrightness, value));
                if (value == _brightness) return;
                _brightness = value;

                _hwService.OledSetBrightness((byte)_brightness);
            }
        }


        public bool On
        {
            get { return _on; }
            set
            {
                if (_on == value) return;
                _on = value;
                if (_on)
                {
                    //_dcDcEnable.Write(true);
                    Thread.Sleep(50);
                    TurnDisplay(true);
                }
                else
                {
                    TurnDisplay(false);
                    Thread.Sleep(50);
                    //_dcDcEnable.Write(false);
                }
            }
        }

        public Oled(HardwareService hwService)
            : base(128, 64)
        {
            _hwService = hwService;
            _bufferData = new byte[Width * Height / 8];
        }

        private void TurnDisplay(bool turnOn)
        {
            _hwService.OledTurn(turnOn);
        }

        private byte GetData(int x, int y)
        {
            byte data = 0;
            for (var yy = 7; yy >= 0; yy--)
            {
                if (Buffer[x, y * 8 + yy] == Color.White)
                    data |= (byte)(1 << (7 - yy));
            }
            return data;
        }

        private readonly byte[] _bufferData;

        public void Flush()
        {
            if (!_hasChanges) return;

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < Width; x++)
                    _bufferData[y * Width + x] = GetData(x, 7 - y);
            }
            _hwService.OledSendBufferAndFlush(_bufferData);

            ResetChanges();
        }

        public void Dispose()
        {
            On = false;
        }
    }
}
