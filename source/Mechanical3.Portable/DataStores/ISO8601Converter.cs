using System;
using System.Collections.Generic;
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

        #region TimeResolution

        /// <summary>
        /// The time resolution to use when saving or loading ISO8601 time values.
        /// When saving, data that can not be represented by the specified resolution,
        /// will be silently lost.
        /// </summary>
        public enum TimeResolution
        {
            /// <summary>
            /// Print hours and minutes.
            /// </summary>
            Minutes,

            /// <summary>
            /// Print hours, minutes and seconds.
            /// </summary>
            Seconds,

            /// <summary>
            /// Print hours, minutes, seconds, and 3 fractional digits.
            /// </summary>
            Milliseconds
        }

        #endregion

        #region Truncate

        private static long Truncate( long ticks, TimeResolution resolution )
        {
            //// NOTE: Math.Truncate sometimes seemed to produce strange rounding errors
            ////       (this could have been caused by TimeSpan members using double (e.g. TimeSpan.FromSeconds(Math.Truncate(value.TotalSeconds))),
            ////        or my previous code could easily have been at fault as well)

            switch( resolution )
            {
            case TimeResolution.Minutes:
                return (ticks / TimeSpan.TicksPerMinute) * TimeSpan.TicksPerMinute;

            case TimeResolution.Seconds:
                return (ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;

            case TimeResolution.Milliseconds:
                return (ticks / TimeSpan.TicksPerMillisecond) * TimeSpan.TicksPerMillisecond;

            default:
                throw NamedArgumentException.Store(nameof(resolution), resolution).Store(nameof(ticks), ticks);
            }
        }

        /// <summary>
        /// Truncates the specified time, to the target <see cref="TimeResolution"/>.
        /// </summary>
        /// <param name="value">The value to truncate.</param>
        /// <param name="resolution">The <see cref="TimeResolution"/> to preserve.</param>
        /// <returns>The truncated value.</returns>
        public static TimeSpan Truncate( TimeSpan value, TimeResolution resolution )
        {
            return new TimeSpan(Truncate(value.Ticks, resolution));
        }

        /// <summary>
        /// Truncates the time, to the target <see cref="TimeResolution"/>.
        /// </summary>
        /// <param name="value">The value to truncate.</param>
        /// <param name="resolution">The <see cref="TimeResolution"/> to preserve.</param>
        /// <returns>The truncated value.</returns>
        public static DateTime Truncate( DateTime value, TimeResolution resolution )
        {
            return new DateTime(Truncate(value.Ticks, resolution), value.Kind);
        }

        /// <summary>
        /// Truncates the time, to the target <see cref="TimeResolution"/>.
        /// </summary>
        /// <param name="value">The value to truncate.</param>
        /// <param name="resolution">The <see cref="TimeResolution"/> to preserve.</param>
        /// <returns>The truncated value.</returns>
        public static DateTimeOffset Truncate( DateTimeOffset value, TimeResolution resolution )
        {
            return new DateTimeOffset(Truncate(value.DateTime, resolution), value.Offset);
        }

        #endregion

        #region FormatStrings

        //// NOTE: Dictionary is thread seafe, as long as it is treated read-only

        private static readonly Dictionary<TimeResolution, string> DateTimeFormats = GetDateTimeFormats();
        private static readonly Dictionary<TimeResolution, string> TimeSpanFormats = GetTimeSpanFormats();

        private static Dictionary<TimeResolution, string> GetDateTimeFormats()
        {
            var result = new Dictionary<TimeResolution, string>();
            result.Add(TimeResolution.Minutes, "yyyy-MM-dd'T'HH:mmK");
            result.Add(TimeResolution.Seconds, "yyyy-MM-dd'T'HH:mm:ssK");
            result.Add(TimeResolution.Milliseconds, "yyyy-MM-dd'T'HH:mm:ss.fffK");
            return result;
        }

        private static Dictionary<TimeResolution, string> GetTimeSpanFormats()
        {
            var result = new Dictionary<TimeResolution, string>();
            result.Add(TimeResolution.Minutes, "hh':'mm");
            result.Add(TimeResolution.Seconds, "hh':'mm':'ss");
            result.Add(TimeResolution.Milliseconds, "hh':'mm':'ss'.'fff");
            return result;
        }

        #endregion

        #region Private Fields

        private static readonly TimeSpan OneDayTimeSpan = TimeSpan.FromDays(1);

        private readonly TimeResolution timeResolution;
        private readonly string dateTimeFormat;
        private readonly string timeSpanFormat;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ISO8601Converter"/> class.
        /// </summary>
        /// <param name="timeRes">The <see cref="TimeResolution"/> to use.</param>
        public ISO8601Converter( TimeResolution timeRes )
        {
            if( !Enum.IsDefined(typeof(TimeResolution), timeRes) )
                throw NamedArgumentException.Store(nameof(timeRes), timeRes);

            this.timeResolution = timeRes;
            this.dateTimeFormat = DateTimeFormats[timeRes];
            this.timeSpanFormat = TimeSpanFormats[timeRes];
        }

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

            obj = Truncate(obj, this.timeResolution);

            //// NOTE: this will produce different outputs for Local and UTC values.
            return obj.ToString(this.dateTimeFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        public bool TryParse( string str, out DateTime obj )
        {
            return DateTime.TryParseExact(str, this.dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out obj);
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
            obj = Truncate(obj, this.timeResolution);

            return obj.ToString(this.dateTimeFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        public bool TryParse( string str, out DateTimeOffset obj )
        {
            return DateTimeOffset.TryParseExact(str, this.dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out obj);
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

            obj = Truncate(obj, this.timeResolution);

            // NOTE: string conversion rounds to the lowest seconds, so "00:00:00.999" prints as "00:00:00"!
            return obj.ToString(this.timeSpanFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Tries to parse the specified <see cref="string"/>.
        /// </summary>
        /// <param name="str">The string representation to parse.</param>
        /// <param name="obj">The result of a successful parsing; or otherwise the default instance of the type.</param>
        /// <returns><c>true</c> if the string was successfully parsed; otherwise, <c>false</c>.</returns>
        public bool TryParse( string str, out TimeSpan obj )
        {
            return TimeSpan.TryParseExact(str?.Trim(), this.timeSpanFormat, CultureInfo.InvariantCulture, TimeSpanStyles.None, out obj);
        }

        #endregion
    }
}
