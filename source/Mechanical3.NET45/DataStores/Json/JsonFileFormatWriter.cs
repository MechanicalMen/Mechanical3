using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mechanical3.Core;
using Newtonsoft.Json;

namespace Mechanical3.DataStores.Json
{
    /// <summary>
    /// Produces the JSON data store file format.
    /// </summary>
    public class JsonFileFormatWriter : DisposableObject, IDataStoreTextFileFormatWriter
    {
        #region StringWriterWithEncoding

        private class StringWriterWithEncoding : StringWriter
        {
            private readonly Encoding encoding;

            internal StringWriterWithEncoding( StringBuilder sb, Encoding encoding )
                : base(sb)
            {
                if( encoding.NullReference() )
                    throw new ArgumentNullException(nameof(encoding)).StoreFileLine();

                this.encoding = encoding;
            }

            public override Encoding Encoding
            {
                get { return this.encoding; }
            }
        }

        #endregion

        #region Private Fields

        private readonly HashSet<Type> rawValueTypes;
        private readonly Stack<DataStoreToken> parents;
        private JsonWriter jsonWriter;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFileFormatWriter"/> class.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to use.</param>
        public JsonFileFormatWriter( JsonWriter writer )
        {
            if( writer.NullReference() )
                throw new ArgumentNullException(nameof(writer)).StoreFileLine();

            this.rawValueTypes = new HashSet<Type>();
            this.rawValueTypes.Add(typeof(byte));
            this.rawValueTypes.Add(typeof(sbyte));
            this.rawValueTypes.Add(typeof(short));
            this.rawValueTypes.Add(typeof(ushort));
            this.rawValueTypes.Add(typeof(int));
            this.rawValueTypes.Add(typeof(uint));
            this.rawValueTypes.Add(typeof(long));
            this.rawValueTypes.Add(typeof(ulong));
            this.rawValueTypes.Add(typeof(float));
            this.rawValueTypes.Add(typeof(double));
            this.rawValueTypes.Add(typeof(decimal));
            //// this.rawValueTypes.Add(typeof(bool)); // we handle boolean separately

            this.parents = new Stack<DataStoreToken>();
            this.jsonWriter = writer;

            this.jsonWriter.WriteStartObject();
            this.jsonWriter.WritePropertyName("FormatVersion");
            this.jsonWriter.WriteRawValue("2");
        }

        /// <summary>
        /// Creates a writer from a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="textWriter">The <see cref="TextWriter"/> to use.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new json file format writer instance.</returns>
        public static IDataStoreTextFileFormatWriter From( TextWriter textWriter, DataStoreFileFormatWriterOptions options = null )
        {
            if( textWriter.NullReference() )
                throw new ArgumentNullException(nameof(textWriter)).StoreFileLine();

            if( options.NullReference() )
                options = DataStoreFileFormatWriterOptions.Default;

            if( textWriter.Encoding != options.Encoding )
                throw new ArgumentException("Invalid TextWriter encoding!").Store("actualEncoding", textWriter.Encoding.EncodingName).Store("expectedEncoding", options.Encoding.EncodingName);

            textWriter.NewLine = options.NewLine;
            var jsonWriter = new JsonTextWriter(textWriter);
            jsonWriter.Formatting = options.Indent ? Formatting.Indented : Formatting.None;
            return new JsonFileFormatWriter(jsonWriter);
        }

        /// <summary>
        /// Creates a writer from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to use.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new json file format writer instance.</returns>
        public static IDataStoreTextFileFormatWriter From( Stream stream, DataStoreFileFormatWriterOptions options = null )
        {
            if( stream.NullReference() )
                throw new ArgumentNullException(nameof(stream)).StoreFileLine();

            if( options.NullReference() )
                options = DataStoreFileFormatWriterOptions.Default;

            return From(new StreamWriter(stream, options.Encoding));
        }

        /// <summary>
        /// Creates a writer from a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to write to.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new json file format writer instance.</returns>
        public static IDataStoreTextFileFormatWriter From( StringBuilder sb, DataStoreFileFormatWriterOptions options = null )
        {
            if( sb.NullReference() )
                throw new ArgumentNullException(nameof(sb)).StoreFileLine();

            if( options.NullReference() )
                options = DataStoreFileFormatWriterOptions.Default;

            return From(new StringWriterWithEncoding(sb, options.Encoding));
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

                if( this.jsonWriter.NotNullReference() )
                {
                    if( this.parents.Count == 0 )
                        this.jsonWriter.WriteEndObject();

                    this.jsonWriter.Close();
                    this.jsonWriter = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region Private Methods

        private static bool IsNumber( string str )
        {
            foreach( char ch in str )
            {
                if( (ch < '0' || '9' < ch) // not a digit
                 && ch != '.'
                 && ch != 'e'
                 && ch != 'E'
                 && ch != '+'
                 && ch != '-' )
                    return false;
            }
            return true;
        }

        private static bool IsBoolean( string str )
        {
            return string.Equals(str, "true", StringComparison.Ordinal)
                || string.Equals(str, "false", StringComparison.Ordinal);
        }

        #endregion

        #region IDataStoreTextFileFormatWriter

        /// <summary>
        /// Writes the next token of the file format.
        /// </summary>
        /// <param name="token">The token to write.</param>
        /// <param name="name">The data store name of the token.</param>
        /// <param name="value">The serialized value of a <see cref="DataStoreToken.Value"/>.</param>
        /// <param name="valueType">The type whose instance <paramref name="value"/> was serialized from.</param>
        public void WriteToken( DataStoreToken token, string name, string value, Type valueType )
        {
            this.ThrowIfDisposed();

            if( this.parents.Count == 0 )
            {
                this.jsonWriter.WritePropertyName("DataStore");
            }
            else
            {
                if( this.parents.Peek() == DataStoreToken.ObjectStart
                 && token != DataStoreToken.End )
                {
                    if( name.NullOrEmpty() )
                        throw new ArgumentException().Store(nameof(token), token).Store(nameof(name), name);
                    else
                        this.jsonWriter.WritePropertyName(name);
                }
            }

            switch( token )
            {
            case DataStoreToken.Value:
                if( value.NullReference() )
                {
                    this.jsonWriter.WriteNull();
                }
                else if( valueType == typeof(bool)
                      && IsBoolean(value) ) // make sure this is valid JSON
                {
                    this.jsonWriter.WriteRawValue(value);
                }
                else if( this.rawValueTypes.Contains(valueType)
                      && IsNumber(value) ) // make sure the number format is supported by the JSON format
                {
                    this.jsonWriter.WriteRawValue(value);
                }
                else
                {
                    this.jsonWriter.WriteRawValue('"' + value + '"');
                }
                break;

            case DataStoreToken.ObjectStart:
                this.parents.Push(token);
                this.jsonWriter.WriteStartObject();
                break;

            case DataStoreToken.ArrayStart:
                this.parents.Push(token);
                this.jsonWriter.WriteStartArray();
                break;

            case DataStoreToken.End:
                {
                    if( this.parents.Count == 0 )
                        throw new ArgumentException("Invalid root token!").Store(nameof(token), token);
                    var parentToken = this.parents.Pop();
                    if( parentToken == DataStoreToken.ObjectStart )
                        this.jsonWriter.WriteEndObject();
                    else
                        this.jsonWriter.WriteEndArray();
                }
                break;

            default:
                throw new ArgumentException("Unknown token!").Store(nameof(token), token).Store(nameof(name), name);
            }
        }

        #endregion
    }
}
