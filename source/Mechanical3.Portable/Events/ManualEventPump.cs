using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mechanical3.Core;
using Mechanical3.Misc;

namespace Mechanical3.Events
{
    /// <summary>
    /// A thread-safe <see cref="IEventQueue"/>, that transmits events only when instructed to.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names must not contain underscore", Justification = "In the rare case where I use C/C++ style constants, I want to highlight their nature, by using an unusual naming convention.")]
    public class ManualEventPump : IEventQueue
    {
        #region Private Fields

        private const int STATUS_OPEN = 0;
        private const int STATUS_CLOSING_ENQUEUED = 1;
        private const int STATUS_CLOSED_ENQUEUED = 2;
        private const int STATUS_CLOSED = 3;

        private readonly object eventsLock = new object();
        private readonly EventSubscriberCollection subscribers;
        private readonly List<EnqueuedEvent> events; // a FIFO queue
        private readonly ManualResetEventSlim eventsAvailableWaitHandle;
        private int status = STATUS_OPEN;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualEventPump"/> class.
        /// </summary>
        public ManualEventPump()
        {
            this.subscribers = new EventSubscriberCollection();
            this.events = new List<EnqueuedEvent>();
            this.eventsAvailableWaitHandle = new ManualResetEventSlim(initialState: false); // nonsignaled, blocks
        }

        #endregion

        #region Private Methods

        private bool CanEnqueue( EventBase evnt )
        {
            if( evnt is EventQueueCloseRequestEvent )
            {
                // no more close requests, once closing has already begun
                if( this.status >= STATUS_CLOSING_ENQUEUED )
                    return false;
            }
            else if( evnt is EventQueueClosingEvent )
            {
                if( Interlocked.CompareExchange(ref this.status, STATUS_CLOSING_ENQUEUED, comparand: STATUS_OPEN) == STATUS_OPEN )
                {
                    // No more close requests are enqueued after this (see above).
                    // Existing close events are removed now.
                    // Other events are still OK, while this is being handled (but not afterwards).
                    lock ( this.eventsLock )
                    {
                        // NOTE: we don't simply keep them and ignore them instead of letting them be handled
                        //       so that the 'events.Count' (and through it 'HasEvents') stays correct
                        for( int i = 0; i < this.events.Count; )
                        {
                            if( this.events[i].Event is EventQueueCloseRequestEvent )
                                this.events.RemoveAt(i);
                            else
                                ++i;
                        }
                    }
                }
                else
                {
                    // closing already in progress (or finished):
                    // instead of adding duplicate event, we skip it silently
                    return false;
                }
            }
            else if( evnt is EventQueueClosedEvent )
            {
                if( Interlocked.CompareExchange(ref this.status, STATUS_CLOSED_ENQUEUED, comparand: STATUS_CLOSING_ENQUEUED) != STATUS_CLOSING_ENQUEUED )
                    throw new Exception("Invalid internal state!").Store(nameof(this.status), this.status);
            }
            else
            {
                // all other events
                if( this.status >= STATUS_CLOSED_ENQUEUED )
                    throw new InvalidOperationException("No more events may be enqueued, after the closed event!").Store("eventType", evnt.GetType());
            }

            return true;
        }

        private void OnHandlingFinished( EventBase evnt )
        {
            if( evnt is EventQueueCloseRequestEvent )
            {
                if( ((EventQueueCloseRequestEvent)evnt).CanBeginClose )
                    this.BeginClose();
            }
            else if( evnt is EventQueueClosingEvent )
            {
                this.Enqueue(new EventQueueClosedEvent());
            }
            else if( evnt is EventQueueClosedEvent )
            {
                if( Interlocked.CompareExchange(ref this.status, STATUS_CLOSED, comparand: STATUS_CLOSED_ENQUEUED) != STATUS_CLOSED_ENQUEUED )
                    throw new Exception("Invalid internal state!").Store(nameof(this.status), this.status);

                // release resources
                this.subscribers.Clear();
                this.eventsAvailableWaitHandle.Set(); // signaled, does not block
            }
        }

