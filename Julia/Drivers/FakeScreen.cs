using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Julia.Interfaces.Drivers;
using Julia.Interfaces.Drawing;
using Color = System.Drawing.Color;
using Bitmap = System.Drawing.Bitmap;
using Font = Julia.Interfaces.Drawing.Font;
using Graphics = Julia.Interfaces.Drawing.Graphics;
using Image = Julia.Interfaces.Drawing.Image;

namespace Julia.Drivers
{
    class FakeScreen : Graphics, IScreen
    {
        private readonly IScreen _realScreen;
        private const int ScaleFactor = 2;

        private readonly Color _backColor = Color.Black;
        private readonly Color _whiteColor = Color.Orange;
        private readonly Form _form;
        private readonly PictureBox _pictureBox;
        private readonly Bitmap _image;
        private bool _disposed;
        private readonly IGraphics _buffer;

        public Form Form { get { return _form; } }
        public Control MouseControl { get { return _pictureBox; } }

        public bool On
        {
            get { return _realScreen.On; }
            set
            {
                if (_realScreen.On == value) return;
                _realScreen.On = value;
                FlushFromBuffer();
            }
        }

        class FakeRealScreen : IScreen
        {
            public int Width { get; private set; }
            public int Height { get; private set; }

            public FakeRealScreen(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public void Clear(Interfaces.Drawing.Color color = Interfaces.Drawing.Color.Black)
            {
            }

            public void DrawHLine(int x, int y, int width, Interfaces.Drawing.Color color)
            {
            }

            public void DrawVLine(int x, int y, int height, Interfaces.Drawing.Color color)
            {
            }

            public void DrawLine(int x1, int y1, int x2, int y2, Interfaces.Drawing.Color color)
            {
            }

            public void DrawRectangle(int x, int y, int width, int height, Interfaces.Drawing.Color color)
            {
            }

            public void FillRectangle(int x, int y, int width, int height, Interfaces.Drawing.Color color)
            {
            }

            public void SetPixel(int x, int y, Interfaces.Drawing.Color color, Interfaces.Drawing.Color maskColor = Interfaces.Drawing.Color.Transparent)
            {
            }

            public Interfaces.Drawing.Color GetPixel(int x, int y)
            {
                return Interfaces.Drawing.Color.Black;
            }

            public void DrawGraphics(IGraphics source, int srcX, int srcY, int destX, int destY, int width, int height)
            {
            }

            public void DrawImage(int x, int y, Image image, Interfaces.Drawing.Color maskColor = Interfaces.Drawing.Color.Transparent)
            {
            }

            public void DrawText(int x, int y, string text, Font font, Interfaces.Drawing.Color color)
            {
            }

            public void Dispose()
            {
            }

            public int MaxBrightness { get { return 250; } }
            public int MinBrightness { get { return 0; } }
            public int Brightness { get; set; }
            public bool On { get; set; }
            public void Flush()
            {
            }
        }

        public FakeScreen(int width, int height)
            : this(new FakeRealScreen(width, height))
        { }

        public FakeScreen(IScreen realScreen)
            : base(realScreen.Width, realScreen.Height)
        {
            _realScreen = realScreen;
            _buffer = new Graphics(Width, Height);
            _form = new Form
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                Text = "Oled " + Width + "x" + Height
            };
            _pictureBox = new PictureBox { SizeMode = PictureBoxSizeMode.AutoSize };
            _pictureBox.MouseHover += (sender, args) => _pictureBox.Focus();
            _image = new Bitmap(Width * ScaleFactor, Height * ScaleFactor);
            _pictureBox.Image = _image;
            _form.Controls.Add(_pictureBox);
            _form.FormClosing +=
                (s, e) =>
                {
                    if (e.CloseReason == CloseReason.UserClosing)
                        e.Cancel = true;
                };

            _form.Show();
            Flush();
        }

        private void DoOnUiThread(Action a)
        {
            if (_form.InvokeRequired)
                _form.Invoke(new MethodInvoker(() => a()));
            else
                a();
        }

        public int MaxBrightness { get { return _realScreen.MaxBrightness; } }
        public int MinBrightness { get { return _realScreen.MinBrightness; } }
        public int Brightness
        {
            get { return _realScreen.Brightness; }
            set
            {
                if (_realScreen.Brightness == value) return;
                _realScreen.Brightness = value;
                FlushFromBuffer();
            }
        }

        public void Flush()
        {
            if (_disposed) return;
            _realScreen.DrawGraphics(this, 0, 0, 0, 0, Width, Height);
            _realScreen.Flush();
            _buffer.DrawGraphics(this, 0, 0, 0, 0, Width, Height);
            FlushFromBuffer();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void FlushFromBuffer()
        {
            DoOnUiThread(
                () =>
                {
                    using (var g = System.Drawing.Graphics.FromImage(_image))
                    {
                        g.Clear(_backColor);

                        if (_realScreen.On)
                        {
                            _form.Text = "Oled " + Width + "x" + Height + "   ON!";
                            var brush = new SolidBrush(Color.FromArgb((Brightness - MinBrightness) / MaxBrightness * 128 + 127, _whiteColor));
                            for (var y = 0; y < Height; y++)
                                for (var x = 0; x < Width; x++)
                                    if (_buffer.GetPixel(x, y) == Interfaces.Drawing.Color.White)
                                        g.FillRectangle(brush, x * ScaleFactor, y * ScaleFactor, ScaleFactor, ScaleFactor);

                            for (var y = 0; y < Height; y++)
                                g.DrawLine(Pens.Black, 0, y * ScaleFactor, Width * ScaleFactor, y * ScaleFactor);
                            for (var x = 0; x < Width; x++)
                                g.DrawLine(Pens.Black, x * ScaleFactor, 0, x * ScaleFactor, Height * ScaleFactor);
                        }
                        else
                        {
                            _form.Text = "Oled " + Width + "x" + Height + "   OFF!";
                        }
                    }

                    _pictureBox.Refresh();
                });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            _disposed = true;
            _realScreen.Dispose();
            DoOnUiThread(
                () =>
                {
                    _form.Close();
                    _form.Dispose();
                });
        }
    }
}
