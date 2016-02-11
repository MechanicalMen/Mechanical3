namespace Mechanical3.Loggers
{
    /// <summary>
    /// The severity of a <see cref="LogEntry"/>.
    /// </summary>
    public enum LogLevel : byte
    {
        /// <summary>
        /// Used only for debugging or testing purposes.
        /// </summary>
        Debug,

        /// <summary>
        /// Information about the system, or current state.
        /// </summary>
        Information,

        /// <summary>
        /// A small error was encountered, and handled.
        /// </summary>
        Warning,

        /// <summary>
        /// An unexpected error occurred. The operation had to be aborted, but the application can keep running.
        /// </summary>
        Error,

        /// <summary>
        /// An unexpected error occurred. The application was greatly disrupted (and probably forced to exit).
        /// </summary>
        Fatal
    }
}
