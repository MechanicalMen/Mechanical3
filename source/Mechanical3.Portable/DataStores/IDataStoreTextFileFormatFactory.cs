using System.IO;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Creates readers and writers for a file format.
    /// </summary>
    public interface IDataStoreTextFileFormatFactory
    {
        /// <summary>
        /// Gets the file name extension to use. <c>null</c> or empty strings mean no extension should be used.
        /// </summary>
        /// <value>The file name extension to use.</value>
        string FileNameExtension { get; }

        /// <summary>
        /// Creates a new <see cref="IDataStoreTextFileFormatReader"/> instance.
        /// </summary>
        /// <param name="textReader">The <see cref="TextReader"/> to read from.</param>
        /// <returns>A new <see cref="IDataStoreTextFileFormatReader"/> instance.</returns>
        IDataStoreTextFileFormatReader CreateReader( TextReader textReader );

        /// <summary>
        /// Creates a new <see cref="IDataStoreTextFileFormatWriter"/> instance.
        /// </summary>
        /// <param name="textWriter">The <see cref="TextWriter"/> to write to.</param>
        /// <param name="options">The file formatting options to use; or <c>null</c> to use the default formatting.</param>
        /// <returns>A new <see cref="IDataStoreTextFileFormatWriter"/> instance.</returns>
        IDataStoreTextFileFormatWriter CreateWriter( TextWriter textWriter, DataStoreFileFormatWriterOptions options = null );
    }
}
