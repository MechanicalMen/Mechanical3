using System;
using System.Globalization;
using Mechanical3.Core;
using Mechanical3.IO.FileSystems;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Implements <see cref="IStringConverter{T}"/> for basic, commonly used, built-in types (like int, float, ... etc.).
    /// </summary>
    public static class RoundTripStringConverter
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        #region SByte

        private class SByteConverter : IStringConverter<sbyte>
        {
            public string ToString( sbyte obj )
            {
                return obj.ToString("D", Culture);
            }

            public bool TryParse( string str, out sbyte obj )
            {
                return sbyte.TryParse(str, NumberStyles.Integer, Culture, out obj);
            }
        }

        #endregion

        #region Byte

        private class ByteConverter : IStringConverter<byte>
        {
            public string ToString( byte obj )
            {
                return obj.ToString("D", Culture);
            }

            public bool TryParse( string str, out byte obj )
            {
                return byte.TryParse(str, NumberStyles.Integer, Culture, out obj);
            }
        }

        #endregion

        #region Int16

        private class Int16Converter : IStringConverter<short>
        {
            public string ToString( short obj )
            {
                return obj.ToString("D", Culture);
            }

            public bool TryParse( string str, out short obj )
            {
                return short.TryParse(str, NumberStyles.Integer, Culture, out obj);
            }
        }

        #endregion

        #region UInt16

        private class UInt16Converter : IStringConverter<ushort>
        {
            public string ToString( ushort obj )
            {
                return obj.ToString("D", Culture);
            }

            public bool TryParse( string str, out ushort obj )
            {
                return ushort.TryParse(str, NumberStyles.Integer, Culture, out obj);
            }
        }

        #endregion

        #region Int32

        private class Int32Converter : IStringConverter<int>
        {
            public string ToString( int obj )
            {
                return obj.ToString("D", Culture);
            }

            public bool TryParse( string str, out int obj )
            {
                return int.TryParse(str, NumberStyles.Integer, Culture, out obj);
            }
        }

        #endregion

        #region UInt32

        private class UInt32Converter : IStringConverter<uint>
        {
            public string ToString( uint obj )
            {
                return obj.ToString("D", Culture);
            }

            public bool TryParse( string str, out uint obj )
            {
                return uint.TryParse(str, NumberStyles.Integer, Culture, out obj);
            }
        }

        #endregion

        #region Int64

        private class Int64Converter : IStringConverter<long>
        {
            public string ToString( long obj )
            {
                return obj.ToString("D", Culture);
            }

            public bool TryParse( string str, out long obj )
            {
                return long.TryParse(str, NumberStyles.Integer, Culture, out obj);
            }
        }

        #endregion

        #region UInt64

        private class UInt64Converter : IStringConverter<ulong>
        {
            public string ToString( ulong obj )
            {
                return obj.ToString("D", Culture);
            }

            public bool TryParse( string str, out ulong obj )
            {
                return ulong.TryParse(str, NumberStyles.Integer, Culture, out obj);
            }
        }

        #endregion

        #region Single

        private class SingleConverter : IStringConverter<float>
        {
            public string ToString( float obj )
            {
                return obj.ToString("R", Culture);
            }

            public bool TryParse( string str, out float obj )
            {
                return float.TryParse(str, NumberStyles.Float, Culture, out obj);
            }
        }

        #endregion

        #region Double

        private class DoubleConverter : IStringConverter<double>
        {
            public string ToString( double obj )
            {
                return obj.ToString("R", Culture);
            }

            public bool TryParse( string str, out double obj )
            {
                return double.TryParse(str, NumberStyles.Float, Culture, out obj);
            }
        }

        #endregion

        #region Decimal

        private class DecimalConverter : IStringConverter<decimal>
        {
            public string ToString( decimal obj )
            {
                return obj.ToString("G", Culture);
            }

            public bool TryParse( string str, out decimal obj )
            {
                return decimal.TryParse(str, NumberStyles.Float, Culture, out obj);
            }
        }

        #endregion

        #region Boolean

        private class BooleanConverter : IStringConverter<bool>
        {
            public string ToString( bool obj )
            {
                return obj ? "true" : "false";
            }

            public bool TryParse( string str, out bool obj )
            {
                if( str.NullReference() )
                {
                    obj = default(bool);
                    return false;
                }
                else
                {
                    str = str.Trim();
                }

                if( str.Equals("true", StringComparison.OrdinalIgnoreCase) )
                {
                    obj = true;
                    return true;
                }
                else if( str.Equals("false", StringComparison.OrdinalIgnoreCase) )
                {
                    obj = false;
                    return true;
                }
                else
                {
                    obj = default(bool);
                    return false;
                }
            }
        }

        #endregion

        #region Char

        private class CharConverter : IStringConverter<char>
        {
            public string ToString( char obj )
            {
                return obj.ToString();
            }

            public bool TryParse( string str, out char obj )
            {
                if( str.NullReference()
                 || str.Length != 1 )
                {
                    obj = default(char);
                    return false;
                }
                else
                {
                    obj = str[0];
                    return true;
                }
            }
        }

        #endregion

        #region String

        private class StringConverter : IStringConverter<string>
        {
            public string ToString( string obj )
            {
                return obj;
            }

            public bool TryParse( string str, out string obj )
            {
                obj = str;
                return true;
            }
        }

        #endregion

        #region DateTime

        private class DateTimeConverter : IStringConverter<DateTime>
        {
            /* NOTE: The goal - for default serializers - is that you should get back the same thing you put in,
             *       no matter on which platform, or where on earth you are. Unfortunatelly in this
             *       case portability won over comfort.
             *
             *       There are some serious limitations to how this implementation handles DateTime!
             *         - UTC DateTime is handled correctly
             *         - Local DateTime is converted to UTC (and therefore deserializes as such)
             *         - Unspecified DateTime results in an exception (at serialization)
             *
             *       With Local values, you may read it in a different time zone as the one it was serialized in.
             *       (If that is an issue, use DateTimeOffset, or search for "Noda Time" instead)
             *
             *       As for Unspecified values: we could convert it to either UTC or Local,
             *       but ToLocalTime assumes it's in UTC, and ToUniversalTime assumes it's in local time...
             */

            public string ToString( DateTime obj )
            {
                if( obj.Kind == DateTimeKind.Unspecified )
                    throw new ArgumentException("DateTimeKind.Unspecified is not supported! Utc and Local are (the latter is converted to Utc).").Store(nameof(obj), obj);

                obj = obj.ToUniversalTime();
                return obj.ToString("o");
            }

            public bool TryParse( string str, out DateTime obj )
            {
                // not using the Culture field on purpose!
                return DateTime.TryParseExact(str, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite, out obj);
            }
        }

        #endregion

        #region DateTimeOffset

        private class DateTimeOffsetConverter : IStringConverter<DateTimeOffset>
        {
            public string ToString( DateTimeOffset obj )
            {
                return obj.ToString("o");
            }

            public bool TryParse( string str, out DateTimeOffset obj )
            {
                // not using the Culture field on purpose!
                return DateTimeOffset.TryParseExact(str, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite, out obj);
            }
        }

        #endregion

        #region TimeSpan

        private class TimeSpanConverter : IStringConverter<TimeSpan>
        {
            public string ToString( TimeSpan obj )
            {
                return obj.ToString("c");
            }

            public bool TryParse( string str, out TimeSpan obj )
            {
                // not using the Culture field on purpose!
                return TimeSpan.TryParseExact(str, "c", CultureInfo.InvariantCulture, TimeSpanStyles.None, out obj);
            }
        }

        #endregion

        #region FilePath

        private class FilePathConverter : IStringConverter<FilePath>
        {
            public string ToString( FilePath obj )
            {
                return obj?.ToString();
            }

            public bool TryParse( string str, out FilePath obj )
            {
                try
                {
                    obj = str.NullReference() ? null : FilePath.From(str);
                    return true;
                }
                catch
                {
                    obj = null;
                    return false;
                }
            }
        }

        #endregion

        #region Enum<TEnum>

        /// <summary>
        /// Converts between <typeparamref name="TEnum"/> and <see cref="string"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration to convert instances of.</typeparam>
        public class Enum<TEnum> : IStringConverter<TEnum>
            where TEnum : struct, IComparable, IFormattable // IConvertible
        {
            /// <summary>
            /// The default instance of the type.
            /// </summary>
            public static readonly Enum<TEnum> Default = new Enum<TEnum>();

            /// <summary>
            /// Converts the specified object to a <see cref="string"/>.
            /// </summary>
            /// <param name="obj">The object to convert to a string.</param>
            /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
            public string ToString( TEnum obj )
            {
                return Enum.GetName(typeof(TEnum), obj);
            }

            /// <summary>
            /// Tries to parse the specified <see cref="string"/>.
            /// </summary>
            /// <param name="str">The string representation to parse.</param>
            /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
            /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
            public bool TryParse( string str, out TEnum obj )
            {
                return Enum.TryParse(str, out obj);
            }
        }

        #endregion

        #region Locator

        static RoundTripStringConverter()
        {
            var collection = new StringConverterCollection();
            collection.Add(new SByteConverter());
            collection.Add(new NullableStringConverter<sbyte>(new SByteConverter()));
            collection.Add(new ByteConverter());
            collection.Add(new NullableStringConverter<byte>(new ByteConverter()));
            collection.Add(new Int16Converter());
            collection.Add(new NullableStringConverter<short>(new Int16Converter()));
            collection.Add(new UInt16Converter());
            collection.Add(new NullableStringConverter<ushort>(new UInt16Converter()));
            collection.Add(new Int32Converter());
            collection.Add(new NullableStringConverter<int>(new Int32Converter()));
            collection.Add(new UInt32Converter());
            collection.Add(new NullableStringConverter<uint>(new UInt32Converter()));
            collection.Add(new Int64Converter());
            collection.Add(new NullableStringConverter<long>(new Int64Converter()));
            collection.Add(new UInt64Converter());
            collection.Add(new NullableStringConverter<ulong>(new UInt64Converter()));
            collection.Add(new SingleConverter());
            collection.Add(new NullableStringConverter<float>(new SingleConverter()));
            collection.Add(new DoubleConverter());
            collection.Add(new NullableStringConverter<double>(new DoubleConverter()));
            collection.Add(new DecimalConverter());
            collection.Add(new NullableStringConverter<decimal>(new DecimalConverter()));
            collection.Add(new BooleanConverter());
            collection.Add(new NullableStringConverter<bool>(new BooleanConverter()));
            collection.Add(new CharConverter());
            collection.Add(new NullableStringConverter<char>(new CharConverter()));
            collection.Add(new StringConverter());
            collection.Add(new DateTimeConverter());
            collection.Add(new NullableStringConverter<DateTime>(new DateTimeConverter()));
            collection.Add(new DateTimeOffsetConverter());
            collection.Add(new NullableStringConverter<DateTimeOffset>(new DateTimeOffsetConverter()));
            collection.Add(new TimeSpanConverter());
            collection.Add(new NullableStringConverter<TimeSpan>(new TimeSpanConverter()));
            collection.Add(new FilePathConverter());
            Locator = collection;
        }

        /// <summary>
        /// Provides a small set of converters for basic, commonly used, built-in types (like int, float, ... etc.).
        /// </summary>
        public static readonly IStringConverterLocator Locator;

        #endregion
    }
}
