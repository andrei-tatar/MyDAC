using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Julia.Interfaces.Drivers;

namespace Julia.Drivers
{
    class HardwareService : PacketStream, IRemoteInterface
    {
        private byte? _waitingFor;

        private RemoteButton? _lastPressedButton;
        private readonly Timer _upTimer;
        private readonly AutoResetEvent _i2CReadWaitEvent;
        private byte[] _i2CReadData;

        public HardwareService(Stream baseStream, IDisposable toDisposeWhenDone = null)
            : base(baseStream, toDisposeWhenDone)
        {
            _i2CReadWaitEvent = new AutoResetEvent(false);

            _upTimer = new Timer(
                o =>
                {
                    if (_lastPressedButton != null && OnButtonUp != null)
                        OnButtonUp(_lastPressedButton.Value);
                    _lastPressedButton = null;
                }, null, Timeout.Infinite, Timeout.Infinite);

            PacketReceived +=
                (s, cmd, data) =>
                {
                    if (_waitingFor != null && _waitingFor.Value == cmd)
                    {
                        _waitingFor = null;
                    }
                    else
                    {
                        switch (cmd)
                        {
                            case HardwareCommands.CmdRemoteButton:
                                if (data.Length == 2)
                                {
                                    var currentButton = Enum.IsDefined(typeof(RemoteButton), (int)data[1]) ? (RemoteButton?)data[1] : null;
                                    var isRepeat = data[0] == 0xFF && data[1] == 0xFF;

                                    if (currentButton == null && !isRepeat) return;

                                    if (_lastPressedButton != currentButton && !isRepeat)
                                    {
                                        if (_lastPressedButton != null)
                                        {
                                            if (OnButtonUp != null)
                                                OnButtonUp(_lastPressedButton.Value);
                                            _lastPressedButton = null;
                                        }

                                        _lastPressedButton = currentButton;

                                        if (OnButtonDown != null)
                                            OnButtonDown(currentButton.Value);

                                        if (OnButtonPressed != null)
                                            OnButtonPressed(currentButton.Value);

                                        _upTimer.Change(200, Timeout.Infinite);
                                    }

                                    if (isRepeat && _lastPressedButton != null)
                                    {
                                        _upTimer.Change(200, Timeout.Infinite);
                                        if (OnButtonPressed != null)
                                            OnButtonPressed(_lastPressedButton.Value);
                                    }
                                }
                                break;
                            case HardwareCommands.CmdI2CRead:
                                _i2CReadData = data.ToArray();
                                _i2CReadWaitEvent.Set();
                                break;
                        }
                    }
                };
        }

        public void OledSendBufferAndFlush(byte[] buffer)
        {
            while (_waitingFor != null) Thread.Sleep(2);
            _waitingFor = HardwareCommands.CmdOledSendAndFlush;
            SendPacket(HardwareCommands.CmdOledSendAndFlush, buffer);
        }

        public void OledTurn(bool displayOn)
        {
            while (_waitingFor != null) Thread.Sleep(2);
            _waitingFor = HardwareCommands.CmdOledPower;
            SendPacket(HardwareCommands.CmdOledPower, displayOn ? (byte)1 : (byte)0);
        }

        public void OledSetBrightness(byte brightness)
        {
            while (_waitingFor != null) Thread.Sleep(2);
            _waitingFor = HardwareCommands.CmdOledBrightness;
            SendPacket(HardwareCommands.CmdOledBrightness, brightness);
        }

        public void I2CWrite(byte slaveAddress, byte[] data, int offset, int length)
        {
            while (_waitingFor != null) Thread.Sleep(2);
            var toSend = new byte[length + 1];
            toSend[0] = slaveAddress;
            Array.Copy(data, offset, toSend, 1, length);

            _waitingFor = HardwareCommands.CmdI2CWrite;
            SendPacket(HardwareCommands.CmdI2CWrite, toSend);
        }

        public byte[] I2CRead(byte slaveAddress, int length)
        {
            SendPacket(HardwareCommands.CmdI2CRead, slaveAddress, (byte)length);
            _i2CReadWaitEvent.WaitOne();
            return _i2CReadData;
        }

        public event ButtonPressedEventhandler OnButtonPressed;
        public event ButtonPressedEventhandler OnButtonDown;
        public event ButtonPressedEventhandler OnButtonUp;
    }
}
