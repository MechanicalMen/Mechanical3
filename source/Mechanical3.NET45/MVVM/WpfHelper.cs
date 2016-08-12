using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Mechanical3.Core;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Json;
using Mechanical3.Events;
using Mechanical3.IO.FileSystems;
using Mechanical3.Loggers;

namespace Mechanical3.MVVM
{
    /// <summary>
    /// Helps with common WPF tasks.
    /// </summary>
    public static class WpfHelper
    {
        #region Strong reference store

        // this collections stores objects until the application is closed (technically until the AppDomain is released).
        // we need this because the event queue only holds weak references.
        private static readonly List<object> strongReferences = new List<object>();

        #endregion

        #region Window life-cycle handling

        /* Window life cycle:
         *  - constructor & initialization
         *  - ... work ...
         *  - one of 3 things:
         *    ~ window closing event (the user tried to close the window, can be cancelled)
         *    ~ event queue close request (some code asked to shut down the queue, can be cancelled)
         *    ~ event queue closing (some code begun shutting down the queue)
         *  - event queue closing: this is the time to release the resources
         *  - event queue closed: all systems have shut down, all resources have been released;
         *    if this was the main event queue, the application can now safely exit
         *  - window closed: the application is terminating
         */

        private class WindowCloseHandler : IEventHandler<EventQueueClosedEvent>
        {
            private readonly Window window;
            private bool canWindowClose = false;

            internal WindowCloseHandler( Window wnd, IEventQueue eventQueue )
            {
                this.window = wnd;
                this.window.Closing += this.Window_Closing;
                eventQueue.Subscribe<EventQueueClosedEvent>(this);
            }

            private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
            {
                // don't close the window, unless the event queue has already shut down
                if( !this.canWindowClose )
                {
                    e.Cancel = true;
                    MechanicalApp.EventQueue.RequestClose();
                }
            }

            public void Handle( EventQueueClosedEvent evnt )
            {
                UI.Invoke(() =>
                {
                    this.canWindowClose = true;
                    this.window.Close();
                });
            }
        }

        /// <summary>
        /// Closes the window when the event queue closes, and vica versa.
        /// </summary>
        /// <param name="window">The <see cref="Window"/> to bind to the event queue.</param>
        /// <param name="eventQueue">The <see cref="IEventQueue"/> to bind to the window.</param>
        public static void BindWindowCloseToEventQueueClose( Window window, IEventQueue eventQueue )
        {
            if( window.NullReference() )
                throw new ArgumentNullException(nameof(window)).StoreFileLine();

            if( eventQueue.NullReference() )
                throw new ArgumentNullException(nameof(eventQueue)).StoreFileLine();

            lock( strongReferences )
                strongReferences.Add(new WindowCloseHandler(window, eventQueue));
        }

        #endregion

        #region AppDomain exceptions

        private static bool appDomainExceptionsHandled = false;

        /// <summary>
        /// Handles <see cref="AppDomain.UnhandledException"/>, immediately logs it's argument,
        /// and then tries to properly close the application.
        /// </summary>
        public static void LogAppDomainExceptions()
        {
            lock( strongReferences )
            {
                if( appDomainExceptionsHandled )
                    throw new InvalidOperationException("The exceptions are already being handled!").StoreFileLine();

                appDomainExceptionsHandled = true;
                AppDomain.CurrentDomain.UnhandledException += ( s, e ) =>
                {
                    //// NOTE: I have experienced this event handler not finishing, and therefore
                    ////       failing to log the exception, presumably when it took too long to execute.
                    ////       (The handler was blocked, waiting for the UnhandledExceptionEvent to be
                    ////       handled on the event queue thread.)

                    //// TODO: a few quick searches did not turn up any articles about the handler being aborted. Try to verify that this can happen! (could it have been a bug in the old code?)

                    // NOTE: MSDN about IsTerminating: "Beginning with the .NET Framework version 2.0, this property returns true for most unhandled exceptions..."
                    if( !e.IsTerminating )
                    {
                        // there is no rush
                        MechanicalApp.EnqueueException((Exception)e.ExceptionObject);
                    }
                    else
                    {
                        // there may not be enough time to properly handle the exception, so just log it and try to finish.
                        Log.Fatal("An unhandled exception terminated the application!", (Exception)e.ExceptionObject);
                        MechanicalApp.EventQueue.BeginClose();
                    }
                };
            }
        }

