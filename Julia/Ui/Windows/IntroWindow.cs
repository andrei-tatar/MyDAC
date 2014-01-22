using Julia.Interfaces.Drawing;

namespace Julia.Ui.Windows
{
    class IntroWindow : Window
    {
        private Image _rpiImage;
        private int _time;

        public override void Refresh(IGraphics graphics)
        {
            if (_rpiImage == null)
            {
                using (var bmp = @"../Resources/rpi.png".LoadImage())
                using (var newBmp = bmp.Resize(graphics.Height))
                    _rpiImage = Image.FromBitmap(newBmp, true);
            }

            graphics.Clear();
            graphics.DrawImage((graphics.Width - _rpiImage.Width) / 2, 0, _rpiImage);

            base.Refresh(graphics);
        }

        public override void OnTick(int deltaTimeMs)
        {
            _time += deltaTimeMs;
            if (_time >= 3000)
                Program.Instance.WindowManager.SwitchWindow(MainWindow.CurrentMainWindow, false, SlideDirection.Right);
        }
    }
}
