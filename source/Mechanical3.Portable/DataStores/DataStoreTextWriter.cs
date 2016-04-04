using System;
using System.IO;
using System.Runtime.CompilerServices;
using Mechanical3.Core;
using Mechanical3.IO.FileSystems;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Combines a file format writer, and data converters, into a single writer.
    /// This is the class you use for serialization.
    /// </summary>
    public sealed class DataStoreTextWriter : DisposableObject
    {
        //// NOTE: since we have no need for it, we do not track the current index in arrays

        #region Private Fields

        private readonly ParentStack parents;
        private readonly IStringConverterLocator converters;
        private IDataStoreTextFileFormatWriter file;
        private string nameOfNextNode = null;
        private bool rootOpened = false;
        private bool rootClosed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreTextWriter"/> class.
        /// </summary>
        /// <param name="fileFormat">The file format writer.</param>
        /// <param name="dataFormat">The data format converters to use; or <c>null</c> for <see cref="RoundTripStringConverter.Locator"/>.</param>
        public DataStoreTextWriter( IDataStoreTextFileFormatWriter fileFormat, IStringConverterLocator dataFormat = null )
        {
            if( fileFormat.NullReference() )
                throw new ArgumentNullException(nameof(fileFormat)).StoreFileLine();

            if( dataFormat.NullReference() )
                dataFormat = RoundTripStringConverter.Locator;

            this.parents = new ParentStack();
            this.converters = dataFormat;
            this.file = fileFormat;
        }

        #endregion

        #region Private Methods

        private FilePath CurrentPath
        {
            get
            {
                if( this.parents.IsRoot )
                    return null;
                else
                    return this.parents.GetCurrentPath(true);
            }
        }

        private void ThrowIfNameRequiredAndMissing(
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( !this.parents.IsRoot
             && this.parents.DirectParent.IsObject
             && this.nameOfNextNode.NullReference() )
                throw new InvalidOperationException("Name not specified!").Store(nameof(this.CurrentPath), this.CurrentPath, file, member, line);
        }

        private void ThrowIfMultipleRoots(
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            if( this.parents.IsRoot
             && this.rootClosed )
                throw new FormatException("There may only be exactly one root node!").Store(nameof(this.CurrentPath), this.CurrentPath, file, member, line);
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

                    if( !this.rootOpened )
                        throw new FormatException("Empty data store documents are not valid: if there is a document to read, then it can not be empty!").StoreFileLine();

                    if( !this.parents.IsRoot )
                        throw new EndOfStreamException("Unexpected end of file: some arrays or objects are missing their ending tokens!").Store(nameof(this.CurrentPath), this.CurrentPath);
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)

            base.OnDispose(disposing);
        }

        #endregion

        #region Basic Members

        /// <summary>
        /// Gets the data format converters to use for serialization.
        /// </summary>
        /// <value>The data format converters to use for serialization.</value>
        public IStringConverterLocator Converters
        {
            get
            {
                this.ThrowIfDisposed();

                return this.converters;
            }
        }

        /// <summary>
        /// Writes an array start token, without specifying a name for it.
        /// </summary>
        public void WriteArrayStart()
        {
            this.ThrowIfDisposed();
            this.ThrowIfNameRequiredAndMissing();
            this.ThrowIfMultipleRoots();

            if( this.parents.IsRoot )
                this.nameOfNextNode = "DataStore";

            this.parents.PushArray(this.nameOfNextNode);
            this.file.WriteToken(DataStoreToken.ArrayStart, this.nameOfNextNode, value: null, valueType: null);
            this.nameOfNextNode = null;
            this.rootOpened = true;
        }

        /// <summary>
        /// Writes an object start token, without specifying a name for it.
        /// </summary>
        public void WriteObjectStart()
        {
            this.ThrowIfDisposed();
            this.ThrowIfNameRequiredAndMissing();
            this.ThrowIfMultipleRoots();

            if( this.parents.IsRoot )
                this.nameOfNextNode = "DataStore";

            this.parents.PushObject(this.nameOfNextNode);
            this.file.WriteToken(DataStoreToken.ObjectStart, this.nameOfNextNode, value: null, valueType: null);
            this.nameOfNextNode = null;
            this.rootOpened = true;
        }

        /// <summary>
        /// Writes an end start token.
        /// </summary>
        public void WriteEnd()
        {
            this.ThrowIfDisposed();

            if( this.parents.IsRoot )
                throw new FormatException("There are no objects or array to close!").StoreFileLine();

            // NOTE: the name may or may not be specified, we will get it from the parent either way
            var parent = this.parents.PopParent();
            this.file.WriteToken(DataStoreToken.End, parent.Name, value: null, valueType: null);
            this.nameOfNextNode = null;

            if( this.parents.IsRoot )
                this.rootClosed = true;
        }

        /// <summary>
        /// Specifies the name of the next token to be written.
        /// </summary>
        /// <param name="name">The data store name to use for the next token.</param>
        public void WriteName( string name )
        {
            this.ThrowIfDisposed();

            if( !DataStore.IsValidName(name) )
                throw new FormatException("Invalid data store name!").Store(nameof(name), name).Store(nameof(this.CurrentPath), this.CurrentPath);

            if( this.parents.IsRoot
             || !this.parents.DirectParent.IsObject )
                throw new InvalidOperationException("Array children and the root node do not have names!").Store(nameof(name), name).Store(nameof(this.CurrentPath), this.CurrentPath);

            this.nameOfNextNode = name;
        }

        /// <summary>
        /// Writes a value token, along with it's content, without specifying a name for it.
        /// </summary>
        /// <param name="value">The value content to write.</param>
        /// <typeparam name="T">The type whose instance <paramref name="value"/> was serialized from.</typeparam>
        public void WriteValue<T>( string value )
        {
            this.ThrowIfDisposed();

            if( this.parents.IsRoot )
                throw new FormatException("A value is not a valid root node!").StoreFileLine();

            this.ThrowIfNameRequiredAndMissing();

            this.file.WriteToken(DataStoreToken.Value, this.nameOfNextNode, value, typeof(T));
            this.nameOfNextNode = null;
        }

        #endregion

        #region Extended Members

        /// <summary>
        /// Writes an array start token.
        /// </summary>
        /// <param name="name">The data store name to use.</param>
        public void WriteArrayStart( string name )
        {
            this.WriteName(name);
            this.WriteArrayStart();
        }

        /// <summary>
        /// Writes an object start token.
        /// </summary>
        /// <param name="name">The data store name to use.</param>
        public void WriteObjectStart( string name )
        {
            this.WriteName(name);
            this.WriteObjectStart();
        }

        /// <summary>
        /// Writes a value.
        /// </summary>
        /// <typeparam name="T">The type to serialize an instance of.</typeparam>
        /// <param name="value">The value content to write.</param>
        /// <param name="converter">The converter to use to serialize the <paramref name="value"/>; or <c>null</c> to get it from the <see cref="Converters"/> property.</param>
        public void Write<T>( T value, IStringConverter<T> converter = null )
        {
            if( converter.NullReference() )
                converter = this.Converters.GetConverter<T>();

            var stringValue = converter.ToString(value);
            this.WriteValue<T>(stringValue);
        }

        /// <summary>
        /// Writes a value.
        /// </summary>
        /// <typeparam name="T">The type to serialize an instance of.</typeparam>
        /// <param name="name">The data store name to use.</param>
        /// <param name="value">The value content to write.</param>
        /// <param name="converter">The converter to use to serialize the <paramref name="value"/>; or <c>null</c> to get it from the <see cref="Converters"/> property.</param>
        public void Write<T>( string name, T value, IStringConverter<T> converter = null )
        {
            this.WriteName(name);
            this.Write<T>(value, converter);
        }

        #endregion
    }
}
