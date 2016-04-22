using System;
using System.Threading;
using System.Threading.Tasks;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Uses a <see cref="SynchronizationContext"/> to invoke delegates from the UI thread.
    /// </summary>
    public class SynchronizationContextUIHandler : IUIThreadHandler
    {
        #region Private Fields

        private readonly SynchronizationContext context;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizationContextUIHandler"/> class.
        /// </summary>
        /// <param name="syncContext">The <see cref="SynchronizationContext"/> associated with the UI thread.</param>
        public SynchronizationContextUIHandler( SynchronizationContext syncContext )
        {
            if( syncContext.NullReference() )
                throw new ArgumentNullException(nameof(syncContext)).StoreFileLine();

            this.context = syncContext;
        }

        /// <summary>
        /// Creates a new <see cref="SynchronizationContextUIHandler"/> using the current <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <returns>A new <see cref="SynchronizationContextUIHandler"/> instance.</returns>
        public static SynchronizationContextUIHandler FromCurrent()
        {
            return new SynchronizationContextUIHandler(SynchronizationContext.Current); // should be null, if not on the main thread
        }

        #endregion

        #region IUIThreadHandler

        /// <summary>
        /// Determines whether the calling code is running on the UI thread.
        /// </summary>
        /// <returns><c>true</c> if the calling code is running on the UI thread; otherwise, <c>false</c>.</returns>
        public bool IsOnUIThread()
        {
            return this.context == SynchronizationContext.Current;
        }

        /// <summary>
        /// Executes the specified <see cref="Action"/> synchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public void Invoke( Action action )
        {
            this.context.Send(state => action(), state: null);
        }

        /// <summary>
        /// Executes the specified <see cref="Action"/> asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public void BeginInvoke( Action action )
        {
            this.context.Post(state => action(), state: null);
        }

        #endregion
    }
}
