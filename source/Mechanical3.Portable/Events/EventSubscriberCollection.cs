using System;
using System.Collections.Generic;
using System.Reflection;
using Mechanical3.Core;

namespace Mechanical3.Events
{
    /// <summary>
    /// Stores event subscriptions. Notifies subscribers of events.
    /// </summary>
    internal class EventSubscriberCollection
    {
#pragma warning disable SA1600 // Elements must be documented

        #region Private Fields

        private readonly object subscriberLock = new object();
        private readonly object invokeLock = new object();
        private readonly List<EventSubscriptionBase> subscribers = new List<EventSubscriptionBase>();

        #endregion

        #region Constructors

        internal EventSubscriberCollection()
        {
        }

        #endregion

        #region Private Methods

        private int IndexOf_NoLock( object handler, Type eventType )
        {
            EventSubscriptionBase subscr = null;
            try
            {
                object tmpStrongRef;
                for( int i = 0; i < this.subscribers.Count; )
                {
                    subscr = this.subscribers[i];
                    if( subscr.EventType == eventType ) //// NOTE: equality test, not assignability!
                    {
                        tmpStrongRef = subscr.AcquireStrongRef();

                        if( tmpStrongRef.NullReference() )
                        {
                            // weak reference was null: GC picked up the object
                            this.subscribers.RemoveAt(i);
                        }
                        else
                        {
                            if( object.ReferenceEquals(handler, tmpStrongRef) )
                                return i; // 'finally' will release the strong ref.
                        }

                        subscr.ReleaseStrongRef(); // NOTE: we always release the strong reference here, unlike with FindSubscribersOf_...
                    }

                    ++i;
                }
                return -1;
            }
            finally
            {
                // do not leave strong reference around, even if interrupted by an exception
                if( subscr.NotNullReference() )
                    subscr.ReleaseStrongRef();
            }
        }

        private List<EventSubscriptionBase> FindSubscribersOf_NoLock( Type eventType )
        {
            var eventSubscribers = new List<EventSubscriptionBase>();

            EventSubscriptionBase subscr = null;
            var eventTypeInfo = eventType.GetTypeInfo();
            try
            {
                object tmpStrongRef;
                for( int i = 0; i < this.subscribers.Count; )
                {
                    subscr = this.subscribers[i];

                    if( subscr.CanHandle(eventTypeInfo) )
                    {
                        tmpStrongRef = subscr.AcquireStrongRef();

                        if( tmpStrongRef.NotNullReference() )
                        {
                            eventSubscribers.Add(subscr); // keep strong reference
                            ++i;
                        }
                        else
                        {
                            this.subscribers.RemoveAt(i);
                        }
                    }
                    else
                    {
                        ++i;
                    }
                }

                subscr = null; // only run 'finally', if there was an exception
            }
            finally
            {
                // do not leave strong reference around, even if interrupted by an exception
                if( subscr.NotNullReference() )
                    subscr.ReleaseStrongRef();
            }

            return eventSubscribers;
        }

        #endregion

        #region Internal Methods

        internal void Add<T>( IEventHandler<T> handler )
            where T : EventBase
        {
            if( handler.NullReference() )
                throw new ArgumentNullException(nameof(handler)).StoreFileLine();

            lock ( this.subscriberLock )
            {
                if( this.IndexOf_NoLock(handler, typeof(T)) == -1 ) // do nothing if this event handler was already registered for (precisely) this event type
                    this.subscribers.Add(new EventSubscription<T>(handler));
            }
        }

        internal bool Remove<T>( IEventHandler<T> handler )
            where T : EventBase
        {
            if( handler.NullReference() )
                throw new ArgumentNullException(nameof(handler)).StoreFileLine();

            lock ( this.subscriberLock )
            {
                int index = this.IndexOf_NoLock(handler, typeof(T));
                if( index != -1 )
                {
                    this.subscribers.RemoveAt(index);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal void Clear()
        {
            lock ( this.subscriberLock )
            {
                foreach( var item in this.subscribers )
                    item.ReleaseStrongRef();

                this.subscribers.Clear();
            }
        }

        internal void InvokeHandlers( EnqueuedEvent enqueuedEvent, out Exception unhandledException )
        {
            // at any time, only a single set of handlers is collected, invoked, and released
            lock ( this.invokeLock )
            {
                // get applicable event subscribers
                List<EventSubscriptionBase> eventSubscribers;
                lock ( this.subscriberLock )
                    eventSubscribers = this.FindSubscribersOf_NoLock(enqueuedEvent.Event.GetType()); // acquires string ref.

                // handle event
                var unhandledExceptions = new List<Exception>();
                for( int i = 0; i < eventSubscribers.Count; ++i )
                {
                    try
                    {
                        eventSubscribers[i].Handle(enqueuedEvent.Event);
                    }
                    catch( Exception ex )
                    {
                        ex.StoreFileLine();
                        unhandledExceptions.Add(ex);
                    }
                    finally
                    {
                        eventSubscribers[i].ReleaseStrongRef();
                    }
                }

                // handle exceptions
                if( unhandledExceptions.Count == 0 )
                {
                    unhandledException = null;
                }
                else
                {
                    if( unhandledExceptions.Count == 1 )
                        unhandledException = unhandledExceptions[0];
                    else
                        unhandledException = new AggregateException("Multiple event handlers threw exceptions!", unhandledExceptions);

                    unhandledException.StoreFileLine();
                    unhandledException.Store(nameof(EventBase.EnqueueSource), enqueuedEvent.Event.EnqueueSource);
                }
            }
        }

        #endregion

#pragma warning restore SA1600 // Elements must be documented
    }
}