        private void Enqueue_NoLock( EnqueuedEvent evnt )
        {
            this.events.Insert(0, evnt);
            this.eventsAvailableWaitHandle.Set(); // signaled, does not block
        }

        private EnqueuedEvent Dequeue_NoLock()
        {
            if( this.events.Count == 0 )
                return null;

            var index = this.events.Count - 1;
            var result = this.events[index];

            this.events.RemoveAt(index);
            if( this.events.Count == 0 )
                this.eventsAvailableWaitHandle.Reset(); // nonsignaled, blocks

            return result;
        }

        #endregion

        #region IEventQueue

        /// <summary>
        /// Stores a weak-reference to <paramref name="handler"/>, so that it may be invoked
        /// when events of type <typeparamref name="T"/> (or inheriting it) are enqueued.
        /// Does nothing, if a subscription already exists.
        /// </summary>
        /// <typeparam name="T">Events of this type, or inheriting it, are handled by the specified <see cref="IEventHandler{T}"/>.</typeparam>
        /// <param name="handler">The <see cref="IEventHandler{T}"/> to store a weak-reference to.</param>
        public void Subscribe<T>( IEventHandler<T> handler )
            where T : EventBase
        {
            // NOTE: once we start closing, we do not accept new subscribers.
            //       This way, you can not get a Closed event, without a Closing event.
            if( this.status >= STATUS_CLOSING_ENQUEUED )
                throw new InvalidOperationException("Event queue already closed (or closing)!").Store(nameof(this.status), this.status);

            this.subscribers.Add(handler);
        }

        /// <summary>
        /// Removes the event subscription for the specified <see cref="IEventHandler{T}"/>.
        /// Any other subscriptions of the same reference, but with different event types, are not affected.
        /// </summary>
        /// <typeparam name="T">Events of this type, or inheriting it, are handled by the specified <see cref="IEventHandler{T}"/>.</typeparam>
        /// <param name="handler">The <see cref="IEventHandler{T}"/> to remove the subscription of type <typeparamref name="T"/> events for.</param>
        /// <returns><c>true</c> if the subscription was found and removed; otherwise, <c>false</c>.</returns>
        public bool Unsubscribe<T>( IEventHandler<T> handler )
            where T : EventBase
        {
            return this.subscribers.Remove(handler);
        }

        /// <summary>
        /// Enqueues an event to be handled by subscribers of this queue.
        /// This method returns immediately.
        /// Any exceptions thrown by event handlers, silently generate an <see cref="UnhandledExceptionEvent"/>.
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public void Enqueue(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( evnt.NullReference() )
                throw new ArgumentNullException(nameof(evnt)).StoreFileLine();

            if( this.CanEnqueue(evnt) )
            {
                evnt.EnqueueSource = new FileLineInfo(file, member, line);
                var e = new EnqueuedEvent(evnt, createTask: false);

                lock ( this.eventsLock )
                    this.Enqueue_NoLock(e);
            }
        }

        /// <summary>
        /// Enqueues an event to be handled by subscribers of this queue.
        /// This method blocks until all event handlers have finished.
        /// Any exceptions thrown by event handlers, will be thrown by this method.
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public void EnqueueAndWait(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( evnt.NullReference() )
                throw new ArgumentNullException(nameof(evnt)).StoreFileLine();

            if( this.CanEnqueue(evnt) )
            {
                evnt.EnqueueSource = new FileLineInfo(file, member, line);
                var e = new EnqueuedEvent(evnt, createTask: true);

                lock ( this.eventsLock )
                    this.Enqueue_NoLock(e);

                e.Task.GetAwaiter().GetResult(); // should work like "await task", but in a blocking call. Won't wrap exceptions in AggregateException.
            }
        }

