using System;
using System.IO;
using System.Text;
using Mechanical3.Core;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Extension methods of the <see cref="Mechanical3.DataStores"/> namespace.
    /// </summary>
    public static class DataStoreExtensions
    {
        #region CreateReader

        /// <summary>
        /// Creates a new <see cref="IDataStoreTextFileFormatReader"/> instance.
        /// </summary>
        /// <param name="factory">The <see cref="IDataStoreTextFileFormatFactory"/> to use.</param>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <returns>A new <see cref="IDataStoreTextFileFormatReader"/> instance.</returns>
        public static IDataStoreTextFileFormatReader CreateReader( this IDataStoreTextFileFormatFactory factory, Stream stream )
        {
            if( stream.NullReference() )
                throw new ArgumentNullException(nameof(stream)).StoreFileLine();

            return factory.CreateReader(new StreamReader(stream));
        }

        /// <summary>
        /// Creates a new <see cref="IDataStoreTextFileFormatReader"/> instance.
        /// </summary>
        /// <param name="factory">The <see cref="IDataStoreTextFileFormatFactory"/> to use.</param>
        /// <param name="str">The <see cref="string"/> to read from.</param>
        /// <returns>A new <see cref="IDataStoreTextFileFormatReader"/> instance.</returns>
        public static IDataStoreTextFileFormatReader CreateReader( this IDataStoreTextFileFormatFactory factory, string str )
        {
            if( str.NullOrEmpty() )
                throw new ArgumentException().Store(nameof(str), str);

            return factory.CreateReader(new StringReader(str));
        }

        #endregion

        #region CreateWriter

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

        /// <summary>
        /// Creates a new <see cref="IDataStoreTextFileFormatWriter"/> instance.
        /// </summary>
        /// <param name="factory">The <see cref="IDataStoreTextFileFormatFactory"/> to use.</param>
        /// <param name="stream">The <see cref="Stream"/> to write to.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new <see cref="IDataStoreTextFileFormatWriter"/> instance.</returns>
        public static IDataStoreTextFileFormatWriter CreateWriter( this IDataStoreTextFileFormatFactory factory, Stream stream, DataStoreFileFormatWriterOptions options = null )
        {
            if( stream.NullReference() )
                throw new ArgumentNullException(nameof(stream)).StoreFileLine();

            if( options.NullReference() )
                options = DataStoreFileFormatWriterOptions.Default;

            return factory.CreateWriter(new StreamWriter(stream, options.Encoding) { NewLine = options.NewLine }, options);
        }

        /// <summary>
        /// Creates a new <see cref="IDataStoreTextFileFormatWriter"/> instance.
        /// </summary>
        /// <param name="factory">The <see cref="IDataStoreTextFileFormatFactory"/> to use.</param>
        /// <param name="sb">The <see cref="StringBuilder"/> to write to.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new <see cref="IDataStoreTextFileFormatWriter"/> instance.</returns>
        public static IDataStoreTextFileFormatWriter CreateWriter( this IDataStoreTextFileFormatFactory factory, StringBuilder sb, DataStoreFileFormatWriterOptions options = null )
        {
            if( sb.NullReference() )
                throw new ArgumentNullException(nameof(sb)).StoreFileLine();

            if( options.NullReference() )
                options = DataStoreFileFormatWriterOptions.Default;

            return factory.CreateWriter(new StringWriterWithEncoding(sb, options.Encoding) { NewLine = options.NewLine }, options);
        }

        #endregion
    }
}
