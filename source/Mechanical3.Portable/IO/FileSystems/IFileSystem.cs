using System.IO;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Represents an abstract file system, that's both readable and writable.
    /// </summary>
    public interface IFileSystem : IFileSystemReader, IFileSystemWriter
    {
        /// <summary>
        /// Gets a value indicating whether the ReadWriteFile method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        bool SupportsReadWriteFile { get; }

        /// <summary>
        /// Opens an existing file, or creates a new one, for both reading and writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        Stream ReadWriteFile( FilePath filePath );
    }
}
