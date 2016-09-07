using System.ComponentModel;
using Mechanical3.Core;

namespace Mechanical3.MVVM
{
#pragma warning disable SA1600 // ElementsMustBeDocumented
    internal class PropertyChangedSource : DisposableObject
    {
        #region Private Fields

        private readonly object syncLock = new object();
        private INotifyPropertyChanged currentSource;
        private PropertyChangedListenerCollection listeners = new PropertyChangedListenerCollection();

        #endregion

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

                this.RegisterPropertyChanged(add: false);

                if( this.listeners.NotNullReference() )
                {
                    this.listeners.Dispose();
                    this.listeners = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region Private Methods

        private void OnPropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            if( this.IsDisposed )
                return;

            lock( this.syncLock )
            {
                if( object.ReferenceEquals(this.currentSource, sender) )
                    this.listeners.NotifyPropertyChanged(this.currentSource, e.PropertyName);
            }
        }

        private void RegisterPropertyChanged( bool add )
        {
            if( this.currentSource.NotNullReference() )
            {
                if( add )
                    this.currentSource.PropertyChanged += this.OnPropertyChanged;
                else
                    this.currentSource.PropertyChanged -= this.OnPropertyChanged;
            }
        }

        #endregion

        #region Internal Members

        internal INotifyPropertyChanged Source
        {
            get
            {
                this.ThrowIfDisposed();

                lock( this.syncLock )
                    return this.currentSource;
            }
            set
            {
                this.ThrowIfDisposed();

                lock( this.syncLock )
                {
                    if( !object.ReferenceEquals(this.currentSource, value) )
                    {
                        // remove handler
                        this.RegisterPropertyChanged(add: false);

                        // change source
                        this.currentSource = value;

                        // register handler
                        this.RegisterPropertyChanged(add: true);

                        // notify listeners of change
                        this.Listeners.NotifyAllPropertiesChanged(this.currentSource);
                    }
                }
            }
        }

        internal PropertyChangedListenerCollection Listeners
        {
            get
            {
                this.ThrowIfDisposed();

                return this.listeners;
            }
        }

        #endregion
    }
#pragma warning restore SA1600 // ElementsMustBeDocumented
}