        /// <summary>
        /// Enqueues an event to be handled by subscribers of this queue.
        /// This method returns immediately with a <see cref="Task"/>,
        /// which completes once all event handlers have finished.
        /// Any exceptions thrown by event handlers may be accessed through this <see cref="Task"/>.
        /// </summary>
        /// <param name="evnt">The event to enqueue.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>An object representing the asynchronous operation.</returns>
        public Task EnqueueAndWaitAsync(
            EventBase evnt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( evnt.NullReference() )
                throw new ArgumentNullException(nameof(evnt)).StoreFileLine();

            if( this.CanEnqueue(evnt) )
            {
                evnt.EnqueueSource = new FileLineInfo(file, member, line);
                var e = new EnqueuedEvent(evnt, createTask: true);

                lock ( this.eventsLock )
                    this.Enqueue_NoLock(e);

                return e.Task;
            }
            else
            {
                return Task.FromResult<object>(null);
            }
        }

        /// <summary>
        /// Enqueues an <see cref="EventQueueCloseRequestEvent"/>.
        /// If all event handlers agree that the queue can be closed,
        /// <see cref="BeginClose"/> is automatically invoked.
        /// </summary>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public void RequestClose(
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.Enqueue(new EventQueueCloseRequestEvent(), file, member, line);
        }

        /// <summary>
        /// Enqueues an <see cref="EventQueueClosingEvent"/>.
        /// Once it is handled, no more events may be enqueued.
        /// The last event handled by the queue will be an <see cref="EventQueueClosedEvent"/>.
        /// </summary>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public void BeginClose(
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.Enqueue(new EventQueueClosingEvent(), file, member, line);
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets a value indicating whether there are currently events enqueued.
        /// </summary>
        /// <value><c>true</c> if there are events enqueued; otherwise, <c>false</c>.</value>
        public bool HasEvents
        {
            get
            {
                lock ( this.eventsLock )
                    return this.events.Count != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether all events have been processed.
        /// </summary>
        /// <value><c>true</c> if all events have been processed; otherwise, <c>false</c>.</value>
        public bool IsClosed
        {
            get { return this.status == STATUS_CLOSED; }
        }

        /// <summary>
        /// Blocks the calling thread, until an event is enqueued, or this event queue is closed.
        /// </summary>
        public void WaitForEvent()
        {
            this.eventsAvailableWaitHandle.Wait();
        }

        /// <summary>
        /// Has the subscribers handle a single event.
        /// Subscribers are invoked from the current thread.
        /// Blocks until all handlers finish.
        /// Exceptions are either handled by event tasks, or an <see cref="UnhandledExceptionEvent"/>.
        /// Does nothing if there are no events (or subscribers).
        /// </summary>
        public void HandleOne()
        {
            // get event
            EnqueuedEvent e;
            lock ( this.eventsLock )
                e = this.Dequeue_NoLock();

            if( e.NullReference() )
                return;

            // invoke handlers
            Exception unhandledException;
            this.subscribers.InvokeHandlers(e, out unhandledException);

            // update internal state
            this.OnHandlingFinished(e.Event);

            // allow those waiting to continue; manage exceptions thrown
            bool exceptionHandled;
            e.SetHandled(unhandledException, out exceptionHandled);
            if( unhandledException.NotNullReference()
             && !exceptionHandled )
            {
                // enqueue & forget
                var exceptionEvent = new UnhandledExceptionEvent(unhandledException);
                try
                {
                    this.Enqueue(exceptionEvent);
                }
                catch( Exception ex )
                {
                    ex = new AggregateException("Failed to enqueue exception!", ex, unhandledException);
                    System.Diagnostics.Debugger.Break();
                    //// TODO: invoke the default unhandled exception processing method
                }
            }
        }

        /// <summary>
        /// Has the subscribers handle all enqueued events.
        /// </summary>
        public void HandleAll()
        {
            while( this.HasEvents )
                this.HandleOne();
        }

        #endregion
    }
}
