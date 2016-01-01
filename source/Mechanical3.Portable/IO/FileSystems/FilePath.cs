using System;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /* File name:
     *  - may not be null or empty
     *  - may not contain path separator
     *  - may not exceed maximum length
     *  - case-sensitive (!)
     *
     * File path:
     *  - contains one or more separated file names
     *  - a directory path ends with the separator
     *  - starts with a file name (no linux style root directory)
     */

    /// <summary>
    /// A platform independent file or directory path, used by the abstract file systems.
    /// </summary>
    public class FilePath
    {
        #region Private Fields

        private static readonly char[] ExtensionChars = new char[] { '.', PathSeparator };

        private readonly string path;

        #endregion

        #region Constructors

        private FilePath( string path )
        {
            this.path = path;
        }

        /// <summary>
        /// Creates a new <see cref="FilePath"/> instance.
        /// </summary>
        /// <param name="abstractPath">The path to the abstract file or directory to represent.</param>
        /// <returns>The new <see cref="FilePath"/> instance.</returns>
        public static FilePath From( string abstractPath )
        {
            if( !IsValidPath(abstractPath) )
                throw new ArgumentException("Invalid file or directory path!").Store(nameof(abstractPath), abstractPath);

            return new FilePath(abstractPath);
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets a value indicating whether this path instance points to an abstract directory.
        /// </summary>
        /// <value><c>true</c> if this path instance points to an abstract directory; <c>false</c> if it points to a file.</value>
        public bool IsDirectory
        {
            get { return this.path[this.path.Length - 1] == PathSeparator; }
        }

        /// <summary>
        /// Gets the name of the file or directory.
        /// </summary>
        /// <value>The name of the file or directory.</value>
        public string Name
        {
            get
            {
                int lastCharIndex = this.IsDirectory ? this.path.Length - 2 : this.path.Length - 1;
                int separatorIndex = this.path.LastIndexOf(PathSeparator, lastCharIndex);
                if( separatorIndex != -1 )
                    return this.path.Substring(startIndex: separatorIndex + 1, length: lastCharIndex - separatorIndex);
                else
                    return this.path.Substring(startIndex: 0, length: lastCharIndex + 1);
            }
        }

        /// <summary>
        /// Gets the parent directory, if there is one.
        /// </summary>
        /// <value>The parent directory, or <c>null</c> if there isn't one.</value>
        public FilePath Parent
        {
            get
            {
                int lastCharIndex = this.IsDirectory ? this.path.Length - 2 : this.path.Length - 1;
                int separatorIndex = this.path.LastIndexOf(PathSeparator, lastCharIndex);
                if( separatorIndex != -1 )
                    return new FilePath(this.path.Substring(startIndex: 0, length: separatorIndex + 1));
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets the file extension.
        /// Returns <see cref="string.Empty"/> if there isn't one, or this is a directory.
        /// </summary>
        public string Extension
        {
            get
            {
                if( !this.IsDirectory )
                {
                    int index = this.path.LastIndexOfAny(ExtensionChars);
                    if( index != -1
                     && this.path[index] == '.' )
                    {
                        return this.path.Substring(index, this.path.Length - index);
                    }
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the name of the file, without it's extension.
        /// Always returns the whole name for directories.
        /// </summary>
        public string NameWithoutExtension
        {
            get
            {
                var name = this.Name;
                if( !this.IsDirectory )
                {
                    int index = name.LastIndexOf('.');
                    if( index != -1 )
                        return name.Substring(startIndex: 0, length: index);
                }
                return name;
            }
        }

        /// <summary>
        /// Gets the string representation of this abstract path.
        /// </summary>
        /// <returns>The string representation of this abstract path.</returns>
        public override string ToString()
        {
            return this.path;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Combines two paths.
        /// </summary>
        /// <param name="path1">The first path to combine.</param>
        /// <param name="path2">The second path to combine.</param>
        /// <returns>The combined path.</returns>
        public static FilePath operator +( FilePath path1, FilePath path2 )
        {
            if( path1.NullReference()
             || path2.NullReference() )
                throw new NullReferenceException("Could not combine file paths!").StoreFileLine();

            if( !path1.IsDirectory )
                throw new InvalidOperationException("A directory is required to combine paths!").StoreFileLine();

            return new FilePath(path1.ToString() + path2.ToString());
        }

        #endregion

        #region Static Members

        /// <summary>
        /// The character that separates the file names in a path from one another.
        /// </summary>
        public static readonly char PathSeparator = '/';

        /// <summary>
        /// The maximum length of a file name or path.
        /// </summary>
        public static readonly int MaxLength = 255;
        //// based on: http://stackoverflow.com/questions/265769/maximum-filename-length-in-ntfs-windows-xp-and-windows-vista
        //// NOTE: It is still very much possible to exceed the maximum path length on windows,
        ////       since there is no restriction on what parent directory this FilePath can be applied to.
        ////       This is mainly here to catch the cases that are clearly wrong (e.g. a length of 1000).

        /// <summary>
        /// Determines whether the specified abstract file or directory name is valid.
        /// </summary>
        /// <param name="fileName">The abstract file or directory name to check.</param>
        /// <returns><c>true</c> if the specified name is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValidName( string fileName )
        {
            if( fileName.NullReference() )
                return false;

            if( fileName.Length == 0
             || fileName.Length > MaxLength )
                return false;

            if( fileName.IndexOf(PathSeparator) != -1 )
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether the specified abstract file or directory path is valid.
        /// </summary>
        /// <param name="filePath">The abstract file or directory path to check.</param>
        /// <returns><c>true</c> if the specified path is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValidPath( string filePath )
        {
            if( filePath.NullReference() )
                return false;

            if( filePath.Length == 0
             || filePath.Length > MaxLength )
                return false;

            // check each name
            int startIndex = 0;
            int separatorAt;
            while( (separatorAt = filePath.IndexOf(PathSeparator, startIndex)) != -1 )
            {
                // empty name?
                if( separatorAt - startIndex <= 0 )
                    return false;

                startIndex = separatorAt + 1;

                // directory marker?
                if( startIndex == filePath.Length )
                    return true;
            }

            // there is still the last name to check
            return filePath.Length - startIndex > 0;
        }

        #endregion
    }
}
