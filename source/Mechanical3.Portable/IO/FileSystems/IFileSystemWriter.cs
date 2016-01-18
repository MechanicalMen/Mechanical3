using System;
using System.IO;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Represents an abstract, writable file system.
    /// </summary>
    public interface IFileSystemWriter : IFileSystemBase
    {
        /// <summary>
        /// Creates the specified directory (and any directories along the path) should it not exist.
        /// </summary>
        /// <param name="directoryPath">The path specifying the directory to create.</param>
        void CreateDirectory( FilePath directoryPath );

        /// <summary>
        /// Deletes the specified file or directory. Does nothing if it does not exist.
        /// </summary>
        /// <param name="path">The path specifying the file or directory to delete.</param>
        void Delete( FilePath path );

        /// <summary>
        /// Creates a new empty file, and opens it for writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to create.</param>
        /// <param name="overwriteIfExists"><c>true</c> to overwrite the file if it already exists; or <c>false</c> to throw an exception.</param>
        /// <returns>A <see cref="Stream"/> representing the file.</returns>
        Stream CreateFile( FilePath filePath, bool overwriteIfExists );
    }

    /// <content>
    /// Methods extending the <see cref="IFileSystemWriter"/> interface.
    /// </content>
    public static partial class FileSystemExtensions
    {
        #region DeleteAllFrom

        /// <summary>
        /// Deletes all content from the specified directory.
        /// The directory itself is not removed.
        /// Does nothing if the directory does not exist.
        /// </summary>
        /// <param name="fileSystem">The file system to query.</param>
        /// <param name="directoryPath">The path specifying the directory to delete all content from; or <c>null</c> to specify the root of this file system.</param>
        public static void DeleteAllFrom( this IFileSystem fileSystem, FilePath directoryPath = null )
        {
            if( fileSystem.NullReference() )
                throw new ArgumentNullException(nameof(fileSystem)).StoreFileLine();

            if( directoryPath.NullReference()
             || fileSystem.Exists(directoryPath) )
            {
                var entries = fileSystem.GetPaths(directoryPath);
                foreach( var entry in entries )
                    fileSystem.Delete(entry);
            }
        }

        #endregion
    }
}
