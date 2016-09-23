using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Implements <see cref="IFileSystem"/>, by keeping all data in memory.
    /// All operations should be thread-safe, but all streams MUST
    /// be closed manually.
    /// </summary>
    public class MemoryFileSystem : DisposableObject, IFileSystem
    {
        /* NOTE: We are keeping the contents of files in MemoryStream instances.
                 However those, once disposed, throw exceptions when accessed.
                 Therefore we wrap them, and only dispose them, when the
                 file system is released.
         */

        //// NOTE: Access to entries and each file must be synchronous.
        ////       This still makes it possible to have multiple streams open at the same time.

        #region EchoStream

        private class EchoStream : Stream
        {
            private Entry entry;
            private Stream wrappedStream;

            private EchoStream( Stream stream, Entry entry )
            {
                if( stream.NullReference() )
                    throw new ArgumentNullException(nameof(stream)).StoreFileLine();

                if( entry.NullReference() )
                    throw new ArgumentNullException(nameof(entry)).StoreFileLine();

                this.entry = entry;
                this.wrappedStream = stream;
            }

            internal static EchoStream WrapOpenFile( Stream stream, Entry entry )
            {
                return new EchoStream(stream, entry);
            }

            protected override void Dispose( bool disposing )
            {
                this.wrappedStream = null;

                if( this.entry.NotNullReference() )
                {
                    this.entry.CloseFile();
                    this.entry = null;
                }

                base.Dispose(disposing);
            }

            private Stream Stream
            {
                get
                {
                    if( this.wrappedStream.NullReference() )
                        throw new ObjectDisposedException(null).StoreFileLine();
                    else
                        return this.wrappedStream;
                }
            }

            public override bool CanRead
            {
                get { return this.Stream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return this.Stream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return this.Stream.CanWrite; }
            }

            public override long Length
            {
                get { return this.Stream.Length; }
            }

            public override long Position
            {
                get { return this.Stream.Position; }
                set { this.Stream.Position = value; }
            }

            public override void Flush()
            {
                this.Stream.Flush();
            }

            public override int Read( byte[] buffer, int offset, int count )
            {
                return this.Stream.Read(buffer, offset, count);
            }

            public override long Seek( long offset, SeekOrigin origin )
            {
                return this.Stream.Seek(offset, origin);
            }

            public override void SetLength( long value )
            {
                this.Stream.SetLength(value);
            }

            public override void Write( byte[] buffer, int offset, int count )
            {
                this.Stream.Write(buffer, offset, count);
            }
        }

        #endregion

        #region Entry

        private class Entry : DisposableObject
        {
            #region Private Fields

            private readonly FilePath path;
            private MemoryStream stream;
            private bool isOpen;

            #endregion

            #region Constructor

            internal Entry( FilePath path )
            {
                if( path.NullReference() )
                    throw new ArgumentNullException(nameof(path)).StoreFileLine();

                this.path = path;
                this.stream = null;
                this.isOpen = false;
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

                    if( this.stream.NotNullReference() )
                    {
                        this.stream.Dispose();
                        this.stream = null;
                    }
                }

                //// shared cleanup logic
                //// (unmanaged resources)
                if( this.isOpen )
                {
                    Monitor.Exit(this);
                    this.isOpen = false;
                }

                base.OnDispose(disposing);
            }

            #endregion

            #region Internal Members

            internal bool IsFileOpen
            {
                get
                {
                    this.ThrowIfDisposed();

                    lock( this )
                        return this.isOpen;
                }
            }

            internal long Length
            {
                get
                {
                    this.ThrowIfDisposed();

                    lock( this )
                    {
                        if( this.path.IsDirectory
                         || this.stream.NullReference() )
                            throw new InvalidOperationException("Directories have no length!").Store(nameof(this.path), this.path);

                        return this.stream.Length;
                    }
                }
            }

            internal EchoStream OpenFile( bool alwaysCreateNew )
            {
                try
                {
                    this.ThrowIfDisposed();

                    Monitor.Enter(this); // start locking...
                    try
                    {
                        if( this.path.IsDirectory )
                            throw new InvalidOperationException("Only files can have streams!").StoreFileLine();

                        if( this.isOpen )
                            throw new IOException("File already open!").StoreFileLine();

                        if( this.stream.NullReference() )
                            this.stream = new MemoryStream();
                        else
                            this.stream.Position = 0;

                        if( alwaysCreateNew )
                            this.stream.SetLength(0);

                        this.isOpen = true;
                        return EchoStream.WrapOpenFile(this.stream, this);
                    }
                    catch
                    {
                        Monitor.Exit(this); // ... stop locking, if the file could not be opened
                        throw;
                    }
                }
                catch( Exception ex )
                {
                    ex.Store(nameof(this.path), this.path);
                    throw;
                }
            }

            internal void CloseFile()
            {
                try
                {
                    this.ThrowIfDisposed();

                    if( this.path.IsDirectory )
                        throw new InvalidOperationException("Only files can be closed!").StoreFileLine();

                    if( !this.isOpen )
                        throw new InvalidOperationException("File already closed!").StoreFileLine();

                    this.isOpen = false;
                    Monitor.Exit(this); // ... or stop locking once the file is closed.
                }
                catch( Exception ex )
                {
                    ex.Store(nameof(this.path), this.path);
                    throw;
                }
            }

            #endregion
        }

        #endregion

        #region Private Fields

        private readonly Dictionary<FilePath, Entry> entries;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryFileSystem"/> class.
        /// </summary>
        public MemoryFileSystem()
        {
            this.entries = new Dictionary<FilePath, Entry>();
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

                lock( this.entries )
                {
                    if( this.entries.Count != 0 )
                    {
                        foreach( var entry in this.entries.Values )
                            entry.Dispose();

                        this.entries.Clear();
                    }
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
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
            this.ThrowIfDisposed();

            return path?.ToString();
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
                if( directoryPath.NotNullReference()
                 && !directoryPath.IsDirectory )
                    throw new ArgumentException("Invalid directory path!").StoreFileLine();

                lock( this.entries )
                {
                    if( directoryPath.NotNullReference()
                     && !this.entries.ContainsKey(directoryPath) )
                        throw new FileNotFoundException().StoreFileLine(); // NOTE: a DirectoryNotFound exception would be nicer, but unfortunately it is not supported by the portable library

                    var results = new List<FilePath>();
                    foreach( var path in this.entries.Keys )
                    {
                        if( (directoryPath.NullReference() && !path.HasParent)
                         || (directoryPath.NotNullReference() && directoryPath.IsParentOf(path)) )
                            results.Add(path);
                    }
                    return results.ToArray();
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

                lock( this.entries )
                {
                    Entry entry;
                    if( !this.entries.TryGetValue(filePath, out entry) )
                        throw new FileNotFoundException().StoreFileLine();

                    return entry.OpenFile(alwaysCreateNew: false); // throws if file is already open
                }
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
            try
            {
                this.ThrowIfDisposed();

                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                lock( this.entries )
                {
                    Entry entry;
                    if( !this.entries.TryGetValue(filePath, out entry) )
                        throw new FileNotFoundException().StoreFileLine();

                    return entry.Length;
                }
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
                this.ThrowIfDisposed();

                if( directoryPath.NullReference()
                 || !directoryPath.IsDirectory )
                    throw new ArgumentException("Invalid directory path!").StoreFileLine();

                lock( this.entries )
                {
                    if( this.entries.ContainsKey(directoryPath.ToFilePath()) )
                        throw new IOException("A file with the same name already exists!").StoreFileLine();

                    // add parents, if they are missing
                    if( directoryPath.HasParent )
                        this.CreateDirectory(directoryPath.Parent);

                    // add directory
                    if( !this.entries.ContainsKey(directoryPath) )
                        this.entries.Add(directoryPath, new Entry(directoryPath));
                }
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

                lock( this.entries )
                {
                    List<KeyValuePair<FilePath, Entry>> entriesToDelete = null;

                    // get the entry
                    Entry entry;
                    if( !this.entries.TryGetValue(path, out entry) )
                    {
                        return;
                    }
                    else
                    {
                        entriesToDelete = new List<KeyValuePair<FilePath, Entry>>();
                        entriesToDelete.Add(new KeyValuePair<FilePath, Entry>(path, entry));
                    }

                    // if this is a directory, remove all it's descendants as well
                    if( path.IsDirectory )
                    {
                        foreach( var pair in this.entries )
                        {
                            if( path.IsAncestorOf(pair.Key) )
                                entriesToDelete.Add(pair);
                        }
                    }

                    // remove all entries
                    foreach( var pair in entriesToDelete )
                        this.entries.Remove(pair.Key);

                    // dispose of entries
                    if( entriesToDelete.NotNullReference() )
                    {
                        foreach( var pair in entriesToDelete )
                            pair.Value.Dispose();

                        entriesToDelete.Clear();
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
                this.ThrowIfDisposed();

                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                Stream stream;
                lock( this.entries )
                {
                    Entry entry;
                    if( this.entries.TryGetValue(filePath, out entry) )
                    {
                        // entry already exists
                        if( !overwriteIfExists )
                            throw new IOException("The file already exists!").StoreFileLine();
                    }
                    else
                    {
                        //// create entry if it doesn't exist

                        // create parent directory
                        if( filePath.HasParent )
                            this.CreateDirectory(filePath.Parent);

                        // add entry
                        entry = new Entry(filePath);
                        this.entries.Add(filePath, entry);
                    }

                    stream = entry.OpenFile(alwaysCreateNew: true); // throws if file is already open
                }

                // may or may not be a new entry
                return stream;
            }
            catch( Exception ex )
            {
                ex.Store(nameof(filePath), filePath);
                ex.Store(nameof(overwriteIfExists), overwriteIfExists);
                throw;
            }
        }

        /// <summary>
        /// Creates a new file from the content of the specified stream.
        /// The stream being copied will NOT be closed at the end of the method.
        /// </summary>
        /// <param name="filePath">The path specifying the file to create.</param>
        /// <param name="overwriteIfExists"><c>true</c> to overwrite the file if it already exists; or <c>false</c> to throw an exception.</param>
        /// <param name="streamToCopy">The <see cref="Stream"/> to copy the content of (from the current position, until the end of the stream).</param>
        public void CreateFile( FilePath filePath, bool overwriteIfExists, Stream streamToCopy )
        {
            using( var s = this.CreateFile(filePath, overwriteIfExists) )
                streamToCopy.CopyTo(s);
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
                this.ThrowIfDisposed();

                if( filePath.NullReference()
                 || filePath.IsDirectory )
                    throw new ArgumentException("Invalid file path!").StoreFileLine();

                lock( this.entries )
                {
                    Entry entry;
                    if( !this.entries.TryGetValue(filePath, out entry) )
                    {
                        //// create entry if it doesn't exist

                        // create parent directory
                        if( filePath.HasParent )
                            this.CreateDirectory(filePath.Parent);

                        // add entry
                        entry = new Entry(filePath);
                        this.entries.Add(filePath, entry);
                    }

                    // may or may not be a new entry
                    return entry.OpenFile(alwaysCreateNew: false); // throws if file is already open
                }
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
