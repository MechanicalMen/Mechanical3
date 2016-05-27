using System;
using System.IO;
using System.Linq;
using Mechanical3.Core;
using Mechanical3.DataStores;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores
{
    [TestFixture(Category = "DataStores")]
    public static class DataStoreTextReaderTests
    {
        #region Complex tests

        private static DataStoreTextReader CreateComplexTextReader()
        {
            return new DataStoreTextReader(
                new TestData.FileFormatReaderOutput.Reader(
                    TestData.TextReaderOutput.ComplexOutputs.Select(o => TestData.FileFormatReaderOutput.From(o, nullNameReplacement: null)).ToArray()));
        }

        [Test]
        public static void ComplexTextReaderTests()
        {
            TestData.AssertEquals(
                CreateComplexTextReader(),
                TestData.TextReaderOutput.ComplexOutputs);
        }

        #endregion

        #region Simple Tests

        [Test]
        public static void SimpleTextReaderTests()
        {
            // accessing before the first Read
            using( var reader = CreateComplexTextReader() )
            {
                Assert.Throws<InvalidOperationException>(() => reader.Token.ToString());
                Assert.Throws<InvalidOperationException>(() => reader.Name.NotNullReference());
                Assert.Throws<InvalidOperationException>(() => reader.Index.ToString());
                Assert.Throws<InvalidOperationException>(() => reader.Value.NotNullReference());
                Assert.Throws<InvalidOperationException>(() => reader.Path.NotNullReference());
            }


            // empty data store
            var emptyFileOutputs = new TestData.FileFormatReaderOutput[0];
            using( var reader = new DataStoreTextReader(new TestData.FileFormatReaderOutput.Reader(emptyFileOutputs)) )
            {
                Assert.Throws<FormatException>(() => reader.Read());
            }


            // unexpected end of file
            var eofFileOutputs = new TestData.FileFormatReaderOutput[] {
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart),
            };
            using( var reader = new DataStoreTextReader(new TestData.FileFormatReaderOutput.Reader(eofFileOutputs)) )
            {
                Assert.True(reader.Read());
                Assert.Throws<EndOfStreamException>(() => reader.Read());
            }


            // multiple root nodes
            var multiFileOutputs = new TestData.FileFormatReaderOutput[] {
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
            };
            using( var reader = new DataStoreTextReader(new TestData.FileFormatReaderOutput.Reader(multiFileOutputs)) )
            {
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Throws<FormatException>(() => reader.Read());
            }


            // invalid name
            var nameFileOutputs = new TestData.FileFormatReaderOutput[] {
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, new string('a', count: 1000), value: "b"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
            };
            using( var reader = new DataStoreTextReader(new TestData.FileFormatReaderOutput.Reader(nameFileOutputs)) )
            {
                Assert.True(reader.Read());
                Assert.Throws<FormatException>(() => reader.Read());
            }


            // invalid starting token
            var startFileOutput1 = new TestData.FileFormatReaderOutput[] {
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, "a", "b"),
            };
            using( var reader = new DataStoreTextReader(new TestData.FileFormatReaderOutput.Reader(startFileOutput1)) )
            {
                Assert.Throws<FormatException>(() => reader.Read());
            }

            var startFileOutput2 = new TestData.FileFormatReaderOutput[] {
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
            };
            using( var reader = new DataStoreTextReader(new TestData.FileFormatReaderOutput.Reader(startFileOutput2)) )
            {
                Assert.Throws<FormatException>(() => reader.Read());
            }
        }

        #endregion
    }
}
