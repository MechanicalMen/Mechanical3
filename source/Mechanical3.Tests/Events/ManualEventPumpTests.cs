using System;
using System.Threading.Tasks;
using Mechanical3.Events;
using NUnit.Framework;

namespace Mechanical3.Tests.Events
{
    public static class ManualEventPumpTests
    {
        #region Helpers

        private class TestEvent<T> : EventBase
        {
            public T Value { get; private set; } = default(T);
        }

        private class EventRecorder<T> : IEventHandler<T>
            where T : EventBase
        {
            public T LastEvent { get; private set; } = null;

            public void Handle( T evnt )
            {
                this.LastEvent = evnt;
            }
        }

        private class RequestNegationHandler : IEventHandler<EventQueueCloseRequestEvent>
        {
            public void Handle( EventQueueCloseRequestEvent evnt )
            {
                evnt.CanBeginClose = !evnt.CanBeginClose;
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
                Assert.AreEqual(68, recorder.LastEvent.EnqueueSource.Value.Line);
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
                Assert.AreEqual(84, recorder.LastEvent.EnqueueSource.Value.Line);
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

                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(100));
                Assert.False(handlerTask.IsCompleted);
                pump.EnqueueAndWait(new TestEvent<int>());
                System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(100));
                Assert.True(handlerTask.IsCompleted);

                Test.OrdinalEquals("ManualEventPumpTests.cs", recorder.LastEvent.EnqueueSource.Value.File);
                Test.OrdinalEquals("EnqueueTests", recorder.LastEvent.EnqueueSource.Value.Member);
                Assert.AreEqual(112, recorder.LastEvent.EnqueueSource.Value.Line);
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
            var negationHandler = new RequestNegationHandler();
            pump.Subscribe(negationHandler);
            pump.RequestClose();
            pump.HandleAll();
            Assert.False(pump.IsClosed);
            Assert.False(pump.HasEvents);
            pump.Unsubscribe(negationHandler);

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
    }
}
