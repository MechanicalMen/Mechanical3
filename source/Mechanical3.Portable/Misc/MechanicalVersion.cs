using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Version information about an assembly, independent of the assembly version attributes.
    /// </summary>
    public sealed class MechanicalVersion
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MechanicalVersion"/> class.
        /// </summary>
        /// <param name="assembly">The assembly to load the embedded version file from.</param>
        /// <param name="manifestResourceName">The case-sensitive manifest resource name of the embedded version file.</param>
        public MechanicalVersion( Assembly assembly, string manifestResourceName )
            : this(ReadAll(assembly, manifestResourceName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MechanicalVersion"/> class.
        /// </summary>
        /// <param name="json">The version data to load.</param>
        public MechanicalVersion( string json )
        {
            this.Name = Regex.Match(json, @"\""name\""\s*:\s*\""([^""]*)\""").Groups[1].ToString(); // this will fail if the value contains a double-quote character
            this.Version = Regex.Match(json, @"\""version\""\s*:\s*\""([^""]*)\""").Groups[1].ToString(); // this will fail if the value contains a double-quote character
            this.VersionBuildCount = int.Parse(Regex.Match(json, @"\""versionBuildCount\""\s*:\s*(\d+)").Groups[1].ToString(), NumberStyles.None, CultureInfo.InvariantCulture); // fails if not integer or has leading sign
        }

        private static string ReadAll( Assembly assembly, string manifestResourceName )
        {
            var stream = assembly.GetManifestResourceStream(manifestResourceName);
            if( stream == null )
                throw new ArgumentException($"Could not find \"{manifestResourceName}\" in \"{assembly.FullName}\"!");

            using( var reader = new StreamReader(stream) )
                return reader.ReadToEnd();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the informal name of the assembly.
        /// </summary>
        /// <value>The informal name of the assembly.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the informal version of the assembly.
        /// </summary>
        /// <value>The informal version of the assembly.</value>
        public string Version { get; }

        /// <summary>
        /// Gets the number of times the current version of the assembly was built.
        /// </summary>
        /// <value>The number of times the current version of the assembly was built.</value>
        public int VersionBuildCount { get; }

        #endregion

        #region Static Members

        private static MechanicalVersion portableVersion;

        /// <summary>
        /// Gets the informal version of the portable Mechanical3 assembly.
        /// </summary>
        /// <value>The informal version of the portable Mechanical3 assembly.</value>
        public static MechanicalVersion PortableVersion
        {
            get
            {
                if( object.ReferenceEquals(portableVersion, null) )
                {
                    Interlocked.CompareExchange(
                        ref portableVersion,
                        new MechanicalVersion(typeof(MechanicalVersion).GetTypeInfo().Assembly, "Mechanical3.version.json"),
                        comparand: null);
                }

                return portableVersion;
            }
        }

        #endregion
    }

    //// TODO: parse JSON properly
}
