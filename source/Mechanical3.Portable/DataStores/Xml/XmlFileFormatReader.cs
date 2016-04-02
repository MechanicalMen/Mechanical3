using System;
using System.Globalization;
using System.IO;
using System.Xml;
using Mechanical3.Core;

namespace Mechanical3.DataStores.Xml
{
    /// <summary>
    /// Creates an <see cref="IDataStoreTextFileFormatReader"/> for Mechanical3 and Mechanical2 xml data store formats.
    /// </summary>
    public static class XmlFileFormatReader
    {
        /// <summary>
        /// Creates a reader from an <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="xmlReader">The <see cref="XmlReader"/> to use.</param>
        /// <returns>A new xml file format reader instance.</returns>
        public static IDataStoreTextFileFormatReader From( XmlReader xmlReader )
        {
            if( xmlReader.NullReference() )
                throw new ArgumentNullException(nameof(xmlReader)).StoreFileLine();

            // move to root node
            while( xmlReader.MoveToContent() != XmlNodeType.Element )
            {
            }

            // look for formatVersion attribute
            int formatVersion = 2; // no formatVersion attribute == assume Mechanical2 format
            if( xmlReader.HasAttributes )
            {
                string versionText = null;
                xmlReader.MoveToFirstAttribute();
                do
                {
                    if( string.Equals(xmlReader.Name, "formatVersion", StringComparison.Ordinal) )
                    {
                        versionText = xmlReader.Value;
                        break;
                    }
                }
                while( xmlReader.MoveToNextAttribute() );

                if( versionText.NotNullReference() )
                {
                    if( !int.TryParse(versionText, NumberStyles.None, CultureInfo.InvariantCulture, out formatVersion) )
                        throw new FormatException("Could not parse \"formatVersion\" attribute!").Store(nameof(versionText), versionText);
                }

                // move back to root node
                xmlReader.MoveToElement();
            }

            // create the reader for the expected format
            switch( formatVersion )
            {
            case 2:
                return new XmlFileFormatReader2(xmlReader);

            case 3:
                return new XmlFileFormatReader3(xmlReader);

            default:
                throw new FormatException("Unknown xml format version!").Store(nameof(formatVersion), formatVersion);
            }
        }

        /// <summary>
        /// Creates a reader from a <see cref="TextReader"/>.
        /// </summary>
        /// <param name="textReader">The <see cref="TextReader"/> to use.</param>
        /// <returns>A new xml file format reader instance.</returns>
        public static IDataStoreTextFileFormatReader From( TextReader textReader )
        {
            if( textReader.NullReference() )
                throw new ArgumentNullException(nameof(textReader)).StoreFileLine();

            return From(XmlReader.Create(textReader));
        }

        /// <summary>
        /// Creates a reader from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to use.</param>
        /// <returns>A new xml file format reader instance.</returns>
        public static IDataStoreTextFileFormatReader From( Stream stream )
        {
            if( stream.NullReference() )
                throw new ArgumentNullException(nameof(stream)).StoreFileLine();

            return From(XmlReader.Create(stream));
        }

        /// <summary>
        /// Creates a reader from an XML string.
        /// </summary>
        /// <param name="xml">The XML string to read.</param>
        /// <returns>A new xml file format reader instance.</returns>
        public static IDataStoreTextFileFormatReader FromXml( string xml )
        {
            if( xml.NullOrEmpty() )
                throw new ArgumentException("Xml readers can only be created from valid xml ").Store(nameof(xml), xml);

            return From(new StringReader(xml));
        }
    }
}
