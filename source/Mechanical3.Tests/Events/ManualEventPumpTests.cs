using System;
using System.Threading.Tasks;
using Mechanical3.Core;
using Mechanical3.Events;
using NUnit.Framework;

namespace Mechanical3.Tests.Events
{
    [TestFixture(Category = "Events")]
    public static class ManualEventPumpTests
    {
        #region Helpers

        internal static readonly TimeSpan SmallSleepTime = TimeSpan.FromMilliseconds(100);

        internal class TestEvent<T> : EventBase
        {
            public T Value { get; private set; } = default(T);
        }

        internal class EventRecorder<T> : IEventHandler<T>
            where T : EventBase
        {
            public T LastEvent { get; private set; } = null;

            public void Handle( T evnt )
            {
                this.LastEvent = evnt;
            }
        }

        private class DelegateHandler<T> : IEventHandler<T>
            where T : EventBase
        {
            private readonly Action<T> handler;

            public DelegateHandler( Action<T> handler )
            {
                if( handler.NullReference() )
                    throw new ArgumentNullException(nameof(handler)).StoreFileLine();

                this.handler = handler;
            }

            public void Handle( T evnt )
            {
                this.handler(evnt);
            }
        }

        private static void Using( Action<ManualEventPump> action )
        {
            Assert.NotNull(action);

            var pump = new ManualEventPump();
            Assert.False(pump.IsClosed);
            try
            {
                action(pump);
            }
            finally
            {
                if( !pump.IsClosed )
                {
                    pump.BeginClose();
                    pump.HandleAll();
                    Assert.True(pump.IsClosed);
                }
            }
        }

        #endregion

        [Test]
        public static void EnqueueTests()
        {
            // non-blocking, no return value
            Using(pump =>
            {
                var recorder = new EventRecorder<TestEvent<int>>();
                pump.Subscribe(recorder);

                pump.Enqueue(new TestEvent<int>());
                Assert.True(pump.HasEvents);
                pump.HandleOne();
                Assert.False(pump.HasEvents);

                Test.OrdinalEquals("ManualEventPumpTests.cs", recorder.LastEvent.EnqueueSource.Value.File);
                Test.OrdinalEquals("EnqueueTests", recorder.LastEvent.EnqueueSource.Value.Member);
                Assert.AreEqual(83, recorder.LastEvent.EnqueueSource.Value.Line);
            });

            // non-blocking, returns Task
            Using(pump =>
            {
                var recorder = new EventRecorder<TestEvent<int>>();
                pump.Subscribe(recorder);

                var eventTask = pump.EnqueueAndWaitAsync(new TestEvent<int>());
                Assert.True(pump.HasEvents);
                Assert.False(eventTask.IsCompleted);
                pump.HandleOne();
                Assert.False(pump.HasEvents);
                Assert.True(eventTask.IsCompleted);

                Test.OrdinalEquals("ManualEventPumpTests.cs", recorder.LastEvent.EnqueueSource.Value.File);
                Test.OrdinalEquals("EnqueueTests", recorder.LastEvent.EnqueueSource.Value.Member);
                Assert.AreEqual(99, recorder.LastEvent.EnqueueSource.Value.Line);
            });

            // blocking, no return value
            Using(pump =>
            {
                var recorder = new EventRecorder<TestEvent<int>>();
                pump.Subscribe(recorder);

                var handlerTask = Task.Factory.StartNew(() =>
                {
                    pump.WaitForEvent();
                    Assert.True(pump.HasEvents);
                    pump.HandleOne();
                    Assert.False(pump.HasEvents);
                });

                System.Threading.Thread.Sleep(SmallSleepTime);
                Assert.False(handlerTask.IsCompleted);
                pump.EnqueueAndWait(new TestEvent<int>());
                System.Threading.Thread.Sleep(SmallSleepTime);
                Assert.True(handlerTask.IsCompleted);

                Test.OrdinalEquals("ManualEventPumpTests.cs", recorder.LastEvent.EnqueueSource.Value.File);
                Test.OrdinalEquals("EnqueueTests", recorder.LastEvent.EnqueueSource.Value.Member);
                Assert.AreEqual(127, recorder.LastEvent.EnqueueSource.Value.Line);
            });
        }

