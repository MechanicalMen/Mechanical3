using System;
using Mechanical3.Core;

namespace Mechanical3.Loggers
{
    /// <summary>
    /// Logs <see cref="LogEntry"/> messages to the Console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly bool printExceptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        /// <param name="printExceptions"><c>true</c> to print associated exceptions; <c>false</c> to ignore them.</param>
        public ConsoleLogger( bool printExceptions )
        {
            this.printExceptions = printExceptions;
        }

        /// <summary>
        /// Logs the specified <see cref="LogEntry"/>.
        /// </summary>
        /// <param name="entry">The <see cref="LogEntry"/> to log.</param>
        public void Log( LogEntry entry )
        {
            if( entry.NullReference() )
                throw new ArgumentNullException(nameof(entry)).StoreFileLine();

            switch( entry.Level )
            {
            case LogLevel.Debug:
                this.Write(entry, textColor: ConsoleColor.Gray);
                break;

            case LogLevel.Information:
                this.Write(entry, textColor: ConsoleColor.Cyan);
                break;

            case LogLevel.Warning:
                this.Write(entry, textColor: ConsoleColor.Yellow);
                break;

            case LogLevel.Error:
                this.Write(entry, textColor: ConsoleColor.Red);
                break;

            case LogLevel.Fatal:
                this.Write(entry, textColor: ConsoleColor.Black, backColor: ConsoleColor.Red);
                break;

            default:
                throw new NotImplementedException().StoreFileLine();
            }
        }

        private void Write( LogEntry entry, ConsoleColor textColor, ConsoleColor backColor = ConsoleColor.Black )
        {
            //// NOTE: here are all the combinations: https://scissortools.wordpress.com/2011/12/01/setting-text-color-in-the-console-output/

            var prevBackground = Console.BackgroundColor;
            var prevForeground = Console.ForegroundColor;
            Console.BackgroundColor = backColor;
            Console.ForegroundColor = textColor;

            Console.WriteLine($"{entry.Timestamp.ToLocalTime().ToString("s").Replace('T', ' ')} [{entry.Level.ToString()[0]}] {entry.Message}");

            if( this.printExceptions
             && entry.Exception.NotNullReference() )
                Console.WriteLine(SafeString.DebugPrint(entry.Exception));

            Console.BackgroundColor = prevBackground;
            Console.ForegroundColor = prevForeground;
        }
    }
}
