using System;
using System.Collections.Generic;
using System.Linq;
using Julia.Drivers;
using Julia.Interfaces.Drawing;
using Julia.Interfaces.Drivers;
using Julia.Ui.Windows.Bluetooth;

namespace Julia.Ui.Windows
{
    abstract class MainWindow : BaseWindow
    {
        private static readonly VolumeWindow VolWindow;
        private static readonly List<MenuWindow> InputSelectionWindows = new List<MenuWindow>();
        private static readonly SettingsWindowManager SettingsManager;

        private static readonly Image SettingsImage;
        private static readonly Dictionary<InputType, Image> InputImages;

        private static int _selectedInput;

        private static readonly Dictionary<InputType, MainWindow> MainWindows;

        private static readonly MpcWrapper MpcWrapper;

        private static MainWindow _mainWindow;
        public static MainWindow CurrentMainWindow
        {
            get { return _mainWindow; }
            private set
            {
                if (_mainWindow == value) return;

                if (_mainWindow != null)
                {
                    _mainWindow.OnMainWindowChanged(false);
                    Program.Instance.WindowManager.ReplaceInStack(_mainWindow, value);

                    if (Program.Instance.WindowManager.CurrentWindow == _mainWindow && value != null)
                        Program.Instance.WindowManager.SwitchWindow(value);
                }

                _mainWindow = value;
                _mainWindow.OnMainWindowChanged(true);
            }
        }

        private static void ChangeInputDevice(InputDescriptor descriptor)
        {
            _selectedInput = descriptor.Id;
            Program.Instance.Sound.Input = descriptor.InputIndex;
            Settings.Instance.SelectedInput = descriptor.Id;

            CurrentMainWindow = MainWindows[descriptor.Type];

            Program.Instance.WindowManager.SwitchWindowBack();
        }

        static MainWindow()
        {
            MainWindows = new Dictionary<InputType, MainWindow>();

            using (var bmp = @"../Resources/settings.png".LoadImage())
            using (var newBmp = bmp.Resize(36))
                SettingsImage = Image.FromBitmap(newBmp, true);

            InputImages = new Dictionary<InputType, Image>();
            foreach (InputType inputType in Enum.GetValues(typeof(InputType)))
            {
                using (var bmp = (@"../Resources/input" + inputType + ".png").LoadImage())
                using (var newBmp = bmp.Resize(32))
                    InputImages.Add(inputType, Image.FromBitmap(newBmp));
            }

            MenuWindow previous = null;
            var allInputs = Settings.Instance.Inputs.Where(input => input.Visible).ToList();
            foreach (var inputDescriptor in allInputs.ToList())
            {
                switch (inputDescriptor.Type)
                {
                    case InputType.Bluetooth:
                        try
                        {
                            var btWindow = new BluetoothWindow();
                            Program.Instance.WindowManager.AddWindow(btWindow);
                            MainWindows[InputType.Bluetooth] = btWindow;
                        }
                        catch (Exception ex)
                        {
                            allInputs.Remove(inputDescriptor);
                            Console.WriteLine("No bluetooth device found (" + ex.Message + "), skipping bluetooth window");
                            continue;
                        }
                        break;
                    case InputType.Music:
                        try
                        {
                            MpcWrapper = new MpcWrapper();

                            var musicWindow = new MusicWindow(MpcWrapper);
                            MainWindows[InputType.Music] = musicWindow;
                            Program.Instance.WindowManager.AddWindow(musicWindow);
                        }
                        catch (Exception ex)
                        {
                            allInputs.Remove(inputDescriptor);
                            Console.WriteLine("MPC not found (" + ex.Message + "), skipping music window");
                            continue;
                        }
                        break;
                }


                var current = new MenuWindow(
                    (w, g) =>
                    {
                        var descriptor = (InputDescriptor)w.Tag;

                        var text = descriptor.Name;
                        g.Clear();

                        var inImage = InputImages[descriptor.Type];
                        var margin = (g.Height - inImage.Height) / 2;
                        g.DrawImage(8, margin, inImage);
                        g.DrawText(8 + inImage.Width + 6, 20, text, Fonts.Condensed, Color.White);
                    },
                    w =>
                    {
                        var descriptor = (InputDescriptor)w.Tag;
                        ChangeInputDevice(descriptor);
                    }) { Tag = inputDescriptor };
                if (previous != null)
                    previous.Next = current;
                previous = current;

                InputSelectionWindows.Add(current);

                Program.Instance.WindowManager.AddWindow(current);
            }

            VolWindow = new VolumeWindow();
            Program.Instance.WindowManager.AddWindow(VolWindow);

            SettingsManager = new SettingsWindowManager(VolWindow);
            var settingsOption = new MenuWindow(
                (w, g) =>
                {
                    g.Clear();

                    var margin = (g.Height - SettingsImage.Height) / 2;
                    g.DrawImage(8, margin, SettingsImage);
                    g.DrawText(14 + SettingsImage.Width, 20, "Settings", Fonts.Condensed, Color.White);
                },
                w => Program.Instance.WindowManager.SwitchWindow(SettingsManager.FirstWindow, true, SlideDirection.Top));
            if (previous != null)
                previous.Next = settingsOption;
            Program.Instance.WindowManager.AddWindow(settingsOption);

            var back = new GoBackWindow();
            settingsOption.Next = back;
            Program.Instance.WindowManager.AddWindow(back);

            var timeWindow = new MainWindowTime();
            MainWindows[InputType.Generic] = timeWindow;
            MainWindows[InputType.Pc] = timeWindow;
            Program.Instance.WindowManager.AddWindow(timeWindow);

            var selectedDescriptor = allInputs.FirstOrDefault(i => i.Id == Settings.Instance.SelectedInput) ?? allInputs.First();
            ChangeInputDevice(selectedDescriptor);
        }

        protected virtual void OnMainWindowChanged(bool amIVisible)
        {

        }

        public override bool OnScroll(int delta)
        {
            HandleVolumeChange(delta);
            return true;
        }

        public override bool OnButtonDown()
        {
            GoToInputMenu();
            return true;
        }

        public override bool OnRemoteButtonPressed(RemoteButton button)
        {
            DefaultRemoteControlButtonPressed(button);
            return true;
        }

        public static void DefaultRemoteControlButtonUp(RemoteButton button)
        {

        }

        public static void DefaultRemoteControlButtonDown(RemoteButton button)
        {

        }

        public static void DefaultRemoteControlButtonPressed(RemoteButton button)
        {
            switch (button)
            {
                case RemoteButton.VolumeDown: HandleVolumeChange(-2); break;
                case RemoteButton.VolumeUp: HandleVolumeChange(2); break;
            }
        }

        private static void HandleVolumeChange(int scroll)
        {
            VolWindow.OnScroll(scroll);
            Program.Instance.WindowManager.SwitchWindow(VolWindow, true, SlideDirection.Bottom);
        }

        protected void GoToInputMenu()
        {
            Window found = InputSelectionWindows.FirstOrDefault(i => ((InputDescriptor)i.Tag).Id == _selectedInput) ?? InputSelectionWindows.FirstOrDefault();
            Program.Instance.WindowManager.SwitchWindow(found, true, SlideDirection.Top);
        }
    }
}
