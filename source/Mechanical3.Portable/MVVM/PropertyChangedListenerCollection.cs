using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Mechanical3.Core;

namespace Mechanical3.MVVM
{
#pragma warning disable SA1600 // ElementsMustBeDocumented
    internal class PropertyChangedListenerCollection : DisposableObject
    {
        #region Private Fields

        private readonly Dictionary<string, List<IPropertyChangedListener>> listeners;

        #endregion

        #region Constructors

        internal protected PropertyChangedListenerCollection()
            : base()
        {
            this.listeners = new Dictionary<string, List<IPropertyChangedListener>>(StringComparer.Ordinal);
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

                this.RemoveAll();
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region Members

        internal void AddListener( string propertyName, IPropertyChangedListener listener )
        {
            this.ThrowIfDisposed();

            if( propertyName.NullOrEmpty() )
                throw new ArgumentException().Store(nameof(propertyName), propertyName);

            if( listener.NullReference() )
                throw new ArgumentNullException(nameof(listener)).StoreFileLine();

            lock( this.listeners )
            {
                List<IPropertyChangedListener> propertyListeners;
                if( !this.listeners.TryGetValue(propertyName, out propertyListeners) )
                {
                    propertyListeners = new List<IPropertyChangedListener>();
                    this.listeners.Add(propertyName, propertyListeners);
                }

                propertyListeners.Add(listener); // NOTE: a listener may be added multiple times!
            }
        }

        internal void RemoveListener( IPropertyChangedListener listener )
        {
            this.ThrowIfDisposed();

            if( listener.NullReference() )
                throw new ArgumentNullException(nameof(listener)).StoreFileLine();

            bool removed = false;
            lock( this.listeners )
            {
                foreach( var list in this.listeners.Values )
                {
                    for( int i = 0; i < list.Count; )
                    {
                        if( object.ReferenceEquals(list[i], listener) )
                        {
                            list.RemoveAt(i);
                            removed = true;
                        }
                        else
                        {
                            ++i;
                        }
                    }
                }
            }

            if( removed )
            {
                // only dispose once
                var asDisposable = listener as IDisposable;
                if( asDisposable.NotNullReference() )
                    asDisposable.Dispose();
            }
        }

        internal void RemoveAll()
        {
            this.ThrowIfDisposed();

            lock( this.listeners )
            {
                while( this.listeners.Count != 0 )
                {
                    var propertyName = this.listeners.Keys.First();

                    var list = this.listeners[propertyName];
                    if( list.Count != 0 )
                    {
                        this.RemoveListener(list[0]); // removes listener from ALL lists, and disposes it at most once
                    }
                    else
                    {
                        this.listeners.Remove(propertyName);
                    }
                }
            }
        }

        private static void InvokeListeners( List<IPropertyChangedListener> listeners, INotifyPropertyChanged source, string propertyName, ref List<Exception> exceptions )
        {
            foreach( var l in listeners )
            {
                try
                {
                    l.OnPropertyChanged(source, propertyName);
                }
                catch( Exception ex )
                {
                    if( exceptions.NullReference() )
                        exceptions = new List<Exception>();

                    exceptions.Add(ex.Store(nameof(propertyName), propertyName));
                }
            }
        }

        internal void NotifyPropertyChanged( INotifyPropertyChanged source, string propertyName )
        {
            this.ThrowIfDisposed();

            List<Exception> exceptions = null;
            lock( this.listeners )
            {
                List<IPropertyChangedListener> list;

                if( this.listeners.TryGetValue(propertyName, out list) )
                    InvokeListeners(list, source, propertyName, ref exceptions);
            }

            if( exceptions.NotNullReference() )
                throw new AggregateException(exceptions).Store("sourceType", source?.GetType()); // property name already added to exceptions
        }

        internal void NotifyAllPropertiesChanged( INotifyPropertyChanged source )
        {
            this.ThrowIfDisposed();

            List<Exception> exceptions = null;
            lock( this.listeners )
            {
                foreach( var pair in this.listeners )
                    InvokeListeners(pair.Value, source, pair.Key, ref exceptions);
            }

            if( exceptions.NotNullReference() )
                throw new AggregateException(exceptions).Store("sourceType", source?.GetType());
        }

        #endregion
    }
#pragma warning restore SA1600 // ElementsMustBeDocumented
}
