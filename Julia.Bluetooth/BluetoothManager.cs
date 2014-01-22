using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Julia.Utils;

namespace Julia.Bluetooth
{
    public partial class BluetoothManager : IDisposable
    {
        private readonly string _btDevice;
        private bool _discoverable, _connectable, _listening;
        private readonly Thread _checkConnectionsThread;
        private bool _disposed;

        public delegate void DeviceConnectionChangedHandler(string address);
        public event DeviceConnectionChangedHandler DeviceConnected;
        public event DeviceConnectionChangedHandler DeviceDisconnected;

        public void ChangeMode(bool connectable, bool discoverable)
        {
            if (connectable == _connectable && discoverable == _discoverable)
                return;

            _connectable = connectable;
            _discoverable = discoverable;
            if (_connectable && _discoverable)
                ConsoleUtils.Execute("hciconfig", _btDevice, "piscan").CheckForExceptionOrError(ErrorCantChangeScanType);
            else if (_connectable)
                ConsoleUtils.Execute("hciconfig", _btDevice, "pscan").CheckForExceptionOrError(ErrorCantChangeScanType);
            else if (_discoverable)
                ConsoleUtils.Execute("hciconfig", _btDevice, "iscan").CheckForExceptionOrError(ErrorCantChangeScanType);
            else
                ConsoleUtils.Execute("hciconfig", _btDevice, "noscan").CheckForExceptionOrError(ErrorCantChangeScanType);
        }

        public BluetoothManager(string btDevice)
        {
            _btDevice = btDevice;
            _disposed = false;

            var status = GetDeviceStatus().ToLower();
            _connectable = status.Contains("pscan");
            _discoverable = status.Contains("iscan");

            _checkConnectionsThread = new Thread(
                () =>
                {
                    try
                    {
                        var allConnectedDevices = new List<string>();
                        while (!_disposed)
                        {
                            var connectedAddresses = (from d in ConsoleUtils.Execute("hcitool", "con").Output.GetLines()
                                                      where d.Contains("ACL") && d.Contains("AUTH")
                                                      let parts =
                                                          d.Split(new[] { ' ', '\t' },
                                                                  StringSplitOptions.RemoveEmptyEntries)
                                                      select parts[2]).ToList();

                            foreach (var address in connectedAddresses)
                            {
                                if (allConnectedDevices.Contains(address)) continue;

                                allConnectedDevices.Add(address);
                                if (DeviceConnected != null) DeviceConnected(address);
                            }

                            for (var i = 0; i < allConnectedDevices.Count; i++)
                            {
                                var address = allConnectedDevices[i];
                                if (connectedAddresses.Contains(address)) continue;

                                if (DeviceDisconnected != null) DeviceDisconnected(address);
                                allConnectedDevices.RemoveAt(i);
                                i--;
                            }

                            Thread.Sleep(500);
                        }
                    }
                    catch (ThreadAbortException)
                    {

                    }
                });
            _checkConnectionsThread.Start();
        }

        ~BluetoothManager()
        {
            StopListeningForPairing();
        }

        private string GetDeviceStatus()
        {
            return ConsoleUtils.Execute("hciconfig", _btDevice).CheckForExceptionOrError("Could not get status for " + _btDevice).Output;
        }

        public void StartListeningForPairing(string pin)
        {
            StopListeningForPairing();

            ConsoleUtils.Execute("start-stop-daemon", "-S -x /usr/bin/bluetooth-agent -c andrei -b -- " + pin.KeepNumerals(4)).CheckForExceptionOrError("Could not start daemon");
            _listening = true;
        }

        public void StopListeningForPairing()
        {
            if (!_listening) return;

            ConsoleUtils.Execute("start-stop-daemon", "-K -x /usr/bin/bluetooth-agent").CheckForExceptionOrError("Could not stop daemon");
            _listening = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            StopListeningForPairing();

            _disposed = true;
            _checkConnectionsThread.Abort();
            //while (_checkConnectionsThread.ThreadState != ThreadState.Aborted && _checkConnectionsThread.ThreadState != ThreadState.Stopped)
            //    Thread.Sleep(100);
        }
    }
}

