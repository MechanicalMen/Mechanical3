using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mechanical3.Core;
using Mechanical3.Misc;

namespace Mechanical3.MVVM
{
    /// <summary>
    /// Tracks handlers for PropertyChanged notifications.
    /// </summary>
    public class PropertyChangedActions : DisposableObject
    {
        #region ChangeHandler

        private struct ChangeHandler
        {
            internal Action Action { get; }
            internal FileLineInfo RegisteredFrom { get; }

            internal ChangeHandler( Action action, FileLineInfo registeredFrom )
            {
                if( action.NullReference() )
                    throw new ArgumentNullException(nameof(action)).StoreFileLine();

                this.Action = action;
                this.RegisteredFrom = registeredFrom;
            }
        }

        #endregion

        #region ChangeListeners

        private class ChangeListeners : DisposableObject
        {
            private readonly Dictionary<string, List<ChangeHandler>> handlers;
            private INotifyPropertyChanged source;

            internal ChangeListeners( INotifyPropertyChanged src )
            {
                if( src.NullReference() )
                    throw new ArgumentNullException(nameof(src)).StoreFileLine();

                this.handlers = new Dictionary<string, List<ChangeHandler>>(StringComparer.Ordinal);
                this.source = src;
                this.Source.PropertyChanged += this.OnPropertyChanged;
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

                    if( this.source.NotNullReference() )
                    {
                        this.source.PropertyChanged -= this.OnPropertyChanged;
                        this.source = null;

                        lock( this.handlers )
                        {
                            foreach( var h in this.handlers.Values )
                                h.Clear();
                            this.handlers.Clear();
                        }
                    }
                }

                //// shared cleanup logic
                //// (unmanaged resources)


                base.OnDispose(disposing);
            }

            #endregion

            private void OnPropertyChanged( object sender, PropertyChangedEventArgs e )
            {
                if( this.IsDisposed )
                    return;

                lock( this.handlers )
                {
                    List<ChangeHandler> propertyHandlers;
                    if( this.handlers.TryGetValue(e.PropertyName, out propertyHandlers) )
                    {
                        foreach( var h in propertyHandlers )
                        {
                            try
                            {
                                h.Action();
                            }
                            catch( Exception ex )
                            {
                                ex.StoreFileLine(h.RegisteredFrom);
                                MechanicalApp.EnqueueException(ex);
                                break;
                            }
                        }
                    }
                }
            }

            internal INotifyPropertyChanged Source
            {
                get
                {
                    this.ThrowIfDisposed();

                    return this.source;
                }
            }

            internal void Register( string propertyName, Action action, FileLineInfo registeredFrom )
            {
                this.ThrowIfDisposed();

                if( propertyName.NullOrLengthy() )
                    throw new ArgumentException().Store(nameof(propertyName), propertyName);

                if( action.NullReference() )
                    throw new ArgumentNullException(nameof(action)).StoreFileLine();

                lock( this.handlers )
                {
                    List<ChangeHandler> propertyHandlers;
                    if( !this.handlers.TryGetValue(propertyName, out propertyHandlers) )
                    {
                        propertyHandlers = new List<ChangeHandler>();
                        this.handlers.Add(propertyName, propertyHandlers);
                    }

                    propertyHandlers.Add(new ChangeHandler(action, registeredFrom));
                }
            }
        }

        #endregion

        #region Private Fields

        private readonly List<ChangeListeners> listeners;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangedActions"/> class.
        /// </summary>
        public PropertyChangedActions()
            : base()
        {
            this.listeners = new List<ChangeListeners>();
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

                lock( this.listeners )
                {
                    foreach( var l in this.listeners )
                        l.Dispose();

                    this.listeners.Clear();
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region Private Methods

        private int IndexOf_NotLocked( INotifyPropertyChanged source )
        {
            for( int i = 0; i < this.listeners.Count; ++i )
            {
                if( object.ReferenceEquals(this.listeners[i].Source, source) )
                    return i;
            }
            return -1;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a new PropertyChanged listener for the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The object whose PropertyChanged event will fire.</param>
        /// <param name="propertyName">The name of the property to listen to.</param>
        /// <param name="action">The delegate to invoke, when the specified property changes.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public void Register(
            INotifyPropertyChanged source,
            string propertyName,
            Action action,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.ThrowIfDisposed();

            lock( this.listeners )
            {
                ChangeListeners l;
                int index = this.IndexOf_NotLocked(source);
                if( index != -1 )
                {
                    l = this.listeners[index];
                }
                else
                {
                    l = new ChangeListeners(source);
                    this.listeners.Add(l);
                }

                l.Register(propertyName, action, new FileLineInfo(file, member, line));
            }
        }

        /// <summary>
        /// Removes all PropertyChanged handlers registered for the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The object to remove registered PropertyChanged listeners from.</param>
        public void RemoveSource( INotifyPropertyChanged source )
        {
            this.ThrowIfDisposed();

            lock( this.listeners )
            {
                int index = this.IndexOf_NotLocked(source);
                if( index != -1 )
                {
                    var listener = this.listeners[index];
                    this.listeners.RemoveAt(index);
                    listener.Dispose();
                }
            }
        }

        #endregion
    }
}