        [Test]
        public static void SubscriberTests()
        {
            // no subscribers
            Using(pump =>
            {
                Assert.False(pump.HasEvents);
                pump.Enqueue(new TestEvent<object>());
                Assert.True(pump.HasEvents);
                pump.HandleOne();
                Assert.False(pump.HasEvents);
            });

            // pump holds weak references to subscribers
            Using(pump =>
            {
                WeakReference<object> weakRef;
                {
                    var recorder = new EventRecorder<TestEvent<int>>();
                    weakRef = new WeakReference<object>(recorder);

                    pump.Subscribe(recorder);
                    recorder = null;
                }
                GC.Collect();

                object obj;
                Assert.False(weakRef.TryGetTarget(out obj));
            });

            // [un]subscribe (twice)
            Using(pump =>
            {
                var recorder = new EventRecorder<TestEvent<int>>();
                pump.Subscribe(recorder);
                pump.Subscribe(recorder);

                Assert.True(pump.Unsubscribe(recorder));
                Assert.False(pump.Unsubscribe(recorder));
            });
        }

        [Test]
        public static void BeginCloseTests()
        {
            var pump = new ManualEventPump();
            Assert.False(pump.IsClosed);

            var testRecorder = new EventRecorder<TestEvent<int>>();
            var closingRecorder = new EventRecorder<EventQueueClosingEvent>();
            var closedRecorder = new EventRecorder<EventQueueClosedEvent>();
            pump.Subscribe(testRecorder);
            pump.Subscribe(closingRecorder);
            pump.Subscribe(closedRecorder);
            Assert.Null(closedRecorder.LastEvent);

            Assert.False(pump.HasEvents);
            pump.BeginClose();
            Assert.True(pump.HasEvents);

            // no more subscriptions after Closing is enqueued
            Assert.Throws<InvalidOperationException>(() => pump.Subscribe(new EventRecorder<TestEvent<int>>()));

            // handle closing
            Assert.Null(closingRecorder.LastEvent);
            pump.HandleOne();
            Assert.NotNull(closingRecorder.LastEvent);

            // enqueue throws after Closing is handled
            Assert.Throws<InvalidOperationException>(() => pump.Enqueue(new TestEvent<int>()));
            Assert.Throws<InvalidOperationException>(() => pump.EnqueueAndWait(new TestEvent<int>()));
            Assert.Throws<InvalidOperationException>(() => pump.EnqueueAndWaitAsync(new TestEvent<int>()));

            // handle closed
            Assert.Null(closedRecorder.LastEvent);
            pump.HandleOne();
            Assert.NotNull(closedRecorder.LastEvent);

            // pump is closed
            Assert.False(pump.HasEvents);
            Assert.True(pump.IsClosed);

            // subscribers removed when the pump is closed
            Assert.False(pump.Unsubscribe(testRecorder));
        }

        [Test]
        public static void RequestCloseTests()
        {
            var pump = new ManualEventPump();
            Assert.False(pump.IsClosed);
            Assert.False(pump.HasEvents);

            // close request cancelling
            var cancelRequestHandler = new DelegateHandler<EventQueueCloseRequestEvent>(evnt => evnt.CanBeginClose = false);
            pump.Subscribe(cancelRequestHandler);
            pump.RequestClose();
            pump.HandleAll();
            Assert.False(pump.IsClosed);
            Assert.False(pump.HasEvents);
            pump.Unsubscribe(cancelRequestHandler);

            // multiple requests, without cancelling
            var requestRecorder = new EventRecorder<EventQueueCloseRequestEvent>();
            pump.Subscribe(requestRecorder);

            pump.RequestClose();
            Assert.True(pump.HasEvents); // close request is an event
            pump.RequestClose();
            pump.RequestClose();

            pump.HandleOne();
            var firstRequest = requestRecorder.LastEvent;

            pump.HandleAll();
            Assert.True(pump.IsClosed);
            Assert.False(pump.HasEvents);

            var lastRequest = requestRecorder.LastEvent;
            Assert.True(object.ReferenceEquals(firstRequest, lastRequest));
        }

