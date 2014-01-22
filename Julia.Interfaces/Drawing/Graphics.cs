using System;

namespace Julia.Interfaces.Drawing
{
    public class Graphics : IGraphics
    {
        public readonly int Width;
        public readonly int Height;

        protected readonly Color[,] Buffer;
        protected bool _hasChanges;
        protected int _minX, _minY, _maxX, _maxY;

        int IGraphics.Width { get { return Width; } }
        int IGraphics.Height { get { return Height; } }

        public Graphics(int width, int height)
        {
            Width = width;
            Height = height;
            Buffer = new Color[width, height];
        }

        public void Clear(Color color = Color.Black)
        {
            for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++)
                    Buffer[x, y] = color;

            _minX = 0; _maxX = Width - 1;
            _minY = 0; _maxY = Height - 1;
            _hasChanges = true;
        }

        protected void ResetChanges()
        {
            _hasChanges = false;
            _minX = Width;
            _maxX = 0;
            _minY = Height;
            _maxY = 0;
        }

        protected void SetPx(int x, int y, Color color, Color maskColor = Color.Transparent)
        {
            if (color == Color.Mask)
                color = maskColor;

            switch (color)
            {
                case Color.Transparent:
                    return;

                case Color.Invert:
                    switch (Buffer[x, y])
                    {
                        case Color.Black:
                            Buffer[x, y] = Color.White;
                            return;
                        case Color.White:
                            Buffer[x, y] = Color.Black;
                            return;
                        default:
                            return;
                    }

                default:
                    Buffer[x, y] = color;
                    break;
            }
        }

        public void DrawHLine(int x, int y, int width, Color color)
        {
            if (x + width <= 0 || x >= Width || y < 0 || y >= Height) return;

            if (x < 0) { width += x; x = 0; }
            if (x + width > Width) width = Width - x;

            for (var i = 0; i < width; i++)
                SetPx(x + i, y, color);

            _minX = Math.Min(_minX, x);
            _maxX = Math.Max(_maxX, x + width - 1);
            _minY = Math.Min(_minY, y);
            _maxY = Math.Max(_maxY, y);
            _hasChanges = true;
        }

        public void DrawVLine(int x, int y, int height, Color color)
        {
            if (x < 0 || x >= Width || y + height <= 0 || y >= Height) return;

            if (y < 0) { height += y; y = 0; }
            if (y + height > Height) height = Height - y;

            for (var i = 0; i < height; i++)
                SetPx(x, y + i, color);

            _minX = Math.Min(_minX, x);
            _maxX = Math.Max(_maxX, x);
            _minY = Math.Min(_minY, y);
            _maxY = Math.Max(_maxY, y + height - 1);
            _hasChanges = true;
        }

        public void DrawLine(int x1, int y1, int x2, int y2, Color color)
        {
            int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1),
                sx = ((x1 < x2) ? 1 : -1), sy = ((y1 < y2) ? 1 : -1),
                err = (dx - dy);
            do
            {
                if (x1 >= 0 && y1 >= 0)
                    SetPixel(x1, y1, color);

                if (x1 == x2 && y1 == y2)
                    break;

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 = x1 + sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y1 = y1 + sy;
                }
            } while (true);
        }

        public void DrawRectangle(int x, int y, int width, int height, Color color)
        {
            DrawHLine(x, y, width, color);
            DrawHLine(x, y + height - 1, width, color);
            DrawVLine(x, y + 1, height - 2, color);
            DrawVLine(x + width - 1, y + 1, height - 2, color);
        }

        public void FillRectangle(int x, int y, int width, int height, Color color)
        {
            while (height-- != 0)
                DrawHLine(x, y++, width, color);
        }

        public void SetPixel(int x, int y, Color color, Color maskColor = Color.Transparent)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;

            SetPx(x, y, color, maskColor);
            _minX = Math.Min(_minX, x);
            _maxX = Math.Max(_maxX, x);
            _minY = Math.Min(_minY, y);
            _maxY = Math.Max(_maxY, y);
            _hasChanges = true;
        }

        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Width) return Color.Transparent;
            return Buffer[x, y];
        }

        public void DrawGraphics(IGraphics source, int srcX, int srcY, int destX, int destY, int width, int height)
        {
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var color = source.GetPixel(x + srcX, y + srcY);
                    SetPixel(x + destX, y + destY, color);
                }
        }

        public void DrawImage(int x, int y, Image image, Color maskColor = Color.Transparent)
        {
            if (x + image.Width <= 0 || x >= Width || y + image.Height <= 0 || y >= Height) return;

            for (var yy = 0; yy < image.Height; yy++)
                for (var xx = 0; xx < image.Width; xx++)
                    SetPixel(x + xx, y + yy, image.Data[xx, yy], maskColor);
        }

        public void DrawText(int x, int y, string text, Font font, Color color)
        {
            if (x >= Width || y >= Height) return;

            foreach (var @char in text)
            {
                var charImage = font.GetImage(@char);
                if (charImage == null) continue;
                DrawImage(x, y, charImage, color);
                x += charImage.Width;

                if (x >= Width) return;
            }
        }
    }
}
