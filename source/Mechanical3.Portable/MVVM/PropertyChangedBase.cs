using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mechanical3.Core;

namespace Mechanical3.MVVM
{
    /// <summary>
    /// Implements <see cref="INotifyPropertyChanged"/>.
    /// Raises events on the UI thread.
    /// </summary>
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        #region Disposable

        /// <summary>
        /// Implements <see cref="INotifyPropertyChanged"/> (and <see cref="IDisposableObject"/>).
        /// Raises events on the UI thread.
        /// </summary>
        public class Disposable : DisposableObject, INotifyPropertyChanged
        {
            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="Disposable"/> class.
            /// </summary>
            public Disposable()
            {
            }

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
                }

                //// shared cleanup logic
                //// (unmanaged resources)
                this.PropertyChanged = null;

                base.OnDispose(disposing);
            }

            #endregion

            #region INotifyPropertyChanged

            /// <summary>
            /// Occurs when a property value changes.
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Protected Methods

            /// <summary>
            /// Raises the <see cref="PropertyChanged"/> event synchronously on the UI thread.
            /// </summary>
            /// <param name="e">Specifies the property that changed.</param>
            protected void RaisePropertyChanged( PropertyChangedEventArgs e )
            {
                if( e.NullReference() )
                    throw new ArgumentNullException().StoreFileLine();

                UI.Invoke(() =>
                {
                    var handler = this.PropertyChanged;
                    if( handler.NotNullReference() )
                    {
                        try
                        {
                            handler(this, e);
                        }
                        catch( Exception ex )
                        {
                            MechanicalApp.EnqueueException(ex);
                        }
                    }
                });
            }

            /// <summary>
            /// Raises the <see cref="PropertyChanged"/> event asynchronously on the UI thread.
            /// </summary>
            /// <param name="e">Specifies the property that changed.</param>
            /// <returns>The <see cref="Task"/> representing the operation.</returns>
            protected Task RaisePropertyChangedAsync( PropertyChangedEventArgs e )
            {
                if( e.NullReference() )
                    throw new ArgumentNullException().StoreFileLine();

                return UI.InvokeAsync(() =>
                {
                    var handler = this.PropertyChanged;
                    if( handler.NotNullReference() )
                    {
                        try
                        {
                            handler(this, e);
                        }
                        catch( Exception ex )
                        {
                            MechanicalApp.EnqueueException(ex);
                        }
                    }
                });
            }

            /// <summary>
            /// Raises the <see cref="PropertyChanged"/> event synchronously on the UI thread.
            /// </summary>
            /// <param name="property">The name of the property that changed.</param>
            protected void RaisePropertyChanged( [CallerMemberName] string property = null )
            {
                if( property.NullReference() )
                    throw new ArgumentNullException().StoreFileLine();

                this.RaisePropertyChanged(new PropertyChangedEventArgs(property));
            }

            /// <summary>
            /// Raises the <see cref="PropertyChanged"/> event asynchronously on the UI thread.
            /// </summary>
            /// <param name="property">The name of the property that changed.</param>
            /// <returns>The <see cref="Task"/> representing the operation.</returns>
            protected Task RaisePropertyChangedAsync( [CallerMemberName] string property = null )
            {
                if( property.NullReference() )
                    throw new ArgumentNullException().StoreFileLine();

                return this.RaisePropertyChangedAsync(new PropertyChangedEventArgs(property));
            }

            #endregion
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangedBase"/> class.
        /// </summary>
        public PropertyChangedBase()
        {
        }

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Protected Methods

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event synchronously on the UI thread.
        /// </summary>
        /// <param name="e">Specifies the property that changed.</param>
        protected void RaisePropertyChanged( PropertyChangedEventArgs e )
        {
            if( e.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            UI.Invoke(() =>
            {
                var handler = this.PropertyChanged;
                if( handler.NotNullReference() )
                {
                    try
                    {
                        handler(this, e);
                    }
                    catch( Exception ex )
                    {
                        MechanicalApp.EnqueueException(ex);
                    }
                }
            });
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event asynchronously on the UI thread.
        /// </summary>
        /// <param name="e">Specifies the property that changed.</param>
        /// <returns>The <see cref="Task"/> representing the operation.</returns>
        protected Task RaisePropertyChangedAsync( PropertyChangedEventArgs e )
        {
            if( e.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            return UI.InvokeAsync(() =>
            {
                var handler = this.PropertyChanged;
                if( handler.NotNullReference() )
                {
                    try
                    {
                        handler(this, e);
                    }
                    catch( Exception ex )
                    {
                        MechanicalApp.EnqueueException(ex);
                    }
                }
            });
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event synchronously on the UI thread.
        /// </summary>
        /// <param name="property">The name of the property that changed.</param>
        protected void RaisePropertyChanged( [CallerMemberName] string property = null )
        {
            if( property.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            this.RaisePropertyChanged(new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event asynchronously on the UI thread.
        /// </summary>
        /// <param name="property">The name of the property that changed.</param>
        /// <returns>The <see cref="Task"/> representing the operation.</returns>
        protected Task RaisePropertyChangedAsync( [CallerMemberName] string property = null )
        {
            if( property.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            return this.RaisePropertyChangedAsync(new PropertyChangedEventArgs(property));
        }

        #endregion
    }
}
