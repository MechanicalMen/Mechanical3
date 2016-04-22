using System;
using Mechanical3.Core;
using Mechanical3.Events;
using Mechanical3.Loggers;
using Mechanical3.Misc;
using Mechanical3.MVVM;
using NUnit.Framework;

namespace Mechanical3.Tests.Core
{
    [TestFixture(Category = "Core")]
    public static class MechanicalAppTests
    {
        [Test]
        public static void AppTests()
        {
            Test.CreateInstanceAndRunInNewAppDomain<Test1>();
            Test.CreateInstanceAndRunInNewAppDomain<Test2>();
        }

        public class Test1 : MarshalByRefObject, Test.IAppDomainRunnable
        {
            public void Run()
            {
                DoTests(withDefaultExceptionLogging: false);
            }
        }

        public class Test2 : MarshalByRefObject, Test.IAppDomainRunnable
        {
            public void Run()
            {
                DoTests(withDefaultExceptionLogging: true);
            }
        }

        public static void DoTests( bool withDefaultExceptionLogging )
        {
            // exception before initialization
            Assert.Throws<InvalidOperationException>(() => MechanicalApp.EventQueue.ToString());
            Assert.Throws<InvalidOperationException>(() => MechanicalApp.EnqueueException(new Exception()));
            Assert.Throws<InvalidOperationException>(() => UI.InvokeAsync(() => { }));
            Assert.Throws<InvalidOperationException>(() => Log.SetLogger(new MemoryLogger()));

            using( var consoleHandler = ConsoleEventQueueUIThread.FromMainThread() )
            {
                // initialization
                Assert.Throws<ArgumentNullException>(() => MechanicalApp.Initialize((IUIThreadHandler)null, consoleHandler.EventPump));
                Assert.Throws<ArgumentNullException>(() => MechanicalApp.Initialize(consoleHandler.UIThreadHandler, (IEventQueue)null));
                MechanicalApp.Initialize(consoleHandler.UIThreadHandler, consoleHandler.EventPump, withDefaultExceptionLogging);

                // second initialization fails
                Assert.Throws<InvalidOperationException>(() => MechanicalApp.Initialize(consoleHandler.UIThreadHandler, consoleHandler.EventPump));

                // after initialization, member access is OK
                var eventPump = consoleHandler.EventPump;
                Assert.AreSame(eventPump, MechanicalApp.EventQueue);
                UI.InvokeAsync(() => { });
                eventPump.HandleOne(); // consume the event created by the UI class
                var memLogger = new MemoryLogger();
                Log.SetLogger(memLogger);

                // HandleException enqueues an UnhandledExceptionEvent
                var exception = new Exception();
                var recorder = new Mechanical3.Tests.Events.ManualEventPumpTests.EventRecorder<UnhandledExceptionEvent>();
                eventPump.Subscribe(recorder);
                MechanicalApp.EnqueueException(exception);
                Assert.True(eventPump.HasEvents);
                eventPump.HandleOne();
                Assert.False(eventPump.HasEvents);
                Assert.NotNull(recorder.LastEvent);
                Assert.True(object.ReferenceEquals(exception, recorder.LastEvent.Exception));

                // default exception logging produces a new LogEntry
                if( withDefaultExceptionLogging )
                {
                    var entries = memLogger.ToArray();
                    Assert.AreEqual(1, entries.Length);
                    Assert.AreEqual(LogLevel.Error, entries[0].Level);
                }
                else
                {
                    Assert.AreEqual(0, memLogger.ToArray().Length);
                }
            }
        }
    }
}
