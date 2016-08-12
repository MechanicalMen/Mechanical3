using System;
using System.Globalization;
using Mechanical3.Core;
using Newtonsoft.Json;

namespace Mechanical3.DataStores.Json
{
    /// <summary>
    /// Parses the JSON data store file format.
    /// </summary>
    public class JsonFileFormatReader : DisposableObject, IDataStoreTextFileFormatReader
    {
        #region Private Fields

        private const int BeforeFirstReadDepth = int.MinValue;

        private JsonReader jsonReader;
        private int depth = BeforeFirstReadDepth;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFileFormatReader"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to use.</param>
        internal JsonFileFormatReader( JsonReader reader )
        {
            if( reader.NullReference() )
                throw new ArgumentNullException(nameof(reader)).StoreFileLine();

            this.jsonReader = reader;

            // read object start
            this.AssertCanRead();
            this.AssertToken(JsonToken.StartObject);

            // read version number
            this.ReadPropertyName("FormatVersion");
            this.AssertCanRead();
            this.AssertToken(JsonToken.Integer);
            if( Convert.ToInt32(this.jsonReader.Value) != 2 ) // the value type was "long" in tests
                throw new FormatException("Unknown JSON data store format version!").Store("expectedFormatVersion", 2).Store("actualFormatVersion", this.jsonReader.Value);

            // read data store start
            this.ReadPropertyName("DataStore");
        }

        #endregion

        #region Private Methods

        private void AssertCanRead()
        {
            if( !this.jsonReader.Read() )
                throw new FormatException("Unexpected end of stream!");
        }

        private void AssertToken( JsonToken token )
        {
            if( this.jsonReader.TokenType != token )
                throw new FormatException("Unexpected token found!").Store("expectedToken", token).Store("actualToken", this.jsonReader.TokenType);
        }

        private void ReadPropertyName( string name )
        {
            this.AssertCanRead();
            this.AssertToken(JsonToken.PropertyName);

            if( !string.Equals(name, (string)this.jsonReader.Value, StringComparison.Ordinal) )
                throw new FormatException("Unexpected property name found!").Store("expectedPropertyName", name).Store("actualPropertyName", this.jsonReader.Value);
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

                if( this.jsonReader.NotNullReference() )
                {
                    this.jsonReader.Close();
                    this.jsonReader = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


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

            return this.jsonReader is IJsonLineInfo;
        }

        /// <summary>
        /// Gets the current line number.
        /// </summary>
        /// <value>The current line number or <c>0</c> if no line information is available (for example, <see cref="HasLineInfo"/> returns <c>false</c>).</value>
        public int LineNumber
        {
            get
            {
                if( this.HasLineInfo() )
                    return ((IJsonLineInfo)this.jsonReader).LineNumber;
                else
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets the current line position.
        /// </summary>
        /// <value>The current line position or <c>0</c> if no line information is available (for example, <see cref="HasLineInfo"/> returns <c>false</c>).</value>
        public int LinePosition
        {
            get
            {
                if( this.HasLineInfo() )
                    return ((IJsonLineInfo)this.jsonReader).LinePosition;
                else
                    throw new NotSupportedException();
            }
        }

        #endregion

        #region IDataStoreTextFileFormatReader

        /// <summary>
        /// Tries to read the next token from the file.
        /// </summary>
        /// <param name="token">The token found.</param>
        /// <param name="name">The optional name associated with the token found.</param>
        /// <param name="value">The optional value associated with the token found.</param>
        /// <returns><c>true</c> if a token could be read; <c>false</c>, if the file has ended.</returns>
        public bool TryReadToken( out DataStoreToken token, out string name, out string value )
        {
            this.ThrowIfDisposed();

            // already finished reading?
            if( this.depth == 0 )
            {
                token = default(DataStoreToken);
                name = null;
                value = null;
                return false;
            }
            else
            {
                if( this.depth == BeforeFirstReadDepth )
                    this.depth = 0;
            }

            // read the next token
            if( !this.jsonReader.Read() )
            {
                token = default(DataStoreToken);
                name = null;
                value = null;
                return false;
            }

            // parse the token found
            switch( this.jsonReader.TokenType )
            {
            case JsonToken.Comment:
            case JsonToken.None:
                // skip
                return this.TryReadToken(out token, out name, out value);

            case JsonToken.PropertyName:
                {
                    name = (string)this.jsonReader.Value;
                    string nullName;
                    return this.TryReadToken(out token, out nullName, out value); // discard null name
                }

            case JsonToken.StartObject:
                ++this.depth;
                token = DataStoreToken.ObjectStart;
                name = null;
                value = null;
                break;

            case JsonToken.StartArray:
                ++this.depth;
                token = DataStoreToken.ArrayStart;
                name = null;
                value = null;
                break;

            case JsonToken.EndObject:
            case JsonToken.EndArray:
                --this.depth;
                token = DataStoreToken.End;
                name = null;
                value = null;
                break;

            //// NOTE: as of the writing of this class (Json.NET v8.0.3) there is no
            ////       way to get the raw string value: it is already converted by the time Read() returns.
            ////       Unfortunately this means that we need to convert it back to a valid JSON string,
            ////       but we don't know which format to use! (should floatnig point numbers have an exponent?)
            ////       We are basically just guessing, and hopefilly the string converter used later won't break.
            case JsonToken.Null:
                token = DataStoreToken.Value;
                name = null;
                value = null;
                break;
            case JsonToken.String:
                token = DataStoreToken.Value;
                name = null;
                value = (string)this.jsonReader.Value;
                break;
            case JsonToken.Boolean:
                token = DataStoreToken.Value;
                name = null;
                value = (bool)this.jsonReader.Value ? "true" : "false";
                break;
            case JsonToken.Integer:
                token = DataStoreToken.Value;
                name = null;
                value = SafeString.Print(this.jsonReader.Value, "D", CultureInfo.InvariantCulture);
                break;
            case JsonToken.Float:
                token = DataStoreToken.Value;
                name = null;
                value = SafeString.Print(
                    this.jsonReader.Value,
                    this.jsonReader.ValueType != typeof(decimal) ? "R" : "G", // float or double, vs. decimal
                    CultureInfo.InvariantCulture);
                break;

            default:
                throw new NotSupportedException().Store(nameof(this.jsonReader.TokenType), this.jsonReader.TokenType);
            }

            return true;
        }

        #endregion
    }
}
