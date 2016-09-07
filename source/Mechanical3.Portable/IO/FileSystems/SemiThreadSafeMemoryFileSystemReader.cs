using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    //// NOTE: For still more speed, you could ditch abstract file systems, file paths and streams alltogether, and just use byte arrays directly.
    ////       Obviously this is a compromize between performance and ease of use.

    /// <summary>
    /// Provides performant, semi thread-safe access to an in-memory copy of the contants of an <see cref="IFileSystemReader"/>.
    /// Access to <see cref="IFileSystemReader"/> members is thread-safe.
    /// <see cref="Stream"/> instances returned are NOT thread-safe, and they must not be used after they are closed or disposed (one of which should happen exactly one time).
    /// </summary>
    public class SemiThreadSafeMemoryFileSystemReader : IFileSystemReader
    {
        #region ByteArrayReaderStream

        /// <summary>
        /// Provides thread-safe, read-only access to the wrapped byte array.
        /// </summary>
        private class ByteArrayReaderStream : Stream
        {
            //// NOTE: the default asynchronous implementations call their synchronous versions.
            //// NOTE: we don't use Store*, to save a few allocations

            #region ObjectPool

            internal static class ObjectPool
            {
                private static readonly ConcurrentBag<ByteArrayReaderStream> Pool = new ConcurrentBag<ByteArrayReaderStream>();

                internal static ByteArrayReaderStream Get( byte[] data )
                {
                    ByteArrayReaderStream stream;
                    if( !Pool.TryTake(out stream) )
                        stream = new ByteArrayReaderStream();

                    stream.OnInitialize(data);
                    return stream;
                }

                internal static void Put( ByteArrayReaderStream stream )
                {
                    Pool.Add(stream);
                }

                internal static void Clear()
                {
                    ByteArrayReaderStream stream;
                    while( true )
                    {
                        if( Pool.TryTake(out stream) )
                            GC.SuppressFinalize(stream);
                        else
                            break; // pool is empty
                    }
                }
            }

            #endregion

            #region Private Fields

            private byte[] array;
            private int position; // NOTE: (position == length) == EOF
            private bool isOpen;

            #endregion

            #region Constructor

            private ByteArrayReaderStream()
                : base()
            {
            }

            #endregion

            #region Private Methods

            private void OnInitialize( byte[] data )
            {
                this.array = data;
                this.position = 0;
                this.isOpen = true;
            }

            private void OnClose()
            {
                this.isOpen = false;
                this.array = null;
                this.position = -1;
            }

            private void ThrowIfClosed()
            {
                if( !this.isOpen )
                    throw new ObjectDisposedException(message: "The stream was already closed!", innerException: null);
            }

            #endregion

            #region Stream

            protected override void Dispose( bool disposing )
            {
                this.OnClose();
                ObjectPool.Put(this);

                if( !disposing )
                {
                    // called from finalizer
                    GC.ReRegisterForFinalize(this);
                }
            }

            public override bool CanRead
            {
                get
                {
                    this.ThrowIfClosed();
                    return true;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    this.ThrowIfClosed();
                    return true;
                }
            }

            public override bool CanTimeout
            {
                get
                {
                    this.ThrowIfClosed();
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    this.ThrowIfClosed();
                    return false;
                }
            }

            public override long Length
            {
                get
                {
                    this.ThrowIfClosed();
                    return this.array.Length;
                }
            }

            public override long Position
            {
                get
                {
                    this.ThrowIfClosed();
                    return this.position;
                }
                set
                {
                    this.ThrowIfClosed();

                    if( value < 0 || this.array.Length < value )
                        throw new ArgumentOutOfRangeException();

                    this.position = (int)value;
                }
            }

            public override long Seek( long offset, SeekOrigin origin )
            {
                this.ThrowIfClosed();

                int newPosition;
                switch( origin )
                {
                case SeekOrigin.Begin:
                    newPosition = (int)offset;
                    break;

                case SeekOrigin.Current:
                    newPosition = this.position + (int)offset;
                    break;

                case SeekOrigin.End:
                    newPosition = this.array.Length + (int)offset;
                    break;

                default:
                    throw new ArgumentException("Invalid SeekOrigin!");
                }

                if( newPosition < 0
                 || newPosition > this.array.Length )
                    throw new ArgumentOutOfRangeException();

                this.position = newPosition;
                return this.position;
            }

            public override int ReadByte()
            {
                this.ThrowIfClosed();

                if( this.position == this.array.Length )
                {
                    // end of stream
                    return -1;
                }
                else
                {
                    return this.array[this.position++];
                }
            }

            public override int Read( byte[] buffer, int offset, int bytesToRead )
            {
                this.ThrowIfClosed();

                int bytesLeft = this.array.Length - this.position;
                if( bytesLeft < bytesToRead ) bytesToRead = bytesLeft;
                if( bytesToRead == 0 )
                    return 0;

                Buffer.BlockCopy(src: this.array, srcOffset: this.position, dst: buffer, dstOffset: offset, count: bytesToRead);
                this.position += bytesToRead;
                return bytesToRead;
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override void SetLength( long value )
            {
                throw new NotSupportedException();
            }

            public override void WriteByte( byte value )
            {
                throw new NotSupportedException();
            }

            public override void Write( byte[] buffer, int offset, int count )
            {
                throw new NotSupportedException();
            }

            #endregion
        }

        #endregion

        #region Private Fields

        /* From MSDN:
         * A Dictionary can support multiple readers concurrently, as long as the collection is not modified.
         * Even so, enumerating through a collection is intrinsically not a thread-safe procedure. In the
         * rare case where an enumeration contends with write accesses, the collection must be locked during
         * the entire enumeration. To allow the collection to be accessed by multiple threads for reading
         * and writing, you must implement your own synchronization.
         *
         * ... (or there is also ConcurrentDictionary)
         */

        //// NOTE: since after they are filled, the dictionaries won't be modified, we are fine.

        private readonly FilePath[] rootFolderEntries;
        private readonly Dictionary<FilePath, FilePath[]> nonRootFolderEntries;
        private readonly Dictionary<FilePath, byte[]> fileContents;
        private readonly Dictionary<FilePath, string> hostPaths;
        private readonly string rootHostPath;

        #endregion

        #region Constructors

        /// <summary>
        /// Copies the current contents of the specified <see cref="IFileSystemReader"/>
        /// into a new <see cref="SemiThreadSafeMemoryFileSystemReader"/> instance.
        /// </summary>
        /// <param name="readerToCopy">The abstract file system to copy the current contents of, into memory.</param>
        /// <returns>A new <see cref="SemiThreadSafeMemoryFileSystemReader"/> instance.</returns>
        public static SemiThreadSafeMemoryFileSystemReader CopyFrom( IFileSystemReader readerToCopy )
        {
            return new SemiThreadSafeMemoryFileSystemReader(readerToCopy);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SemiThreadSafeMemoryFileSystemReader"/> class.
        /// </summary>
        /// <param name="readerToCopy">The abstract file system to copy the current contents of, into memory.</param>
        private SemiThreadSafeMemoryFileSystemReader( IFileSystemReader readerToCopy )
        {
            this.rootFolderEntries = readerToCopy.GetPaths();

            this.nonRootFolderEntries = new Dictionary<FilePath, FilePath[]>();
            this.fileContents = new Dictionary<FilePath, byte[]>();

            if( readerToCopy.SupportsToHostPath )
            {
                this.hostPaths = new Dictionary<FilePath, string>();
                this.rootHostPath = readerToCopy.ToHostPath(null);
            }

            using( var tmpStream = new MemoryStream() )
            {
                foreach( var e in this.rootFolderEntries )
                    this.AddRecursively(e, readerToCopy, tmpStream);
            }
        }

        #endregion

        #region Private Methods

        private void AddRecursively( FilePath entry, IFileSystemReader readerToCopy, MemoryStream tmpStream )
        {
            if( entry.IsDirectory )
            {
                var subEntries = readerToCopy.GetPaths(entry);
                this.nonRootFolderEntries.Add(entry, subEntries);

                foreach( var e in subEntries )
                    this.AddRecursively(e, readerToCopy, tmpStream);
            }
            else
            {
                this.fileContents.Add(entry, ReadFileContents(entry, readerToCopy, tmpStream));
            }

            if( this.SupportsToHostPath )
                this.hostPaths.Add(entry, readerToCopy.ToHostPath(entry));
        }

        private static byte[] ReadFileContents( FilePath filePath, IFileSystemReader readerToCopy, MemoryStream tmpStream )
        {
            tmpStream.SetLength(0);

            long? fileSize = null;
            if( readerToCopy.SupportsGetFileSize )
            {
                fileSize = readerToCopy.GetFileSize(filePath);
                ThrowIfFileTooBig(filePath, fileSize.Value);
            }

            using( var stream = readerToCopy.ReadFile(filePath) )
            {
                if( !fileSize.HasValue
                 && stream.CanSeek )
                {
                    fileSize = stream.Length;
                    ThrowIfFileTooBig(filePath, fileSize.Value);
                }

                stream.CopyTo(tmpStream);

                if( !fileSize.HasValue )
                    ThrowIfFileTooBig(filePath, tmpStream.Length);

                return tmpStream.ToArray();
            }
        }

        private static void ThrowIfFileTooBig( FilePath filePath, long fileSize )
        {
            if( fileSize > int.MaxValue ) // that's the largest our stream implementation can support
                throw new Exception("One of the files is too large!").Store(nameof(filePath), filePath).Store(nameof(fileSize), fileSize);
        }

        #endregion

        #region IFileSystemBase

        /// <summary>
        /// Gets a value indicating whether the ToHostPath method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public bool SupportsToHostPath
        {
            get { return this.hostPaths.NotNullReference(); }
        }

        /// <summary>
        /// Gets the string the underlying system uses to represent the specified file or directory.
        /// </summary>
        /// <param name="path">The path to the file or directory.</param>
        /// <returns>The string the underlying system uses to represent the specified <paramref name="path"/>.</returns>
        public string ToHostPath( FilePath path )
        {
            if( !this.SupportsToHostPath )
                throw new NotSupportedException().StoreFileLine();

            if( path.NullReference() )
                return this.rootHostPath;

            string result;
            if( this.hostPaths.TryGetValue(path, out result) )
                return result;
            else
                throw new FileNotFoundException("The specified file or directory was not found!").Store(nameof(path), path);
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
                throw new ArgumentException("Argument is not a directory!").Store(nameof(directoryPath), directoryPath);

            FilePath[] paths;
            if( directoryPath.NullReference() )
            {
                paths = this.rootFolderEntries;
            }
            else
            {
                if( !this.nonRootFolderEntries.TryGetValue(directoryPath, out paths) )
                    throw new FileNotFoundException("The specified file or directory was not found!").Store(nameof(directoryPath), directoryPath);
            }

            // NOTE: Unfortunately we need to make a copy, since arrays are writable.
            // TODO: return ImmutableArray from GetPaths.
            var copy = new FilePath[paths.Length];
            Array.Copy(sourceArray: paths, destinationArray: copy, length: paths.Length);
            return copy;
        }

        /// <summary>
        /// Opens the specified file for reading.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        public Stream ReadFile( FilePath filePath )
        {
            if( filePath.NullReference()
             || filePath.IsDirectory )
                throw new ArgumentException("Argument is not a file!").Store(nameof(filePath), filePath);

            byte[] bytes;
            if( !this.fileContents.TryGetValue(filePath, out bytes) )
                throw new FileNotFoundException("The specified file or directory was not found!").Store(nameof(filePath), filePath);

            return ByteArrayReaderStream.ObjectPool.Get(bytes);
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
            if( filePath.NullReference()
             || filePath.IsDirectory )
                throw new ArgumentException("Argument is not a file!").Store(nameof(filePath), filePath);

            byte[] bytes;
            if( !this.fileContents.TryGetValue(filePath, out bytes) )
                throw new FileNotFoundException("The specified file or directory was not found!").Store(nameof(filePath), filePath);

            return bytes.Length;
        }

        #endregion
    }
}
