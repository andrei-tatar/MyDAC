using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Julia
{
    class PacketStream : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly IDisposable _toDisposeWhenDone;
        private readonly Thread _worker;
        private readonly byte[] _packetStart = new byte[] { 0x91, 0xEB, 0xFA, 0x58 };
        private readonly byte[] _length = new byte[2];

        public delegate void PacketReceivedEventHandler(PacketStream sender, byte command, byte[] data);

        public event PacketReceivedEventHandler PacketReceived;

        public PacketStream(Stream baseStream, IDisposable toDisposeWhenDone = null)
        {
            _baseStream = baseStream;
            _toDisposeWhenDone = toDisposeWhenDone;

            _worker = new Thread(ProcessData);
            _worker.Start();
        }

        enum State
        {
            Idle,
            LengthLsb,
            LengthMsb,
            Data,
            Checksum,
        }

        private void ProcessData()
        {
            try
            {
                int dataOffset = 0, headerOffset = 0, length = 0;
                var state = State.Idle;
                byte[] data = null;
                byte checksum = 0, command = 0;

                while (true)
                {
                    var cData = _baseStream.ReadByte();
                    if (cData == -1) continue;

                    var cByte = (byte)cData;

                    if (cByte == _packetStart[headerOffset])
                    {
                        if (++headerOffset == _packetStart.Length)
                        {
                            state = State.LengthLsb;
                            checksum = 0;
                            headerOffset = 0;
                            continue;
                        }
                    }
                    else
                        headerOffset = 0;

                    switch (state)
                    {
                        case State.Idle:
                            break;

                        case State.LengthLsb:
                            length = cByte;
                            checksum ^= cByte;
                            state = State.LengthMsb;
                            break;

                        case State.LengthMsb:
                            length |= cByte << 8;
                            checksum ^= cByte;

                            state = State.Data;
                            data = new byte[length - 1];
                            dataOffset = 0;
                            break;

                        case State.Data:
                            if (dataOffset == 0)
                                command = cByte;
                            else
                                data[dataOffset - 1] = cByte;
                            checksum ^= cByte;
                            if (++dataOffset == length)
                                state = State.Checksum;
                            break;

                        case State.Checksum:
                            if (cByte == checksum && PacketReceived != null)
                                PacketReceived(this, command, data);

                            state = State.Idle;
                            headerOffset = 0;
                            break;

                    }
                }
            }
            catch (ThreadAbortException)
            {

            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendPacket(byte command, params byte[] data)
        {
            var length = 1 + data.Length;
            _length[0] = (byte)(length & 0xFF);
            _length[1] = (byte)((length >> 8) & 0xFF);

            var checksum = (byte)(_length[0] ^ _length[1] ^ command);
            for (var i = 0; i < data.Length; i++) checksum ^= data[i];

            _baseStream.Write(_packetStart, 0, _packetStart.Length);
            _baseStream.Write(_length, 0, _length.Length);
            _baseStream.WriteByte(command);
            _baseStream.Write(data, 0, data.Length);
            _baseStream.WriteByte(checksum);
        }

        public void Dispose()
        {
            _worker.Abort();
            if (_toDisposeWhenDone != null)
                _toDisposeWhenDone.Dispose();
        }
    }
}
