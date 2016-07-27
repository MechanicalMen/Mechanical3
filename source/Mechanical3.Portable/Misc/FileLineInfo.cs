using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Represents a line in a source file.
    /// Only the file name is stored (instead of the full path).
    /// </summary>
    public struct FileLineInfo
    {
        //// NOTE: Unfortunately parsing stack frames is very hard, since they are localized, e.g.:
        ////        - english: "  at <member> in <file>:line <line>"
        ////        - hungarian: "  a következő helyen: <member> hely: <file>, sor: <line>"
        ////
        ////       The situation is made worse by some of the strangeness turning up in Xamarin stack traces:
        ////       "  at (wrapper remoting-invoke-with-check) System.Net.NetworkInformation.Ping:Send (string,int)"
        ////
        ////       Using System.Diagnostics.StackTrace and StackFrame would be nice, but as of the time of writing this
        ////       neither the portable libraries, nor the .NET Platform Standard seem to support them.

        #region Constructors

        //// NOTE: Unfortunately the default constructor takes precedence over other applicable constructors,
        ////       so we use a static method instead, and remove default values from the constructor below.

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLineInfo"/> struct.
        /// </summary>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        public FileLineInfo( string file, string member, int line )
        {
            if( member.NullOrWhiteSpace() )
                throw NamedArgumentException.Store(nameof(member), member);

            if( line < 0 )
                throw new ArgumentOutOfRangeException(nameof(line)).Store(nameof(line), line);

            this.File = file.NullOrWhiteSpace() ? null : ToFileName(file)?.Trim();
            this.Member = member.Trim();
            this.Line = line;
        }

        /// <summary>
        /// Creates a new <see cref="FileLineInfo"/> instance.
        /// Default values are replaced with the caller information by the compiler.
        /// </summary>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>A new <see cref="FileLineInfo"/> instance.</returns>
        public static FileLineInfo Current(
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            return new FileLineInfo(file, member, line);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the source file that contains the caller.
        /// </summary>
        /// <value>The source file that contains the caller.</value>
        public string File { get; }

        /// <summary>
        /// Gets the name of the method or property, that the source code line implements.
        /// </summary>
        /// <value>The name of the method or property, that the source code line implements.</value>
        public string Member { get; }

        /// <summary>
        /// Gets the line in the source file that this instance points to.
        /// </summary>
        /// <value>The line in the source file that this instance points to.</value>
        public int Line { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Appends the string representation of this instance to the specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to append to.</param>
        public void ToString( StringBuilder sb )
        {
            if( sb.NullReference() )
                throw new ArgumentNullException(nameof(sb)).StoreFileLine();

            sb.Append("  at ");
            sb.Append(this.Member);
            if( this.File.NotNullReference() )
            {
                sb.Append(" in ");
                sb.Append(this.File);
                sb.Append(":line ");
                sb.Append(this.Line.ToString("D", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Returns a stack frame string that represents this instance.
        /// </summary>
        /// <returns>A stack frame string that represents this instance.</returns>
        public override string ToString()
        {
            const int InitialCapacity = 32 + 64 + 64; // 64 characters for file and member names, 32 for everything else
            var sb = new StringBuilder(InitialCapacity);
            this.ToString(sb);
            return sb.ToString();
        }

        #endregion

        #region Private Static Members

        private static readonly char[] DirectorySeparatorChars = new char[] { '\\', '/' };

        private static string ToFileName( string filePath )
        {
            //// let's not expose the developer's directory structure!
            //// (may contain sensitive information, like user names, ... etc.)

            if( !filePath.NullOrWhiteSpace() )
            {
                // System.IO.Path expects the directory separators
                // of the platform this code is being run on. But code may
                // have been compiled on a different platform! (e.g. building an app on Windows, and running it on Android)
                int directorySeparatorAt = filePath.LastIndexOfAny(DirectorySeparatorChars);
                if( directorySeparatorAt != -1 )
                {
                    filePath = filePath.Substring(startIndex: directorySeparatorAt + 1);
                }
                else
                {
                    //// no directory separator?
                    //// only if this string was not (directly) generated by the compiler!
                }
            }

            return filePath;
        }

        #endregion
    }
}
