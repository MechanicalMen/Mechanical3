using System;
using Mechanical3.Core;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Writes a text based file format, like XML or JSON.
    /// </summary>
    public interface IDataStoreTextFileFormatWriter : IDisposableObject
    {
        /// <summary>
        /// Writes the next token of the file format.
        /// </summary>
        /// <param name="token">The token to write.</param>
        /// <param name="name">The data store name of the token.</param>
        /// <param name="value">The serialized value of a <see cref="DataStoreToken.Value"/>.</param>
        /// <param name="valueType">The type whose instance <paramref name="value"/> was serialized from.</param>
        void WriteToken( DataStoreToken token, string name, string value, Type valueType );
    }
}
