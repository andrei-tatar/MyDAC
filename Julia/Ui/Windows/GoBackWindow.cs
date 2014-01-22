using Julia.Interfaces.Drawing;

namespace Julia.Ui.Windows
{
    class GoBackWindow : MenuWindow
    {
        private static readonly Image BackImage;

        static GoBackWindow()
        {
            using (var bmp = @"../Resources/back.png".LoadImage())
            using (var newBmp = bmp.Resize(32))
                BackImage = Image.FromBitmap(newBmp, true);
        }

        public GoBackWindow(Orientation orientation = Orientation.Horizontal, int goBackTimeout = 3000)
            : base((window, graphics) => DoTheRefresh(graphics), window => Program.Instance.WindowManager.SwitchWindowBack(), orientation, goBackTimeout)
        { }

        private static void DoTheRefresh(IGraphics graphics)
        {
            graphics.Clear();
            var margin = (graphics.Height - BackImage.Height) / 2;
            graphics.DrawImage(margin, margin, BackImage);
            graphics.DrawText(margin + BackImage.Width + margin / 2, 20, "Back", Fonts.Condensed, Color.White);
        }
    }
}
