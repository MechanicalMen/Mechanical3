using System;
using System.IO;
using System.Xml;
using Mechanical3.Core;

namespace Mechanical3.DataStores.Xml
{
    /// <summary>
    /// Creates readers and writers for the XML file format.
    /// </summary>
    public class XmlFileFormatFactory : IDataStoreTextFileFormatFactory
    {
        /// <summary>
        /// The default instance of the type.
        /// </summary>
        public static readonly XmlFileFormatFactory Default = new XmlFileFormatFactory();

        #region IDataStoreTextFileFormatFactory

        /// <summary>
        /// Gets the file name extension to use. <c>null</c> or empty strings mean no extension should be used.
        /// </summary>
        /// <value>The file name extension to use.</value>
        public string FileNameExtension
        {
            get { return ".xml"; }
        }

        /// <summary>
        /// Creates a new <see cref="IDataStoreTextFileFormatReader"/> instance.
        /// </summary>
        /// <param name="textReader">The <see cref="TextReader"/> to read from.</param>
        /// <returns>A new <see cref="IDataStoreTextFileFormatReader"/> instance.</returns>
        public IDataStoreTextFileFormatReader CreateReader( TextReader textReader )
        {
            if( textReader.NullReference() )
                throw new ArgumentNullException(nameof(textReader)).StoreFileLine();

            return XmlFileFormatReader.From(XmlReader.Create(textReader));
        }

        /// <summary>
        /// Creates a new <see cref="IDataStoreTextFileFormatWriter"/> instance.
        /// </summary>
        /// <param name="textWriter">The <see cref="TextWriter"/> to write to.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new <see cref="IDataStoreTextFileFormatWriter"/> instance.</returns>
        public IDataStoreTextFileFormatWriter CreateWriter( TextWriter textWriter, DataStoreFileFormatWriterOptions options = null )
        {
            if( textWriter.NullReference() )
                throw new ArgumentNullException(nameof(textWriter)).StoreFileLine();

            if( options.NullReference() )
                options = DataStoreFileFormatWriterOptions.Default;

            if( textWriter.Encoding != options.Encoding )
                throw new ArgumentException("Invalid TextWriter encoding!").Store("actualEncoding", textWriter.Encoding.WebName).Store("expectedEncoding", options.Encoding.WebName);

            var settings = new XmlWriterSettings()
            {
                Indent = options.Indent,
                NewLineChars = options.NewLine,
                Encoding = options.Encoding,
                CloseOutput = true
            };

            return new XmlFileFormatWriter(
                XmlWriter.Create(
                    textWriter,
                    settings));
        }

        #endregion
    }
}
