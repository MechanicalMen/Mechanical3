using System;
using System.Threading;
using Mechanical3.Core;
using Mechanical3.Events;
using Mechanical3.MVVM;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Implements an <see cref="IUIThreadHandler"/> using a <see cref="ManualEventPump"/>.
    /// This allows console application to run on a single thread, BUT it will
    /// deadlock, if an event handler starts a blocking <see cref="UI"/> call (or vice versa).
    /// The UI thread "stops" when the event queue is closed.
    /// </summary>
    public class ConsoleEventQueueUIHandler : DisposableObject
    {
        #region ActionEvent

        private class ActionEvent : EventBase
        {
            internal ActionEvent( Action action )
                : base()
            {
                if( action.NullReference() )
                    throw new ArgumentNullException(nameof(action)).StoreFileLine();

                this.Action = action;
            }

            public Action Action;
        }

        #endregion

        #region EventQueueUIThreadHandler

        private class EventQueueUIThreadHandler : DisposableObject,
                                                  IUIThreadHandler,
                                                  IEventHandler<ActionEvent>,
                                                  IEventHandler<EventQueueClosedEvent>
        {
            #region Private Fields

            private readonly int mainThreadID;
            private IEventQueue eventQueue;

            #endregion

            #region Constructor

            internal EventQueueUIThreadHandler( int mainThreadID, IEventQueue queue )
            {
                if( queue.NullReference() )
                    throw new ArgumentNullException(nameof(queue)).StoreFileLine();

                this.mainThreadID = mainThreadID;
                this.eventQueue = queue;

                this.eventQueue.Subscribe<ActionEvent>(this);
                this.eventQueue.Subscribe<EventQueueClosedEvent>(this);
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

                    if( this.eventQueue.NotNullReference() )
                    {
                        this.eventQueue.Unsubscribe<ActionEvent>(this);
                        this.eventQueue.Unsubscribe<EventQueueClosedEvent>(this);
                        this.eventQueue = null;
                    }
                }

                //// shared cleanup logic
                //// (unmanaged resources)


                base.OnDispose(disposing);
            }

            #endregion

            #region IUIThreadHandler

            /// <summary>
            /// Determines whether the calling code is running on the UI thread.
            /// </summary>
            /// <returns><c>true</c> if the calling code is running on the UI thread; otherwise, <c>false</c>.</returns>
            public bool IsOnUIThread()
            {
                this.ThrowIfDisposed();

                // NOTE: Thread ID is unique per process during a thread's lifecycle. After the thread terminates, its number can be reused. (see: http://stackoverflow.com/a/2221963)
                //       But since the main thread won't stop while the process runs, this is just fine.
                return this.mainThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId;
            }

            /// <summary>
            /// Executes the specified <see cref="Action"/> synchronously on the UI thread.
            /// </summary>
            /// <param name="action">The delegate to invoke.</param>
            public void Invoke( Action action )
            {
                this.ThrowIfDisposed();

                this.eventQueue.EnqueueAndWait(new ActionEvent(action));
            }

            /// <summary>
            /// Executes the specified <see cref="Action"/> asynchronously on the UI thread.
            /// </summary>
            /// <param name="action">The delegate to invoke.</param>
            public void BeginInvoke( Action action )
            {
                this.ThrowIfDisposed();

                this.eventQueue.Enqueue(new ActionEvent(action));
            }

            #endregion

            #region IEventHandler

            /// <summary>
            /// Handles the specified event.
            /// </summary>
            /// <param name="evnt">The event to handle.</param>
            public void Handle( ActionEvent evnt )
            {
                this.ThrowIfDisposed();

                // this is invoked by the event queue, which should run from the main thread
                evnt.Action();
            }

            /// <summary>
            /// Handles the specified event.
            /// </summary>
            /// <param name="evnt">The event to handle.</param>
            public void Handle( EventQueueClosedEvent evnt )
            {
                this.Dispose();
            }

            #endregion
        }

        #endregion

        #region Private Fields

        private readonly int mainThreadID;
        private ManualEventPump eventPump;
        private EventQueueUIThreadHandler uiThreadHandler;

        #endregion

        #region Constructors

        private ConsoleEventQueueUIHandler( int mainThreadID )
        {
            this.mainThreadID = mainThreadID;
            this.eventPump = new ManualEventPump();
            this.uiThreadHandler = new EventQueueUIThreadHandler(mainThreadID, this.eventPump);
        }

        /// <summary>
        /// Creates a new <see cref="ConsoleEventQueueUIHandler"/> instance.
        /// </summary>
        /// <param name="mainThread">The main thread; or <c>null</c> to use <see cref="Thread.CurrentThread"/>.</param>
        /// <returns>A new <see cref="ConsoleEventQueueUIHandler"/> instance.</returns>
        public static ConsoleEventQueueUIHandler FromMainThread( Thread mainThread = null )
        {
            if( mainThread.NullReference() )
                mainThread = Thread.CurrentThread;

            return new ConsoleEventQueueUIHandler(mainThread.ManagedThreadId);
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

                if( this.eventPump.NotNullReference() )
                {
                    if( !this.eventPump.IsClosed )
                    {
                        if( this.mainThreadID != Thread.CurrentThread.ManagedThreadId )
                            throw new InvalidOperationException("Either the event queue must be closed before dispose, or dispose must be called from the main thread!").StoreFileLine();

                        this.eventPump.BeginClose();
                        this.eventPump.HandleAll();
                    }
                    this.eventPump = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)
            this.uiThreadHandler = null; // disposed by EventQueueClosedEvent

            base.OnDispose(disposing);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the <see cref="ManualEventPump"/> to use.
        /// If it is not invoked to handle events, then <see cref="UI"/> calls will not be invoked either.
        /// Dispose will close it automatically, unless that already happened.
        /// </summary>
        public ManualEventPump EventPump
        {
            get
            {
                this.ThrowIfDisposed();

                return this.eventPump;
            }
        }

        /// <summary>
        /// Gets the <see cref="IUIThreadHandler"/>, that sends calls to the <see cref="EventPump"/>.
        /// </summary>
        /// <value>The <see cref="IUIThreadHandler"/> implemented using the <see cref="EventPump"/>.</value>
        public IUIThreadHandler UIThreadHandler
        {
            get
            {
                this.ThrowIfDisposed();

                return this.uiThreadHandler;
            }
        }

        #endregion
    }
}
