using System;
using Mechanical3.DataStores;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores
{
    [TestFixture(Category = "DataStores")]
    public static class RoundTripStringConverterTests
    {
        #region Internal Static Methods

        internal static void ToStringParse<T>( T obj, string str, IStringConverter<T> converter )
        {
            Assert.NotNull(converter);

            string asString = converter.ToString(obj);
            Test.OrdinalEquals(str.Trim(), asString);

            var asObj = DataStore.Parse(str, converter);
            Assert.AreEqual((object)obj, (object)asObj);
        }

        internal static void TryParseFails<T>( string str, IStringConverter<T> converter )
        {
            Assert.NotNull(converter);

            T result;
            Assert.False(converter.TryParse(str, out result));
        }

        internal static void TryParseSucceeds<T>( string str, IStringConverter<T> converter )
        {
            Assert.NotNull(converter);

            T result;
            Assert.True(converter.TryParse(str, out result));
        }

        internal static void GeneralTryParseTests<T>( IStringConverter<T> converter )
        {
            TryParseFails(null, converter);
            TryParseFails(string.Empty, converter);
            TryParseFails("a", converter);
            TryParseFails(" ", converter);
        }

        #endregion

        [Test]
        public static void RoundTripConverterTests()
        {
            var locator = RoundTripStringConverter.Locator;

            // SByte
            var sbyteConverter = locator.GetConverter<sbyte>();
            ToStringParse<sbyte>(SByte.MaxValue, " 127 ", sbyteConverter);
            ToStringParse<sbyte>(SByte.MinValue, "-128", sbyteConverter);
            TryParseFails("128", sbyteConverter);
            TryParseFails("-129", sbyteConverter);
            GeneralTryParseTests(sbyteConverter);

            // Byte
            var byteConverter = locator.GetConverter<byte>();
            ToStringParse<byte>(Byte.MaxValue, " 255 ", byteConverter);
            ToStringParse<byte>(Byte.MinValue, "0", byteConverter);
            TryParseFails("256", byteConverter);
            TryParseFails("-1", byteConverter);
            GeneralTryParseTests(byteConverter);

            // Int16
            var shortConverter = locator.GetConverter<short>();
            ToStringParse<short>(Int16.MaxValue, " 32767 ", shortConverter);
            ToStringParse<short>(Int16.MinValue, "-32768", shortConverter);
            TryParseFails("32,767", shortConverter);
            TryParseFails("32768", shortConverter);
            TryParseFails("-32769", shortConverter);
            GeneralTryParseTests(shortConverter);

            // UInt16
            var ushortConverter = locator.GetConverter<ushort>();
            ToStringParse<ushort>(UInt16.MaxValue, " 65535 ", ushortConverter);
            ToStringParse<ushort>(UInt16.MinValue, "0", ushortConverter);
            TryParseFails("65,535", ushortConverter);
            TryParseFails("65536", ushortConverter);
            TryParseFails("-1", ushortConverter);
            GeneralTryParseTests(ushortConverter);

            // Int32
            var intConverter = locator.GetConverter<int>();
            ToStringParse<int>(Int32.MaxValue, " 2147483647 ", intConverter);
            ToStringParse<int>(Int32.MinValue, "-2147483648", intConverter);
            TryParseFails("2,147,483,647", intConverter);
            TryParseFails("2147483648", intConverter);
            TryParseFails("-2147483649", intConverter);
            GeneralTryParseTests(intConverter);

            // UInt32
            var uintConverter = locator.GetConverter<uint>();
            ToStringParse<uint>(UInt32.MaxValue, " 4294967295 ", uintConverter);
            ToStringParse<uint>(UInt32.MinValue, "0", uintConverter);
            TryParseFails("4,294,967,295", uintConverter);
            TryParseFails("4294967296", uintConverter);
            TryParseFails("-1", uintConverter);
            GeneralTryParseTests(uintConverter);

            // Int64
            var longConverter = locator.GetConverter<long>();
            ToStringParse<long>(Int64.MaxValue, " 9223372036854775807 ", longConverter);
            ToStringParse<long>(Int64.MinValue, "-9223372036854775808", longConverter);
            TryParseFails("9,223,372,036,854,775,807", longConverter);
            TryParseFails("9223372036854775808", longConverter);
            TryParseFails("-9223372036854775809", longConverter);
            GeneralTryParseTests(longConverter);

            // UInt64
            var ulongConverter = locator.GetConverter<ulong>();
            ToStringParse<ulong>(UInt64.MaxValue, " 18446744073709551615 ", ulongConverter);
            ToStringParse<ulong>(UInt64.MinValue, "0", ulongConverter);
            TryParseFails("18,446,744,073,709,551,615", ulongConverter);
            TryParseFails("18446744073709551616", ulongConverter);
            TryParseFails("-1", ulongConverter);
            GeneralTryParseTests(ulongConverter);

            // Single
            var floatConverter = locator.GetConverter<float>();
            ToStringParse<float>(Single.MaxValue, " 3.40282347E+38 ", floatConverter);
            ToStringParse<float>(Single.MinValue, "-3.40282347E+38", floatConverter);
            ToStringParse<float>(Single.Epsilon, "1.401298E-45", floatConverter);
            ToStringParse<float>(Single.NaN, "NaN", floatConverter);
            ToStringParse<float>(Single.PositiveInfinity, "Infinity", floatConverter);
            ToStringParse<float>(Single.NegativeInfinity, "-Infinity", floatConverter);
            ToStringParse<float>(1234.56f, "1234.56", floatConverter);
            TryParseSucceeds("1E2", floatConverter); // no exponent sign
            TryParseSucceeds("1e+2", floatConverter); // small 'e'
            TryParseFails("1,234.56", floatConverter);
            TryParseFails("3.402824E+38", floatConverter);
            TryParseFails("-3.402824E38", floatConverter);
            GeneralTryParseTests(floatConverter);

            // Double
            var doubleConverter = locator.GetConverter<double>();
            ToStringParse<double>(Double.MaxValue, " 1.7976931348623157E+308 ", doubleConverter);
            ToStringParse<double>(Double.MinValue, "-1.7976931348623157E+308", doubleConverter);
            ToStringParse<double>(Double.Epsilon, "4.94065645841247E-324", doubleConverter);
            ToStringParse<double>(Double.NaN, "NaN", doubleConverter);
            ToStringParse<double>(Double.PositiveInfinity, "Infinity", doubleConverter);
            ToStringParse<double>(Double.NegativeInfinity, "-Infinity", doubleConverter);
            ToStringParse<double>(1234.56d, "1234.56", doubleConverter);
            TryParseSucceeds("1E2", doubleConverter); // no exponent sign
            TryParseSucceeds("1e+2", doubleConverter); // small 'e'
            TryParseFails("1,234.56", doubleConverter);
            TryParseFails("1.79769313486232E+308", doubleConverter);
            TryParseFails("-1.79769313486232E+308", doubleConverter);
            GeneralTryParseTests(doubleConverter);

            // Decimal
            var decimalConverter = locator.GetConverter<decimal>();
            ToStringParse<decimal>(Decimal.MaxValue, " 79228162514264337593543950335 ", decimalConverter);
            ToStringParse<decimal>(Decimal.MinValue, "-79228162514264337593543950335", decimalConverter);
            ToStringParse<decimal>(1234.56m, "1234.56", decimalConverter);
            TryParseSucceeds("1E2", decimalConverter); // no exponent sign
            TryParseSucceeds("1e+2", decimalConverter); // small 'e'
            TryParseFails("1,234.56", decimalConverter);
            TryParseFails("79,228,162,514,264,337,593,543,950,335", decimalConverter);
            TryParseFails("79228162514264337593543950336", decimalConverter);
            TryParseFails("-79228162514264337593543950336", decimalConverter);
            GeneralTryParseTests(decimalConverter);

            // Boolean
            var boolConverter = locator.GetConverter<bool>();
            ToStringParse<bool>(true, " true ", boolConverter);
            ToStringParse<bool>(false, "false", boolConverter);
            TryParseSucceeds("TrUe", boolConverter);
            TryParseFails("yes", boolConverter);
            TryParseFails("0", boolConverter);
            GeneralTryParseTests(boolConverter);

            // Char
            var charConverter = locator.GetConverter<char>();
            ToStringParse<char>('x', "x", charConverter);
            TryParseSucceeds(" ", charConverter); // ToStringParse fails because of implicit .Trim()
            TryParseFails("ab", charConverter);
            TryParseFails(null, charConverter);
            TryParseFails(string.Empty, charConverter);

            // String
            var stringConverter = locator.GetConverter<string>();
            ToStringParse<string>(string.Empty, string.Empty, stringConverter);
            ToStringParse<string>("a", "a", stringConverter);
            Test.OrdinalEquals(" b", stringConverter.ToString(" b")); // ToStringParse fails because of implicit .Trim()
            TryParseFails(null, stringConverter);

            // DateTime
            var dateTimeConverter = locator.GetConverter<DateTime>();
            Func<DateTime, DateTime> toUtc = dt => new DateTime(dt.Ticks, DateTimeKind.Utc);
            ToStringParse<DateTime>(toUtc(DateTime.MaxValue), " 9999-12-31T23:59:59.9999999Z ", dateTimeConverter);
            ToStringParse<DateTime>(toUtc(DateTime.MinValue), "0001-01-01T00:00:00.0000000Z", dateTimeConverter);
            Assert.AreEqual(DateTimeKind.Utc, DataStore.Parse(dateTimeConverter.ToString(DateTime.Now), dateTimeConverter).Kind); // Local converted to Utc
            TryParseFails("10000-01-01T00:00:00.0000000Z", dateTimeConverter);
            TryParseFails("0000-12-31T23:59:59.9999999Z", dateTimeConverter);
            Assert.Throws<ArgumentException>(() => dateTimeConverter.ToString(new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Unspecified))); // Unspecified throws
            GeneralTryParseTests(dateTimeConverter);

            // DateTimeOffset
            var dateTimeOffsetConverter = locator.GetConverter<DateTimeOffset>();
            ToStringParse<DateTimeOffset>(DateTimeOffset.MaxValue, " 9999-12-31T23:59:59.9999999+00:00 ", dateTimeOffsetConverter);
            ToStringParse<DateTimeOffset>(DateTimeOffset.MinValue, "0001-01-01T00:00:00.0000000+00:00", dateTimeOffsetConverter);
            TryParseFails("10000-01-01T00:00:00.0000000+00:00", dateTimeOffsetConverter);
            TryParseFails("0000-12-31T23:59:59.9999999+00:00", dateTimeOffsetConverter);
            GeneralTryParseTests(dateTimeOffsetConverter);

            // TimeSpan
            var timeSpanConverter = locator.GetConverter<TimeSpan>();
            ToStringParse<TimeSpan>(TimeSpan.MaxValue, " 10675199.02:48:05.4775807 ", timeSpanConverter);
            ToStringParse<TimeSpan>(TimeSpan.MinValue, "-10675199.02:48:05.4775808", timeSpanConverter);
            TryParseFails("10675199.02:48:05.4775808", timeSpanConverter);
            TryParseFails("-10675199.02:48:05.4775809", timeSpanConverter);
            GeneralTryParseTests(timeSpanConverter);
        }
    }
}
