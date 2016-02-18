using System;
using Mechanical3.Core;
using Mechanical3.Events;
using Mechanical3.Loggers;
using NUnit.Framework;

namespace Mechanical3.Tests.Core
{
    [TestFixture(Category = "Core")]
    public static class MechanicalAppTests
    {
        [Test]
        public static void AppTests()
        {
            RunFromNewAppDomain(() => DoTests(withDefaultExceptionLogging: false));
            RunFromNewAppDomain(() => DoTests(withDefaultExceptionLogging: true));
        }

        private static void RunFromNewAppDomain( CrossAppDomainDelegate dlgt )
        {
            var mechAppDomain = AppDomain.CreateDomain(
                friendlyName: "MechanicalAppDomain",
                securityInfo: null,
                info: new AppDomainSetup()
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                    ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName
                });
            mechAppDomain.DoCallBack(dlgt);
            AppDomain.Unload(mechAppDomain);
        }

        public static void DoTests( bool withDefaultExceptionLogging )
        {
            // exception before initialization
            Assert.Throws<InvalidOperationException>(() => MechanicalApp.MainEventQueue.ToString());
            Assert.Throws<InvalidOperationException>(() => MechanicalApp.HandleException(new Exception()));
            Assert.Throws<InvalidOperationException>(() => Log.SetLogger(new MemoryLogger()));

            // initialization
            var eventPump = new ManualEventPump();
            Assert.Throws<ArgumentNullException>(() => MechanicalApp.Initialize(null));
            MechanicalApp.Initialize(eventPump, withDefaultExceptionLogging);

            // second initialization fails
            Assert.Throws<InvalidOperationException>(() => MechanicalApp.Initialize(eventPump));

            // after initialization, member access is OK
            Assert.True(object.ReferenceEquals(eventPump, MechanicalApp.MainEventQueue));
            var memLogger = new MemoryLogger();
            Log.SetLogger(memLogger);

            // HandleException enqueues an UnhandledExceptionEvent
            var exception = new Exception();
            var recorder = new Mechanical3.Tests.Events.ManualEventPumpTests.EventRecorder<UnhandledExceptionEvent>();
            eventPump.Subscribe(recorder);
            MechanicalApp.HandleException(exception);
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

            // release event pump
            eventPump.BeginClose();
            eventPump.HandleAll();
        }
    }
}
