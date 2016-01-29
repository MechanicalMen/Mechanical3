using System;
using Mechanical3.DataStores;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores
{
    [TestFixture(Category = "DataStores")]
    public static class ISO8601ConverterTests
    {
        private static void ParsedDateTimeEquals( DateTime expectedDateTime, string dateTimeString, IStringConverter<DateTime> dateTimeConverter )
        {
            var parsedDateTime = DataStore.Parse(dateTimeString, dateTimeConverter);
            Assert.AreEqual(expectedDateTime.Ticks, parsedDateTime.Ticks);
            Assert.AreEqual(expectedDateTime.Kind, parsedDateTime.Kind);
        }

        private static void ParsedDateTimeOffsetEquals( DateTimeOffset expectedDateTimeOffset, string dateTimeOffsetString, IStringConverter<DateTimeOffset> dateTimeOffsetConverter )
        {
            var parsedDateTimeOffset = DataStore.Parse(dateTimeOffsetString, dateTimeOffsetConverter);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.Ticks, parsedDateTimeOffset.DateTime.Ticks);
            Assert.AreEqual(expectedDateTimeOffset.Offset.Ticks, parsedDateTimeOffset.Offset.Ticks);
        }

        [Test]
        public static void ISO8601Tests()
        {
            Func<DateTime, DateTimeKind, DateTime> toKind = (dt, k) => new DateTime(dt.Ticks, k);
            var testDateTime = new DateTime(2015, 02, 28, 20, 16, 12);

            // Utc DateTime
            var dateTimeConverter = (IStringConverter<DateTime>)ISO8601Converter.Default;
            RoundTripStringConverterTests.ToStringParse<DateTime>(toKind(testDateTime, DateTimeKind.Utc), " 2015-02-28T20:16:12Z ", dateTimeConverter);
            ParsedDateTimeEquals(toKind(testDateTime, DateTimeKind.Utc), "2015-02-28T20:16:12+00:00", dateTimeConverter);

            //// Local DateTime (converter returns Utc)

            //// NOTE: Unfortunately the only way to test the current time zone,
            ////       is through dependency injection, which does not help here.
            ParsedDateTimeEquals(toKind(testDateTime, DateTimeKind.Local).ToUniversalTime(), "2015-02-28T20:16:12+01:00", dateTimeConverter); // tested on 2016.01.29
            ParsedDateTimeEquals(new DateTimeOffset(testDateTime.Ticks, TimeSpan.FromHours(3)).UtcDateTime, "2015-02-28T20:16:12+03:00", dateTimeConverter);

            // sub-second precision lost
            var testDateTime2 = DataStore.Parse<DateTime>("2015-03-01T13:38:49.3323722Z");
            var parsedDateTime2 = DataStore.Parse(dateTimeConverter.ToString(testDateTime2), dateTimeConverter);
            Assert.AreEqual(testDateTime2.Year, parsedDateTime2.Year);
            Assert.AreEqual(testDateTime2.Month, parsedDateTime2.Month);
            Assert.AreEqual(testDateTime2.Day, parsedDateTime2.Day);
            Assert.AreEqual(testDateTime2.Hour, parsedDateTime2.Hour);
            Assert.AreEqual(testDateTime2.Minute, parsedDateTime2.Minute);
            Assert.AreEqual(testDateTime2.Second, parsedDateTime2.Second);
            Assert.AreNotEqual(testDateTime2.Millisecond, parsedDateTime2.Millisecond);

            // other DateTime tests
            Assert.Throws<ArgumentException>(() => dateTimeConverter.ToString(new DateTime(testDateTime.Ticks, DateTimeKind.Unspecified))); // Unspecified throws
            RoundTripStringConverterTests.GeneralTryParseTests(dateTimeConverter);



            // Utc DateTimeOffset
            var dateTimeOffsetConverter = (IStringConverter<DateTimeOffset>)ISO8601Converter.Default;
            RoundTripStringConverterTests.ToStringParse<DateTimeOffset>(new DateTimeOffset(testDateTime.Ticks, TimeSpan.Zero), " 2015-02-28T20:16:12+00:00 ", dateTimeOffsetConverter);
            ParsedDateTimeOffsetEquals(new DateTimeOffset(testDateTime.Ticks, TimeSpan.Zero), "2015-02-28T20:16:12Z", dateTimeOffsetConverter);

            // "Local" DateTimeOffset
            RoundTripStringConverterTests.ToStringParse<DateTimeOffset>(new DateTimeOffset(testDateTime.Ticks, TimeSpan.FromHours(3)), " 2015-02-28T20:16:12+03:00 ", dateTimeOffsetConverter);

            // sub-second precision lost
            var testDateTimeOffset2 = new DateTimeOffset(testDateTime2.Ticks, TimeSpan.Zero);
            var parsedDateTimeOffset2 = DataStore.Parse(dateTimeOffsetConverter.ToString(testDateTimeOffset2), dateTimeOffsetConverter);
            Assert.AreEqual(testDateTimeOffset2.Year, parsedDateTimeOffset2.Year);
            Assert.AreEqual(testDateTimeOffset2.Month, parsedDateTimeOffset2.Month);
            Assert.AreEqual(testDateTimeOffset2.Day, parsedDateTimeOffset2.Day);
            Assert.AreEqual(testDateTimeOffset2.Hour, parsedDateTimeOffset2.Hour);
            Assert.AreEqual(testDateTimeOffset2.Minute, parsedDateTimeOffset2.Minute);
            Assert.AreEqual(testDateTimeOffset2.Second, parsedDateTimeOffset2.Second);
            Assert.AreNotEqual(testDateTimeOffset2.Millisecond, parsedDateTimeOffset2.Millisecond);

            // other DateTimeOffset tests
            RoundTripStringConverterTests.GeneralTryParseTests(dateTimeOffsetConverter);



            // TimeSpan
            var timeSpanConverter = (IStringConverter<TimeSpan>)ISO8601Converter.Default;
            RoundTripStringConverterTests.ToStringParse<TimeSpan>(TimeSpan.Zero, " 00:00:00 ", timeSpanConverter);
            RoundTripStringConverterTests.ToStringParse<TimeSpan>(testDateTime.TimeOfDay, "20:16:12", timeSpanConverter);

            // sub-second precision lost
            Test.OrdinalEquals("00:00:00", timeSpanConverter.ToString(TimeSpan.FromSeconds(0.999)));

            // everything less than a day (and positive) should be printable
            Test.OrdinalEquals("23:59:59", timeSpanConverter.ToString(TimeSpan.FromDays(1) - TimeSpan.FromTicks(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => timeSpanConverter.ToString(TimeSpan.FromDays(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => timeSpanConverter.ToString(TimeSpan.FromTicks(-1)));

            // other TimeSpan tests
            RoundTripStringConverterTests.GeneralTryParseTests(timeSpanConverter);
        }
    }
}
