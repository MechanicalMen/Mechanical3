using System;
using System.Threading;
using System.Threading.Tasks;
using Mechanical3.Core;
using Mechanical3.Events;
using Mechanical3.Misc;
using Mechanical3.Tests.Events;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class ConsoleEventQueueUIThreadTests
    {
        private static void ExactlyOneEventToHandle( ManualEventPump eventPump )
        {
            Assert.True(eventPump.HasEvents);
            eventPump.HandleOne();
            Assert.False(eventPump.HasEvents);
        }

        [Test]
        public static void ConsoleQueueUITest()
        {
            var mainThread = Thread.CurrentThread;

            ManualEventPump eventPump;
            IUIThreadHandler uiHandler;
            using( var consoleHandler = ConsoleEventQueueUIHandler.FromMainThread() )
            {
                // event queue and ui thread handler are available
                Assert.NotNull(eventPump = consoleHandler.EventPump);
                Assert.NotNull(uiHandler = consoleHandler.UIThreadHandler);
                Assert.False(eventPump.HasEvents);

                // ui thread handler correctly identifies main thread
                Assert.True(uiHandler.IsOnUIThread());
                Assert.False(Task.Run(() => uiHandler.IsOnUIThread()).Result);
                Assert.False(eventPump.HasEvents); // we didn't use events just to check

                // BeginInvoke does not immediately run
                bool invokeFinished = false;
                uiHandler.BeginInvoke(() => invokeFinished = true);
                Assert.False(invokeFinished);
                ExactlyOneEventToHandle(eventPump);
                Assert.True(invokeFinished);

                // Invoke does not immediately run
                invokeFinished = false;
                Task.Run(() => uiHandler.Invoke(() => invokeFinished = true));
                Thread.Sleep(ManualEventPumpTests.SmallSleepTime);
                Assert.False(invokeFinished);
                ExactlyOneEventToHandle(eventPump);
                Assert.True(invokeFinished);

                // disposal automatically closes event queue
                Assert.False(eventPump.IsClosed);
            }
            Assert.True(eventPump.IsClosed);


            // closing the event pump disables the ui thread handler
            var consoleHandler2 = ConsoleEventQueueUIHandler.FromMainThread();
            using( consoleHandler2 )
            {
                eventPump = consoleHandler2.EventPump;
                uiHandler = consoleHandler2.UIThreadHandler;

                // close the pump manually
                eventPump.BeginClose();
                eventPump.HandleAll();

                // closing does not disable the properties
                Assert.AreSame(eventPump, consoleHandler2.EventPump);
                Assert.AreSame(uiHandler, consoleHandler2.UIThreadHandler);

                // but it does disable the ui thread handler
                Assert.Throws<ObjectDisposedException>(() => uiHandler.IsOnUIThread());
                Assert.Throws<ObjectDisposedException>(() => uiHandler.Invoke(() => { }));
                Assert.Throws<ObjectDisposedException>(() => uiHandler.BeginInvoke(() => { }));
            }
            Assert.Throws<ObjectDisposedException>(() => consoleHandler2.EventPump.NotNullReference());
            Assert.Throws<ObjectDisposedException>(() => consoleHandler2.UIThreadHandler.NotNullReference());
        }
    }
}
