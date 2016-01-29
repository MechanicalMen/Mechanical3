using System;
using System.Globalization;
using Mechanical3.Core;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Serializes and deserializes <see cref="DateTime"/> and <see cref="TimeSpan"/>. Uses (a subset of) the ISO 8601 format.
    /// Has similar characteristics to the default serializer (returns UTC; Unspecified throws exceptions.)
    /// Sub-second precision will be lost!
    /// TimeSpan values must be less than a day (and positive).
    /// </summary>
    public class ISO8601Converter : IStringConverter<DateTime>,
                                    IStringConverter<DateTimeOffset>,
                                    IStringConverter<TimeSpan>
    {
        //// NOTE: Some cases do not handle well:
        ////        - DateTime parsing fails on "<some date>T24:00:00Z" (though this is a valid ISO8601 time format)
        ////        - TimeSpan string conversion fails, when the value is less than zero, or greater or equal to a day
        ////          (which is perfectly valid, as long as TimeSpan values refer to the time part of a DateTime - which this serializer is intended for)
        ////        - DateTime string conversion of UTC values ands in "Z", while the same for DateTimeOffset ends in "+00:00"
        ////          (both are valid, but it's inconsistent)

        #region Private Fields

        private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ssK";
        private const string TimeSpanFormat = "hh':'mm':'ss";

        private static readonly TimeSpan OneDayTimeSpan = TimeSpan.FromDays(1);

        #endregion

        #region IStringConverter<DateTime>

        /// <summary>
        /// Converts the specified object to a <see cref="string"/>.
        /// </summary>
        /// <param name="obj">The object to convert to a string.</param>
        /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
        public string ToString( DateTime obj )
        {
            if( obj.Kind == DateTimeKind.Unspecified )
                throw new ArgumentException("DateTimeKind.Unspecified is not supported! Utc and Local are (the latter is converted to Utc).").Store(nameof(obj), obj);

            //// NOTE: this will produce different outputs for Local and UTC values.
            return obj.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <c>null</c>.</exception>
        public bool TryParse( string str, out DateTime obj )
        {
            return DateTime.TryParseExact(str, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out obj);
        }

        #endregion

        #region IStringConverter<DateTimeOffset>

        /// <summary>
        /// Converts the specified object to a <see cref="string"/>.
        /// </summary>
        /// <param name="obj">The object to convert to a string.</param>
        /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
        public string ToString( DateTimeOffset obj )
        {
            return obj.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <c>null</c>.</exception>
        public bool TryParse( string str, out DateTimeOffset obj )
        {
            return DateTimeOffset.TryParseExact(str, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out obj);
        }

        #endregion

        #region IStringConverter<TimeSpan>

        /// <summary>
        /// Converts the specified object to a <see cref="string"/>.
        /// </summary>
        /// <param name="obj">The object to convert to a string.</param>
        /// <returns>The string representation of the specified object. Never <c>null</c>.</returns>
        public string ToString( TimeSpan obj )
        {
            if( obj < TimeSpan.Zero
             || obj >= OneDayTimeSpan )
                throw new ArgumentOutOfRangeException().Store("timeSpan", obj);

            // NOTE: string conversion rounds to the lowest seconds, so "00:00:00.999" prints as "00:00:00"!
            return obj.ToString(TimeSpanFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <c>null</c>.</exception>
        public bool TryParse( string str, out TimeSpan obj )
        {
            return TimeSpan.TryParseExact(str?.Trim(), TimeSpanFormat, CultureInfo.InvariantCulture, TimeSpanStyles.None, out obj);
        }

        #endregion

        /// <summary>
        /// The default instance of the class.
        /// </summary>
        public static readonly ISO8601Converter Default = new ISO8601Converter();
    }
}
