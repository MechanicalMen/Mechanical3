using System;
using System.Xml;
using Mechanical3.Core;

namespace Mechanical3.DataStores.Xml
{
    /* The legacy Mechanical2 XML format, compared to the new version 3 format:
     *  - mechanical2 encloses everything in a tag that is always named "root", and has no attributes
     *    (this was done to support empty data stores, similar to IEnumerator, but was never actually used by serializers)
     *  - mechanical2 has names for root objects
     *  - non empty values and objects are differentiated based on the first child node
     *  - empty values are represented by an empty element, and null values are not supported
     *  - arrays are not supported
     */

    /// <summary>
    /// Parses an xml file using xml data store format version 2 (the format used in Mechanical2).
    /// Expects the reader to be positioned at the root of the document.
    /// </summary>
    internal class XmlFileFormatReader2 : DisposableObject, IDataStoreTextFileFormatReader
    {
        //// NOTE: this is not intended to be very rugged and well rounded,
        ////       it just needs to reliably parse programmatically generated Mechanical2 XML data stores.

        #region Private Fields

        private XmlReader xmlReader;
        private bool needToMoveToNextToken = true;
        private int depth = 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileFormatReader2"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="XmlReader"/> to use.</param>
        internal XmlFileFormatReader2( XmlReader reader )
        {
            if( reader.NullReference() )
                throw new ArgumentNullException(nameof(reader)).StoreFileLine();

            this.xmlReader = reader;
        }

        #endregion

        #region Private Methods

        private bool MoveToNextStartOrEndOrTextElement()
        {
            do
            {
                if( !this.xmlReader.Read() )
                    return false;
            }
            while( this.xmlReader.NodeType != XmlNodeType.Element
                && this.xmlReader.NodeType != XmlNodeType.EndElement
                && this.xmlReader.NodeType != XmlNodeType.Text );

            return true;
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

                if( this.xmlReader.NotNullReference() )
                {
                    this.xmlReader.Dispose();
                    this.xmlReader = null;
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

            return ((IXmlLineInfo)this.xmlReader).HasLineInfo();
        }

        /// <summary>
        /// Gets the current line number.
        /// </summary>
        /// <value>The current line number or <c>0</c> if no line information is available (for example, <see cref="HasLineInfo"/> returns <c>false</c>).</value>
        public int LineNumber
        {
            get
            {
                this.ThrowIfDisposed();

                return ((IXmlLineInfo)this.xmlReader).LineNumber;
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
                this.ThrowIfDisposed();

                return ((IXmlLineInfo)this.xmlReader).LinePosition;
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

            // read up until the next node of interest
            if( this.needToMoveToNextToken )
            {
                if( !this.MoveToNextStartOrEndOrTextElement() )
                {
                    token = default(DataStoreToken);
                    name = null;
                    value = null;
                    return false;
                }
            }

            // parse the node found
            switch( this.xmlReader.NodeType )
            {
            case XmlNodeType.Element:
                {
                    name = this.xmlReader.Name;

                    if( this.xmlReader.IsEmptyElement )
                    {
                        token = DataStoreToken.Value;
                        value = string.Empty;
                        this.needToMoveToNextToken = true;
                        return true;
                    }
                    else
                    {
                        //// value or object?

                        this.MoveToNextStartOrEndOrTextElement();
                        if( this.xmlReader.NodeType == XmlNodeType.Text )
                        {
                            token = DataStoreToken.Value;
                            value = this.xmlReader.Value;
                            this.MoveToNextStartOrEndOrTextElement(); // read end element
                            this.needToMoveToNextToken = true;
                            return true;
                        }
                        else
                        {
                            token = DataStoreToken.ObjectStart;
                            value = null;
                            this.needToMoveToNextToken = false;
                            ++this.depth;
                            return true;
                        }
                    }
                }

            case XmlNodeType.EndElement:
                if( this.depth > 0 )
                {
                    token = DataStoreToken.End;
                    name = this.xmlReader.Name;
                    value = null;
                    this.needToMoveToNextToken = true;
                    --this.depth;
                    return true;
                }
                else
                {
                    token = default(DataStoreToken);
                    name = null;
                    value = null;
                    this.needToMoveToNextToken = true;
                    return false;
                }

            case XmlNodeType.Text:
                throw new FormatException("Unexpected text content found!").Store(nameof(XmlReader.Value), this.xmlReader.Value);

            default:
                throw new NotImplementedException().Store(nameof(XmlReader.NodeType), this.xmlReader.NodeType);
            }
        }

        #endregion
    }
}
