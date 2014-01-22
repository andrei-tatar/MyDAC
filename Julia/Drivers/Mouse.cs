using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Julia.Interfaces.Drivers;

namespace Julia.Drivers
{
    class Mouse : IMouse
    {
        private readonly Thread _worker;
        private readonly FileStream _file;
        private bool _cancel, _disposed;

        public Mouse(string filePathOrName, bool isFilePath = true)
        {
            if (!isFilePath)
            {
                var lines = File.ReadAllLines("/proc/bus/input/devices");
                var foundSection = false;
                var foundFile = false;
                foreach (var line in lines)
                {
                    if (line.StartsWith("N: Name=\""))
                    {
                        foundSection = false;

                        var name = line.Split('=')[1].Trim('"');
                        if (name.Equals(filePathOrName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            foundSection = true;
                        }
                    }
                    else if (foundSection && line.StartsWith("H: Handlers="))
                    {
                        var handlers = line.Split('=')[1].Split(' ');
                        foreach (var handler in handlers)
                        {
                            if (!handler.StartsWith("event")) continue;
                            filePathOrName = "/dev/input/" + handler;
                            foundFile = true;
                            break;
                        }

                        foundSection = false;
                    }

                    if (foundFile) break;
                }

                if (!foundFile)
                    throw new Exception("Could not find device named \"" + filePathOrName + "\"");
            }

            _file = File.Open(filePathOrName, FileMode.Open, FileAccess.Read);

            _worker = new Thread(DoWork);
            _worker.Start();
        }

        ~Mouse()
        {
            Dispose();
        }

        private void DoWork()
        {
            int exceptions = 0;
            try
            {
                var buffer = new byte[16];

                while (!_cancel)
                {
                    try
                    {
                        if (_file.Read(buffer, 0, buffer.Length) != buffer.Length) continue;
                    }
                    catch (IOException)
                    {
                        exceptions++;
                        if (exceptions == 3)
                        {
                            Console.WriteLine("Mouse failed!");
                            return;
                        }
                        continue;
                    }

                    uint type = BitConverter.ToUInt16(buffer, 8);
                    uint code = BitConverter.ToUInt16(buffer, 10);
                    var value = BitConverter.ToInt32(buffer, 12);

                    if (_cancel)
                        return;

                    if (type == 1)
                    {
                        if (value == 1)
                        {
                            if (OnMouseDown != null)
                                OnMouseDown((MouseButton)code);
                        }
                        else
                            if (OnMouseUp != null)
                                OnMouseUp((MouseButton)code);
                    }
                    else if (type == 2 && code == 8)
                    {
                        if (OnMouseWheel != null)
                            OnMouseWheel(-value);
                    }
                }
            }
            catch (Exception)
            {

            }

            Console.WriteLine("Mouse thread done!!!!");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cancel = true;

            if (_file != null)
            {
                _file.Close();
                _file.Dispose();
            }

            if (_worker != null)
            {
                _worker.Abort();
                //while (_worker.ThreadState != ThreadState.Aborted && _worker.ThreadState != ThreadState.Stopped)
                //    Thread.Sleep(100);
            }
        }

        public event MouseButtonEventHandler OnMouseDown;
        public event MouseButtonEventHandler OnMouseUp;
        public event MouseWheelEventHandler OnMouseWheel;
    }
}
