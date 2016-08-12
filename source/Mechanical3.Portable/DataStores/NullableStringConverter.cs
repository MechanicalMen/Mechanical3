using System;
using Mechanical3.Core;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Wraps the converter of a value type, to convert nullable instances of it.
    /// </summary>
    /// <typeparam name="T">The value type to convert nullable instances of.</typeparam>
    public class NullableStringConverter<T> : IStringConverter<T?>
        where T : struct
    {
        private readonly IStringConverter<T> converter;

        /// <summary>
        /// Initializes a new instance of the <see cref="NullableStringConverter{T}"/> class.
        /// </summary>
        /// <param name="valueConverter">The converter to wrap.</param>
        public NullableStringConverter( IStringConverter<T> valueConverter )
        {
            if( valueConverter.NullReference() )
                throw new ArgumentNullException(nameof(valueConverter)).StoreFileLine();

            this.converter = valueConverter;
        }

        /// <summary>
        /// Converts the specified object to a <see cref="string"/>.
        /// </summary>
        /// <param name="obj">The object to convert to a string.</param>
        /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
        public string ToString( T? obj )
        {
            if( obj.HasValue )
                return this.converter.ToString(obj.Value);
            else
                return null;
        }

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        public bool TryParse( string str, out T? obj )
        {
            if( str.NullReference() )
            {
                obj = null;
                return true;
            }
            else
            {
                T tmp;
                if( this.converter.TryParse(str, out tmp) )
                {
                    obj = tmp;
                    return true;
                }
                else
                {
                    obj = null;
                    return false;
                }
            }
        }
    }
}
