using System;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Converts instances of type <typeparamref name="T"/> to and from a <see cref="string"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert instances of to and from a <see cref="string"/>.</typeparam>
    public interface IStringConverter<T>
    {
        /// <summary>
        /// Converts the specified object to a <see cref="string"/>.
        /// </summary>
        /// <param name="obj">The object to convert to a string.</param>
        /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
        string ToString( T obj );

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <c>null</c>.</exception>
        bool TryParse( string str, out T obj );
    }
}
