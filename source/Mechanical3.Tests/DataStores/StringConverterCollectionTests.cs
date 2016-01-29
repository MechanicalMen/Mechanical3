using System;
using System.Collections.Generic;
using Mechanical3.DataStores;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores
{
    [TestFixture(Category = "DataStores")]
    public static class StringConverterCollectionTests
    {
        private class DummyConverter : IStringConverter<int>
        {
            public string ToString( int obj )
            {
                throw new NotImplementedException();
            }

            public bool TryParse( string str, out int obj )
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public static void ConverterCollectionTests()
        {
            var collection = new StringConverterCollection();
            Assert.Throws<KeyNotFoundException>(() => collection.GetConverter<int>());

            var converter = new DummyConverter();
            collection.Add(converter);
            Assert.Throws<ArgumentException>(() => collection.Add(converter)); // can not add the same converter twice
            Assert.Throws<ArgumentException>(() => collection.Add(new DummyConverter())); // can not add two converters for the same type

            Assert.True(object.ReferenceEquals(converter, collection.GetConverter<int>()));
        }
    }
}
