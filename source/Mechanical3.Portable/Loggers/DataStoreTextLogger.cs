using System;
using System.Collections.Generic;
using Mechanical3.Core;
using Mechanical3.DataStores;

namespace Mechanical3.Loggers
{
    /// <summary>
    /// Serializes log entries to a text data store.
    /// </summary>
    public class DataStoreTextLogger : DisposableObject, ILogger
    {
        #region Private Fields

        private DataStoreTextWriter writer;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreTextLogger"/> class.
        /// </summary>
        /// <param name="dataStoreWriter">The <see cref="DataStoreTextWriter"/> to serialize entries to.</param>
        public DataStoreTextLogger( DataStoreTextWriter dataStoreWriter )
            : base()
        {
            if( dataStoreWriter.NullReference() )
                throw new ArgumentNullException(nameof(dataStoreWriter)).StoreFileLine();

            this.writer = dataStoreWriter;

            this.writer.WriteArrayStart();
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

                if( this.writer.NotNullReference() )
                {
                    this.writer.WriteEnd();
                    this.writer.Dispose();
                    this.writer = null;
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
            if( entry.NullReference() )
                throw new ArgumentNullException(nameof(entry)).StoreFileLine();

            LogEntry.Save(entry, this.writer);
            this.writer.Flush();
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Deserializes the saved log entries.
        /// </summary>
        /// <param name="reader">The <see cref="DataStoreTextReader"/> to use.</param>
        /// <returns>The log entries deserialized.</returns>
        public static LogEntry[] LoadEntriesFrom( DataStoreTextReader reader )
        {
            if( reader.NullReference() )
                throw new ArgumentNullException(nameof(reader)).StoreFileLine();

            var results = new List<LogEntry>();

            reader.ReadArrayStart();
            reader.AssertCanRead();
            while( reader.Token != DataStoreToken.End )
                results.Add(LogEntry.LoadFrom(reader));
            reader.AssertEnd();

            return results.ToArray();
        }

        #endregion
    }
}
