using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTS.Common
{
    /// <summary>
    /// Class to wrap something that is not disposable into an IDisposable to enable a using pattern.
    /// This can be useful for e.g. database connections where Open() and Close() must otherwise be manually called.
    /// Wrapping such in this class would ensure that Close() is called without needing to provide tons of boilerplate code each time it is used.
    /// </summary>
    public class DisposableWrapper : IDisposable
    {
        private readonly Action _dispose = null;

        /// <summary>
        /// Construct a new DisposableWrapper
        /// </summary>
        /// <param name="open">Callback to execute immediately</param>
        /// <param name="dispose">Callback to execute once this wrapper leaves the scope of the using() block</param>
        public DisposableWrapper(Action open, Action dispose)
        {
            open?.Invoke();
            _dispose = dispose;
        }

        /// <summary>
        /// Calls the wrapped dispose callback
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Calls the wrapped dispose callback
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dispose?.Invoke();
            }
        }
    }
}
