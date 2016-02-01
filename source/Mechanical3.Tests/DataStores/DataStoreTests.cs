using System;
using System.Collections.Generic;
using Mechanical3.DataStores;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores
{
    [TestFixture(Category = "DataStores")]
    public static class DataStoreTests
    {
        private class TestStringConverter : IStringConverter<int>
        {
            public string ToString( int obj )
            {
                return "a";
            }

            public bool TryParse( string str, out int obj )
            {
                obj = 5;
                return true;
            }
        }

        [Test]
        public static void IsValidNameTests()
        {
            Assert.False(DataStore.IsValidName(null));
            Assert.False(DataStore.IsValidName(string.Empty));

            Assert.False(DataStore.IsValidName(new string('a', count: 256)));
            Assert.True(DataStore.IsValidName(new string('a', count: 255)));

            Assert.True(DataStore.IsValidName("A"));
            Assert.True(DataStore.IsValidName("_"));
            Assert.False(DataStore.IsValidName("á"));
            Assert.False(DataStore.IsValidName("3"));
            Assert.False(DataStore.IsValidName(" "));
            Assert.False(DataStore.IsValidName(" a"));
            Assert.False(DataStore.IsValidName("a "));
        }

        [Test]
        public static void ToStringTests()
        {
            var testLocator = new StringConverterCollection();
            testLocator.Add(new TestStringConverter());

            Assert.Throws<KeyNotFoundException>(() => DataStore.ToString<float>(0f, testLocator));
            Test.OrdinalEquals("0", DataStore.ToString<float>(0f));

            Test.OrdinalEquals("a", DataStore.ToString<int>(0, testLocator));
            Test.OrdinalEquals("0", DataStore.ToString<int>(0));
        }

        [Test]
        public static void TryParseTests()
        {
            var testLocator = new StringConverterCollection();
            testLocator.Add(new TestStringConverter());

            int value;
            Assert.False(DataStore.TryParse<int>("a", out value));
            Assert.True(DataStore.TryParse<int>("a", out value, testLocator));
            Assert.AreEqual(5, value);

            float single;
            Assert.False(DataStore.TryParse<float>("1", out single, testLocator));
            Assert.True(DataStore.TryParse<float>("1", out single));
            Assert.AreEqual(1f, single);

            Assert.False(DataStore.TryParse<int>(null, out value, testLocator)); // fails, even though the converter could handle it
        }

        [Test]
        public static void ParseTests()
        {
            IStringConverter<int> converter = new TestStringConverter();
            var testLocator = new StringConverterCollection();
            testLocator.Add(converter);

            // converter tests
            Assert.AreEqual(5, DataStore.Parse("asd", converter));
            Assert.Throws<ArgumentNullException>(() => DataStore.Parse("asd", (IStringConverter<int>)null));
            Assert.Throws<ArgumentNullException>(() => DataStore.Parse(null, converter)); // throws, even though the converter could handle it
            Assert.Throws<FormatException>(() => DataStore.Parse("asd", RoundTripStringConverter.Locator.GetConverter<int>()));

            // locator tests
            Assert.AreEqual(5, DataStore.Parse<int>("a", testLocator));
            Assert.Throws<ArgumentNullException>(() => DataStore.Parse<int>(null, testLocator)); // throws, even though the converter could handle it
            Assert.Throws<FormatException>(() => DataStore.Parse<int>("a"));
            Assert.Throws<KeyNotFoundException>(() => DataStore.Parse<float>("1", testLocator));
            Assert.AreEqual(1f, DataStore.Parse<float>("1"));
        }
    }
}
