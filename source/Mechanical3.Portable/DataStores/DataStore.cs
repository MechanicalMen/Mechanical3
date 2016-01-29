using System;
using Mechanical3.Core;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Data store related helper members.
    /// </summary>
    public static class DataStore
    {
        #region ToString, TryParse, Parse

        /// <summary>
        /// Converts the specified object to a <see cref="string"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert an instance of.</typeparam>
        /// <param name="obj">The object to convert to a string.</param>
        /// <param name="locator">The <see cref="IStringConverterLocator"/> to use; or <c>null</c> for <see cref="RoundTripStringConverter.Locator"/>.</param>
        /// <returns>The string representation of the specified object.</returns>
        public static string ToString<T>( T obj, IStringConverterLocator locator = null )
        {
            if( locator.NullReference() )
                locator = RoundTripStringConverter.Locator;

            try
            {
                var converter = locator.GetConverter<T>();
                return converter.ToString(obj);
            }
            catch( Exception e )
            {
                e.Store(nameof(obj), obj);
                throw;
            }
        }

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert an instance of.</typeparam>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <param name="locator">The <see cref="IStringConverterLocator"/> to use; or <c>null</c> for <see cref="RoundTripStringConverter.Locator"/>.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <c>null</c>.</exception>
        public static bool TryParse<T>( string str, out T obj, IStringConverterLocator locator = null )
        {
            if( str.NullReference() )
            {
                obj = default(T);
                return false;
            }

            if( locator.NullReference() )
                locator = RoundTripStringConverter.Locator;

            var converter = locator.GetConverter<T>();
            return converter.TryParse(str, out obj);
        }

        /// <summary>
        /// Parses the specified <see cref="string"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert an instance of.</typeparam>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="converter">The <see cref="IStringConverter{T}"/> to use.</param>
        /// <returns>A restored instance of type <typeparamref name="T"/>.</returns>
        public static T Parse<T>( string str, IStringConverter<T> converter )
        {
            if( str.NullReference() )
                throw new ArgumentNullException(nameof(str)).StoreFileLine();

            if( converter.NullReference() )
                throw new ArgumentNullException(nameof(converter)).StoreFileLine();

            T result;
            if( converter.TryParse(str, out result) )
                return result;
            else
                throw new FormatException().Store(nameof(str), str);
        }

        /// <summary>
        /// Parses the specified <see cref="string"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert an instance of.</typeparam>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="locator">The <see cref="IStringConverterLocator"/> to use; or <c>null</c> for <see cref="RoundTripStringConverter.Locator"/>.</param>
        /// <returns>A restored instance of type <typeparamref name="T"/>.</returns>
        public static T Parse<T>( string str, IStringConverterLocator locator = null )
        {
            if( str.NullReference() )
                throw new ArgumentNullException(nameof(str)).StoreFileLine();

            T result;
            if( TryParse(str, out result) )
                return result;
            else
                throw new FormatException().Store(nameof(str), str);
        }

        #endregion
    }
}
