using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Julia.Interfaces.Drawing;

namespace Julia.Ui.Windows
{
    class MainWindowTime : MainWindow
    {
        private int _refreshTime;

        public override void Refresh(IGraphics graphics)
        {
            graphics.Clear();

            var time = DateTime.Now;
            var text = string.Format("{0:00}:{1:00}", time.Hour, time.Minute);
            int w, h;
            Fonts.Clocktopia.Measure(text, out w, out h);
            graphics.DrawText((graphics.Width - w) / 2, (graphics.Height - h) / 2 - 5, text, Fonts.Clocktopia, Color.White);

            text = string.Format("{0}/{1:00}/{2:0000}", time.Day, time.Month, time.Year);
            Fonts.Console.Measure(text, out w, out h);
            graphics.DrawText((graphics.Width - w) / 2, (graphics.Height - h) / 2 + 20, text, Fonts.Console, Color.White);

            base.Refresh(graphics);
        }

        public override void OnTick(int deltaMs)
        {
            _refreshTime += deltaMs;
            if (_refreshTime < 10000) return; //once every 10 sec

            Settings.Save();
            _refreshTime = 0;
            Refresh();
        }
    }
}