        [Test]
        public static void HandlerExceptionTests()
        {
            Using(pump =>
            {
                var exceptionThrower = new DelegateHandler<TestEvent<int>>(evnt => { throw new TimeZoneNotFoundException(); });
                pump.Subscribe(exceptionThrower);

                // exception thrown normally
                var handlerTask = Task.Factory.StartNew(() =>
                {
                    pump.WaitForEvent();
                    pump.HandleOne();
                });
                Assert.Throws<TimeZoneNotFoundException>(() => pump.EnqueueAndWait(new TestEvent<int>()));

                // exception reported via Task
                var task = pump.EnqueueAndWaitAsync(new TestEvent<int>());
                pump.HandleOne();
                Assert.True(task.IsFaulted);
                Assert.IsInstanceOf<TimeZoneNotFoundException>(task.Exception.InnerException);

                // multiple unhandled exceptions are collected in an AggregateException
                pump.Subscribe(new DelegateHandler<TestEvent<int>>(evnt => { throw new TimeZoneNotFoundException(); }));
                task = pump.EnqueueAndWaitAsync(new TestEvent<int>());
                pump.HandleOne();
                Assert.True(task.IsFaulted);
                Assert.IsInstanceOf<AggregateException>(task.Exception.InnerException);
                pump.Unsubscribe(exceptionThrower); // remove the original exception thrower

                // exception reported via UnhandledExceptionEvent
                var exceptionRecorder = new EventRecorder<UnhandledExceptionEvent>();
                pump.Subscribe(exceptionRecorder);

                Assert.False(pump.HasEvents);
                var testEvent = new TestEvent<int>();
                pump.Enqueue(testEvent);
                pump.HandleOne();

                Assert.True(pump.HasEvents); // exception results in new event, that has to be handled separately, like all other events
                Assert.Null(exceptionRecorder.LastEvent);
                pump.HandleOne();
                Assert.NotNull(exceptionRecorder.LastEvent);
                Assert.IsInstanceOf<TimeZoneNotFoundException>(exceptionRecorder.LastEvent.Exception);

                Assert.False(pump.IsClosed); // unhandled exceptions do not close the pump
                Assert.False(pump.HasEvents);

                Test.OrdinalEquals(testEvent.EnqueueSource.Value.File, exceptionRecorder.LastEvent.EnqueueSource.Value.File); // unhandled exceptions inherit the FileLineInfo of the original event
                Test.OrdinalEquals(testEvent.EnqueueSource.Value.Member, exceptionRecorder.LastEvent.EnqueueSource.Value.Member);
                Assert.AreEqual(testEvent.EnqueueSource.Value.Line, exceptionRecorder.LastEvent.EnqueueSource.Value.Line);
            });
        }

        [Test]
        public static void WaitForClosedTests()
        {
            var pump = new ManualEventPump();
            var waitingTask = Task.Factory.StartNew(() =>
            {
                pump.WaitForClosed();
            });

            // blocked by default
            System.Threading.Thread.Sleep(SmallSleepTime);
            Assert.False(waitingTask.IsCompleted);

            // handling events does not unblock it
            pump.Enqueue(new TestEvent<int>());
            pump.HandleOne();
            System.Threading.Thread.Sleep(SmallSleepTime);
            Assert.False(waitingTask.IsCompleted);

            // closing event does not unblock it
            pump.BeginClose();
            pump.HandleOne();
            System.Threading.Thread.Sleep(SmallSleepTime);
            Assert.False(waitingTask.IsCompleted);

            // unblocked after closed event has been handled
            pump.HandleOne();
            System.Threading.Thread.Sleep(SmallSleepTime);
            Assert.True(pump.IsClosed);
            Assert.True(waitingTask.IsCompleted);

            // does not block afterwards
            pump.WaitForClosed();
        }
    }
}
