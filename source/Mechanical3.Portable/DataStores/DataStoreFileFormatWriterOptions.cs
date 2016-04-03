using System;
using System.Text;
using Mechanical3.Core;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Common options of data store file format writers.
    /// </summary>
    public class DataStoreFileFormatWriterOptions
    {
        #region Private Fields

        private bool indent = true;
        private string newLine = "\n";
        private Encoding encoding = Encoding.UTF8;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether to indent the file format.
        /// </summary>
        /// <value><c>true</c> to indent the file format; otherwise, <c>false</c>.</value>
        public bool Indent
        {
            get { return this.indent; }
            set { this.indent = value; }
        }

        /// <summary>
        /// Gets or sets the line terminator string to use.
        /// </summary>
        /// <value>The line terminator string to use.</value>
        public string NewLine
        {
            get
            {
                return this.newLine;
            }
            set
            {
                if( value.NullOrEmpty() )
                    throw new ArgumentException().Store(nameof(value), value);

                this.newLine = value;
            }
        }

        /// <summary>
        /// Gets or sets the text encoding to use.
        /// </summary>
        /// <value>The text encoding to use.</value>
        public Encoding Encoding
        {
            get
            {
                return this.encoding;
            }
            set
            {
                if( value.NullReference() )
                    throw new ArgumentNullException(nameof(value));

                this.encoding = value;
            }
        }

        #endregion

        /// <summary>
        /// The default instance of the <see cref="DataStoreFileFormatWriterOptions"/> class.
        /// </summary>
        public static readonly DataStoreFileFormatWriterOptions Default = new DataStoreFileFormatWriterOptions();
    }
}
