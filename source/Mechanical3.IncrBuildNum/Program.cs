using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mechanical3.IncrBuildNum
{
    class Program
    {
        /*
        {
            "name": "Mechanical3 (Portable)",
	        "version": "1.2", // the informal version, independent of the assembly version
	        "totalBuildCount": 123, // the total number of builds
	        "versionBuildCount": 5, // the number of builds since the last version increase (set manually to 0 when changing "version")
	        "lastBuildDate": "2015-12-05T11:47:44.8260740Z" // when the last build happened
        }
        */

        private static int Main( string[] args )
        {
            string originalJson = null;
            try
            {
                // get file path
                if( args.Length == 0 )
                    throw new FileNotFoundException("No version file specified!");

                var filePath = args[0];
                if( !File.Exists(filePath) )
                    throw new FileNotFoundException("Version file not found!");

                // parse file
                originalJson = File.ReadAllText(filePath);
                var obj = JObject.Parse(originalJson);

                // get previous values
                var totalBuildCount = (int)obj["totalBuildCount"];
                var versionBuildCount = (int)obj["versionBuildCount"];

                // set new values
                obj["totalBuildCount"] = totalBuildCount + 1;
                obj["versionBuildCount"] = versionBuildCount + 1;
                obj["lastBuildDate"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

                // generate file contents
                File.WriteAllText(filePath, obj.ToString(Formatting.Indented), Encoding.ASCII);
                return 0;
            }
            catch( Exception ex )
            {
                // display error
                IgnoreException(() => ConsoleWindow.Show());
                Console.WriteLine();
                Console.WriteLine("Unhandled exception caught:");
                Console.WriteLine(ex.ToString());
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");

                // log error
                var sb = new StringBuilder();
                sb.AppendLine("Arguments:");
                foreach( var arg in args )
                    sb.AppendLine(arg);
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("Original version file contents:");
                sb.AppendLine(originalJson);
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("Unhandled exception:");
                sb.Append(ex.ToString());
                IgnoreException(() => Log.AppendLine(sb.ToString()));

                // try to wait for user
                IgnoreException(() => Console.ReadKey(intercept: true)); // throws in post build events, see: http://stackoverflow.com/a/22554678
                return 1;
            }
        }

        private static void IgnoreException( Action action )
        {
            if( action == null )
                return;

            try
            {
                action();
            }
            catch( Exception ex )
            {
                if( Debugger.IsAttached )
                {
                    string strex = ex.ToString();
                    strex.ToString(); // no longer an unused variable
                    Debugger.Break();
                }
            }
        }
    }
}
