using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Julia.Utils
{
    public class Disposable : IDisposable
    {
        private readonly Action _action;

        private Disposable(Action action)
        {
            _action = action;
        }

        public static IDisposable Create(Action dispose)
        {
            return new Disposable(dispose);
        }

        public void Dispose()
        {
            _action();
        }
    }
}
