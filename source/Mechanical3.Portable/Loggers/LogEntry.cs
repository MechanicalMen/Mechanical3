using System;
using Mechanical3.Core;
using Mechanical3.DataStores;
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

        #region Serialization

        private static class Keys
        {
            internal const string Timestamp = "Timestamp";
            internal const string Level = "Level";
            internal const string Message = "Message";
            internal const string Exception = "Exception";
            internal const string File = "File";
            internal const string Member = "Member";
            internal const string Line = "Line";
        }

        /// <summary>
        /// Saves the specified instance.
        /// </summary>
        /// <param name="entry">The instance to save. May be <c>null</c>.</param>
        /// <param name="writer">The <see cref="DataStoreTextWriter"/> to use.</param>
        public static void Save( LogEntry entry, DataStoreTextWriter writer )
        {
            if( writer.NullReference() )
                throw new ArgumentNullException(nameof(writer)).StoreFileLine();

            if( entry.NullReference() )
            {
                writer.WriteNull();
                return;
            }

            writer.WriteObjectStart();
            writer.WriteValue(Keys.Timestamp, entry.Timestamp);
            writer.WriteValue(Keys.Level, entry.Level.ToString());
            writer.WriteValue(Keys.Message, entry.Message);

            writer.WriteName(Keys.Exception);
            ExceptionInfo.Save(entry.exception, writer);

            writer.WriteValue(Keys.File, entry.SourcePos.File);
            writer.WriteValue(Keys.Member, entry.SourcePos.Member);
            writer.WriteValue(Keys.Line, entry.SourcePos.Line);
            writer.WriteEnd();
        }

        /// <summary>
        /// Loads the specified instance.
        /// </summary>
        /// <param name="reader">The <see cref="DataStoreTextReader"/> to use.</param>
        /// <returns>The instance loaded. May be <c>null</c>.</returns>
        public static LogEntry LoadFrom( DataStoreTextReader reader )
        {
            if( reader.NullReference() )
                throw new ArgumentNullException(nameof(reader)).StoreFileLine();

            if( reader.Token == DataStoreToken.Value )
            {
                reader.AssertNull();
                return null;
            }
            else
            {
                reader.AssertObjectStart();
                var timestamp = reader.ReadValue<DateTime>(Keys.Timestamp);
                var level = (LogLevel)Enum.Parse(typeof(LogLevel), reader.ReadValue<string>(Keys.Level));
                var message = reader.ReadValue<string>(Keys.Message);

                reader.AssertCanRead(Keys.Exception);
                var exception = ExceptionInfo.LoadFrom(reader);

                var file = reader.ReadValue<string>(Keys.File);
                var member = reader.ReadValue<string>(Keys.Member);
                var line = reader.ReadValue<int>(Keys.Line);
                return new LogEntry(timestamp, level, message, exception, new FileLineInfo(file, member, line));
            }
        }

        #endregion
    }
}
