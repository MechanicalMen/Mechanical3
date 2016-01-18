using System;
using System.Collections.Generic;
using System.IO;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Wraps the specified host directory as an abstract file system.
    /// </summary>
    public class DirectoryFileSystem : IFileSystem
    {
        #region Private Fields

        private readonly string rootDirectoryFullPath;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryFileSystem"/> class
        /// </summary>
        /// <param name="hostDirectoryPath">The path specifying the contents at the root of this abstract file system. If the directory does not exist, it is created.</param>
        public DirectoryFileSystem( string hostDirectoryPath )
        {
            try
            {
                if( !Directory.Exists(hostDirectoryPath) )
                    Directory.CreateDirectory(hostDirectoryPath);

                this.rootDirectoryFullPath = Path.GetFullPath(hostDirectoryPath); // throws if null or empty

                // remove tailing directory separator
                if( this.rootDirectoryFullPath[this.rootDirectoryFullPath.Length - 1] == Path.DirectorySeparatorChar
                 || this.rootDirectoryFullPath[this.rootDirectoryFullPath.Length - 1] == Path.AltDirectorySeparatorChar )
                    this.rootDirectoryFullPath = this.rootDirectoryFullPath.Substring(startIndex: 0, length: this.rootDirectoryFullPath.Length - 1);
            }
            catch( Exception ex )
            {
                ex.StoreFileLine();
                ex.Store(nameof(hostDirectoryPath), hostDirectoryPath);
                throw;
            }
        }

        #endregion

        #region Private Methods

        private string ToRelativeHostPath( FilePath path )
        {
            try
            {
                if( path.NullReference() )
                    return string.Empty;
                else
                    return path.ToString().Replace(FilePath.PathSeparator, Path.DirectorySeparatorChar);
            }
            catch( Exception ex )
            {
                ex.Store(nameof(path), path?.ToString());
                throw;
            }
        }

        private string ToFullHostPath( FilePath path )
        {
            var relativeHostPath = this.ToRelativeHostPath(path);
            var fullHostPath = Path.Combine(this.rootDirectoryFullPath, relativeHostPath);
            return fullHostPath;
        }

        private FilePath FromRelativeHostPath( string hostPath, bool isDirectory )
        {
            try
            {
                string currentPath = hostPath;
                string parentPath, name;
                FilePath path = null;
                while( !currentPath.NullOrEmpty() ) //// while we can find a parent directory
                {
                    parentPath = Path.GetDirectoryName(currentPath);
                    name = Path.GetFileName(currentPath);
                    currentPath = parentPath;

                    if( path.NullReference() )
                        path = isDirectory ? FilePath.FromDirectoryName(name) : FilePath.FromFileName(name);
                    else
                        path = FilePath.FromDirectoryName(name) + path;
                }
                return path;
            }
            catch( Exception ex )
            {
                ex.Store(nameof(hostPath), hostPath);
                throw;
            }
        }

        private void AddNames( List<FilePath> results, FilePath path, Func<string, string[]> getFilesOrDirectories, bool getsDirectories )
        {
            try
            {
                var fullHostPath = this.ToFullHostPath(path);
                var filesOrDirectories = getFilesOrDirectories(fullHostPath);

                string relativeHostPath;
                foreach( var f in filesOrDirectories )
                {
                    relativeHostPath = f.Substring(startIndex: this.rootDirectoryFullPath.Length + 1);
                    results.Add(this.FromRelativeHostPath(relativeHostPath, isDirectory: getsDirectories));
                }
            }
            catch( Exception ex )
            {
                ex.StoreFileLine();
                ex.Store(nameof(path), path);
                ex.Store(nameof(getsDirectories), getsDirectories);
                throw;
            }
        }

        private static void RemoveReadOnlyAttribute( string fileOrDirectory )
        {
            var attributes = File.GetAttributes(fileOrDirectory);
            if( attributes.HasFlag(FileAttributes.ReadOnly) )
            {
                attributes &= ~FileAttributes.ReadOnly;
                File.SetAttributes(fileOrDirectory, attributes);
            }
        }

        private static void RecursivelyDeleteExistingDirectory( string directoryPath )
        {
            foreach( var d in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly) )
                RecursivelyDeleteExistingDirectory(d);

            foreach( var f in Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly) )
            {
                RemoveReadOnlyAttribute(f);
                File.Delete(f);
            }

            RemoveReadOnlyAttribute(directoryPath);
            Directory.Delete(directoryPath);
        }

        #endregion

        #region IFileSystemBase

        /// <summary>
        /// Gets a value indicating whether the ToHostPath method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public bool SupportsToHostPath
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the string the underlying system uses to represent the specified file or directory.
        /// </summary>
        /// <param name="path">The path to the file or directory.</param>
        /// <returns>The string the underlying system uses to represent the specified <paramref name="path"/>.</returns>
        public string ToHostPath( FilePath path )
        {
            return this.ToFullHostPath(path); // handles null
        }

        #endregion

        #region IFileSystemReader

        /// <summary>
        /// Gets the path to the direct children of the specified directory.
        /// Subdirectories are not searched.
        /// </summary>
        /// <param name="directoryPath">The path specifying the directory to list the direct children of; or <c>null</c> to specify the root of this file system.</param>
        /// <returns>The paths of the files and directories found.</returns>
        public FilePath[] GetPaths( FilePath directoryPath = null )
        {
            try
            {
                if( directoryPath.NotNullReference()
                 && !directoryPath.IsDirectory )
                    throw new ArgumentException("Invalid directory path!").StoreFileLine();

                if( directoryPath.NotNullReference()
                 && !Directory.Exists(this.ToFullHostPath(directoryPath)) )
                    throw new FileNotFoundException().StoreFileLine(); // NOTE: a DirectoryNotFound exception would be nicer, but unfortunately it is not supported by the portable library

                var list = new List<FilePath>();
                this.AddNames(list, directoryPath, path => Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly), getsDirectories: false);
                this.AddNames(list, directoryPath, path => Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly), getsDirectories: true);
                return list.ToArray();
            }
            catch( Exception ex )
            {
                ex.Store(nameof(directoryPath), directoryPath);
                throw;
            }
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

                var fullHostPath = this.ToFullHostPath(filePath);
                return new FileStream(fullHostPath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
            get { return true; }
        }

        /// <summary>
        /// Gets the size, in bytes, of the specified file.
        /// </summary>
        /// <param name="filePath">The file to get the size of.</param>
        /// <returns>The size of the specified file in bytes.</returns>
        public long GetFileSize( FilePath filePath )
        {
            try
            {
                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                var fullHostPath = this.ToFullHostPath(filePath);
                return new FileInfo(fullHostPath).Length;
            }
            catch( Exception ex )
            {
                ex.Store(nameof(filePath), filePath);
                throw;
            }
        }

        #endregion

        #region IFileSystemWriter

        /// <summary>
        /// Creates the specified directory (and any directories along the path) should it not exist.
        /// </summary>
        /// <param name="directoryPath">The path specifying the directory to create.</param>
        public void CreateDirectory( FilePath directoryPath )
        {
            try
            {
                if( directoryPath.NullReference()
                 || !directoryPath.IsDirectory )
                    throw new ArgumentException("Invalid directory path!").StoreFileLine();

                var fullHostPath = this.ToFullHostPath(directoryPath);
                Directory.CreateDirectory(fullHostPath);
            }
            catch( Exception ex )
            {
                ex.Store(nameof(directoryPath), directoryPath);
                throw;
            }
        }

        /// <summary>
        /// Deletes the specified file or directory. Does nothing if it does not exist.
        /// </summary>
        /// <param name="path">The path specifying the file or directory to delete.</param>
        public void Delete( FilePath path )
        {
            try
            {
                if( path.NullReference() )
                    throw new ArgumentException("Invalid file or directory path!").StoreFileLine();

                var fullHostPath = this.ToFullHostPath(path);
                if( path.IsDirectory )
                {
                    // delete directory
                    if( Directory.Exists(fullHostPath) )
                        RecursivelyDeleteExistingDirectory(fullHostPath);
                }
                else
                {
                    // delete file
                    if( File.Exists(fullHostPath) )
                    {
                        RemoveReadOnlyAttribute(fullHostPath);
                        File.Delete(fullHostPath);
                    }
                }
            }
            catch( Exception ex )
            {
                ex.Store(nameof(path), path);
                throw;
            }
        }

        /// <summary>
        /// Creates a new empty file, and opens it for writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to create.</param>
        /// <param name="overwriteIfExists"><c>true</c> to overwrite the file if it already exists; or <c>false</c> to throw an exception.</param>
        /// <returns>A <see cref="Stream"/> representing the file.</returns>
        public Stream CreateFile( FilePath filePath, bool overwriteIfExists )
        {
            try
            {
                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                var fullHostPath = this.ToFullHostPath(filePath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullHostPath));
                return new FileStream(fullHostPath, overwriteIfExists ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            }
            catch( Exception ex )
            {
                ex.Store(nameof(filePath), filePath);
                ex.Store(nameof(overwriteIfExists), overwriteIfExists);
                throw;
            }
        }

        #endregion

        #region IFileSystem

        /// <summary>
        /// Gets a value indicating whether the ReadWriteFile method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public bool SupportsReadWriteFile
        {
            get { return true; }
        }

        /// <summary>
        /// Opens an existing file, or creates a new one, for both reading and writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        public Stream ReadWriteFile( FilePath filePath )
        {
            try
            {
                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                var fullHostPath = this.ToFullHostPath(filePath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullHostPath));
                return new FileStream(fullHostPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            }
            catch( Exception ex )
            {
                ex.Store(nameof(filePath), filePath);
                throw;
            }
        }

        #endregion
    }
}
