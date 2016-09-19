using System;
using System.IO;
using System.Threading;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Notifies implementers when a stream
    /// (created by the implementing abstract file system) is being closed.
    /// </summary>
    public abstract class StreamTrackingFileSystemBase : DisposableObject, IFileSystem
    {
        #region EchoStream

        /// <summary>
        /// Wraps the implementing stream, and notifies the file system when it is about to be closed.
        /// </summary>
        private class EchoStream : Stream
        {
            private readonly StreamTrackingFileSystemBase fileSystem;
            private readonly FilePath filePath;
            private readonly StreamSource streamSource;
            private Stream wrappedStream;

            internal EchoStream( StreamTrackingFileSystemBase fs, Stream stream, FilePath path, StreamSource source )
            {
                if( fs.NullReference() )
                    throw new ArgumentNullException(nameof(fs)).StoreFileLine();

                if( stream.NullReference() )
                    throw new ArgumentNullException(nameof(stream)).StoreFileLine();

                if( path.NullReference()
                 || path.IsDirectory )
                    throw NamedArgumentException.Store(nameof(path), path);

                if( !Enum.IsDefined(typeof(StreamSource), source) )
                    throw NamedArgumentException.Store(nameof(source), source);

                if( !stream.CanRead
                 && (source == StreamSource.ReadFile || source == StreamSource.ReadWriteFile) )
                    throw new InvalidOperationException("Stream must be readable!").Store(nameof(source), source);

                if( !stream.CanWrite
                 && (source == StreamSource.CreateFile_Result || source == StreamSource.ReadWriteFile) )
                    throw new InvalidOperationException("Stream must be writable!").Store(nameof(source), source);

                this.fileSystem = fs;
                this.filePath = path;
                this.streamSource = source;
                this.wrappedStream = stream;
            }

            protected override void Dispose( bool disposing )
            {
                try
                {
                    if( disposing )
                    {
                        // thread-safe replacement of wrapped stream with null
                        var wrappee = Interlocked.Exchange(ref this.wrappedStream, null);
                        if( wrappee.NotNullReference() )
                        {
                            // the wrapped stream has not yet been disposed of
                            try
                            {
                                this.fileSystem.OnStreamClosing(wrappee, this.filePath, this.streamSource);
                            }
                            finally
                            {
                                wrappee.Dispose();
                                wrappee = null;
                            }
                        }
                    }
                    else
                    {
                        // NOTE: Since this was called from the finalizer, managed fields may or may not be null.
                        //       At least this way it is consistent, and detectable.
                        this.fileSystem.OnStreamClosing(null, null, this.streamSource);
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }

            private Stream BaseStream
            {
                get
                {
                    //// NOTE: for thread-safety, we only get the reference once.

                    var w = this.wrappedStream;
                    if( w.NullReference() )
                        throw new ObjectDisposedException(null).StoreFileLine();
                    else
                        return w;
                }
            }

            public override bool CanRead
            {
                get { return this.BaseStream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return this.BaseStream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return this.BaseStream.CanWrite; }
            }

            public override long Length
            {
                get { return this.BaseStream.Length; }
            }

            public override long Position
            {
                get { return this.BaseStream.Position; }
                set { this.BaseStream.Position = value; }
            }

            public override void Flush()
            {
                this.BaseStream.Flush();
            }

            public override int Read( byte[] buffer, int offset, int count )
            {
                return this.BaseStream.Read(buffer, offset, count);
            }

            public override long Seek( long offset, SeekOrigin origin )
            {
                return this.BaseStream.Seek(offset, origin);
            }

            public override void SetLength( long value )
            {
                this.BaseStream.SetLength(value);
            }

            public override void Write( byte[] buffer, int offset, int count )
            {
                this.BaseStream.Write(buffer, offset, count);
            }
        }

        #endregion

        #region StreamSource

        /// <summary>
        /// Specifies which method invocation created the stream.
        /// </summary>
        protected enum StreamSource
        {
            /// <summary>
            /// The stream was created by <see cref="IFileSystemReader.ReadFile"/>.
            /// </summary>
            ReadFile,

            /// <summary>
            /// The stream was created by <see cref="IFileSystemWriter.CreateFile(FilePath, bool)"/>.
            /// </summary>
            CreateFile_Result,

            /// <summary>
            /// The stream was created by <see cref="IFileSystem.ReadWriteFile"/>.
            /// </summary>
            ReadWriteFile
        }

        #endregion

        #region Protected Abstract Methods

        /// <summary>
        /// Opens the specified file for reading.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        protected abstract Stream OpenRead( FilePath filePath );

        /// <summary>
        /// Creates a new empty file, and opens it for writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to create.</param>
        /// <param name="overwriteIfExists"><c>true</c> to overwrite the file if it already exists; or <c>false</c> to throw an exception.</param>
        /// <returns>A <see cref="Stream"/> representing the file.</returns>
        protected abstract Stream OpenWrite( FilePath filePath, bool overwriteIfExists );

        /// <summary>
        /// Opens an existing file, or creates a new one, for both reading and writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        protected abstract Stream OpenReadWrite( FilePath filePath );

        /// <summary>
        /// Invoked before the file stream is closed.
        /// This method is NOT invoked for <see cref="IFileSystemWriter.CreateFile(FilePath, bool, Stream)"/>,
        /// since the only stream available there, is not one created by this file system.
        /// </summary>
        /// <param name="stream">The stream being closed (or <c>null</c> if the method was called from the stream's finalizer).</param>
        /// <param name="filePath">The file path the stream was opened with (or <c>null</c> if the method was called from the stream's finalizer).</param>
        /// <param name="source">Determines which method created the <paramref name="stream"/>.</param>
        protected abstract void OnStreamClosing( Stream stream, FilePath filePath, StreamSource source );

        #endregion

        #region IFileSystemBase

        /// <summary>
        /// Gets a value indicating whether the ToHostPath method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public abstract bool SupportsToHostPath { get; }

        /// <summary>
        /// Gets the string the underlying system uses to represent the specified file or directory.
        /// </summary>
        /// <param name="path">The path to the file or directory.</param>
        /// <returns>The string the underlying system uses to represent the specified <paramref name="path"/>.</returns>
        public abstract string ToHostPath( FilePath path );

        #endregion

        #region IFileSystemReader

        /// <summary>
        /// Gets the paths to the direct children of the specified directory.
        /// Subdirectories are not searched.
        /// </summary>
        /// <param name="directoryPath">The path specifying the directory to list the direct children of; or <c>null</c> to specify the root of this file system.</param>
        /// <returns>The paths of the files and directories found.</returns>
        public abstract FilePath[] GetPaths( FilePath directoryPath = null );

        /// <summary>
        /// Opens the specified file for reading.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        public Stream ReadFile( FilePath filePath )
        {
            return new EchoStream(this, this.OpenRead(filePath), filePath, StreamSource.ReadFile);
        }


        /// <summary>
        /// Gets a value indicating whether the GetFileSize method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public abstract bool SupportsGetFileSize { get; }

        /// <summary>
        /// Gets the size, in bytes, of the specified file.
        /// </summary>
        /// <param name="filePath">The file to get the size of.</param>
        /// <returns>The size of the specified file in bytes.</returns>
        public abstract long GetFileSize( FilePath filePath );

        #endregion

        #region IFileSystemWriter

        /// <summary>
        /// Creates the specified directory (and any directories along the path) should it not exist.
        /// </summary>
        /// <param name="directoryPath">The path specifying the directory to create.</param>
        public abstract void CreateDirectory( FilePath directoryPath );

        /// <summary>
        /// Deletes the specified file or directory. Does nothing if it does not exist.
        /// </summary>
        /// <param name="path">The path specifying the file or directory to delete.</param>
        public abstract void Delete( FilePath path );

        /// <summary>
        /// Creates a new empty file, and opens it for writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to create.</param>
        /// <param name="overwriteIfExists"><c>true</c> to overwrite the file if it already exists; or <c>false</c> to throw an exception.</param>
        /// <returns>A <see cref="Stream"/> representing the file.</returns>
        public Stream CreateFile( FilePath filePath, bool overwriteIfExists )
        {
            return new EchoStream(this, this.OpenWrite(filePath, overwriteIfExists), filePath, StreamSource.CreateFile_Result);
        }

        /// <summary>
        /// Creates a new file from the content of the specified stream.
        /// The stream being copied will NOT be closed at the end of the method.
        /// </summary>
        /// <param name="filePath">The path specifying the file to create.</param>
        /// <param name="overwriteIfExists"><c>true</c> to overwrite the file if it already exists; or <c>false</c> to throw an exception.</param>
        /// <param name="streamToCopy">The <see cref="Stream"/> to copy the content of (from the current position, until the end of the stream).</param>
        public abstract void CreateFile( FilePath filePath, bool overwriteIfExists, Stream streamToCopy );

        #endregion

        #region IFileSystem

        /// <summary>
        /// Gets a value indicating whether the ReadWriteFile method is supported.
        /// </summary>
        /// <value><c>true</c> if the method is supported; otherwise, <c>false</c>.</value>
        public abstract bool SupportsReadWriteFile { get; }

        /// <summary>
        /// Opens an existing file, or creates a new one, for both reading and writing.
        /// </summary>
        /// <param name="filePath">The path specifying the file to open.</param>
        /// <returns>A <see cref="Stream"/> representing the file opened.</returns>
        public Stream ReadWriteFile( FilePath filePath )
        {
            return new EchoStream(this, this.OpenReadWrite(filePath), filePath, StreamSource.ReadWriteFile);
        }

        #endregion
    }
}
