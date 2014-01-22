using System;
using System.Collections.Generic;
using System.Threading;
using Julia.Interfaces.Drivers;
using Julia.Interfaces.Drawing;

namespace Julia.Ui
{
    public sealed class WindowManager : IDisposable
    {
        class WindowSaveItem
        {
            public Window Window;
            public SlideDirection SlideDirection;
        }

        private readonly Stack<WindowSaveItem> _lastWindows = new Stack<WindowSaveItem>();
        private readonly List<Window> _windows = new List<Window>();

        private bool _disposed;
        private readonly IScreen _screen;
        private readonly IMouse _mouse;
        private readonly IRemoteInterface _remoteInterface;
        private readonly Timer _tickTimer, _turnOffTimer;
        private readonly IGraphics _oldScreen, _newScreen;
        private readonly Queue<Action> _invokes = new Queue<Action>();
        private readonly AutoResetEvent _notifier = new AutoResetEvent(false);
        private readonly Window _offWindow;
        private int _mainThreadId;
        private int _turnOffTime;
        private volatile bool _cancelTransition;

        public Window CurrentWindow { get; private set; }
        public bool InvokeNeeded { get { return Thread.CurrentThread.ManagedThreadId != _mainThreadId; } }

        public int Brightness { get { return _screen.Brightness; } set { _screen.Brightness = value; } }
        public int MinBrightness { get { return _screen.MinBrightness; } }
        public int MaxBrightness { get { return _screen.MaxBrightness; } }
        public MenuSpeed MenuSpeed { get; set; }

        public int TurnOffTime
        {
            get { return _turnOffTime; }
            set
            {
                if (value < 5000) value = Timeout.Infinite;
                _turnOffTime = value;
            }
        }

        public WindowManager(IScreen screen, IMouse mouse, IRemoteInterface remoteInterface)
        {
            _screen = screen;
            _mouse = mouse;
            _remoteInterface = remoteInterface;

            _offWindow = new OffWindow(_screen);

            if (_mouse != null)
            {
                _mouse.OnMouseDown += MouseOnOnMouseDown;
                _mouse.OnMouseUp += MouseOnOnMouseUp;
                _mouse.OnMouseWheel += MouseOnOnMouseWheel;
            }

            if (_remoteInterface != null)
            {
                _remoteInterface.OnButtonPressed += RemoteInterfaceOnButtonPressed;
                _remoteInterface.OnButtonDown += RemoteInterfaceOnButtonDown;
                _remoteInterface.OnButtonUp += RemoteInterfaceOnButtonUp;
            }

            const int _timeDelta = 200;
            _tickTimer = new Timer(
                state => Invoke(
                    () =>
                    {
                        if (CurrentWindow != null)
                            CurrentWindow.OnTick(_timeDelta);
                    }), null, 1000, _timeDelta);

            _oldScreen = new Graphics(_screen.Width, _screen.Height);
            _newScreen = new Graphics(_screen.Width, _screen.Height);

            TurnOffTime = Timeout.Infinite;
            _turnOffTimer = new Timer(state => Invoke(() => SwitchWindow(_offWindow, true, SlideDirection.Right, SlideDirection.Right)), null, TurnOffTime, Timeout.Infinite);

            AddWindowAndSwitchIt(_offWindow);
        }

        #region event handlers
        void RemoteInterfaceOnButtonPressed(RemoteButton button)
        {
            _turnOffTimer.Change(TurnOffTime, Timeout.Infinite);
            Invoke(() =>
                   {
                       if (CurrentWindow != null)
                           CurrentWindow.OnRemoteButtonPressed(button);
                   });
        }

        void RemoteInterfaceOnButtonDown(RemoteButton button)
        {
            _cancelTransition = true;

            _turnOffTimer.Change(TurnOffTime, Timeout.Infinite);
            Invoke(() =>
            {
                if (CurrentWindow != null)
                    CurrentWindow.OnRemoteButtonDown(button);
            });
        }

        void RemoteInterfaceOnButtonUp(RemoteButton button)
        {
            _turnOffTimer.Change(TurnOffTime, Timeout.Infinite);
            Invoke(() =>
            {
                if (CurrentWindow != null)
                    CurrentWindow.OnRemoteButtonUp(button);
            });
        }

        private void MouseOnOnMouseWheel(int wheelDelta)
        {
            _cancelTransition = true;

            _turnOffTimer.Change(TurnOffTime, Timeout.Infinite);
            Invoke(() =>
            {
                if (CurrentWindow != null)
                    CurrentWindow.OnScroll(wheelDelta);
            });
        }

        private void MouseOnOnMouseUp(MouseButton button)
        {
            _turnOffTimer.Change(TurnOffTime, Timeout.Infinite);
            Invoke(() =>
                   {
                       if (CurrentWindow != null)
                           CurrentWindow.OnButtonUp();
                   });
        }

        private void MouseOnOnMouseDown(MouseButton button)
        {
            _cancelTransition = true;

            _turnOffTimer.Change(TurnOffTime, Timeout.Infinite);
            Invoke(() =>
            {
                if (CurrentWindow != null)
                    CurrentWindow.OnButtonDown();
            });
        }
        #endregion

        public void ReplaceInStack(Window toFind, Window toReplace)
        {
            foreach (var window in _lastWindows)
            {
                if (window.Window == toFind)
                    window.Window = toReplace;
            }
        }

        public void Invoke(Action action)
        {
            if (!InvokeNeeded)
            {
                action();
                return;
            }

            lock (_invokes)
            {
                _invokes.Enqueue(action);
            }
            _notifier.Set();
        }

