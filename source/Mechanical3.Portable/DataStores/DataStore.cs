using System;
using System.Runtime.CompilerServices;
using Mechanical3.Core;

namespace Mechanical3.DataStores
{
    /* Data stores abstract away:
     *    - the file format (XML, JSON, ... etc.)
     *    - the data format (decimal separator, date time format string, ... etc.)
     */

    /* A data store is a hierarchical data structure:
     *    - There are 3 types of nodes: values, objects and arrays
     *    - values have a string content, but no children (null and empty strings are valid)
     *    - objects have named children, while arrays have indexed children
     *    - the root node may not be a value, and has no name
     *    - there must always be a root node
     *    - both objects and arrays preserve the writing order of their children
     */

    /// <summary>
    /// Data store related helper members.
    /// </summary>
    public static class DataStore
    {
        /// <summary>
        /// Compares data store names.
        /// </summary>
        public static readonly StringComparer NameComparer = StringComparer.Ordinal;

        #region IsValidName

        /// <summary>
        /// Determines whether the specified string is a valid data store name.
        /// </summary>
        /// <param name="name">The string to examine.</param>
        /// <returns><c>true</c> if the specified string is a valid data store name; otherwise, <c>false</c>.</returns>
        public static bool IsValidName( string name )
        {
            if( name.NullOrEmpty() )
                return false;

            if( name.Length > 255 ) // max. file name length
                return false;

            if( !IsValidFirstCharacter(name[0]) )
                return false;

            for( int i = 1; i < name.Length; ++i )
            {
                if( !IsValidMiddleCharacter(name[i]) )
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidFirstCharacter( char ch )
        {
            return ('a' <= ch && ch <= 'z')
                || ('A' <= ch && ch <= 'Z')
                || ch == '_';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidMiddleCharacter( char ch )
        {
            return IsValidFirstCharacter(ch)
                || ('0' <= ch && ch <= '9');
        }

        #endregion

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
            if( locator.NullReference() )
                locator = RoundTripStringConverter.Locator;

            IStringConverter<T> converter;
            try
            {
                converter = locator.GetConverter<T>();
            }
            catch
            {
                obj = default(T);
                return false;
            }
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
            if( locator.NullReference() )
                locator = RoundTripStringConverter.Locator;

            T result;
            var converter = locator.GetConverter<T>();
            if( converter.TryParse(str, out result) )
                return result;
            else
                throw new FormatException().Store(nameof(str), str);
        }

        #endregion
    }
}
