using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using Julia.Drivers;
using Julia.Interfaces.Drivers;
using Julia.Ui;
using Julia.Ui.Windows;
using Julia.Utils;

namespace Julia
{
    class Program : IDisposable
    {
        private static Program _instance;
        public static Program Instance
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get { return _instance ?? (_instance = new Program()); }
        }

        private IScreen _screen;
        private IMouse _mouse;
        private bool _disposed;
        private readonly HardwareService _hardwareService;

        public WindowManager WindowManager { get; private set; }
        public JuliaSound Sound { get; private set; }

        private Program()
        {
            var port = new SerialPort(ConsoleUtils.OsType == OsType.Linux ? "/dev/ttyUSB0" : "COM1", 460800, Parity.None, 8, StopBits.One);
            port.Open();

            _hardwareService = new HardwareService(port.BaseStream, Disposable.Create(port.Close));
            _screen = new Oled(_hardwareService);
            try
            {
                _mouse = new Mouse(" USB OPTICAL MOUSE", false);
            }
            catch
            {
                try
                {
                    _mouse = new Mouse("Microsoft Comfort Mouse 6000", false);
                }
                catch
                {
                    Console.WriteLine("No mouse found, skipping");
                    _mouse = null;
                }
            }

            Sound = new JuliaSound(_hardwareService);
            WindowManager = new WindowManager(realScreen, _mouse, _hardwareService);
        }

        ~Program()
        {
            Dispose();
        }

        public void Run()
        {
            Console.CancelKeyPress += (sender, e) => Dispose();

            Console.WriteLine("Start at : " + DateTime.Now);
            Console.WriteLine("OS : " + ConsoleUtils.OsType);

            WindowManager.AddWindow(MainWindow.CurrentMainWindow);
            WindowManager.AddWindowAndSwitchIt(new IntroWindow(), false, SlideDirection.Right);

            Console.WriteLine("Running");

            try
            {
                WindowManager.Run();
            }
            catch (Exception ex)
            {
                Dispose();

                Console.WriteLine("Unhandled exception on main thread. Closing");
                Console.WriteLine(ex);
            }
        }

        private static void Main()
        {
            Instance.Run();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                Console.WriteLine("Stopping...");

                Settings.Save();
                Console.WriteLine("Settings saved");

                WindowManager.Dispose();
                Console.WriteLine("Wmgr disposed");
                WindowManager = null;
                Sound = null;
                _screen.Dispose();
                Console.WriteLine("Screen disposed");
                _screen = null;

                _hardwareService.Dispose();
                Console.WriteLine("HW service disposed");

                if (_mouse != null)
                {
                    _mouse.Dispose();
                    Console.WriteLine("Mouse disposed");
                    _mouse = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}