        public void Run()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            while (true)
            {
                _notifier.WaitOne();

                bool more;
                do
                {
                    Action action = null;
                    lock (_invokes)
                    {
                        more = _invokes.Count != 0;
                        if (more)
                            action = _invokes.Dequeue();
                    }

                    if (action != null)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Unhandled exception:");
                            Console.WriteLine(ex);
                        }
                    }

                    if (_disposed) break;
                } while (more);

                if (_disposed) break;
            }
        }

        public void AddWindow(Window window)
        {
            if (_windows.Contains(window)) return;
            _windows.Add(window);
            window.Parent = this;
            window.Visible = false;
        }

        public void AddWindowAndSwitchIt(Window window, bool addToStack = false, SlideDirection slideTo = SlideDirection.None)
        {
            AddWindow(window);
            SwitchWindow(window, addToStack, slideTo);
        }

        public void SwitchWindowBack()
        {
            SwitchWindowBack(true);
        }

        public void SwitchWindow(Window window, bool addToStack = false, SlideDirection slide = SlideDirection.None)
        {
            SwitchWindowPrivate(window, addToStack, slide);
        }

        public void SwitchWindow(Window window, bool addToStack, SlideDirection slide, SlideDirection switchBackSlide)
        {
            SwitchWindowPrivate(window, addToStack, slide, switchBackSlide);
        }

        internal void RefreshWindow(Window sender)
        {
            if (sender == CurrentWindow)
                sender.Refresh(_screen);
        }

        private void CheckForScreenOff()
        {
            if (CurrentWindow == _offWindow)
                SwitchWindowBack(false);
        }

        internal void SwitchWindowBack(bool checkForScreenOff)
        {
            if (checkForScreenOff) CheckForScreenOff();

            if (_lastWindows.Count == 0) return;
            var popped = _lastWindows.Pop();
            SwitchWindowPrivate(popped.Window, false, popped.SlideDirection, checkForScreenOff);
        }

        private void SwitchWindowPrivate(Window window, bool addToStack, SlideDirection slide, bool checkForScreenOff = true)
        {
            SwitchWindowPrivate(window, addToStack, slide, slide.Reverse(), checkForScreenOff);
        }

        private void SwitchWindowPrivate(Window window, bool addToStack, SlideDirection slide, SlideDirection switchBackSlide, bool checkForScreenOff = true)
        {
            if (checkForScreenOff) CheckForScreenOff();

            _cancelTransition = false;

            if (CurrentWindow != null)
            {
                CurrentWindow.Visible = false;
                if (addToStack)
                    _lastWindows.Push(new WindowSaveItem { Window = CurrentWindow, SlideDirection = switchBackSlide });
                CurrentWindow.Refresh(_oldScreen);
            }
            else
                _oldScreen.Clear();

            window.Refresh(_newScreen);

            #region do the animation
            if (MenuSpeed != MenuSpeed.None)
            {
                const int _timeout = 15;
                var divFactor = (float)MenuSpeed / 60;

                var toLeft = slide == SlideDirection.Left;
                var toTop = slide == SlideDirection.Top;
                if (toLeft || slide == SlideDirection.Right)
                {
                    for (float x = _screen.Width; x >= 0; x -= 1 + x * divFactor)
                    {
                        var srcX = _screen.Width - (int)x;
                        var windowLeft = toLeft ? _oldScreen : _newScreen;
                        var windowRight = toLeft ? _newScreen : _oldScreen;

                        if (windowLeft != null) _screen.DrawGraphics(windowLeft, toLeft ? srcX : (int)x, 0, 0, 0, toLeft ? (int)x : srcX, _screen.Height);
                        if (windowRight != null) _screen.DrawGraphics(windowRight, 0, 0, toLeft ? (int)x : srcX, 0, toLeft ? srcX : (int)x, _screen.Height);
                        _screen.Flush();
                        Thread.Sleep(_timeout);

                        if (_cancelTransition) break;
                    }
                }
                else if (toTop || slide == SlideDirection.Bottom)
                {
                    for (float y = _screen.Height; y >= 0; y -= 1 + y * divFactor)
                    {
                        var srcY = _screen.Height - (int)y;
                        var windowTop = toTop ? _oldScreen : _newScreen;
                        var windowBottom = toTop ? _newScreen : _oldScreen;

                        if (windowTop != null) _screen.DrawGraphics(windowTop, 0, toTop ? srcY : (int)y, 0, 0, _screen.Width, toTop ? (int)y : srcY);
                        if (windowBottom != null) _screen.DrawGraphics(windowBottom, 0, 0, 0, toTop ? (int)y : srcY, _screen.Width, toTop ? srcY : (int)y);
                        _screen.Flush();
                        Thread.Sleep(_timeout);

                        if (_cancelTransition) break;
                    }
                }
                _cancelTransition = false;
            }
            #endregion

            _screen.DrawGraphics(_newScreen, 0, 0, 0, 0, _newScreen.Width, _newScreen.Height);
            _screen.Flush();

            CurrentWindow = window;
            CurrentWindow.Visible = true;
            if (CurrentWindow != _offWindow)
                _turnOffTimer.Change(TurnOffTime, Timeout.Infinite);

        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _windows.ForEach(w => w.Dispose());
            _windows.Clear();

            if (_mouse != null)
            {
                _mouse.OnMouseDown -= MouseOnOnMouseDown;
                _mouse.OnMouseUp -= MouseOnOnMouseUp;
                _mouse.OnMouseWheel -= MouseOnOnMouseWheel;
            }

            if (_remoteInterface != null)
            {
                _remoteInterface.OnButtonPressed -= RemoteInterfaceOnButtonPressed;
            }

            _notifier.Set();
            _turnOffTimer.Dispose();
            _tickTimer.Dispose();
        }
    }


}
