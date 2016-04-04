using System;
using System.IO;
using System.Text;
using System.Xml;
using Mechanical3.Core;

namespace Mechanical3.DataStores.Xml
{
    /// <summary>
    /// Produces the latest XML data store file format.
    /// </summary>
    public class XmlFileFormatWriter : DisposableObject, IDataStoreTextFileFormatWriter
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

        private readonly ParentStack parents;
        private XmlWriter xmlWriter;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileFormatWriter"/> class.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> to use.</param>
        public XmlFileFormatWriter( XmlWriter writer )
        {
            if( writer.NullReference() )
                throw new ArgumentNullException(nameof(writer)).StoreFileLine();

            this.parents = new ParentStack();
            this.xmlWriter = writer;
        }

        /// <summary>
        /// Creates a writer from a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="textWriter">The <see cref="TextWriter"/> to use.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new xml file format writer instance.</returns>
        public static IDataStoreTextFileFormatWriter From( TextWriter textWriter, DataStoreFileFormatWriterOptions options = null )
        {
            if( textWriter.NullReference() )
                throw new ArgumentNullException(nameof(textWriter)).StoreFileLine();

            return new XmlFileFormatWriter(
                XmlWriter.Create(
                    textWriter,
                    ToXmlWriterSettings(options)));
        }

        /// <summary>
        /// Creates a writer from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to use.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new xml file format writer instance.</returns>
        public static IDataStoreTextFileFormatWriter From( Stream stream, DataStoreFileFormatWriterOptions options = null )
        {
            if( stream.NullReference() )
                throw new ArgumentNullException(nameof(stream)).StoreFileLine();

            if( options.NullReference() )
                options = DataStoreFileFormatWriterOptions.Default;

            return new XmlFileFormatWriter(
                XmlWriter.Create(
                    new StreamWriter(stream, options.Encoding) { NewLine = options.NewLine },
                    ToXmlWriterSettings(options)));
        }

        /// <summary>
        /// Creates a writer from a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to write to.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new xml file format writer instance.</returns>
        public static IDataStoreTextFileFormatWriter From( StringBuilder sb, DataStoreFileFormatWriterOptions options = null )
        {
            if( sb.NullReference() )
                throw new ArgumentNullException(nameof(sb)).StoreFileLine();

            if( options.NullReference() )
                options = DataStoreFileFormatWriterOptions.Default;

            return new XmlFileFormatWriter(
                XmlWriter.Create(
                    new StringWriterWithEncoding(sb, options.Encoding) { NewLine = options.NewLine },
                    ToXmlWriterSettings(options)));
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

                if( this.xmlWriter.NotNullReference() )
                {
                    this.xmlWriter.Dispose();
                    this.xmlWriter = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region Private Methods

        private static XmlWriterSettings ToXmlWriterSettings( DataStoreFileFormatWriterOptions options )
        {
            if( options.NullReference() )
                options = DataStoreFileFormatWriterOptions.Default;

            return new XmlWriterSettings()
            {
                Indent = options.Indent,
                NewLineChars = options.NewLine,
                Encoding = options.Encoding,
                CloseOutput = true
            };
        }

        private void WriteFormatVersion()
        {
            if( this.parents.IsRoot )
                this.xmlWriter.WriteAttributeString("formatVersion", "3");
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

            if( !this.parents.IsRoot
             && !this.parents.DirectParent.IsObject )
            {
                // generate name automatically for array children
                name = "i";
            }
            else if( !this.parents.IsRoot
                  && this.parents.DirectParent.IsObject
                  && token == DataStoreToken.End )
            {
                // use stored name for End tokens
                name = this.parents.DirectParent.Name;
            }
            else if( name.NullOrEmpty() )
            {
                throw new ArgumentException().Store(nameof(token), token).Store(nameof(name), name);
            }

            switch( token )
            {
            case DataStoreToken.Value:
                this.xmlWriter.WriteStartElement(name);
                this.WriteFormatVersion();

                if( value.NullReference() )
                {
                    this.xmlWriter.WriteEndElement(); // creates an empty element
                }
                else if( value.Length == 0 )
                {
                    this.xmlWriter.WriteAttributeString("type", "value");
                    this.xmlWriter.WriteFullEndElement();
                }
                else
                {
                    this.xmlWriter.WriteString(value);
                    this.xmlWriter.WriteEndElement();
                }
                break;

            case DataStoreToken.ObjectStart:
                this.xmlWriter.WriteStartElement(name);
                this.WriteFormatVersion();
                this.parents.PushObject(name);
                this.xmlWriter.WriteAttributeString("type", "object");
                break;

            case DataStoreToken.ArrayStart:
                this.xmlWriter.WriteStartElement(name);
                this.WriteFormatVersion();
                this.parents.PushArray(name);
                this.xmlWriter.WriteAttributeString("type", "array");
                break;

            case DataStoreToken.End:
                if( this.parents.IsRoot )
                    throw new ArgumentException("Invalid root token!").Store(nameof(token), token);
                this.parents.PopParent();
                this.xmlWriter.WriteFullEndElement();
                break;

            default:
                throw new ArgumentException("Unknown token!").Store(nameof(token), token).Store(nameof(name), name);
            }
        }

        #endregion
    }
}
