using System;
using Mechanical3.Core;
using Mechanical3.Events;
using Mechanical3.Loggers;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Core
{
    [TestFixture(Category = "Core")]
    public static class LogTests
    {
        private class DisposableNullLogger : DisposableObject, ILogger
        {
            public void Log( LogEntry entry )
            {
            }
        }

        [Test]
        public static void LogClassTests()
        {
            var logDomain = AppDomain.CreateDomain(
                friendlyName: "LogDomain",
                securityInfo: null,
                info: new AppDomainSetup()
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                    ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName
                });
            logDomain.DoCallBack(DoTests);
            AppDomain.Unload(logDomain);
        }

        public static void DoTests()
        {
            // exception before initialization
            Assert.Throws<InvalidOperationException>(() => Log.Debug("test"));
            Assert.Throws<InvalidOperationException>(() => Log.Info("test"));
            Assert.Throws<InvalidOperationException>(() => Log.Warn("test"));
            Assert.Throws<InvalidOperationException>(() => Log.Error("test"));
            Assert.Throws<InvalidOperationException>(() => Log.Fatal("test"));
            Assert.Throws<InvalidOperationException>(() => Log.SetLogger(new MemoryLogger()));

            // initialization
            var eventPump = new ManualEventPump();
            Assert.Throws<ArgumentNullException>(() => Log.Initialize(null));
            Log.Initialize(eventPump);
            Assert.Throws<InvalidOperationException>(() => Log.Initialize(eventPump)); // subsequent initialization throws

            // record entries
            Log.Debug(nameof(LogLevel.Debug));
            Log.Info(nameof(LogLevel.Information));
            Log.Warn(nameof(LogLevel.Warning));
            Log.Error(nameof(LogLevel.Error));
            Log.Fatal(nameof(LogLevel.Fatal));
            Log.Debug(null);
            Log.Debug(string.Empty);

            // transfer entries to first logger
            var memoryLogger = new MemoryLogger();
            Assert.Throws<ArgumentNullException>(() => Log.SetLogger(null));
            Log.SetLogger(memoryLogger);

            // test recorded entries
            var entries = memoryLogger.ToArray();
            Assert.AreEqual(7, entries.Length);
            Action<LogEntry, LogLevel, string> testLevelMessage = ( entry, level, message ) =>
            {
                Assert.AreEqual(entry.Level, level);
                Test.OrdinalEquals(entry.Message, message);
            };
            testLevelMessage(entries[0], LogLevel.Debug, "Debug");
            testLevelMessage(entries[1], LogLevel.Information, "Information");
            testLevelMessage(entries[2], LogLevel.Warning, "Warning");
            testLevelMessage(entries[3], LogLevel.Error, "Error");
            testLevelMessage(entries[4], LogLevel.Fatal, "Fatal");
            testLevelMessage(entries[5], LogLevel.Debug, string.Empty);
            testLevelMessage(entries[6], LogLevel.Debug, string.Empty);
            Test.OrdinalEquals("LogTests.cs", entries[0].SourcePos.File);
            Test.OrdinalEquals("DoTests", entries[0].SourcePos.Member);
            Assert.AreEqual(53, entries[0].SourcePos.Line);

            // new logger does not get transfer
            memoryLogger = new MemoryLogger();
            Log.SetLogger(memoryLogger);
            Assert.AreEqual(0, memoryLogger.ToArray().Length);

            // attached exception test
            var testException = new Exception("test message").Store("testValue", 5);
            Log.Debug(null, testException);
            Test.OrdinalEquals(memoryLogger.ToArray()[0].Exception.ToString(), new ExceptionInfo(testException).ToString());

            // when a disposable logger is replaced, it is disposed of
            var disposableLogger = new DisposableNullLogger();
            Log.SetLogger(disposableLogger);
            Log.SetLogger(new MemoryLogger());
            Assert.True(disposableLogger.IsDisposed);

            // Log class does not produce events
            Assert.False(eventPump.HasEvents);

            // event queue shutdown releases logger...
            disposableLogger = new DisposableNullLogger();
            Log.SetLogger(disposableLogger);
            eventPump.BeginClose();
            eventPump.HandleAll();
            Assert.True(eventPump.IsClosed);
            Assert.True(disposableLogger.IsDisposed);

            // and disables all methods
            Assert.Throws<ObjectDisposedException>(() => Log.Debug("test"));
            Assert.Throws<ObjectDisposedException>(() => Log.Info("test"));
            Assert.Throws<ObjectDisposedException>(() => Log.Warn("test"));
            Assert.Throws<ObjectDisposedException>(() => Log.Error("test"));
            Assert.Throws<ObjectDisposedException>(() => Log.Fatal("test"));
            Assert.Throws<ObjectDisposedException>(() => Log.SetLogger(new MemoryLogger()));
            Assert.Throws<ObjectDisposedException>(() => Log.Initialize(eventPump));
        }
    }
}
