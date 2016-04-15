using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Mechanical3.IncrBuildNum
{
    /// <summary>
    /// Extremely basic logging.
    /// </summary>
    public static class Log
    {
        private static string filePath = null;
        private static string FilePath
        {
            get
            {
                if( filePath == null )
                {
                    // generate name
                    var logdir = Path.Combine(Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)), "logs");
                    var nameWithoutExt = DateTime.UtcNow.ToString("yyyy-MM-ddThh_mm_ss", CultureInfo.InvariantCulture);
                    filePath = Path.Combine(logdir, nameWithoutExt + ".txt");

                    // make sure it's unique
                    int i = 2;
                    while( File.Exists(filePath) )
                    {
                        filePath = Path.Combine(logdir, nameWithoutExt + "." + i.ToString(CultureInfo.InvariantCulture) + ".txt");
                        ++i;
                    }

                    // make sure the directory exists
                    var dir = Path.GetDirectoryName(filePath);
                    if( !Directory.Exists(dir) )
                        Directory.CreateDirectory(dir);
                }
                return filePath;
            }
        }

        /// <summary>
        /// Appends the specified string, followed by the line terminator, to the unique log file of the process.
        /// </summary>
        /// <param name="str">The <see cref="string"/> content to add.</param>
        public static void AppendLine( string str = null )
        {
            str = str != null ? (str + Environment.NewLine) : Environment.NewLine;
            File.AppendAllText(FilePath, str);
        }
    }
}
