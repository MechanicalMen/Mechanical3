using System.Xml;
using Mechanical3.Core;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Reads a text based file format, like XML or JSON.
    /// </summary>
    public interface IDataStoreTextFileFormatReader : IDisposableObject, IXmlLineInfo
    {
        /// <summary>
        /// Tries to read the next token from the file.
        /// </summary>
        /// <param name="token">The token found.</param>
        /// <param name="name">The optional name associated with the token found.</param>
        /// <param name="value">The optional value associated with the token found.</param>
        /// <returns><c>true</c> if a token could be read; <c>false</c>, if the file has ended.</returns>
        bool TryReadToken( out DataStoreToken token, out string name, out string value );
    }
}
