using System;
using System.Collections.Generic;
using Mechanical3.Core;

namespace Mechanical3.Loggers
{
    /// <summary>
    /// Keeps all log entries in memory, so that they can be transferred to another <see cref="ILogger"/>.
    /// </summary>
    public class MemoryLogger : ILogger
    {
        #region Private Fields

        private readonly List<LogEntry> entries;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryLogger"/> class.
        /// </summary>
        public MemoryLogger()
        {
            this.entries = new List<LogEntry>();
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

            this.entries.Add(entry);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates an array from the recorded entries.
        /// </summary>
        /// <returns>The log entries currently recorded.</returns>
        public LogEntry[] ToArray()
        {
            return this.entries.ToArray();
        }

        #endregion
    }
}
