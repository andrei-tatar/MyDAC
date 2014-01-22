using System.Linq;
using System.Collections.Generic;
using System;

namespace Julia.Drivers
{
    static class HardwareCommands
    {
        public const byte CmdNothing = 0x00;

        public const byte CmdOledSendAndFlush = 0xDE;
        public const byte CmdOledPower = 0xA9;
        public const byte CmdOledBrightness = 0xBA;

        public const byte CmdRemoteButton = 0xDA;

        public const byte CmdI2CWrite = 0x9A;
        public const byte CmdI2CRead = 0x9B;
    }
}
