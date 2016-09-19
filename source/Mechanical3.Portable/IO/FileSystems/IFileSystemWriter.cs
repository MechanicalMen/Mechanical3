using System.IO;

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

        /// <summary>
        /// Creates a new file from the content of the specified stream.
        /// The stream being copied will NOT be closed at the end of the method.
        /// </summary>
        /// <param name="filePath">The path specifying the file to create.</param>
        /// <param name="overwriteIfExists"><c>true</c> to overwrite the file if it already exists; or <c>false</c> to throw an exception.</param>
        /// <param name="streamToCopy">The <see cref="Stream"/> to copy the content of (from the current position, until the end of the stream).</param>
        void CreateFile( FilePath filePath, bool overwriteIfExists, Stream streamToCopy );
    }

    /// <content>
    /// Methods extending the <see cref="IFileSystemWriter"/> interface.
    /// </content>
    public static partial class FileSystemExtensions
    {
    }
}
