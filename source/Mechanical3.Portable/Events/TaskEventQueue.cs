using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mechanical3.Core;

namespace Mechanical3.Events
{
    /// <summary>
    /// Executes event handlers from a (single) <see cref="Task"/>.
    /// </summary>
    public class TaskEventQueue : IEventQueue
    {
        #region Private Fields

        private readonly Task task;
        private readonly ManualEventPump eventPump;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskEventQueue"/> class.
        /// </summary>
        /// <param name="scheduler">The <see cref="TaskScheduler"/> used to start the underlying <see cref="Task"/>; or <c>null</c> for <see cref="TaskScheduler.Default"/>.</param>
        public TaskEventQueue( TaskScheduler scheduler = null )
        {
            if( scheduler.NullReference() )
                scheduler = TaskScheduler.Default;

            this.eventPump = new ManualEventPump();
            this.task = Task.Factory.StartNew(
                this.TaskBody,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                scheduler);
        }

        #endregion

        #region Private Methods

        private void TaskBody()
        {
            while( true )
            {
                this.eventPump.WaitForEvent();
                if( this.eventPump.IsClosed )
                    break;

                this.eventPump.HandleAll();
            }
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
            this.eventPump.Subscribe<T>(handler);
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
            return this.eventPump.Unsubscribe<T>(handler);
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
            this.eventPump.Enqueue(evnt, file, member, line);
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
            this.EnqueueAndWait(evnt, file, member, line);
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
            return this.EnqueueAndWaitAsync(evnt, file, member, line);
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
            this.eventPump.RequestClose();
        }

        /// <summary>
        /// Enqueues an <see cref="EventQueueClosingEvent"/>.
        /// Once it is handled, no more events may be enqueued.
        /// The last event handled by the queue will be an <see cref="EventQueueClosedEvent"/>.
        /// </summary>
        /// <param name="timeLimit">Indicates to handlers that closing the application must happen as soon as possible.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public void BeginClose(
            bool timeLimit = false,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.eventPump.BeginClose(timeLimit, file, member, line);
        }

        /// <summary>
        /// Blocks the calling thread, until <see cref="EventQueueClosedEvent"/> has finished handling
        /// and this instance has released it's resources.
        /// </summary>
        public void WaitForClosed()
        {
            this.eventPump.WaitForClosed();
        }

        #endregion
    }
}
