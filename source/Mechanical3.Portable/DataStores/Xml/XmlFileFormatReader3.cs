using System;
using System.Collections.Generic;
using System.Xml;
using Mechanical3.Core;

namespace Mechanical3.DataStores.Xml
{
    /// <summary>
    /// Parses an xml file using xml data store format version 3.
    /// Expects the reader to be positioned at the root of the document,
    /// with the format version having already been verified.
    /// </summary>
    internal class XmlFileFormatReader3 : DisposableObject, IDataStoreTextFileFormatReader
    {
        #region Private Fields

        private readonly Stack<DataStoreToken> parents;
        private XmlReader xmlReader;
        private bool isAtRootNode;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileFormatReader3"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="XmlReader"/> to use.</param>
        internal XmlFileFormatReader3( XmlReader reader )
        {
            if( reader.NullReference() )
                throw new ArgumentNullException(nameof(reader)).StoreFileLine();

            this.parents = new Stack<DataStoreToken>();
            this.xmlReader = reader;
            this.isAtRootNode = true;
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

        private void ReadValueClosingNode( out string value )
        {
            //// assuming we are at the opening node of the value,
            //// or one of the opening node's attributes

            if( !this.MoveToNextStartOrEndOrTextElement() )
                throw new FormatException("Unexpected end of stream!").StoreFileLine();

            // consume content (and move to closing tnode if not found)
            if( this.xmlReader.NodeType == XmlNodeType.Text )
            {
                value = this.xmlReader.Value;
                if( !this.MoveToNextStartOrEndOrTextElement() )
                    throw new FormatException("Unexpected end of stream!").StoreFileLine();
            }
            else
            {
                value = string.Empty;
            }

            // consume closing node
            if( this.xmlReader.NodeType != XmlNodeType.EndElement )
                throw new FormatException("Closing node not found!").Store(nameof(XmlReader.NodeType), this.xmlReader.NodeType);
        }

        private bool HasTypeAttribute( out string typeAttr )
        {
            if( this.xmlReader.MoveToFirstAttribute() )
            {
                do
                {
                    if( string.Equals(this.xmlReader.Name, "type", StringComparison.Ordinal) )
                    {
                        typeAttr = this.xmlReader.Value;
                        return true; // any remaining attributes will be skipped later
                    }
                }
                while( this.xmlReader.MoveToNextAttribute() );
            }

            typeAttr = null;
            return false;
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

            return this.xmlReader is IXmlLineInfo;
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
                    return ((IXmlLineInfo)this.xmlReader).LineNumber;
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
                    return ((IXmlLineInfo)this.xmlReader).LinePosition;
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

            // read up until the next node of interest,
            // unless we are just starting from the root node
            if( this.isAtRootNode )
            {
                this.isAtRootNode = false;
            }
            else
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

                    string typeAttr;
                    if( this.xmlReader.IsEmptyElement )
                    {
                        // <null_value />
                        token = DataStoreToken.Value;
                        value = null;
                        return true;
                    }
                    else if( !this.HasTypeAttribute(out typeAttr) )
                    {
                        // <value>3.14</value>
                        token = DataStoreToken.Value;
                        this.ReadValueClosingNode(out value);
                        return true;
                    }
                    else
                    {
                        // <node type="???">
                        if( string.Equals(typeAttr, "value", StringComparison.Ordinal) )
                        {
                            token = DataStoreToken.Value;
                            this.ReadValueClosingNode(out value);
                            return true;
                        }
                        else if( string.Equals(typeAttr, "object", StringComparison.Ordinal) )
                        {
                            token = DataStoreToken.ObjectStart;
                            value = null;

                            this.parents.Push(token);
                            return true;
                        }
                        else if( string.Equals(typeAttr, "array", StringComparison.Ordinal) )
                        {
                            token = DataStoreToken.ArrayStart;
                            value = null;

                            this.parents.Push(token);
                            return true;
                        }
                        else
                        {
                            throw new FormatException("Unknown type attribute value!").Store(nameof(typeAttr), typeAttr);
                        }
                    }
                }

            case XmlNodeType.EndElement:
                {
                    if( this.parents.Count == 0 )
                        throw new FormatException("Unexpected closing node found!").StoreFileLine();

                    // closing array or object node
                    token = DataStoreToken.End;
                    name = this.xmlReader.Name;
                    value = null;

                    this.parents.Pop();
                    return true;
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
