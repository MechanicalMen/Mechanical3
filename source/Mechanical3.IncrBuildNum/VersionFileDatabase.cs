using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mechanical3.IncrBuildNum
{
    public static class VersionFileDatabase
    {
        #region Private Static Fields

        private const string databaseFileName = "versionFiles.json";
        private const int DatabaseFileFormatVersion = 1;

        private const int MaxMutexNameLength = 260;
        private const string MutextNameStart = "Mechanical3.IncrBuildNum ";

        private static readonly string databaseDirectoryPath;

        #endregion

        #region Static Constructor

        static VersionFileDatabase()
        {
            databaseDirectoryPath = Path.GetFullPath(Path.GetDirectoryName(typeof(VersionFileDatabase).Assembly.Location));
        }

        #endregion

        #region Private Static Members

        private static string MutexName
        {
            get
            {
                //// NOTE: each database has it's own mutex

                string name;
                if( MutextNameStart.Length + databaseDirectoryPath.Length <= MaxMutexNameLength )
                {
                    name = MutextNameStart + databaseDirectoryPath;
                }
                else
                {
                    var shortenedPathLength = MaxMutexNameLength - MutextNameStart.Length;
                    var shortenedPath = databaseDirectoryPath.Substring(startIndex: databaseDirectoryPath.Length - shortenedPathLength);
                    name = MutextNameStart + shortenedPath;
                }

                return name
                    .Replace(Path.DirectorySeparatorChar, '_')
                    .Replace(Path.AltDirectorySeparatorChar, '_');
            }
        }

        private static string DatabaseFilePath
        {
            get { return Path.Combine(databaseDirectoryPath, databaseFileName); }
        }

        #endregion

        #region Public Static Methods

        public static void SetCanIncreaseVersion( string versionFilePath, bool newValue, out bool previousValue )
        {
            if( string.IsNullOrEmpty(versionFilePath) )
                throw new ArgumentException("Invalid path!");

            versionFilePath = Path.GetFullPath(versionFilePath);

            using( var mutex = new Mutex(false, MutexName) )
            {
                if( !mutex.WaitOne(TimeSpan.FromSeconds(5)) )
                    throw new Exception("Getting access to version database file timed out!");

                // load database
                HashSet<string> versionFilesThatCanNotBeIncreased = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if( File.Exists(DatabaseFilePath) )
                {
                    var db = JObject.Parse(File.ReadAllText(DatabaseFilePath));
                    int fileVersion = (int)db["formatVersion"];
                    if( fileVersion != DatabaseFileFormatVersion )
                        throw new FormatException($"Unexpected file format! (expected {DatabaseFileFormatVersion}, but found {fileVersion})");

                    var array = (JArray)db["canNotIncrease"];
                    for( int i = 0; i < array.Count; ++i )
                        versionFilesThatCanNotBeIncreased.Add((string)array[i]);
                }

                // determine what the previous value was
                previousValue = true; // version can be increased by default
                foreach( var path in versionFilesThatCanNotBeIncreased )
                {
                    if( string.Equals(path, versionFilePath, StringComparison.OrdinalIgnoreCase) )
                    {
                        // version can not be increased
                        previousValue = false;
                        break;
                    }
                }

                // set new value
                if( newValue )
                    versionFilesThatCanNotBeIncreased.Remove(versionFilePath);
                else
                    versionFilesThatCanNotBeIncreased.Add(versionFilePath);

                // update database
                File.WriteAllText(
                    DatabaseFilePath,
                    new JObject(
                        new JProperty("formatVersion", DatabaseFileFormatVersion),
                        new JProperty("canNotIncrease",
                            new JArray(
                                versionFilesThatCanNotBeIncreased.ToArray()))).ToString(Formatting.Indented),
                    Encoding.UTF8);
            }
            //// NOTE: disposing of the mutex automatically releases it
        }

        #endregion
    }
}
