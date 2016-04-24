using System;
using System.Threading;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Uses a <see cref="SynchronizationContext"/> to invoke delegates from the UI thread.
    /// </summary>
    public abstract class SynchronizationContextUIHandlerBase : IUIThreadHandler
    {
        //// NOTE: Unfortunately SynchronizationContext equality is not reliable, so there
        ////       needs to be some other method of determining whether we're on the UI thread.
        ////       see: http://stackoverflow.com/questions/13500030/comparing-synchronizationcontext

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizationContextUIHandlerBase"/> class.
        /// </summary>
        /// <param name="syncContext">The <see cref="SynchronizationContext"/> associated with the UI thread.</param>
        protected SynchronizationContextUIHandlerBase( SynchronizationContext syncContext )
        {
            if( syncContext.NullReference() )
                throw new ArgumentNullException(nameof(syncContext)).StoreFileLine();

            this.Context = syncContext;
        }

        #endregion

        #region IUIThreadHandler

        /// <summary>
        /// Determines whether the calling code is running on the UI thread.
        /// </summary>
        /// <returns><c>true</c> if the calling code is running on the UI thread; otherwise, <c>false</c>.</returns>
        public abstract bool IsOnUIThread();

        /// <summary>
        /// Executes the specified <see cref="Action"/> synchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public void Invoke( Action action )
        {
            this.Context.Send(state => action(), state: null);
        }

        /// <summary>
        /// Executes the specified <see cref="Action"/> asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public void BeginInvoke( Action action )
        {
            this.Context.Post(state => action(), state: null);
        }

        #endregion

        #region Protected Members

        /// <summary>
        /// Gets the UI <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <value>The UI <see cref="SynchronizationContext"/>.</value>
        protected SynchronizationContext Context
        {
            get;
        }

        #endregion
    }
}
