using System;
using System.Threading;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Implements <see cref="IUIThreadHandler"/> using a <see cref="Thread"/> for identification, and <see cref="SynchronizationContext"/> for code execution.
    /// </summary>
    public class ThreadSynchronizationContextUIHandler : SynchronizationContextUIHandlerBase
    {
        #region Private Fields

        private readonly Thread thread;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSynchronizationContextUIHandler"/> class.
        /// </summary>
        /// <param name="uiThread">The UI <see cref="Thread"/>.</param>
        /// <param name="uiContext">The UI <see cref="SynchronizationContext"/>.</param>
        public ThreadSynchronizationContextUIHandler( Thread uiThread, SynchronizationContext uiContext )
            : base(uiContext)
        {
            if( uiThread.NullReference() )
                throw new ArgumentNullException(nameof(uiThread)).StoreFileLine();

            this.thread = uiThread;
        }

        /// <summary>
        /// Creates a new <see cref="IUIThreadHandler"/> from the current <see cref="Thread"/> and <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <returns>A new <see cref="ThreadSynchronizationContextUIHandler"/> instance.</returns>
        public static ThreadSynchronizationContextUIHandler FromCurrent()
        {
            return new ThreadSynchronizationContextUIHandler(Thread.CurrentThread, SynchronizationContext.Current);
        }

        #endregion

        #region IUIThreadHandler

        /// <summary>
        /// Determines whether the calling code is running on the UI thread.
        /// </summary>
        /// <returns><c>true</c> if the calling code is running on the UI thread; otherwise, <c>false</c>.</returns>
        public sealed override bool IsOnUIThread()
        {
            // NOTE: ManagedThreadId is only unique during while a thread is alive, but can be reused afterwards
            return this.thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId
                && this.thread.IsAlive;
        }

        #endregion
    }
}
