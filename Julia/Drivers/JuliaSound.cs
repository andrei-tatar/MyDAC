using System;

namespace Julia.Drivers
{
    class JuliaSound
    {
        private readonly HardwareService _hwService;
        private const byte DacAddress = 0x11, SelectorAddress = 0x10;

        private int _volume;
        private int _input;

        public const int VolumeMax = 127;
        public const int VolumeMin = -1;
        public const int NumberOfInputs = 8;

        public bool Mute { get { return Volume == VolumeMin; } }

        private readonly byte[] _aux = new byte[16];

        public int Volume
        {
            get { return _volume; }
            set
            {
                value = Math.Min(VolumeMax, Math.Max(VolumeMin, value));
                if (_volume == value) return;
                _volume = value;

                for (var i = 0; i < 8; i++)
                {
                    _aux[i * 2 + 0] = (byte)(0x04 + i + (i >= 6 ? 1 : 0));
                    _aux[i * 2 + 1] = (byte)(_volume == VolumeMin ? 0x00 : 0x80 | _volume);
                }

                SendData(_aux, 16, DacAddress);
            }
        }

        public int Input
        {
            get { return _input; }
            set
            {
                value = Math.Min(NumberOfInputs - 1, Math.Max(0, value));
                if (_input == value) return;
                _input = value;

                _aux[0] = 0x02;
                _aux[1] = (byte)(0x88 | (_input << 4) | (_input)); //input
                _aux[2] = 0x03;
                _aux[3] = (byte)(0x48 | (_input)); //input

                SendData(_aux, _aux.Length, SelectorAddress);
            }
        }

        public int GetSampleRate()
        {
            var result = _hwService.I2CRead(0x07, 1);
            var fs = result[0] >> 4;
            switch (fs)
            {
                case 0:
                    return 44100;
                case 1:
                    return -1; //Reserved
                case 2:
                    return 48000;
                case 3:
                    return 32000;
                case 8:
                    return 88200;
                case 10:
                    return 96000;
                case 12:
                    return 176400;
                case 14:
                    return 192000;

                default:
                    return -1;
            }
        }

        public float VolumeInDb { get { return Volume == VolumeMin ? float.NegativeInfinity : (Volume - 127) / 2.0f; } }

        private void SendData(byte[] init, int legnth, byte address)
        {
            for (var i = 0; i < legnth / 2; i++)
            {
                _hwService.I2CWrite(address, init, i * 2, 2);
            }
        }

        public void Init()
        {
            //init DAC
            var init =
                new byte[]
                    {
                        0x00, 0x97,
                        0x01, 0x01, /* 1: reset and soft-mute */
                        //0x02, 0x4f, /* 2: DA's power up, normal speed, RSTN#=0 */
                        0x02, 0x6F, /* quad speed */
                        0x03, 0x01, /* 3: de-emphasis off */

                        0x04, 0x00, /* 4: LOUT1 mute*/
                        0x05, 0x00, /* 5: ROUT1 mute*/
                        0x06, 0x00, /* 6: LOUT2 mute*/
                        0x07, 0x00, /* 7: ROUT2 mute*/
                        0x08, 0x00, /* 8: LOUT3 mute*/
                        0x09, 0x00, /* 9: ROUT3 mute*/
                        0x0B, 0x00, /* b: LOUT4 mute*/
                        0x0C, 0x00, /* c: ROUT4 mute*/

                        0x0A, 0x00 /* a: DATT speed=0, ignore DZF */
                    };
            SendData(init, init.Length, DacAddress);

            _volume = VolumeMin;

            _input = 0x01;

            //init selector
            init =
                new byte[]
                    {
                        0x00, 0x4F, //init
                        0x01, 0x5A, //I2S output
                        0x02, (byte) (0x88 | (_input << 4) | (_input)), //input
                        0x03, (byte) (0x48 | (_input)), //input

                        0x04, 0x00,
                        0x05, 0x00
                    };

            SendData(init, init.Length, SelectorAddress);
        }

        public JuliaSound(HardwareService hwService)
        {
            _hwService = hwService;
            Init();
        }
    }
}
