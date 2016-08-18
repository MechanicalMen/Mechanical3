using System;
using Mechanical3.Core;

namespace Mechanical3.Loggers
{
    /// <summary>
    /// Allows logging to multiple loggers (sequentially).
    /// </summary>
    public class AggregateLogger : DisposableObject, ILogger
    {
        #region Private Fields

        private ILogger[] loggers;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateLogger"/> class.
        /// </summary>
        /// <param name="loggersToUse">The loggers to pass log entries to.</param>
        public AggregateLogger( params ILogger[] loggersToUse )
        {
            if( loggersToUse.NullEmptyOrSparse() )
                throw NamedArgumentException.From(nameof(loggersToUse)).StoreFileLine();

            this.loggers = loggersToUse;
        }

        #endregion

        #region IDisposableObject

        /// <summary>
        /// Called when the object is being disposed of. Inheritors must call base.OnDispose to be properly disposed.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c>, release both managed and unmanaged resources; otherwise release only the unmanaged resources.</param>
        protected override void OnDispose( bool disposing )
        {
            if( disposing )
            {
                //// dispose-only (i.e. non-finalizable) logic
                //// (managed, disposable resources you own)

                if( this.loggers.NotNullReference() )
                {
                    foreach( var l in this.loggers )
                    {
                        var asDisposable = l as IDisposable;
                        if( asDisposable.NotNullReference() )
                            asDisposable.Dispose();
                    }
                    this.loggers = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region ILogger

        /// <summary>
        /// Logs the specified <see cref="LogEntry"/>.
        /// </summary>
        /// <param name="entry">The <see cref="LogEntry"/> to log.</param>
        public void Log( LogEntry entry )
        {
            this.ThrowIfDisposed();

            foreach( var l in this.loggers )
                l.Log(entry);
        }

        #endregion
    }
}
