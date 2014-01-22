using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Julia.Interfaces.Drawing
{
    public class Image
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Color[,] Data { get; private set; }

        public static Image FromBitmap(System.Drawing.Image image, Func<ushort, Color> colorConversion)
        {
            var bitmap = (Bitmap)image;

            int width = bitmap.Width, height = bitmap.Height;
            var data = new Color[width, height];

            var array = BitmapToArray(bitmap, false);

            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Width; x++)
                    data[x, y] = colorConversion(array[x, y]);

            return new Image(data);
        }

        public static Image FromBitmap(System.Drawing.Image bitmap, bool invert = false)
        {
            return FromBitmap(bitmap, color => DefaultColorConverter(color, invert));
        }

        private static Color DefaultColorConverter(ushort color, bool invert)
        {
            return (color >> 8) < 128 ? Color.Transparent : ((color & 0xFF) < 150 ^ invert ? Color.Black : Color.White);
        }

        public Image(Color[,] data)
        {
            Width = data.GetLength(0);
            Height = data.GetLength(1);
            Data = data;
        }

        unsafe static ushort[,] Pic1BppToArray(Bitmap img, bool invert)
        {
            if (img.PixelFormat != PixelFormat.Format1bppIndexed)
                throw new Exception("Invalid image format! Expected 1 bpp");

            var imgMatrix = new ushort[img.Width, img.Height];
            var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);

            int imgWidth = img.Width,
                dataHeight = data.Height,
                dataStride = data.Stride;

            var bmpBytes = (byte*)data.Scan0.ToPointer();
            for (var y = 0; y < dataHeight; y++)
                for (var x = 0; x < dataStride; x++)
                    for (var j = 7; j >= 0; j--) //each bit in the byte is one pixel (MSb is first)
                    {
                        int px = x * 8 + (7 - j);
                        if (px >= imgWidth) break;

                        imgMatrix[px, y] = (ushort)(((bmpBytes[x + y * dataStride] & (1 << j)) != 0) ^ (!invert) ? 0xFF00 : 0xFFFF);
                    }

            img.UnlockBits(data);
            return imgMatrix;
        }

        static byte GetGrayScale(byte r, byte g, byte b)
        {
            return (byte)(0.3 * r + 0.59 * g + 0.11 * b);
        }

        static byte GetGrayScale(System.Drawing.Color c)
        {
            return (byte)(0.3 * c.R + 0.59 * c.G + 0.11 * c.B);
        }

        unsafe static ushort[,] Pic8BppToArray(Bitmap img, bool invert)
        {
            if (img.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new Exception("Invalid image format! Expected 8 bpp");

            var cPalette = img.Palette;

            var imgMatrix = new ushort[img.Width, img.Height];
            var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);

            int imgWidth = img.Width,
                dataHeight = data.Height,
                dataStride = data.Stride;

            var bmpBytes = (byte*)data.Scan0.ToPointer();
            for (var y = 0; y < dataHeight; y++)
                for (var x = 0; x < dataStride; x++)
                {
                    if (x >= imgWidth) break;
                    if (invert)
                        imgMatrix[x, y] = (ushort)(0xFF00 | (255 - GetGrayScale(cPalette.Entries[bmpBytes[x + y * dataStride]])));
                    else
                        imgMatrix[x, y] = (ushort)(0xFF00 | GetGrayScale(cPalette.Entries[bmpBytes[x + y * dataStride]]));
                }

            img.UnlockBits(data);
            return imgMatrix;
        }

        unsafe static ushort[,] Pic24BppToArray(Bitmap img, bool invert)
        {
            if (img.PixelFormat != PixelFormat.Format24bppRgb)
                throw new Exception("Invalid image format! Expected 24 bpp");

            var imgMatrix = new ushort[img.Width, img.Height];
            var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);

            int imgWidth = img.Width,
                dataHeight = data.Height,
                dataStride = data.Stride;

            var bmpBytes = (byte*)data.Scan0.ToPointer();
            for (var y = 0; y < dataHeight; y++)
                for (var x = 0; x < dataStride; x += 3)
                {
                    var px = x / 3;
                    if (px >= imgWidth) break;

                    byte gray = GetGrayScale(bmpBytes[x + 2 + y * dataStride], bmpBytes[x + 1 + y * dataStride], bmpBytes[x + y * dataStride]);
                    if (invert)
                        imgMatrix[px, y] = (ushort)(0xFF00 | (255 - gray));
                    else
                        imgMatrix[px, y] = (ushort)(0xFF00 | gray);
                }

            img.UnlockBits(data);
            return imgMatrix;
        }

        unsafe static ushort[,] Pic32BppToArray(Bitmap img, bool invert)
        {
            if (img.PixelFormat != PixelFormat.Format32bppArgb && img.PixelFormat != PixelFormat.Format32bppRgb)
                throw new Exception("Invalid image format! Expected 32 bpp");

            var imgMatrix = new ushort[img.Width, img.Height];
            var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);

            int imgWidth = img.Width,
                dataHeight = data.Height,
                dataStride = data.Stride;

            var bmpBytes = (byte*)data.Scan0.ToPointer();
            for (var y = 0; y < dataHeight; y++)
                for (var x = 0; x < dataStride; x += 4)
                {
                    var px = x / 4;
                    if (px >= imgWidth) break;

                    var gray = GetGrayScale(bmpBytes[x + 2 + y * dataStride], bmpBytes[x + 1 + y * dataStride], bmpBytes[x + y * dataStride]);
                    var alpha = bmpBytes[x + 3 + y * dataStride];
                    if (invert)
                        imgMatrix[px, y] = (ushort)(alpha << 8 | (255 - gray));
                    else
                        imgMatrix[px, y] = (ushort)(alpha << 8 | gray);
                }

            img.UnlockBits(data);
            return imgMatrix;
        }

        private static ushort[,] BitmapToArray(Bitmap img, bool invert)
        {
            ushort[,] res;
            switch (img.PixelFormat)
            {

                case PixelFormat.Format1bppIndexed:
                    res = Pic1BppToArray(img, invert);
                    break;
                case PixelFormat.Format8bppIndexed:
                    res = Pic8BppToArray(img, invert);
                    break;
                case PixelFormat.Format24bppRgb:
                    res = Pic24BppToArray(img, invert);
                    break;
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb: //same as 32bppRGB, alpha channel is ignored
                    res = Pic32BppToArray(img, invert);
                    break;
                default:
                    throw new Exception("Format not supported");
            }
            return res;
        }
    }
}
