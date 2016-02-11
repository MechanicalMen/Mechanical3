using System;
using System.Runtime.CompilerServices;
using Mechanical3.Events;
using Mechanical3.Loggers;
using Mechanical3.Misc;

namespace Mechanical3.Core
{
    /// <summary>
    /// Handles thread-safe logging for it's AppDomain.
    /// Logging is disabled by an <see cref="EventQueueClosedEvent"/> handler,
    /// therefore no handler of that event, or any code running after it's been handled
    /// should depend on this class.
    /// </summary>
    public static class Log
    {
        #region ExceptionLogger

        private class ExceptionLogger : ILogger
        {
            public void Log( LogEntry entry )
            {
                throw new ObjectDisposedException(
                    message: "Logger released, no more logging is possible! Please finish logging before EventQueueClosedEvent starts being handled.",
                    innerException: null);
            }
        }

        #endregion

        #region EventHandler

        private class EventHandler : IEventHandler<EventQueueClosedEvent>
        {
            internal event Action EventQueueClosed;

            public void Handle( EventQueueClosedEvent evnt )
            {
                var handlers = this.EventQueueClosed;
                if( handlers.NotNullReference() )
                    handlers();
            }
        }

        #endregion

        #region Private Static Fields

        private static readonly object LoggerSyncLock = new object();
        private static readonly EventHandler Events = new EventHandler();

        private static bool isInitialLogger = true;
        private static ILogger currentLogger = null; // null == not initialized

        #endregion

        #region Private Static Methods

        private static void ThrowIfNotInitialized_NotLocked()
        {
            if( currentLogger.NullReference() )
                throw new InvalidOperationException("Log.Initialize not yet called!");
        }

        private static void ThrowIfDisposed_NotLocked()
        {
            if( currentLogger is ExceptionLogger )
                throw new ObjectDisposedException(message: "Logging is only possible before the main event queue's EventQueueClosedEvent starts being handled!", innerException: null);
        }

        private static void OnEventQueueClosed()
        {
            SetLogger(new ExceptionLogger());
        }

        private static void DoLog(
            LogLevel level,
            string message,
            Exception exception,
            string file,
            string member,
            int line )
        {
            lock( LoggerSyncLock )
            {
                ThrowIfNotInitialized_NotLocked();
                ThrowIfDisposed_NotLocked();

                currentLogger.Log(
                    new LogEntry(
                        DateTime.UtcNow,
                        level,
                        message,
                        exception.NotNullReference() ? new ExceptionInfo(exception) : null,
                        new FileLineInfo(file, member, line)));
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Initializes the <see cref="Log"/> class.
        /// Sets up an internal memory logger, so that logging can begin
        /// before system resources are accessed. Entries recorded this way
        /// are transferred upon the first call to <see cref="SetLogger"/>.
        /// </summary>
        /// <param name="mainEventQueue">The main <see cref="IEventQueue"/> of the application. Logging is disabled when it's <see cref="EventQueueClosedEvent"/> is being handled.</param>
        public static void Initialize( IEventQueue mainEventQueue )
        {
            if( mainEventQueue.NullReference() )
                throw new ArgumentNullException(nameof(mainEventQueue)).StoreFileLine();

            lock( LoggerSyncLock )
            {
                ThrowIfDisposed_NotLocked();
                if( currentLogger.NotNullReference() )
                    throw new InvalidOperationException("Already initialized!").StoreFileLine();

                // register event handlers
                Events.EventQueueClosed += OnEventQueueClosed;
                mainEventQueue.Subscribe<EventQueueClosedEvent>(Events);

                // set up memory logger
                currentLogger = new MemoryLogger();
            }
        }

        /// <summary>
        /// Replaces the current <see cref="ILogger"/>.
        /// Log entries after <see cref="Initialize"/> and before the first call to this method are recorded,
        /// and will be transferred to the new logger.
        /// </summary>
        /// <param name="newLogger">The new logger to replace the current one.</param>
        public static void SetLogger( ILogger newLogger )
        {
            if( newLogger.NullReference() )
                throw new ArgumentNullException(nameof(newLogger)).StoreFileLine();

            lock( LoggerSyncLock )
            {
                ThrowIfNotInitialized_NotLocked();
                ThrowIfDisposed_NotLocked();

                // transfer recorded entries
                if( isInitialLogger )
                {
                    var asMemoryLogger = currentLogger as MemoryLogger;
                    if( asMemoryLogger.NotNullReference() )
                    {
                        foreach( var entry in asMemoryLogger.ToArray() )
                            newLogger.Log(entry);
                    }
                }

                // dispose of old logger
                var asDisposableLogger = currentLogger as IDisposable;
                if( asDisposableLogger.NotNullReference() )
                    asDisposableLogger.Dispose();

                // set new logger
                currentLogger = newLogger;
                isInitialLogger = false;
            }
        }

        /// <summary>
        /// Logs a message using <see cref="LogLevel.Debug"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to attach to the log entry.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public static void Debug(
            string message,
            Exception exception = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            DoLog(LogLevel.Debug, message, exception, file, member, line);
        }

        /// <summary>
        /// Logs a message using <see cref="LogLevel.Information"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to attach to the log entry.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public static void Info(
            string message,
            Exception exception = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            DoLog(LogLevel.Information, message, exception, file, member, line);
        }

        /// <summary>
        /// Logs a message using <see cref="LogLevel.Warning"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to attach to the log entry.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public static void Warn(
            string message,
            Exception exception = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            DoLog(LogLevel.Warning, message, exception, file, member, line);
        }

        /// <summary>
        /// Logs a message using <see cref="LogLevel.Error"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to attach to the log entry.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public static void Error(
            string message,
            Exception exception = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            DoLog(LogLevel.Error, message, exception, file, member, line);
        }

        /// <summary>
        /// Logs a message using <see cref="LogLevel.Fatal"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An optional <see cref="Exception"/> to attach to the log entry.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public static void Fatal(
            string message,
            Exception exception = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            DoLog(LogLevel.Fatal, message, exception, file, member, line);
        }

        #endregion
    }
}
