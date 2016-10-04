using System;
using Mechanical3.DataStores;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores
{
    [TestFixture(Category = "DataStores")]
    public static class ISO8601ConverterTests
    {
        #region TruncateTests

        private static TimeSpan CreateTimeSpan( long integralUnits, long firstDecimalUnit, long ticksPerOneUnit )
        {
            //// NOTE: TimeSpan.FromSeconds(3.1) =~= CreateTimeSpan(3, 1, TimeSpan.TicksPerSecond)

            if( firstDecimalUnit < 0
             || firstDecimalUnit > 9 )
                throw new ArgumentOutOfRangeException(nameof(firstDecimalUnit));

            if( integralUnits < 0 )
                firstDecimalUnit = -firstDecimalUnit;

            return new TimeSpan(integralUnits * ticksPerOneUnit + ((firstDecimalUnit * ticksPerOneUnit) / 10L));
        }

        private static void TruncateTests( ISO8601Converter converter, long ticksPerUnit )
        {
            Assert.AreEqual(CreateTimeSpan(3, 0, ticksPerUnit), converter.Truncate(CreateTimeSpan(3, 0, ticksPerUnit)));
            Assert.AreEqual(CreateTimeSpan(3, 0, ticksPerUnit), converter.Truncate(CreateTimeSpan(3, 1, ticksPerUnit)));
            Assert.AreEqual(CreateTimeSpan(3, 0, ticksPerUnit), converter.Truncate(CreateTimeSpan(3, 9, ticksPerUnit)));
            Assert.AreEqual(CreateTimeSpan(-3, 0, ticksPerUnit), converter.Truncate(CreateTimeSpan(-3, 5, ticksPerUnit)));

            var baseTruncatedDateTime = new DateTime((new DateTime(2016, 10, 1, 12, 30, 30, 500).Ticks / ticksPerUnit) * ticksPerUnit, DateTimeKind.Utc);
            Test.AssertAreEqual(baseTruncatedDateTime + CreateTimeSpan(3, 0, ticksPerUnit), converter.Truncate(baseTruncatedDateTime + CreateTimeSpan(3, 0, ticksPerUnit)));
            Test.AssertAreEqual(baseTruncatedDateTime + CreateTimeSpan(3, 0, ticksPerUnit), converter.Truncate(baseTruncatedDateTime + CreateTimeSpan(3, 1, ticksPerUnit)));
            Test.AssertAreEqual(baseTruncatedDateTime + CreateTimeSpan(3, 0, ticksPerUnit), converter.Truncate(baseTruncatedDateTime + CreateTimeSpan(3, 9, ticksPerUnit)));
            Test.AssertAreEqual(baseTruncatedDateTime - CreateTimeSpan(4, 0, ticksPerUnit), converter.Truncate(baseTruncatedDateTime - CreateTimeSpan(3, 5, ticksPerUnit)));

            var baseTruncatedDateTimeOffset = new DateTimeOffset(baseTruncatedDateTime.Ticks, TimeSpan.FromMinutes(30));
            Assert.AreEqual(baseTruncatedDateTimeOffset + CreateTimeSpan(3, 0, ticksPerUnit), converter.Truncate(baseTruncatedDateTimeOffset + CreateTimeSpan(3, 0, ticksPerUnit)));
            Assert.AreEqual(baseTruncatedDateTimeOffset + CreateTimeSpan(3, 0, ticksPerUnit), converter.Truncate(baseTruncatedDateTimeOffset + CreateTimeSpan(3, 1, ticksPerUnit)));
            Assert.AreEqual(baseTruncatedDateTimeOffset + CreateTimeSpan(3, 0, ticksPerUnit), converter.Truncate(baseTruncatedDateTimeOffset + CreateTimeSpan(3, 9, ticksPerUnit)));
            Assert.AreEqual(baseTruncatedDateTimeOffset - CreateTimeSpan(4, 0, ticksPerUnit), converter.Truncate(baseTruncatedDateTimeOffset - CreateTimeSpan(3, 5, ticksPerUnit)));
        }

        [Test]
        public static void TruncateTests()
        {
            TruncateTests(ISO8601Converter.MillisecondsPrecision, TimeSpan.TicksPerMillisecond);
            TruncateTests(ISO8601Converter.SecondsPrecision, TimeSpan.TicksPerSecond);
            TruncateTests(ISO8601Converter.MinutesPrecision, TimeSpan.TicksPerMinute);
        }

        #endregion

        #region ToStringTests

        private static void ToStringTests_Seconds()
        {
            Func<DateTime, DateTimeKind, DateTime> specifyKind = ( dt, k ) => new DateTime(dt.Ticks, k);
            var testDateTime = new DateTime(2015, 02, 28, 20, 16, 12, 345);
            var utcOffset = new DateTimeOffset(specifyKind(testDateTime, DateTimeKind.Local)).Offset; // unfortunately there is no way to set the current time zone, therefore this depends on the host OS
            var converter = ISO8601Converter.SecondsPrecision;

            // Utc DateTime
            Test.OrdinalEquals("2015-02-28T20:16:12Z", converter.ToString(specifyKind(testDateTime, DateTimeKind.Utc)));

            // Local DateTime (converts to Utc)
            Test.OrdinalEquals(
                "2015-02-28T20:16:12+" + utcOffset.ToString("hh':'mm"),
                converter.ToString(specifyKind(testDateTime, DateTimeKind.Local)));

            // Unspecified DateTime
            Assert.Throws<ArgumentException>(() => converter.ToString(specifyKind(testDateTime, DateTimeKind.Unspecified)));

            // Utc DateTimeOffset
            Test.OrdinalEquals("2015-02-28T20:16:12+00:00", converter.ToString(new DateTimeOffset(specifyKind(testDateTime, DateTimeKind.Utc))));

            // Local DateTimeOffset (converts to Utc)
            Test.OrdinalEquals(
                "2015-02-28T20:16:12+" + utcOffset.ToString("hh':'mm"),
                converter.ToString(new DateTimeOffset(specifyKind(testDateTime, DateTimeKind.Local))));

            // TimeSpan
            Test.OrdinalEquals("00:00:00", converter.ToString(TimeSpan.Zero));
            Test.OrdinalEquals("20:16:12", converter.ToString(testDateTime.TimeOfDay));
            Test.OrdinalEquals("23:59:59", converter.ToString(TimeSpan.FromDays(1) - TimeSpan.FromTicks(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.ToString(TimeSpan.FromDays(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.ToString(TimeSpan.FromTicks(-1)));
        }

        private static void ToStringTests_Minutes()
        {
            Func<DateTime, DateTimeKind, DateTime> specifyKind = ( dt, k ) => new DateTime(dt.Ticks, k);
            var testDateTime = new DateTime(2015, 02, 28, 20, 16, 12, 345);
            var utcOffset = new DateTimeOffset(specifyKind(testDateTime, DateTimeKind.Local)).Offset; // unfortunately there is no way to set the current time zone, therefore this depends on the host OS
            var converter = ISO8601Converter.MinutesPrecision;

            // Utc DateTime
            Test.OrdinalEquals("2015-02-28T20:16Z", converter.ToString(specifyKind(testDateTime, DateTimeKind.Utc)));

            // Local DateTime (converts to Utc)
            Test.OrdinalEquals(
                "2015-02-28T20:16+" + utcOffset.ToString("hh':'mm"),
                converter.ToString(specifyKind(testDateTime, DateTimeKind.Local)));

            // Unspecified DateTime
            Assert.Throws<ArgumentException>(() => converter.ToString(specifyKind(testDateTime, DateTimeKind.Unspecified)));

            // Utc DateTimeOffset
            Test.OrdinalEquals("2015-02-28T20:16+00:00", converter.ToString(new DateTimeOffset(specifyKind(testDateTime, DateTimeKind.Utc))));

            // Local DateTimeOffset (converts to Utc)
            Test.OrdinalEquals(
                "2015-02-28T20:16+" + utcOffset.ToString("hh':'mm"),
                converter.ToString(new DateTimeOffset(specifyKind(testDateTime, DateTimeKind.Local))));

            // TimeSpan
            Test.OrdinalEquals("00:00", converter.ToString(TimeSpan.Zero));
            Test.OrdinalEquals("20:16", converter.ToString(testDateTime.TimeOfDay));
            Test.OrdinalEquals("23:59", converter.ToString(TimeSpan.FromDays(1) - TimeSpan.FromTicks(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.ToString(TimeSpan.FromDays(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.ToString(TimeSpan.FromTicks(-1)));
        }

        private static void ToStringTests_Milliseconds()
        {
            Func<DateTime, DateTimeKind, DateTime> specifyKind = ( dt, k ) => new DateTime(dt.Ticks, k);
            var testDateTime = new DateTime(2015, 02, 28, 20, 16, 12, 345);
            var utcOffset = new DateTimeOffset(specifyKind(testDateTime, DateTimeKind.Local)).Offset; // unfortunately there is no way to set the current time zone, therefore this depends on the host OS
            var converter = ISO8601Converter.MillisecondsPrecision;

            // Utc DateTime
            Test.OrdinalEquals("2015-02-28T20:16:12.345Z", converter.ToString(specifyKind(testDateTime, DateTimeKind.Utc)));

            // Local DateTime (converts to Utc)
            Test.OrdinalEquals(
                "2015-02-28T20:16:12.345+" + utcOffset.ToString("hh':'mm"),
                converter.ToString(specifyKind(testDateTime, DateTimeKind.Local)));

            // Unspecified DateTime
            Assert.Throws<ArgumentException>(() => converter.ToString(specifyKind(testDateTime, DateTimeKind.Unspecified)));

            // Utc DateTimeOffset
            Test.OrdinalEquals("2015-02-28T20:16:12.345+00:00", converter.ToString(new DateTimeOffset(specifyKind(testDateTime, DateTimeKind.Utc))));

            // Local DateTimeOffset (converts to Utc)
            Test.OrdinalEquals(
                "2015-02-28T20:16:12.345+" + utcOffset.ToString("hh':'mm"),
                converter.ToString(new DateTimeOffset(specifyKind(testDateTime, DateTimeKind.Local))));

            // TimeSpan
            Test.OrdinalEquals("00:00:00.000", converter.ToString(TimeSpan.Zero));
            Test.OrdinalEquals("20:16:12.345", converter.ToString(testDateTime.TimeOfDay));
            Test.OrdinalEquals("23:59:59.999", converter.ToString(TimeSpan.FromDays(1) - TimeSpan.FromTicks(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.ToString(TimeSpan.FromDays(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.ToString(TimeSpan.FromTicks(-1)));
        }

        [Test]
        public static void ToStringTests()
        {
            ToStringTests_Seconds();
            ToStringTests_Minutes();
            ToStringTests_Milliseconds();
        }

        #endregion

        #region ParseTests

        private static void ParseTests_Seconds()
        {
            var testDateTime = new DateTime(2015, 02, 28, 20, 16, 12, DateTimeKind.Utc);
            var converter = ISO8601Converter.SecondsPrecision;

            // Utc DateTime
            Test.AssertAreEqual(testDateTime, DataStore.Parse<DateTime>("2015-02-28T20:16:12Z", converter));
            Test.AssertAreEqual(testDateTime, DataStore.Parse<DateTime>("2015-02-28T20:16:12+00:00", converter));

            // Local DateTime (converts to Utc)
            Test.AssertAreEqual(testDateTime.AddHours(-1), DataStore.Parse<DateTime>("2015-02-28T20:16:12+01:00", converter));

            // Utc DateTimeOffset
            Assert.AreEqual(new DateTimeOffset(testDateTime), DataStore.Parse<DateTimeOffset>("2015-02-28T20:16:12Z", converter));
            Assert.AreEqual(new DateTimeOffset(testDateTime), DataStore.Parse<DateTimeOffset>("2015-02-28T20:16:12+00:00", converter));

            // Local DateTimeOffset (converts to Utc)
            Assert.AreEqual(new DateTimeOffset(testDateTime.AddHours(-1)), DataStore.Parse<DateTimeOffset>("2015-02-28T20:16:12+01:00", converter));

            // TimeSpan
            Assert.AreEqual(TimeSpan.Zero, DataStore.Parse<TimeSpan>("00:00:00", converter));
            Assert.AreEqual(testDateTime.TimeOfDay, DataStore.Parse<TimeSpan>("20:16:12", converter));
            Assert.AreEqual(TimeSpan.FromDays(1) - TimeSpan.FromSeconds(1), DataStore.Parse<TimeSpan>("23:59:59", converter));
        }

        private static void ParseTests_Minutes()
        {
            var testDateTime = new DateTime(2015, 02, 28, 20, 16, 00, DateTimeKind.Utc);
            var converter = ISO8601Converter.MinutesPrecision;

            // Utc DateTime
            Test.AssertAreEqual(testDateTime, DataStore.Parse<DateTime>("2015-02-28T20:16Z", converter));
            Test.AssertAreEqual(testDateTime, DataStore.Parse<DateTime>("2015-02-28T20:16+00:00", converter));

            // Local DateTime (converts to Utc)
            Test.AssertAreEqual(testDateTime.AddHours(-1), DataStore.Parse<DateTime>("2015-02-28T20:16+01:00", converter));

            // Utc DateTimeOffset
            Assert.AreEqual(new DateTimeOffset(testDateTime), DataStore.Parse<DateTimeOffset>("2015-02-28T20:16Z", converter));
            Assert.AreEqual(new DateTimeOffset(testDateTime), DataStore.Parse<DateTimeOffset>("2015-02-28T20:16+00:00", converter));

            // Local DateTimeOffset (converts to Utc)
            Assert.AreEqual(new DateTimeOffset(testDateTime.AddHours(-1)), DataStore.Parse<DateTimeOffset>("2015-02-28T20:16+01:00", converter));

            // TimeSpan
            Assert.AreEqual(TimeSpan.Zero, DataStore.Parse<TimeSpan>("00:00", converter));
            Assert.AreEqual(testDateTime.TimeOfDay, DataStore.Parse<TimeSpan>("20:16", converter));
            Assert.AreEqual(TimeSpan.FromDays(1) - TimeSpan.FromMinutes(1), DataStore.Parse<TimeSpan>("23:59", converter));
        }

        private static void ParseTests_Milliseconds()
        {
            var testDateTime = new DateTime(2015, 02, 28, 20, 16, 12, 345, DateTimeKind.Utc);
            var converter = ISO8601Converter.MillisecondsPrecision;

            // Utc DateTime
            Test.AssertAreEqual(testDateTime, DataStore.Parse<DateTime>("2015-02-28T20:16:12.345Z", converter));
            Test.AssertAreEqual(testDateTime, DataStore.Parse<DateTime>("2015-02-28T20:16:12.345+00:00", converter));

            // Local DateTime (converts to Utc)
            Test.AssertAreEqual(testDateTime.AddHours(-1), DataStore.Parse<DateTime>("2015-02-28T20:16:12.345+01:00", converter));

            // Utc DateTimeOffset
            Assert.AreEqual(new DateTimeOffset(testDateTime), DataStore.Parse<DateTimeOffset>("2015-02-28T20:16:12.345Z", converter));
            Assert.AreEqual(new DateTimeOffset(testDateTime), DataStore.Parse<DateTimeOffset>("2015-02-28T20:16:12.345+00:00", converter));

            // Local DateTimeOffset (converts to Utc)
            Assert.AreEqual(new DateTimeOffset(testDateTime.AddHours(-1)), DataStore.Parse<DateTimeOffset>("2015-02-28T20:16:12.345+01:00", converter));

            // TimeSpan
            Assert.AreEqual(TimeSpan.Zero, DataStore.Parse<TimeSpan>("00:00:00.000", converter));
            Assert.AreEqual(testDateTime.TimeOfDay, DataStore.Parse<TimeSpan>("20:16:12.345", converter));
            Assert.AreEqual(TimeSpan.FromDays(1) - TimeSpan.FromMilliseconds(1), DataStore.Parse<TimeSpan>("23:59:59.999", converter));
        }

        [Test]
        public static void ParseTests()
        {
            ParseTests_Seconds();
            ParseTests_Minutes();
            ParseTests_Milliseconds();
        }

        #endregion
    }
}
