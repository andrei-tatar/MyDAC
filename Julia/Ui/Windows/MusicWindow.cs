using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Julia.Drivers;
using Julia.Interfaces.Drawing;
using Julia.Interfaces.Drivers;

namespace Julia.Ui.Windows
{
    class MusicWindow : MainWindow
    {
        private readonly MpcWrapper _mpcWrapper;
        private const int ScrollEndTimeout = 2500;
        private const int ScrollTimeout = 60;

        private string _title;
        private readonly Font _titleFont;
        private readonly Timer _scrollTimer;
        private readonly WindowManager _wmgr;
        private int _width, _height, _offset, _delta;

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (value && _width > 128)
                {
                    _offset = 0;
                    _scrollTimer.Change(ScrollEndTimeout, ScrollTimeout);
                }
                else
                {
                    _scrollTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }

                base.Visible = value;
            }
        }

        public MusicWindow(MpcWrapper mpcWrapper)
        {
            _mpcWrapper = mpcWrapper;
            _wmgr = Program.Instance.WindowManager;
            _titleFont = Fonts.Condensed;
            _scrollTimer = new Timer(
                state =>
                {
                    _offset = Math.Max(0, Math.Min(_width - 128, _offset + _delta));
                    _wmgr.Invoke(
                        () =>
                        {
                            Refresh();
                            if (_offset >= _width - 128)
                            {
                                _delta = -2;
                                _scrollTimer.Change(ScrollEndTimeout, ScrollTimeout);
                            }
                            else if (_offset <= 0)
                            {
                                _delta = 2;
                                _scrollTimer.Change(ScrollEndTimeout, ScrollTimeout);
                            }
                        });
                }, null, Timeout.Infinite, Timeout.Infinite);

            mpcWrapper.OnSongNameChanged += OnSongNameChanged;
            mpcWrapper.OnPlayStatusChanged += OnPlayStatusChanged;
            mpcWrapper.OnPlayFlagsChanged += OnPlayFlagsChanged;
        }

        private void OnPlayFlagsChanged(PlayFlags arg)
        {
        }

        private void OnPlayStatusChanged(PlayStatus newStatus)
        {
        }

        private void OnSongNameChanged(string newSongName)
        {
            ChangeTitle(newSongName);
        }

        private void ChangeTitle(string text)
        {
            _offset = 0;
            _title = text;
            _titleFont.Measure(text, out _width, out _height);

            if (_width > 128 && Visible)
                _scrollTimer.Change(ScrollEndTimeout, ScrollTimeout);

            Refresh();
        }

        public override void Refresh(IGraphics graphics)
        {
            graphics.Clear();

            var time = DateTime.Now;
            var text = string.Format("{0:00}:{1:00}", time.Hour, time.Minute);
            int w, h;
            Fonts.Console.Measure(text, out w, out h);
            graphics.DrawText(graphics.Width - w - 1, 1, text, Fonts.Console, Color.White);
            graphics.DrawText(-_offset, 15, _title, Fonts.Condensed, Color.White);

            base.Refresh(graphics);
        }

        public override bool OnRemoteButtonPressed(RemoteButton button)
        {
            switch (button)
            {
                case RemoteButton.PlayPause: _mpcWrapper.PlayPause();
                    return true;
                default:
                    return base.OnRemoteButtonPressed(button);
            }
        }
    }
}
