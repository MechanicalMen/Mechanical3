using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// An initially empty file system, that others can be mounted into.
    /// </summary>
    public class MountFileSystem : DisposableObject, IFileSystem
    {
        #region Private Fields

        private readonly List<KeyValuePair<FilePath, IFileSystem>> fileSystems;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MountFileSystem"/> class.
        /// </summary>
        public MountFileSystem()
        {
            this.fileSystems = new List<KeyValuePair<FilePath, IFileSystem>>();
        }

        #endregion

        #region IDisposableObject

        /// <summary>
        /// Called when the object is being disposed of. Inheritors must call base.OnDispose to be properly disposed.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c>, release both managed and unmanaged resources; otherwise release only the unmanaged resources.</param>
        protected override void OnDispose( bool disposing )
        {
            if( disposing )
            {
                //// dispose-only (i.e. non-finalizable) logic
                //// (managed, disposable resources you own)

                if( this.fileSystems.Count != 0 )
                {
                    foreach( var pair in this.fileSystems )
                    {
                        var asDisposable = pair.Value as IDisposable;
                        if( asDisposable.NotNullReference() )
                            asDisposable.Dispose();
                    }
                    this.fileSystems.Clear();
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region Private Methods

        private bool TryGetMountedFileSystem( FilePath mountedPath, out FilePath mountPath, out FilePath unmountedPath, out IFileSystem fs )
        {
            foreach( var pair in this.fileSystems )
            {
                if( pair.Key.IsAncestorOf(mountedPath)
                 || pair.Key == mountedPath )
                {
                    mountPath = pair.Key;
                    unmountedPath = mountedPath.RemoveAncestor(pair.Key);
                    fs = pair.Value;
                    return true;
                }
            }

            mountPath = null;
            unmountedPath = null;
            fs = null;
            return false;
        }

        private bool TryGetMountedFileSystem( FilePath mountedPath, out FilePath unmountedPath, out IFileSystem fs )
        {
            FilePath mountPath;
            return this.TryGetMountedFileSystem(mountedPath, out mountPath, out unmountedPath, out fs);
        }

        private IFileSystem GetMountedFileSystem( FilePath mountedPath, out FilePath unmountedPath )
        {
            IFileSystem result;
            if( this.TryGetMountedFileSystem(mountedPath, out unmountedPath, out result)
             && unmountedPath.NotNullReference() )
                return result;
            else
                throw new KeyNotFoundException("No mounted file system can handle the specified path!").Store(nameof(mountedPath), mountedPath);
        }

        #endregion

        #region IFileSystemBase

        /// <summary>
        /// Gets a value indicating whether the ToHostPath method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public bool SupportsToHostPath
        {
            get
            {
                this.ThrowIfDisposed();

                foreach( var pair in this.fileSystems )
                {
                    if( !pair.Value.SupportsToHostPath )
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the string the underlying system uses to represent the specified file or directory.
        /// </summary>
        /// <param name="path">The path to the file or directory.</param>
        /// <returns>The string the underlying system uses to represent the specified <paramref name="path"/>.</returns>
        public string ToHostPath( FilePath path )
        {
            if( !this.SupportsToHostPath ) // checks for disposal as well
                throw new NotSupportedException().StoreFileLine();

            if( path.NullReference() )
                throw new ArgumentNullException(nameof(path)).StoreFileLine();

            FilePath unmountedPath;
            var mountedFS = this.GetMountedFileSystem(path, out unmountedPath);
            return mountedFS.ToHostPath(unmountedPath);
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
            try
            {
                this.ThrowIfDisposed();

                if( directoryPath.NullReference() )
                {
                    var results = new HashSet<FilePath>();
                    foreach( var pair in this.fileSystems )
                        results.Add(pair.Key.Root);
                    return results.ToArray();
                }
                else
                {
                    if( !directoryPath.IsDirectory )
                        throw new ArgumentException("Path not a directory!").StoreFileLine();

                    FilePath mountPath;
                    FilePath unmountedPath;
                    IFileSystem fs;
                    if( this.TryGetMountedFileSystem(directoryPath, out mountPath, out unmountedPath, out fs) )
                    {
                        return fs.GetPaths(unmountedPath)?.Select(p => mountPath + p).ToArray();
                    }
                    else
                    {
                        var results = new HashSet<FilePath>();
                        foreach( var pair in this.fileSystems )
                        {
                            var path = directoryPath.GetChildFrom(pair.Key);
                            if( path.NotNullReference() )
                                results.Add(path);
                        }
                        return results.ToArray();
                    }
                }
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
                this.ThrowIfDisposed();

                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                FilePath unmountedPath;
                IFileSystem fs;
                if( !this.TryGetMountedFileSystem(filePath, out unmountedPath, out fs)
                 || unmountedPath.NullReference() )
                    throw new FileNotFoundException().StoreFileLine();
                else
                    return fs.ReadFile(unmountedPath);
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
            get
            {
                this.ThrowIfDisposed();

                foreach( var pair in this.fileSystems )
                {
                    if( !pair.Value.SupportsGetFileSize )
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the size, in bytes, of the specified file.
        /// </summary>
        /// <param name="filePath">The file to get the size of.</param>
        /// <returns>The size of the specified file in bytes.</returns>
        public long GetFileSize( FilePath filePath )
        {
            if( !this.SupportsToHostPath ) // checks for disposal as well
                throw new NotSupportedException().StoreFileLine();

            if( filePath.NullReference()
             || filePath.IsDirectory )
                throw new ArgumentException().Store(nameof(filePath), filePath);

            FilePath unmountedPath;
            var mountedFS = this.GetMountedFileSystem(filePath, out unmountedPath);
            return mountedFS.GetFileSize(unmountedPath);
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
                this.ThrowIfDisposed();

                if( directoryPath.NullReference()
                 || !directoryPath.IsDirectory )
                    throw new ArgumentException("Invalid directory path!").StoreFileLine();

                FilePath unmountedPath;
                IFileSystem fs;
                if( !this.TryGetMountedFileSystem(directoryPath, out unmountedPath, out fs)
                 || unmountedPath.NullReference() )
                    throw new InvalidOperationException("The only way to create directories is via mounting or mounted file systems!").StoreFileLine();
                else
                    fs.CreateDirectory(unmountedPath);
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
                this.ThrowIfDisposed();

                if( path.NullReference() )
                    throw new ArgumentException().StoreFileLine();

                FilePath unmountedPath;
                IFileSystem fs;
                if( !this.TryGetMountedFileSystem(path, out unmountedPath, out fs)
                 || unmountedPath.NullReference() )
                    throw new InvalidOperationException("The only way to delete is via mounted file systems!").StoreFileLine();
                else
                    fs.Delete(unmountedPath);
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
                this.ThrowIfDisposed();

                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                FilePath unmountedPath;
                IFileSystem fs;
                if( !this.TryGetMountedFileSystem(filePath, out unmountedPath, out fs)
                 || unmountedPath.NullReference() )
                    throw new InvalidOperationException("The only way to create files is via mounted file systems!").StoreFileLine();
                else
                    return fs.CreateFile(unmountedPath, overwriteIfExists);
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
            get
            {
                this.ThrowIfDisposed();

                foreach( var pair in this.fileSystems )
                {
                    if( !pair.Value.SupportsReadWriteFile )
                        return false;
                }
                return true;
            }
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
                if( !this.SupportsToHostPath ) // checks for disposal as well
                    throw new NotSupportedException().StoreFileLine();

                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                FilePath unmountedPath;
                IFileSystem fs;
                if( !this.TryGetMountedFileSystem(filePath, out unmountedPath, out fs)
                 || unmountedPath.NullReference() )
                    throw new InvalidOperationException("The only way to read-write files is via mounted file systems!").StoreFileLine();
                else
                    return fs.ReadWriteFile(unmountedPath);
            }
            catch( Exception ex )
            {
                ex.Store(nameof(filePath), filePath);
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Mounts a file system under the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to mount the file system to.</param>
        /// <param name="fs">The file system to mount.</param>
        public void Mount( FilePath path, IFileSystem fs )
        {
            if( path.NullReference() )
                throw new ArgumentNullException(nameof(path)).StoreFileLine();

            if( fs.NullReference() )
                throw new ArgumentNullException(nameof(fs)).StoreFileLine();

            if( !path.IsDirectory )
                throw new ArgumentException("Invalid path: files are not mountable!").Store(nameof(path), path);

            foreach( var pair in this.fileSystems )
            {
                if( path == pair.Key
                 || path.IsAncestorOf(pair.Key) )
                    throw new ArgumentException("Invalid path: the same as, or an ancestor of an already mounted path!").Store(nameof(path), path).Store("mountedPath", pair.Key);
            }

            this.fileSystems.Add(new KeyValuePair<FilePath, IFileSystem>(path, fs));
        }

        #endregion
    }
}
