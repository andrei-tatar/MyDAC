using System;
using System.Linq;
using System.Threading;
using Julia.Interfaces.Drawing;

namespace Julia.Ui.Windows
{
    class SettingsWindowManager
    {
        private readonly VolumeWindow _volumeWindow;

        public Window FirstWindow { get; private set; }

        public SettingsWindowManager(VolumeWindow volumeWindow)
        {
            _volumeWindow = volumeWindow;

            MenuWindow previous = null;
            AddNew(ref previous,
                   new ToggleSetting(
                       (setting, graphics) =>
                       {
                           graphics.Clear();
                           var text = "Volume in: " + (setting.IsChecked ? "dB" : "%");
                           graphics.DrawText(7, 20, text, Fonts.Condensed, Color.White);

                           if (setting.IsFocused)
                               graphics.DrawRectangle(2, 2, graphics.Width - 4, graphics.Height - 4, Color.White);
                       },
                       setting =>
                       {
                           _volumeWindow.ShowInDb = setting.IsChecked;
                           Settings.Instance.VolumeInDb = setting.IsChecked;
                       }, Orientation.Vertical)
                       {
                           IsChecked = volumeWindow.ShowInDb
                       });

            const int brightnessSteps = 20;
            var winManager = Program.Instance.WindowManager;
            AddNew(ref previous,
                   new ValueSetting(
                       (setting, graphics) =>
                       {
                           graphics.Clear();
                           graphics.DrawText(7, 10, "Brightness", Fonts.Condensed, Color.White);

                           const int x = 5;
                           var width = (graphics.Width - x * 2) / (float)brightnessSteps;
                           if (setting.IsFocused)
                           {
                               graphics.FillRectangle(x, 40, (int)(setting.Value * width), 17, Color.White);
                               graphics.DrawRectangle(2, 2, graphics.Width - 4, graphics.Height - 4, Color.White);
                           }
                           else
                               graphics.DrawRectangle(x, 40, (int)(setting.Value * width), 17, Color.White);
                       },
                       setting =>
                       {
                           var wmgr = (WindowManager)setting.Tag;
                           wmgr.Brightness = wmgr.MinBrightness + setting.Value * (wmgr.MaxBrightness - wmgr.MinBrightness) / brightnessSteps;
                           Settings.Instance.Brightness = setting.Value;
                       }, brightnessSteps, 1, Orientation.Vertical)
                       {
                           Tag = winManager,
                           Value = Settings.Instance.Brightness
                       });

            const int maxTimeout = 21;
            AddNew(ref previous,
                   new ValueSetting(
                       (setting, graphics) =>
                       {
                           graphics.Clear();
                           graphics.DrawText(7, 10, "Turn off scr.", Fonts.Condensed, Color.White);
                           graphics.DrawText(7, 35, setting.Value == maxTimeout ? "Never" : setting.Value > 10 ? (setting.Value - 10) + " min" : setting.Value * 5 + " sec", Fonts.Condensed, Color.White);
                           if (setting.IsFocused)
                               graphics.DrawRectangle(2, 2, graphics.Width - 4, graphics.Height - 4, Color.White);
                       },
                       setting =>
                       {
                           var wmgr = (WindowManager)setting.Tag;
                           wmgr.TurnOffTime = setting.Value == maxTimeout ? Timeout.Infinite : (setting.Value > 10 ? (setting.Value - 10) * 60 : setting.Value * 5) * 1000;
                           Settings.Instance.TurnOffScreenTimeout = setting.Value;
                       }, maxTimeout, 1, Orientation.Vertical)
                   {
                       Tag = winManager,
                       Value = Settings.Instance.TurnOffScreenTimeout
                   });

            var menuSpeeds = Enum.GetValues(typeof(MenuSpeed)).Cast<MenuSpeed>().ToList();
            AddNew(ref previous,
                   new ValueSetting(
                       (setting, graphics) =>
                       {
                           graphics.Clear();
                           graphics.DrawText(7, 10, "Animation", Fonts.Condensed, Color.White);
                           graphics.DrawText(7, 35, menuSpeeds[setting.Value].GetDescription(), Fonts.Condensed, Color.White);
                           if (setting.IsFocused)
                               graphics.DrawRectangle(2, 2, graphics.Width - 4, graphics.Height - 4, Color.White);
                       },
                       setting =>
                       {
                           var wmgr = (WindowManager)setting.Tag;
                           Settings.Instance.MenuSpeed = setting.Value;
                           wmgr.MenuSpeed = menuSpeeds[setting.Value];
                       }, menuSpeeds.Count - 1, 0, Orientation.Vertical)
                   {
                       Tag = winManager,
                       Value = Settings.Instance.MenuSpeed
                   });

            AddNew(ref previous, new GoBackWindow(Orientation.Vertical, Timeout.Infinite));
        }

        private void AddNew(ref MenuWindow previous, MenuWindow window)
        {
            if (FirstWindow == null) FirstWindow = window;
            if (previous != null) previous.Next = window;
            Program.Instance.WindowManager.AddWindow(window);
            previous = window;
        }
    }
}
