using System.Drawing;

namespace Julia
{
    static class Extensions
    {
        public static Image LoadImage(this string filePath)
        {
            return Image.FromFile(filePath);
        }

        public static Image Resize(this Image img, int newWidth, int newHeight)
        {
            var newBmp = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(newBmp))
            {
                g.DrawImage(img, new Rectangle(0, 0, newWidth, newHeight));
            }
            return newBmp;
        }

        public static Image Resize(this Image img, float scaleFactor)
        {
            return img.Resize((int)(img.Width * scaleFactor), (int)(img.Height * scaleFactor));
        }

        public static Image Resize(this Image img, int newHeight)
        {
            return img.Resize(newHeight / (float)img.Height);
        }
    }
}
