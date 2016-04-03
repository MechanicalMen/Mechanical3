using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using Mechanical3.Core;
using Mechanical3.IO.FileSystems;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Combines a file format reader, and data converters, into a single reader.
    /// This is the class you use for deserialization.
    /// </summary>
    public sealed class DataStoreTextReader : DisposableObject, IXmlLineInfo
    {
        #region ReaderState

        private enum ReaderState
        {
            BeforeFirstRead,
            Reading,
            AllTokensRead
        }

        #endregion

        #region Parent

        private struct Parent
        {
            internal readonly bool IsObject;
            internal readonly string Name;
            internal readonly int Index;

            internal Parent( bool isObject, string name, int index )
            {
                this.IsObject = isObject;
                this.Name = name;
                this.Index = index;
            }
        }

        #endregion

        #region Private Fields

        private const int InvalidIndex = -1;

        private readonly List<Parent> parents;
        private readonly IStringConverterLocator converters;
        private IDataStoreTextFileFormatReader file;
        private ReaderState state;
        private DataStoreToken token = (DataStoreToken)byte.MaxValue; // the first reading should not find a recognizable token
        private string name;
        private int index = InvalidIndex;
        private string value;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreTextReader"/> class.
        /// </summary>
        /// <param name="fileFormat">The file format reader.</param>
        /// <param name="dataFormat">The data format converters to use; or <c>null</c> for <see cref="RoundTripStringConverter.Locator"/>.</param>
        public DataStoreTextReader( IDataStoreTextFileFormatReader fileFormat, IStringConverterLocator dataFormat = null )
        {
            if( fileFormat.NullReference() )
                throw new ArgumentNullException(nameof(fileFormat)).StoreFileLine();

            if( dataFormat.NullReference() )
                dataFormat = RoundTripStringConverter.Locator;

            this.parents = new List<Parent>();
            this.converters = dataFormat;
            this.file = fileFormat;
            this.state = ReaderState.BeforeFirstRead;
        }

        #endregion

        #region Private Methods

        private void ThrowIfNotReading(
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            this.ThrowIfDisposed(file, member, line);

            switch( this.state )
            {
            case ReaderState.BeforeFirstRead:
                throw new InvalidOperationException("Nothing read yet!").StoreFileLine(file, member, line);

            case ReaderState.Reading:
                break;

            case ReaderState.AllTokensRead:
                throw new InvalidOperationException("There is nothing more to read!").StoreFileLine(file, member, line);

            default:
                throw new Exception("Invalid reader state!").Store(nameof(this.state), this.state, file, member, line);
            }
        }

        private TException AddLineInfo<TException>( TException exception )
            where TException : Exception
        {
            try
            {
                if( this.HasLineInfo() )
                {
                    exception.Store(nameof(this.LineNumber), this.LineNumber);
                    exception.Store(nameof(this.LinePosition), this.LinePosition);
                }
            }
            catch
            {
            }
            return exception;
        }

        private Parent DirectParent
        {
            get { return this.parents[this.parents.Count - 1]; }
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

                if( this.file.NotNullReference() )
                {
                    this.file.Dispose();
                    this.file = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)
            this.parents?.Clear();

            base.OnDispose(disposing);
        }

        #endregion

        #region IXmlLineInfo

        /// <summary>
        /// Gets a value indicating whether the class can return line information.
        /// </summary>
        /// <returns><c>true</c> if <see cref="LineNumber"/> and <see cref="LinePosition"/> can be provided; otherwise, <c>false</c>.</returns>
        public bool HasLineInfo()
        {
            this.ThrowIfDisposed();

            return this.file.HasLineInfo();
        }

        /// <summary>
        /// Gets the current line number.
        /// </summary>
        /// <value>The current line number or <c>0</c> if no line information is available.</value>
        public int LineNumber
        {
            get
            {
                if( !this.HasLineInfo() )
                    return 0;

                return this.file.LineNumber;
            }
        }

        /// <summary>
        /// Gets the current line position.
        /// </summary>
        /// <value>The current line position or <c>0</c> if no line information is available.</value>
        public int LinePosition
        {
            get
            {
                if( !this.HasLineInfo() )
                    return 0;

                return this.file.LinePosition;
            }
        }

        #endregion

        #region Basic Members

        /// <summary>
        /// Gets the data format converters to use for deserialization.
        /// </summary>
        /// <value>The data format converters to use for deserialization.</value>
        public IStringConverterLocator Converters
        {
            get
            {
                this.ThrowIfDisposed();

                return this.converters;
            }
        }

        /// <summary>
        /// Reads the next token from the stream.
        /// </summary>
        /// <returns><c>true</c> if a token could be read; <c>false</c> if the stream has ended.</returns>
        public bool Read()
        {
            this.ThrowIfDisposed();

            try
            {
                if( this.state == ReaderState.AllTokensRead )
                    return false;

                // leaving object or array start: add as new parent
                if( this.token == DataStoreToken.ObjectStart )
                {
                    this.parents.Add(new Parent(true, this.name, this.index));
                }
                else if( this.token == DataStoreToken.ArrayStart )
                {
                    this.parents.Add(new Parent(false, this.name, this.index));
                    this.index = -1; // reset index inside array (to -1, since the first thing we'll do is increase the index for the current node)
                }

                // see what we find in the file
                if( !this.file.TryReadToken(out this.token, out this.name, out this.value) )
                {
                    // nothing more to read
                    if( this.state == ReaderState.BeforeFirstRead )
                        throw new FormatException("Empty data store documents are not valid: if there is a document to read, then it can not be empty!").StoreFileLine();

                    if( this.parents.Count != 0 )
                        throw new EndOfStreamException("Unexpected end of file: some arrays or objects are missing their ending tokens!").StoreFileLine();

                    this.state = ReaderState.AllTokensRead;
                    this.name = null;
                    this.index = InvalidIndex;
                    this.value = null;
                    return false;
                }

                // successful read
                if( this.state == ReaderState.BeforeFirstRead )
                {
                    // first read, we are at the first token
                    this.state = ReaderState.Reading;
                }
                else
                {
                    // this is not the first token
                    if( this.parents.Count == 0 )
                        throw new FormatException("There can only be exactly one root node!").StoreFileLine();
                }
#if DEBUG
                // check name returned (undetermined for object and array ends, root, or inside arrays)
                if( this.parents.Count != 0
                 && this.token != DataStoreToken.End
                 && this.DirectParent.IsObject
                 && !DataStore.IsValidName(this.name) )
                        throw new FormatException("Invalid name provided by file format!").Store(nameof(this.name), this.name).Store(nameof(this.token), this.token);
#endif

                // discard unused name information (see End token below for the rest)
                if( this.parents.Count == 0
                 || !this.DirectParent.IsObject )
                    this.name = null; // root or inside array

                // set index (see End token below for the rest)
                if( this.parents.Count != 0
                 && !this.DirectParent.IsObject )
                    ++this.index; // has array parent
                else
                    this.index = InvalidIndex; // root or object parent

                // process the token
                switch( this.token )
                {
                case DataStoreToken.ObjectStart:
                case DataStoreToken.ArrayStart:
                    // next Read() increases depth (see above)
                    break;

                case DataStoreToken.End:
                    {
                        if( this.parents.Count == 0 )
                            throw new FormatException("'End' is not a valid initial token!").StoreFileLine();

                        int lastIndex = this.parents.Count - 1;
                        var parent = this.parents[lastIndex];
                        this.parents.RemoveAt(lastIndex);
                        this.name = parent.Name; // even if the file format provided the name, we discard it
                        this.index = parent.Index;
                    }
                    break;

                case DataStoreToken.Value:
                    if( this.parents.Count == 0 )
                        throw new FormatException("A value is not a valid root node!").StoreFileLine();
                    break;

                default:
                    throw new FormatException("File format returned invalid token!").Store(nameof(this.token), this.token);
                }

                return true;
            }
            catch( Exception ex )
            {
                this.AddLineInfo(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the last <see cref="DataStoreToken"/> read.
        /// </summary>
        /// <value>The last <see cref="DataStoreToken"/> read.</value>
        public DataStoreToken Token
        {
            get
            {
                this.ThrowIfNotReading();

                return this.token;
            }
        }

        /// <summary>
        /// Gets the name of the current node (only when the parent is an object).
        /// </summary>
        /// <value>The name of the current node.</value>
        public string Name
        {
            get
            {
                this.ThrowIfNotReading();

                if( this.parents.Count != 0
                 && this.DirectParent.IsObject )
                    return this.name;
                else
                    throw new InvalidOperationException("This is the root node, or the parent of this node is not an object!").StoreFileLine();
            }
        }

        /// <summary>
        /// Gets the index of the current node (only when the parent is an array).
        /// </summary>
        /// <value>The index of the current node.</value>
        public int Index
        {
            get
            {
                this.ThrowIfNotReading();

                if( this.parents.Count != 0
                 && !this.DirectParent.IsObject )
                    return this.index;
                else
                    throw new InvalidOperationException("This is the root node, or the parent of this node is not an array!").StoreFileLine();
            }
        }

        /// <summary>
        /// Gets the string content of the current value.
        /// </summary>
        /// <value>The string content of the current value.</value>
        public string Value
        {
            get
            {
                this.ThrowIfNotReading();

                if( this.token != DataStoreToken.Value )
                    throw new InvalidOperationException("The current token is not a value!").Store(nameof(this.token), this.token);

                return this.value;
            }
        }

        /// <summary>
        /// Gets the path to the current node.
        /// The root node is not part of it, since it has neither name nor index.
        /// </summary>
        /// <value>The path to the current node.</value>
        public FilePath Path
        {
            get
            {
                this.ThrowIfNotReading();

                if( this.parents.Count == 0 )
                    throw new InvalidOperationException("Root nodes have no path!").StoreFileLine();

                // build path from parents
                string name;
                FilePath node;
                FilePath result = null;

                for( int i = 1; i < this.parents.Count; ++i ) // starting from 1, since the root has neither name nor index
                {
                    var curr = this.parents[i];
                    var par = this.parents[i - 1];
                    name = par.IsObject ? curr.Name : curr.Index.ToString(CultureInfo.InvariantCulture);
                    node = FilePath.FromDirectoryName(name);
                    result = result.NullReference() ? node : result + node;
                }

                // add current node to path
                name = this.DirectParent.IsObject ? this.name : this.index.ToString(CultureInfo.InvariantCulture);
                node = this.token == DataStoreToken.Value ? FilePath.FromFileName(name) : FilePath.FromDirectoryName(name);
                result = result.NullReference() ? node : result + node;
                return result;
            }
        }

        #endregion
    }
}
