using System;
using System.IO;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Represents an abstract, readable file system.
    /// </summary>
    public interface IFileSystemReader : IFileSystemBase
    {
        /// <summary>
        /// Gets the paths to the direct children of the specified directory.
        /// Subdirectories are not searched.
        /// </summary>
        /// <param name="directoryPath">The path specifying the directory to list the direct children of; or <c>null</c> to specify the root of this file system.</param>
        /// <returns>The paths of the files and directories found.</returns>
        FilePath[] GetPaths( FilePath directoryPath = null );

        /// <summary>
        /// Opens the specified file for reading.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        Stream ReadFile( FilePath filePath );


        /// <summary>
        /// Gets a value indicating whether the GetFileSize method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        bool SupportsGetFileSize { get; }

        /// <summary>
        /// Gets the size, in bytes, of the specified file.
        /// </summary>
        /// <param name="filePath">The file to get the size of.</param>
        /// <returns>The size of the specified file in bytes.</returns>
        long GetFileSize( FilePath filePath );
    }

    /// <content>
    /// Methods extending the <see cref="IFileSystemReader"/> interface.
    /// </content>
    public static partial class FileSystemExtensions
    {
        #region Exists

        /// <summary>
        /// Determines whether the specifies file or directory exists.
        /// </summary>
        /// <param name="fileSystem">The file system to query.</param>
        /// <param name="path">The path specifying the file or directory to search for.</param>
        /// <returns><c>true</c> if the file or directory exists; otherwise, <c>false</c>.</returns>
        public static bool Exists( this IFileSystem fileSystem, FilePath path )
        {
            if( fileSystem.NullReference() )
                throw new ArgumentNullException(nameof(fileSystem)).StoreFileLine();

            if( path.NullReference() )
                throw new ArgumentNullException(nameof(path)).StoreFileLine();

            var parentDirectory = path.Parent; // may be null
            FilePath[] entries;
            try
            {
                entries = fileSystem.GetPaths(parentDirectory);
            }
            catch( FileNotFoundException )
            {
                // directory not found
                return false;
            }
            foreach( var entry in entries )
            {
                if( entry == path )
                    return true;
            }

            return false;
        }

        #endregion
    }
}
