using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Mechanical3.Events;
using Mechanical3.Misc;
using Mechanical3.MVVM;

namespace Mechanical3.Core
{
    /// <summary>
    /// Handles the application life cycle.
    /// </summary>
    public class MechanicalApp
    {
        //// NOTE: We do not use a static class, so that this class may be inherited, along with it's static members.

        //// NOTE: This is pretty much the definition of an anti-pattern (service locator), but just like with IStringConverterLocator and Log,
        ////       I just find it too useful to replace with something else.

        //// NOTE: I am also way too lazy to use dependency injection each and every time I want to log, or have an exception be handled
        ////       (nevermind listing each data type being serialized in a data store document)

        #region Initialization

        private static IEventQueue mainQueue;

        /// <summary>
        /// Initializes the <see cref="MechanicalApp"/>, <see cref="Log"/> and <see cref="UI"/> classes.
        /// </summary>
        /// <param name="uiThreadHandler">An object representing the UI thread.</param>
        /// <param name="mainEventQueue">The main <see cref="IEventQueue"/> of the application.</param>
        /// <param name="logUnhandledExceptionEvents">If <c>true</c>, logs each <see cref="UnhandledExceptionEvent"/> of the <see cref="EventQueue"/>.</param>
        public static void Initialize(
            IUIThreadHandler uiThreadHandler,
            IEventQueue mainEventQueue,
            bool logUnhandledExceptionEvents = true )
        {
            if( uiThreadHandler.NullReference() )
                throw new ArgumentNullException(nameof(uiThreadHandler)).StoreFileLine();

            if( mainEventQueue.NullReference() )
                throw new ArgumentNullException(nameof(mainEventQueue)).StoreFileLine();

            UI.Initialize(uiThreadHandler);

            if( Interlocked.CompareExchange(ref mainQueue, mainEventQueue, comparand: null).NotNullReference() )
                throw new InvalidOperationException("Application already initialized!").StoreFileLine();

            Log.Initialize(mainEventQueue);

            if( logUnhandledExceptionEvents )
                EventQueue.Subscribe(DefaultExceptionEventLogger.Instance);
        }

        /// <summary>
        /// Gets the <see cref="IEventQueue"/> of the application.
        /// It will be closed just before the application terminates.
        /// Events may not be handled from the UI thread, in fact there
        /// is no reason that they should.
        /// </summary>
        public static IEventQueue EventQueue
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
        /// Enqueues an <see cref="UnhandledExceptionEvent"/> on the <see cref="EventQueue"/>,
        /// and then returns immediately.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public static void EnqueueException(
            Exception exception,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            // the only exception we'll allow to escape, is one for using this before Initialization
            if( EventQueue.NullReference() )
                throw new InvalidOperationException("Not yet initialized!").StoreFileLine();

            try
            {
                EventQueue.Enqueue(new UnhandledExceptionEvent(exception), file, member, line);
            }
            catch( Exception ex )
            {
                // Probably the event queue is already closed, we may not have persistent logging.
                FailedToHandleException(new AggregateException(ex, exception.StoreFileLine(file, member, line)));
            }
        }

        /// <summary>
        /// This method is used only, when the standard error handling (i.e. <see cref="EnqueueException"/>) fails.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        private static void FailedToHandleException(
            Exception exception,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            // NOTE: When this method is invoked, we may only have a memory logger,
            //       but we can not even assume that much.
            try
            {
                if( Debugger.IsAttached )
                    Debugger.Break();

                Log.Fatal("Error handling failed!", exception.StoreFileLine(file, member, line));
            }
            catch
            {
            }
        }

        private class DefaultExceptionEventLogger : IEventHandler<UnhandledExceptionEvent>
        {
            internal static readonly DefaultExceptionEventLogger Instance = new DefaultExceptionEventLogger();

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
                        GC.KeepAlive(asString); // asString no longer an unused variable
                    }
                }
            }
        }

        #endregion
    }
}
