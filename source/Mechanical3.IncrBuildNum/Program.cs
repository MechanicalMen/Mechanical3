using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mechanical3.IncrBuildNum
{
    class Program
    {
        /* Version file format:
        {
            "name": "Mechanical3 (Portable)",
	        "version": "1.2", // the informal version, independent of the assembly version
	        "totalBuildCount": 123, // the total number of builds
	        "versionBuildCount": 5, // the number of builds since the last version increase (set manually to 0 when changing "version")
	        "lastBuildDate": "2015-12-05T11:47:44.8260740Z" // when the last build happened
	        "gitCommit": "6692ba6f7f091fd687b0780a96375fbd4166acf5" // the currently checked out git commit ID
        }
        */

        /* What it does:
         * 
         * Pre-Build Action (--pre-build):
         *  - checks whether the version can be increased for the specified version file (true by default)
         *  - increases the version if it is allowed
         *  - marks the specified version file, to disallow further version increases to it
         * 
         * Post-Build Action (possibly only when it changes the generated output) (--post-build):
         *  - leave the specified version file alone
         *    (since changing the embedded version file, will necessarily change the generated project output)
         *  - mark the specified version file to allow it's version to be increased
         *    (this of course is not done in the version file)
         */

        private static int Main( string[] args )
        {
            string originalJson = null;
            try
            {
                // get command line arguments
                if( args.Length < 2 || args.Length > 3 )
                    throw new ArgumentException("Exactly 2 or 3 arguments must be specified!");

                var versionFilePath = args[0];
                if( !File.Exists(versionFilePath) )
                    throw new FileNotFoundException("Version file not found!");

                bool HasCommandLineParameter( string parameter )
                {
                    return (args.Length >= 2) && string.Equals(args[1], parameter, StringComparison.Ordinal)
                        || (args.Length >= 3) && string.Equals(args[2], parameter, StringComparison.Ordinal);
                }

                bool isPreBuildAction = default(bool);
                bool preserveVersionNumbers = false;
                if( HasCommandLineParameter("--pre-build") )
                    isPreBuildAction = true;
                else if( HasCommandLineParameter("--post-build") )
                    isPreBuildAction = false;
                else if( HasCommandLineParameter("--preserve-version") )
                    preserveVersionNumbers = true;
                else
                    throw new ArgumentException($"Second parameter could not be recognized: \"{args[1]}\"");

                if( isPreBuildAction )
                {
                    //// NOTE: this is a Pre-Build Action:
                    ////       the embedded version file may be changed.

                    // parse file
                    originalJson = File.ReadAllText(versionFilePath);
                    var obj = JObject.Parse(originalJson);

                    // check if version numbers can be increased
                    bool increasingWasAllowed;
                    VersionFileDatabase.SetCanIncreaseVersion(versionFilePath, false, out increasingWasAllowed);
                    if( increasingWasAllowed
                     && !preserveVersionNumbers )
                    {
                        var totalBuildCount = (int)obj["totalBuildCount"];
                        var versionBuildCount = (int)obj["versionBuildCount"];
                        obj["totalBuildCount"] = totalBuildCount + 1;
                        obj["versionBuildCount"] = versionBuildCount + 1;
                    }

                    // always update meta data
                    obj["lastBuildDate"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    obj["gitCommit"] = FindGitCommitID(versionFilePath);

                    // generate file contents
                    File.WriteAllText(versionFilePath, obj.ToString(Formatting.Indented), Encoding.ASCII);
                }
                else
                {
                    //// NOTE: this is a Post-Build Action:
                    ////       the embedded version file must not be changed!

                    // allow increasing the version next pre-build
                    bool increasingWasAllowed;
                    VersionFileDatabase.SetCanIncreaseVersion(versionFilePath, true, out increasingWasAllowed);
                }

                // return success
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

        private static string FindGitCommitID( string versionFilePath )
        {
            string repositoryPath = Repository.Discover(Path.GetDirectoryName(versionFilePath));
            if( string.IsNullOrEmpty(repositoryPath) )
                return null;

            using( var repository = new Repository(repositoryPath) )
            {
                return repository.Head?.Tip?.Sha;
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
