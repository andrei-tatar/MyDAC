using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Julia.Bluetooth;
using Julia.Interfaces.Drawing;
using Julia.Utils;

namespace Julia.Ui.Windows.Bluetooth
{
    class BluetoothWindow : MainWindow
    {
        private readonly BluetoothManager _btManager;
        private readonly Image _btImage;
        private readonly PulseDevice _sinkDevice;

        private int? _moduleHandle;
        private PulseDevice _connectedBtDevice;

        private const string PulseUser = "andrei";
        private const string BtPin = "1234";

        public BluetoothWindow()
        {
            var devices = BluetoothManager.GetBluetoothDevices();
            if (devices.Length == 0)
                throw new Exception("No bluetooth device");

            _btManager = new BluetoothManager(devices.First());
            _btManager.ChangeMode(false, false);
            _btManager.DeviceConnected += OnDeviceConnected;
            _btManager.DeviceDisconnected += OnDeviceDisconnected;

            using (var bmp = (@"../Resources/Bluetooth.png").LoadImage())
            using (var newBmp = bmp.Resize(34))
                _btImage = Image.FromBitmap(newBmp);

            _sinkDevice = GetDevices(DeviceType.Sink).FirstOrDefault(dev => dev.Name.Contains("C-Media_USB_Audio_Device"));

            if (_sinkDevice == null)
                Console.WriteLine("Could not find USB sound card");

            //max out the volume
            ConsoleUtils.Execute("amixer set Master 100%");

            //start listening for bluetooth pairing
            _btManager.StartListeningForPairing(BtPin);
        }

        private static IEnumerable<PulseDevice> GetDevices(DeviceType devType)
        {
            var result = ConsoleUtils.Execute("sudo", "-u", PulseUser, "pactl list", devType == DeviceType.Sink ? "sinks" : "sources", "short").
                                CheckForExceptionOrError("Could not get pulse sources").
                                Output.GetLines().Select(line => new PulseDevice(line)).ToList();
            Console.WriteLine(devType);
            foreach (var pulseDevice in result)
                Console.WriteLine(pulseDevice.Index + ": " + pulseDevice.Name);
            return result;
        }

        private void OnDeviceConnected(string address)
        {
            Console.WriteLine("BT con: " + address);
            if (_connectedBtDevice != null)
            {
                Console.WriteLine("Audio already connected");
                return;
            }

            _btManager.ChangeMode(false, false);

            var connectionRoutedOk = true;

            try
            {
                ConsoleUtils.Execute("bluez-test-device trusted", address, "yes");

                var sources = GetDevices(DeviceType.Source);
                _connectedBtDevice = sources.FirstOrDefault(src => src.Name.StartsWith("bluez_source"));
                if (_connectedBtDevice == null)
                {
                    Console.WriteLine("No bluez input device found");
                    connectionRoutedOk = false;
                }
                else
                {
                    _moduleHandle =
                        Convert.ToInt32(
                            ConsoleUtils.Execute("sudo", "-u", PulseUser, "pactl load-module module-loopback",
                                                 "source=" + _connectedBtDevice.Name, "sink=" + _sinkDevice.Name).
                                         CheckForExceptionOrError("Could not set loopback").
                                         Output.KeepNumerals());

                    ConsoleUtils.Execute("sudo", "-u", PulseUser, "pacmd set-source-volume", _connectedBtDevice.Index,
                                         "100%").
                                 CheckForExceptionOrError("Could not set source volume");
                    ConsoleUtils.Execute("sudo", "-u", PulseUser, "pacmd set-sink-volume ", _sinkDevice.Index, "100%").
                                 CheckForExceptionOrError("Could not set sink volume");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                connectionRoutedOk = false;
            }

            if (!connectionRoutedOk)
            {
                _btManager.ChangeMode(true, true);
            }
            else
            {
                Parent.Invoke(Refresh);
            }
        }

        void OnDeviceDisconnected(string address)
        {
            Console.WriteLine("BT dis: " + address);
            if (_moduleHandle == null) return;

            ConsoleUtils.Execute("sudo", "-u", PulseUser, "pactl unload-module", _moduleHandle.Value);
            _moduleHandle = null;
            _connectedBtDevice = null;
            _btManager.ChangeMode(true, true);
            Parent.Invoke(Refresh);
        }

        protected override void OnMainWindowChanged(bool amIVisible)
        {
            _btManager.ChangeMode(amIVisible, amIVisible);

            if (_connectedBtDevice != null)
                ConsoleUtils.Execute("sudo", "-u", PulseUser, "pacmd set-source-volume", _connectedBtDevice.Index, amIVisible ? "100%" : "0%");
        }

        public override void Refresh(IGraphics graphics)
        {
            graphics.Clear();

            //graphics.DrawText(1, 1, (Program.Instance.Sound.GetSampleRate() / 1000.0).ToString("0.#") + "kHz", Fonts.Console, Color.White);
            graphics.DrawImage(0, 10 + (graphics.Height - 10 - _btImage.Height) / 2, _btImage);

            var time = DateTime.Now;
            var text = string.Format("{0:00}:{1:00}", time.Hour, time.Minute);
            int w, h;
            Fonts.Console.Measure(text, out w, out h);
            graphics.DrawText(graphics.Width - w - 1, 1, text, Fonts.Console, Color.White);

            graphics.DrawText(_btImage.Width, 26, _connectedBtDevice != null ? "Connected" : "PIN: " + BtPin, Fonts.Condensed, Color.White);

            base.Refresh(graphics);
        }

        public override void Dispose()
        {
            _btManager.Dispose();
            base.Dispose();
        }

        #region inner types
        enum DeviceType
        {
            Source,
            Sink
        }

        class PulseDevice
        {
            public int Index { get; private set; }
            public string Name { get; private set; }

            public PulseDevice(string line)
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                Index = int.Parse(parts[0]);
                Name = parts[1];
            }
        }
        #endregion
    }
}
