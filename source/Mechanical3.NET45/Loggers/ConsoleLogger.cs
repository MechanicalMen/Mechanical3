using System;
using Mechanical3.Core;

namespace Mechanical3.Loggers
{
    /// <summary>
    /// Logs <see cref="LogEntry"/> messages to the Console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        //// NOTE: here are all the combinations: https://scissortools.wordpress.com/2011/12/01/setting-text-color-in-the-console-output/

        private static void Write( LogEntry entry, ConsoleColor textColor, ConsoleColor backColor = ConsoleColor.Black )
        {
            var prevBackground = Console.BackgroundColor;
            var prevForeground = Console.ForegroundColor;
            Console.BackgroundColor = backColor;
            Console.ForegroundColor = textColor;
            Console.WriteLine($"{entry.Timestamp.ToLocalTime().ToString("s").Replace('T', ' ')} [{entry.Level.ToString()[0]}] {entry.Message}");
            Console.BackgroundColor = prevBackground;
            Console.ForegroundColor = prevForeground;
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
                Write(entry, textColor: ConsoleColor.Gray);
                break;

            case LogLevel.Information:
                Write(entry, textColor: ConsoleColor.Cyan);
                break;

            case LogLevel.Warning:
                Write(entry, textColor: ConsoleColor.Yellow);
                break;

            case LogLevel.Error:
                Write(entry, textColor: ConsoleColor.Red);
                break;

            case LogLevel.Fatal:
                Write(entry, textColor: ConsoleColor.Black, backColor: ConsoleColor.Red);
                break;

            default:
                throw new NotImplementedException().StoreFileLine();
            }
        }
    }
}
