using System;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Represents file system related functionality, that is
    /// not directly reading or writing related.
    /// </summary>
    public interface IFileSystemBase
    {
        /// <summary>
        /// Gets a value indicating whether the ToHostPath method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        bool SupportsToHostPath { get; }

        /// <summary>
        /// Gets the string the underlying system uses to represent the specified file or directory.
        /// </summary>
        /// <param name="path">The path to the file or directory.</param>
        /// <returns>The string the underlying system uses to represent the specified <paramref name="path"/>.</returns>
        string ToHostPath( FilePath path );
    }
}
