using System;
using System.Reflection;
using Mechanical3.Core;

namespace Mechanical3.Events
{
    /// <summary>
    /// Holds a weak-reference to a generic <see cref="IEventHandler{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of events the <see cref="IEventHandler{T}"/> handles.</typeparam>
    internal sealed class EventSubscription<T> : EventSubscriptionBase
        where T : EventBase
    {
#pragma warning disable SA1600 // Elements must be documented

        #region Private Fields

        private readonly WeakReference<IEventHandler<T>> weakRef;
        private readonly TypeInfo handlerEventTypeInfo;
        private IEventHandler<T> temporaryStrongRef = null;

        #endregion

        #region Constructor

        internal EventSubscription( IEventHandler<T> handler )
        {
            this.weakRef = new WeakReference<IEventHandler<T>>(handler);
            this.handlerEventTypeInfo = typeof(T).GetTypeInfo();
        }

        #endregion

        #region EventSubscriptionBase

        internal override Type EventType
        {
            get { return typeof(T); }
        }

        internal override object AcquireStrongRef()
        {
            if( this.temporaryStrongRef.NotNullReference()
             || this.weakRef.TryGetTarget(out this.temporaryStrongRef) )
                return this.temporaryStrongRef;
            else
                return null;
        }

        internal override void ReleaseStrongRef()
        {
            this.temporaryStrongRef = null;
        }

        internal override void Handle( EventBase evnt )
        {
            if( this.temporaryStrongRef.NotNullReference() )
                this.temporaryStrongRef.Handle((T)evnt);
        }

        internal override bool CanHandle( TypeInfo eventType )
        {
            return this.handlerEventTypeInfo.IsAssignableFrom(eventType);
        }

        #endregion

#pragma warning restore SA1600 // Elements must be documented
    }
}
