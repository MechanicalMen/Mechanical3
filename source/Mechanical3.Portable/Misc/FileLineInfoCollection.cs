using System.Collections.Generic;
using System.IO;
using System.Text;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// A collection of <see cref="FileLineInfo"/> instances with some minor extra functionality.
    /// Should not be needed in most cases.
    /// </summary>
    public class FileLineInfoCollection : List<FileLineInfo>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLineInfoCollection"/> class.
        /// </summary>
        public FileLineInfoCollection()
            : base()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the string representation of this instance.
        /// </summary>
        /// <returns>The string representation of this instance.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach( var info in this )
            {
                if( sb.Length != 0 )
                    sb.Append('\n');

                sb.Append(info.ToString());
            }
            return sb.ToString();
        }

        #endregion

        #region Static Members

        /// <summary>
        /// Parses the specified string.
        /// </summary>
        /// <param name="str">The string to parse.</param>
        /// <returns>A new <see cref="FileLineInfoCollection"/> instance.</returns>
        public static FileLineInfoCollection Parse( string str )
        {
            if( string.IsNullOrEmpty(str) )
                return new FileLineInfoCollection();

            var result = new FileLineInfoCollection();
            using( var reader = new StringReader(str) )
            {
                string line;
                while( (line = reader.ReadLine()).NotNullReference() )
                    result.Add(FileLineInfo.Parse(line));
            }
            return result;
        }

        #endregion
    }
}
