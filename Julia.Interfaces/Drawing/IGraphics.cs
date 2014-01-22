namespace Julia.Interfaces.Drawing
{
    public interface IGraphics
    {
        int Width { get; }
        int Height { get; }

        void Clear(Color color = Color.Black);

        void DrawHLine(int x, int y, int width, Color color);
        void DrawVLine(int x, int y, int height, Color color);
        void DrawLine(int x1, int y1, int x2, int y2, Color color);

        void DrawRectangle(int x, int y, int width, int height, Color color);
        void FillRectangle(int x, int y, int width, int height, Color color);

        void SetPixel(int x, int y, Color color, Color maskColor = Color.Transparent);
        Color GetPixel(int x, int y);

        void DrawGraphics(IGraphics source, int srcX, int srcY, int destX, int destY, int width, int height);
        void DrawImage(int x, int y, Image image, Color maskColor = Color.Transparent);

        void DrawText(int x, int y, string text, Font font, Color color);
    }
}