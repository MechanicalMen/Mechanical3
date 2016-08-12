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
        #region Private Fields

        private readonly List<PropertyChangedSource> sources;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangedActions"/> class.
        /// </summary>
        public PropertyChangedActions()
            : base()
        {
            this.sources = new List<PropertyChangedSource>();
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

                if( this.sources.Count != 0 )
                    this.Clear();
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region Private Methods

        private int IndexOf_NotLocked( INotifyPropertyChanged source )
        {
            for( int i = 0; i < this.sources.Count; ++i )
            {
                if( object.ReferenceEquals(this.sources[i].Source, source) )
                    return i;
            }

            return -1;
        }

        private PropertyChangedSource AddOrGetSource_NotLocked( INotifyPropertyChanged source )
        {
            int index = this.IndexOf_NotLocked(source);
            if( index != -1 )
            {
                return this.sources[index];
            }
            else
            {
                var s = new PropertyChangedSource() { Source = source };
                this.sources.Add(s);
                return s;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Invokes the delegate, whenever the <paramref name="source"/> raises a PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="source">The object whose PropertyChanged event will fire.</param>
        /// <param name="propertyName">The name of the property to listen to.</param>
        /// <param name="action">The delegate to invoke, when the specified property changes, on the specified <paramref name="source"/>.</param>
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

            lock( this.sources )
            {
                var s = this.AddOrGetSource_NotLocked(source);

                s.Listeners.AddListener(
                    propertyName,
                    new PropertyChangedListenerAction(action, new FileLineInfo(file, member, line)));
            }
        }

        /// <summary>
        /// Invokes the delegate, if the last property in the chain has changed.
        /// </summary>
        /// <param name="source">The object, to start the chain from.</param>
        /// <param name="propertyChain">A chain of properties (e.g. source.P0.P1.P2).</param>
        /// <param name="action">The delegate to invoke, if the last property in the chain has changed.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public void Register(
            INotifyPropertyChanged source,
            string[] propertyChain,
            Action action,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.ThrowIfDisposed();

            //// TODO: throw if chain is null or sparse

            lock( this.sources )
            {
                var s = this.AddOrGetSource_NotLocked(source);

                var listeners = s.Listeners;
                for( int i = 0; i < propertyChain.Length; ++i )
                {
                    // create listener
                    IPropertyChangedListener l;
                    if( i == propertyChain.Length - 1 )
                    {
                        l = new PropertyChangedListenerAction(
                            action,
                            new FileLineInfo(file, member, line));
                    }
                    else
                    {
                        l = new PropertyChangedListenerChain();
                    }

                    // add listener for property name
                    listeners.AddListener(propertyChain[i], l);

                    // move to next link in the chain
                    listeners = (l as PropertyChangedListenerChain)?.Listeners;
                }
            }
        }

        /// <summary>
        /// Removes all PropertyChanged handlers registered for the specified <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The object to remove registered PropertyChanged listeners from.</param>
        public void RemoveSource( INotifyPropertyChanged source )
        {
            this.ThrowIfDisposed();

            lock( this.sources )
            {
                int index = this.IndexOf_NotLocked(source);
                if( index != -1 )
                {
                    var s = this.sources[index];
                    this.sources.RemoveAt(index);
                    s.Dispose();
                }
            }
        }

        /// <summary>
        /// Removes all PropertyChanged handlers.
        /// </summary>
        public void Clear()
        {
            lock( this.sources )
            {
                foreach( var item in this.sources )
                    item.Dispose();
                this.sources.Clear();
            }
        }

        #endregion
    }
}
