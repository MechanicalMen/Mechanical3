using System;
using System.IO;
using System.Linq;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Wraps the subdirectory of a file system, as the root of a new file system.
    /// Does not take ownership of the wrapped file system (it will have to be disposed of manually).
    /// </summary>
    public class SubdirectoryFileSystem : IFileSystem
    {
        #region Private Fields

        private readonly IFileSystem fileSystem;
        private readonly FilePath rootPath;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SubdirectoryFileSystem"/> class.
        /// </summary>
        /// <param name="fileSys">The <see cref="IFileSystem"/> to wrap a subdirectory of.</param>
        /// <param name="subdirectoryPath">The path to the directory in <paramref name="fileSys"/> to be wrapped. The directory does not have to exist beforehand.</param>
        public SubdirectoryFileSystem( IFileSystem fileSys, FilePath subdirectoryPath )
        {
            if( fileSys.NullReference() )
                throw new ArgumentNullException(nameof(fileSys)).StoreFileLine();

            if( subdirectoryPath.NullReference()
             || !subdirectoryPath.IsDirectory )
                throw new ArgumentException("Invalid subdirectory path!").Store(nameof(subdirectoryPath), subdirectoryPath);

            this.fileSystem = fileSys;
            this.rootPath = subdirectoryPath;
        }

        #endregion

        #region Private Methods

        private FilePath ToParentPath( FilePath subdirPath )
        {
            if( subdirPath.NullReference() )
                return this.rootPath;
            else
                return this.rootPath + subdirPath;
        }

        private FilePath ToSubdirPath( FilePath parentPath )
        {
            return parentPath.RemoveAncestor(this.rootPath);
        }

        #endregion

        #region IFileSystemBase

        /// <summary>
        /// Gets a value indicating whether the ToHostPath method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public bool SupportsToHostPath
        {
            get { return this.fileSystem.SupportsToHostPath; }
        }

        /// <summary>
        /// Gets the string the underlying system uses to represent the specified file or directory.
        /// </summary>
        /// <param name="path">The path to the file or directory.</param>
        /// <returns>The string the underlying system uses to represent the specified <paramref name="path"/>.</returns>
        public string ToHostPath( FilePath path )
        {
            if( path.NullReference() )
                throw new ArgumentNullException(nameof(path)).StoreFileLine();

            return this.fileSystem.ToHostPath(this.ToParentPath(path));
        }

        #endregion

        #region IFileSystemReader

        /// <summary>
        /// Gets the paths to the direct children of the specified directory.
        /// Subdirectories are not searched.
        /// </summary>
        /// <param name="directoryPath">The path specifying the directory to list the direct children of; or <c>null</c> to specify the root of this file system.</param>
        /// <returns>The paths of the files and directories found.</returns>
        public FilePath[] GetPaths( FilePath directoryPath = null )
        {
            if( directoryPath.NotNullReference()
             && !directoryPath.IsDirectory )
                throw new ArgumentException("Invalid directory path!").StoreFileLine();

            return this.fileSystem.GetPaths(this.ToParentPath(directoryPath)).Select(p => this.ToSubdirPath(p)).ToArray();
        }

        /// <summary>
        /// Opens the specified file for reading.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        public Stream ReadFile( FilePath filePath )
        {
            try
            {
                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                return this.fileSystem.ReadFile(this.ToParentPath(filePath));
            }
            catch( Exception ex )
            {
                ex.Store(nameof(filePath), filePath);
                throw;
            }
        }


        /// <summary>
        /// Gets a value indicating whether the GetFileSize method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public bool SupportsGetFileSize
        {
            get { return this.fileSystem.SupportsGetFileSize; }
        }

        /// <summary>
        /// Gets the size, in bytes, of the specified file.
        /// </summary>
        /// <param name="filePath">The file to get the size of.</param>
        /// <returns>The size of the specified file in bytes.</returns>
        public long GetFileSize( FilePath filePath )
        {
            if( filePath.NullReference()
             || filePath.IsDirectory )
                throw new ArgumentException("Invalid file path!").StoreFileLine();

            return this.fileSystem.GetFileSize(this.ToParentPath(filePath));
        }

        #endregion

        #region IFileSystemWriter

        /// <summary>
        /// Creates the specified directory (and any directories along the path) should it not exist.
        /// </summary>
        /// <param name="directoryPath">The path specifying the directory to create.</param>
        public void CreateDirectory( FilePath directoryPath )
        {
            if( directoryPath.NullReference()
             || !directoryPath.IsDirectory )
                throw new ArgumentException("Invalid directory path!").StoreFileLine();

            this.fileSystem.CreateDirectory(this.ToParentPath(directoryPath));
        }

        /// <summary>
        /// Deletes the specified file or directory. Does nothing if it does not exist.
        /// </summary>
        /// <param name="path">The path specifying the file or directory to delete.</param>
        public void Delete( FilePath path )
        {
            if( path.NullReference() )
                throw new ArgumentException().StoreFileLine();

            this.fileSystem.Delete(this.ToParentPath(path));
        }

        /// <summary>
        /// Creates a new empty file, and opens it for writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to create.</param>
        /// <param name="overwriteIfExists"><c>true</c> to overwrite the file if it already exists; or <c>false</c> to throw an exception.</param>
        /// <returns>A <see cref="Stream"/> representing the file.</returns>
        public Stream CreateFile( FilePath filePath, bool overwriteIfExists )
        {
            if( filePath.NullReference()
             || filePath.IsDirectory )
                throw new ArgumentException("Invalid file path!").StoreFileLine();

            return this.fileSystem.CreateFile(this.ToParentPath(filePath), overwriteIfExists);
        }

        #endregion

        #region IFileSystem

        /// <summary>
        /// Gets a value indicating whether the ReadWriteFile method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public bool SupportsReadWriteFile
        {
            get { return this.SupportsReadWriteFile; }
        }

        /// <summary>
        /// Opens an existing file, or creates a new one, for both reading and writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        public Stream ReadWriteFile( FilePath filePath )
        {
            if( filePath.NullReference()
             || filePath.IsDirectory )
                throw new ArgumentException("Invalid file path!").StoreFileLine();

            return this.fileSystem.ReadWriteFile(this.ToParentPath(filePath));
        }

        #endregion
    }
}