        #endregion

        #region Dispatcher exceptions

        private static bool dispatcherExceptionsHandled = false;

        /// <summary>
        /// Handles <see cref="Application.DispatcherUnhandledException"/>, and enqueues exceptions
        /// into the main event queue, as <see cref="UnhandledExceptionEvent"/> instances.
        /// </summary>
        /// <param name="application">The <see cref="Application"/> to handle the event of.</param>
        public static void EnqueueDispatcherExceptionsFrom( Application application )
        {
            if( application.NullReference() )
                throw new ArgumentNullException(nameof(application)).StoreFileLine();

            lock( strongReferences )
            {
                if( dispatcherExceptionsHandled )
                    throw new InvalidOperationException("The exceptions are already being handled!").StoreFileLine();

                dispatcherExceptionsHandled = true;
                application.DispatcherUnhandledException += ( s, e ) =>
                {
                    MechanicalApp.EnqueueException(e.Exception);
                    e.Handled = true;
                };
            }
        }

        #endregion

        #region Simple logging

        /// <summary>
        /// Creates a new log file in the specified directory.
        /// Sets it as the current logger.
        /// </summary>
        /// <param name="fileSystem">The <see cref="IFileSystem"/> to use.</param>
        /// <param name="maxLogFileCount">The maximum number of log files allowed. If there are more, the oldest will be removed.</param>
        /// <param name="directoryPath">The directory to put the log files in; or <c>null</c> for the root of the <paramref name="fileSystem"/>.</param>
        public static void CreateAndUseNewJsonLogFile( IFileSystem fileSystem, int maxLogFileCount, FilePath directoryPath = null )
        {
            if( fileSystem.NullReference() )
                throw new ArgumentNullException(nameof(fileSystem)).StoreFileLine();

            if( maxLogFileCount < 1 )
                throw new ArgumentOutOfRangeException().Store(nameof(maxLogFileCount), maxLogFileCount);

            // too many log files?
            var logFiles = GetCurrentLogFiles(fileSystem, directoryPath);
            if( logFiles.Length >= maxLogFileCount )
            {
                // delete the oldest ones
                foreach( var path in logFiles.Take(logFiles.Length - maxLogFileCount + 1) )
                    fileSystem.Delete(path);
            }

            // create new log file
            var newFilePath = FilePath.FromFileName(GetNewLogFileNameWithoutExtension() + ".json");
            var stream = fileSystem.CreateFile(
                directoryPath.NullReference() ? newFilePath : directoryPath + newFilePath,
                overwriteIfExists: true);

            // create and use logger
            var logger = new DataStoreTextLogger(new DataStoreTextWriter(JsonFileFormatFactory.Default.CreateWriter(stream)));
            Log.SetLogger(logger);
        }

        private static string GetNewLogFileNameWithoutExtension()
        {
            return "log " + DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm", CultureInfo.InvariantCulture);
        }

        private static DateTime ParseLogFileName( FilePath logFile )
        {
            return DateTime.ParseExact(
                logFile.NameWithoutExtension.Substring(startIndex: "log ".Length),
                "yyyy-MM-dd HH-mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        private static FilePath[] GetCurrentLogFiles( IFileSystem fileSystem, FilePath directoryPath )
        {
            if( directoryPath.NullReference()
             || fileSystem.Exists(directoryPath) )
            {
                return fileSystem
                    .GetPaths(directoryPath) // get all paths from the directory
                    .Where(p => !p.IsDirectory && string.Equals(p.Extension, ".json", StringComparison.OrdinalIgnoreCase)) // keep only log files
                    .Select(p => Tuple.Create(p, ParseLogFileName(p))) // get the creation date from the file name
                    .OrderBy(t => t.Item2) // order by creation date (ascending)
                    .Select(t => t.Item1)
                    .ToArray();
            }
            else
            {
                // the directory does not exist
                return new FilePath[0];
            }
        }

        #endregion
    }
}
