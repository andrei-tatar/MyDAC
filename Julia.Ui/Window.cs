using System;
using Julia.Interfaces.Drawing;
using Julia.Interfaces.Drivers;

namespace Julia.Ui
{
    public abstract class Window : IDisposable
    {
        public virtual bool Visible { get; set; }

        public delegate void RefresHandler(Window sender);
        public WindowManager Parent { get; internal set; }

        public void Refresh()
        {
            if (Parent != null)
                Parent.RefreshWindow(this);
        }

        public virtual void Refresh(IGraphics graphics)
        {
            var screen = graphics as IScreen;
            if (Parent.CurrentWindow == this && screen != null)
                screen.Flush();
        }

        public virtual void OnTick(int deltaTimeMs)
        {
        }

        public virtual bool OnButtonDown()
        {
            return false;
        }

        public virtual bool OnButtonUp()
        {
            return false;
        }

        public virtual bool OnScroll(int delta)
        {
            return false;
        }

        public virtual bool OnRemoteButtonDown(RemoteButton button)
        {
            return false;
        }

        public virtual bool OnRemoteButtonPressed(RemoteButton button)
        {
            return false;
        }

        public virtual bool OnRemoteButtonUp(RemoteButton button)
        {
            return false;
        }

        public virtual void Dispose()
        {
        }
    }
}
