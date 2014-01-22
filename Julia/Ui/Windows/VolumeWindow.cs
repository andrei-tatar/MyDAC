using Julia.Drivers;
using Julia.Interfaces.Drawing;
using Julia.Interfaces.Drivers;

namespace Julia.Ui.Windows
{
    class VolumeWindow : BaseWindow
    {
        private readonly Image _volumeImage;

        private bool _showInDb;
        private int _offTime;

        public bool ShowInDb
        {
            get { return _showInDb; }
            set
            {
                if (_showInDb == value) return;
                _showInDb = value;
                Refresh();
            }
        }

        public int Volume
        {
            get { return Program.Instance.Sound.Volume; }
            set
            {
                if (Volume == value) return;
                Program.Instance.Sound.Volume = value;
                Settings.Instance.Volume = Volume;
                Refresh();
            }
        }
        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (value) _offTime = 0;
                base.Visible = value;
            }
        }

        public VolumeWindow()
        {
            ShowInDb = Settings.Instance.VolumeInDb;
            Volume = Settings.Instance.Volume;

            using (var bmp = @"../Resources/volume.png".LoadImage())
            using (var newBmp = bmp.Resize(40))
                _volumeImage = Image.FromBitmap(newBmp, true);
        }

        public override void Refresh(IGraphics graphics)
        {
            graphics.Clear();

            var margin = (graphics.Height - _volumeImage.Height) / 2;
            int w, h;

            var mute = Program.Instance.Sound.Mute;
            var text = mute ? "Mute  " : (ShowInDb ? Program.Instance.Sound.VolumeInDb : (JuliaSound.VolumeMin - Volume) * 100.0 / (JuliaSound.VolumeMin - JuliaSound.VolumeMax)).ToString("0.0") + (ShowInDb ? " dB" : " %");
            graphics.DrawImage(margin / 2, margin, _volumeImage);
            if (mute)
            {
                graphics.DrawLine(margin / 2, margin + _volumeImage.Height, margin / 2 + _volumeImage.Width, margin, Color.White);
                graphics.DrawLine(margin / 2, margin + _volumeImage.Height - 1, margin / 2 + _volumeImage.Width - 1, margin, Color.White);
                graphics.DrawLine(margin / 2 + 1, margin + _volumeImage.Height, margin / 2 + _volumeImage.Width, margin + 1, Color.White);
            }
            Fonts.Condensed.Measure(text, out w, out h);
            graphics.DrawText(graphics.Width - w - margin / 2, (graphics.Height - h) / 2, text, Fonts.Condensed, Color.White);

            base.Refresh(graphics);
        }

        public override bool OnScroll(int delta)
        {
            _offTime = 0;
            Volume += delta;
            return true;
        }

        public override bool OnRemoteButtonPressed(Interfaces.Drivers.RemoteButton button)
        {
            switch (button)
            {
                case RemoteButton.VolumeDown: OnScroll(-2); return true;
                case RemoteButton.VolumeUp: OnScroll(2);
                    return true;
                default:
                    return false;
            }
        }

        public override void OnTick(int deltaTimeMs)
        {
            _offTime += deltaTimeMs;
            if (_offTime >= 3000)
                Program.Instance.WindowManager.SwitchWindowBack();
        }

        public override bool OnButtonUp()
        {
            Program.Instance.WindowManager.SwitchWindowBack();
            return true;
        }
    }
}
