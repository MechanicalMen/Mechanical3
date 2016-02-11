namespace Mechanical3.Loggers
{
    /// <summary>
    /// Records log entries.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs the specified <see cref="LogEntry"/>.
        /// </summary>
        /// <param name="entry">The <see cref="LogEntry"/> to log.</param>
        void Log( LogEntry entry );
    }
}
