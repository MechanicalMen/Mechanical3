using System;
using Mechanical3.Core;
using Mechanical3.Misc;

namespace Mechanical3.Loggers
{
    /// <summary>
    /// Represents a log message.
    /// </summary>
    public sealed class LogEntry
    {
        #region Private Fields

        private readonly DateTime timestamp;
        private readonly LogLevel level;
        private readonly string message;
        private readonly ExceptionInfo exception;
        private readonly FileLineInfo sourcePos;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="timestamp">The creation time of the <see cref="LogEntry"/>.</param>
        /// <param name="level">The severity of a <see cref="LogEntry"/>.</param>
        /// <param name="message">The log message.</param>
        /// <param name="exceptionInfo">The <see cref="ExceptionInfo"/> associated with the <see cref="LogEntry"/>; or <c>null</c>.</param>
        /// <param name="sourcePos">The position in the source file, where the entry was created.</param>
        public LogEntry(
            DateTime timestamp,
            LogLevel level,
            string message,
            ExceptionInfo exceptionInfo,
            FileLineInfo sourcePos )
        {
            if( timestamp.Kind != DateTimeKind.Utc )
                throw new ArgumentException("Only UTC timestamps allowed!").Store(nameof(timestamp.Kind), timestamp.Kind).Store(nameof(timestamp), timestamp);

            if( !Enum.IsDefined(typeof(LogLevel), level) )
                throw new ArgumentException("Log level undefined!").Store(nameof(level), level);

            if( message.NullReference() )
                message = string.Empty;

            this.timestamp = timestamp;
            this.level = level;
            this.message = message;
            this.exception = exceptionInfo;
            this.sourcePos = sourcePos;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the creation time of the <see cref="LogEntry"/>.
        /// </summary>
        /// <value>The creation time of the <see cref="LogEntry"/>.</value>
        public DateTime Timestamp
        {
            get { return this.timestamp; }
        }

        /// <summary>
        /// Gets the severity of a <see cref="LogEntry"/>.
        /// </summary>
        /// <value>The severity of a <see cref="LogEntry"/>.</value>
        public LogLevel Level
        {
            get { return this.level; }
        }

        /// <summary>
        /// Gets the log message.
        /// </summary>
        /// <value>The log message.</value>
        public string Message
        {
            get { return this.message; }
        }

        /// <summary>
        /// Gets the <see cref="ExceptionInfo"/> associated with the <see cref="LogEntry"/>; or <c>null</c>.
        /// </summary>
        /// <value>The <see cref="ExceptionInfo"/> associated with the <see cref="LogEntry"/>; or <c>null</c>.</value>
        public ExceptionInfo Exception
        {
            get { return this.exception; }
        }

        /// <summary>
        /// Gets the position in the source file, where the entry was created.
        /// </summary>
        /// <value>The position in the source file, where the entry was created.</value>
        public FileLineInfo SourcePos
        {
            get { return this.sourcePos; }
        }

        #endregion
    }
}
