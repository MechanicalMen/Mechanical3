﻿using System;
using System.IO;
using System.Threading;
using Mechanical3.Core;

namespace Mechanical3.IO.FileSystems
{
    /// <summary>
    /// Notifies inheritors when a stream is closed.
    /// </summary>
    public abstract class StreamTrackingFileSystemBase : IFileSystem
    {
        #region EchoStream

        /// <summary>
        /// Wraps the implementing stream, and notifies the file system when it is about to be closed.
        /// </summary>
        private class EchoStream : Stream
        {
            private readonly StreamTrackingFileSystemBase fileSystem;
            private readonly FilePath filePath;
            private readonly bool? isReading;
            private Stream wrappedStream;

            internal EchoStream( StreamTrackingFileSystemBase fs, Stream stream, FilePath path, bool? read )
            {
                if( fs.NullReference() )
                    throw new ArgumentNullException(nameof(fs)).StoreFileLine();

                if( stream.NullReference() )
                    throw new ArgumentNullException(nameof(stream)).StoreFileLine();

                if( path.NullReference()
                 || path.IsDirectory )
                    throw NamedArgumentException.Store(nameof(path), path?.ToString());

                if( !stream.CanRead
                 && (!read.HasValue || read.Value) ) // ReadWrite or Read
                    throw new InvalidOperationException("Stream must be readable!").StoreFileLine();

                if( !stream.CanWrite
                 && (!read.HasValue || !read.Value) ) // ReadWrite or Write
                    throw new InvalidOperationException("Stream must be writable!").StoreFileLine();

                this.fileSystem = fs;
                this.filePath = path;
                this.isReading = read;
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
                                this.fileSystem.OnStreamClosing(wrappee, this.filePath, this.isReading);
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
                        this.fileSystem.OnStreamClosing(null, null, this.isReading);
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
        /// </summary>
        /// <param name="stream">The stream being closed (or <c>null</c> if the method was called from the stream's finalizer).</param>
        /// <param name="filePath">The file path the stream was opened with (or <c>null</c> if the method was called from the stream's finalizer).</param>
        /// <param name="read"><c>true</c> if the <paramref name="stream"/> was returned by <see cref="OpenRead"/>, <c>false</c> if it was returned by <see cref="OpenWrite"/>, or <c>null</c> if it was returned by <see cref="OpenReadWrite"/>.</param>
        protected abstract void OnStreamClosing( Stream stream, FilePath filePath, bool? read );

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
            return new EchoStream(this, this.OpenRead(filePath), filePath, read: true);
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
            return new EchoStream(this, this.OpenWrite(filePath, overwriteIfExists), filePath, read: false);
        }

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
            return new EchoStream(this, this.OpenReadWrite(filePath), filePath, read: null);
        }

        #endregion
    }
}
