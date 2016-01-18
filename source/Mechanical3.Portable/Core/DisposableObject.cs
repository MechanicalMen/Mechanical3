using System;
using Mechanical3.Core;

namespace Mechanical3.Core
{
    //// NOTE: we use full names because this code may be copy-pasted.

    #region IDisposableObject

#pragma warning disable SA1649 // File name must match first type name
    /// <summary>
    /// A slightly extended <see cref="System.IDisposable"/> interface.
    /// </summary>
    public interface IDisposableObject : IDisposable
#pragma warning restore SA1649 // File name must match first type name
    {
        /// <summary>
        /// Gets a value indicating whether this object has been disposed of.
        /// </summary>
        /// <value><c>true</c> if this object has been disposed of; otherwise, <c>false</c>.</value>
        bool IsDisposed { get; }
    }

    #endregion

    /// <summary>
    /// A class that implements the disposable pattern.
    /// </summary>
    public abstract class DisposableObject : IDisposableObject
    {
        #region IDisposableObject

        private readonly object disposeLock = new object();
        private bool isDisposed = false;

        /// <summary>
        /// Gets a value indicating whether this object has been disposed of.
        /// </summary>
        /// <value><c>true</c> if this object has been disposed of; otherwise, <c>false</c>.</value>
        public bool IsDisposed
        {
            get { return this.isDisposed; }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DisposableObject"/> class.
        /// </summary>
        /// <remarks>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// object is reclaimed by garbage collection.
        /// </remarks>
        ~DisposableObject()
        {
            // call Dispose with false. Since we're in the destructor call,
            // the managed resources will be disposed of anyways.
            this.Dispose(false);
        }

        /// <summary>
        /// Performs tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // dispose of the managed and unmanaged resources
            this.Dispose(true);

            // tell the GC that Finalize no longer needs
            // to be run for this object.
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called before the object is disposed of. Inheritors must call base.OnDisposing to be properly disposed.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c>, the method was called by Dispose; otherwise by the destructor.</param>
        protected virtual void OnDisposing( bool disposing )
        {
        }

        /// <summary>
        /// Called when the object is being disposed of. Inheritors must call base.OnDispose to be properly disposed.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c>, release both managed and unmanaged resources; otherwise release only the unmanaged resources.</param>
        protected virtual void OnDispose( bool disposing )
        {
            if( disposing )
            {
                //// dispose-only (i.e. non-finalizable) logic
                //// (managed, disposable resources you own)

                /*
                if( resource.NotNullReference() )
                {
                    resource.Dispose();
                    resource = null;
                }
                */
            }

            //// shared cleanup logic
            //// (unmanaged resources)
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c>, release both managed and unmanaged resources; otherwise release only the unmanaged resources.</param>
        private void Dispose( bool disposing )
        {
            // don't lock unless we have to
            if( !this.isDisposed )
            {
                // not only do we want only one Dispose to run at a time,
                // we also want to halt other callers until Dispose finishes.
                lock( this.disposeLock )
                {
                    // necessary if there are multiple concurrent calls
                    if( !this.isDisposed )
                    {
                        this.OnDisposing(disposing);
                        this.OnDispose(disposing);
                        this.isDisposed = true; // if an exception interrupts the process, we may not have been properly disposed of! (and isDisposed correctly stores false).
                    }
                }
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Throws an exception, if this instance was already disposed.
        /// </summary>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        protected void ThrowIfDisposed(
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string member = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0 )
        {
            if( this.IsDisposed )
                throw new System.ObjectDisposedException(null).StoreFileLine(file, member, line);
        }

        #endregion

        /*
        public void DoSomethingWithResource()
        {
            this.ThrowIfDisposed();

            // use resource ...
        }
        */
    }

    /*
    /// <summary>
    /// Demonstrates how to derive from a disposable object.
    /// </summary>
    public class DisposableDerivedObject : DisposableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableDerivedObject"/> class.
        /// </summary>
        public DisposableDerivedObject()
            : base()
        {
            //// allocate more resources (in addition to those inherited)
        }

        #region IDisposableObject

        /// <summary>
        /// Called when the object is being disposed of. Inheritors must call base.OnDispose to be properly disposed.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c>, release both managed and unmanaged resources; otherwise release only the unmanaged resources.</param>
        protected override void OnDispose( bool disposing )
        {
            if( disposing )
            {
                //// dispose-only (i.e. non-finalizable) logic
                //// (managed, disposable resources you own)

                if( resource.NotNullReference() )
                {
                    resource.Dispose();
                    resource = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region Advanced IDisposableObject usage

        /// <summary>
        /// Called before the object is disposed of. Inheritors must call base.OnDisposing to be properly disposed.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c>, the method was called by Dispose; otherwise by the destructor.</param>
        protected override void OnDisposing( bool disposing )
        {
            //// before the Disposing event is raised

            base.OnDisposing(disposing);

            //// after the Disposing event was raised
        }

        #endregion
    }
    */
}
