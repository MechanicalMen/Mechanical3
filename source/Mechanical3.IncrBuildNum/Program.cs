﻿using System;
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

        static void Main( string[] args )
        {
            try
            {
                // get file path
                if( args.Length == 0 )
                    throw new FileNotFoundException("No version file specified!");

                var filePath = args[0];
                if( !File.Exists(filePath) )
                    throw new FileNotFoundException("Version file not found!");

                // parse file
                var obj = JObject.Parse(File.ReadAllText(filePath));

                // get previous values
                var totalBuildCount = (int)obj["totalBuildCount"];
                var versionBuildCount = (int)obj["versionBuildCount"];

                // set new values
                obj["totalBuildCount"] = totalBuildCount + 1;
                obj["versionBuildCount"] = versionBuildCount + 1;
                obj["lastBuildDate"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

                // generate file contents
                File.WriteAllText(filePath, obj.ToString(Formatting.Indented), Encoding.ASCII);
            }
            catch( Exception ex )
            {
                Console.WriteLine();
                Console.WriteLine("Unhandled exception caught:");
                Console.WriteLine(ex.ToString());
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(intercept: true);
            }
        }
    }
}
