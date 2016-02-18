using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Mechanical3.Events;
using Mechanical3.Misc;

namespace Mechanical3.Core
{
    /// <summary>
    /// Handles the application life cycle.
    /// </summary>
    public class MechanicalApp
    {
        //// NOTE: We do not use a static class, so that this class may be inherited, along with it's static members.

        //// NOTE: This is pretty much the definition of an anti-pattern (service locator), but just like with the Log class,
        ////       I just find it too useful (for these very common cases: app shutdown, logging, invoking exception handlers)

        //// NOTE: I am also way too lazy to use dependency injection each and every time I want to log, or have an exception be handled.

        #region Initialization

        private static IEventQueue mainQueue;

        /// <summary>
        /// Initializes the <see cref="MechanicalApp"/> and <see cref="Log"/> classes.
        /// </summary>
        /// <param name="mainEventQueue">The main <see cref="IEventQueue"/> of the application.</param>
        /// <param name="defaultExceptionLogging">Logs each <see cref="UnhandledExceptionEvent"/> of the <see cref="MainEventQueue"/>, if <c>true</c>.</param>
        public static void Initialize( IEventQueue mainEventQueue, bool defaultExceptionLogging = true )
        {
            if( mainEventQueue.NullReference() )
                throw new ArgumentNullException(nameof(mainEventQueue)).StoreFileLine();

            if( Interlocked.CompareExchange(ref mainQueue, mainEventQueue, comparand: null).NotNullReference() )
                throw new InvalidOperationException("Application already initialized!").StoreFileLine();

            Log.Initialize(mainEventQueue);

            if( defaultExceptionLogging )
                MainEventQueue.Subscribe(DefaultExceptionLogger.Instance);
        }

        /// <summary>
        /// Gets the main <see cref="IEventQueue"/> of the application.
        /// It will be closed just before the application terminates.
        /// Events may not be handled from the UI thread, in fact there
        /// is no reason that they should.
        /// </summary>
        public static IEventQueue MainEventQueue
        {
            get
            {
                if( mainQueue.NullReference() )
                    throw new InvalidOperationException("Application not yet initialized!").StoreFileLine();

                return mainQueue;
            }
        }

        #endregion

        #region Unhandled exception reporting

        //// NOTE: We do not indicate here whether the exception was properly recovered from,
        ////       wether the application should or should not terminate, ... etc.

        //// NOTE: I would recommend that by default it is expected, that the exception could
        ////       be recovered from, since otherwise the code could ask the main event queue to close.

        //// NOTE: It may be useful to create a FatalExceptionEvent inheriting UnhandledExceptionEvent,
        ////       which would invoke all the default exception handlers, and could
        ////       also initiate a crash report, as well as control application termination.

        /// <summary>
        /// Enqueues an <see cref="UnhandledExceptionEvent"/> on the <see cref="MainEventQueue"/>,
        /// and then returns immediately.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public static void HandleException(
            Exception exception,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            MainEventQueue.Enqueue(new UnhandledExceptionEvent(exception), file, member, line);
        }

        private class DefaultExceptionLogger : IEventHandler<UnhandledExceptionEvent>
        {
            internal static readonly DefaultExceptionLogger Instance = new DefaultExceptionLogger();

            public void Handle( UnhandledExceptionEvent evnt )
            {
                var srcPos = evnt.EnqueueSource.HasValue ? evnt.EnqueueSource.Value : FileLineInfo.Create();
                try
                {
                    Log.Error("Unhandled exception caught!", evnt.Exception, srcPos.File, srcPos.Member, srcPos.Line ?? 0);
                }
                catch( Exception e )
                {
                    //// NOTE: We do not let the exception propagate, since that may result in
                    ////       another UnhandledException event, and therefore an infinite loop.

                    //// NOTE: This either happens because there was a problem with the current logger,
                    ////       or the application is in the final stages of shutting down (there is no logger).

                    if( System.Diagnostics.Debugger.IsAttached )
                    {
                        var asString = SafeString.DebugPrint(e);
                        System.Diagnostics.Debugger.Break();
                        GC.KeepAlive(asString);
                    }
                }
            }
        }

        #endregion
    }
}
