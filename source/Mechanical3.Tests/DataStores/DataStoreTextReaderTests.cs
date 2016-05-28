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

        private static DataStoreTextReader CreateOutputReader( params TestData.FileFormatReaderOutput[] outputs )
        {
            return new DataStoreTextReader(new TestData.FileFormatReaderOutput.Reader(outputs));
        }

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
            using( var reader = CreateOutputReader() )
            {
                Assert.Throws<FormatException>(() => reader.Read());
            }


            // unexpected end of file
            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart)) )
            {
                Assert.True(reader.Read());
                Assert.Throws<EndOfStreamException>(() => reader.Read());
            }


            // multiple root nodes
            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End)) )
            {
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Throws<FormatException>(() => reader.Read());
            }


            // invalid name
            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, new string('a', count: 1000), value: "b"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End)) )
            {
                Assert.True(reader.Read());
                Assert.Throws<FormatException>(() => reader.Read());
            }


            // invalid starting token
            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, "a", "b")) )
            {
                Assert.Throws<FormatException>(() => reader.Read());
            }

            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.True(DataStoreToken.End)) )
            {
                Assert.Throws<FormatException>(() => reader.Read());
            }
        }

        #endregion

        #region Extended member tests

        [Test]
        public static void ExtendedTextReaderMemberTests()
        {
            // AssertCanRead
            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.StartOrEnd(DataStoreToken.ArrayStart),
                TestData.FileFormatReaderOutput.StartOrEnd(DataStoreToken.End)) )
            {
                reader.Read();
                reader.AssertCanRead();
                Assert.Throws<FormatException>(() => reader.AssertCanRead());
            }


            // ReadObjectStart, implicitly AssertObjectStart
            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart, "a"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart, "a"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart, "b"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End)) )
            {
                reader.ReadObjectStart(); // no name
                reader.ReadObjectStart(); // ignores name
                reader.ReadObjectStart("a"); // matches name
                Assert.Throws<FormatException>(() => reader.ReadObjectStart()); // token mismatch
                Assert.Throws<FormatException>(() => reader.ReadObjectStart("X")); // name mismatch
                reader.Read();
                reader.Read();
                reader.Read();
                Assert.Throws<FormatException>(() => reader.ReadObjectStart()); // end of stream
            }


            // ReadArrayStart, implicitly AssertArrayStart
            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart, "a"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart, "b"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart, "c"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End)) )
            {
                reader.Read();
                reader.ReadArrayStart("a"); // matches name
                reader.ReadArrayStart(); // no name
                reader.Read();
                reader.Read();
                Assert.Throws<FormatException>(() => reader.ReadArrayStart("X")); // name mismatch
                Assert.Throws<FormatException>(() => reader.ReadArrayStart()); // token mismatch
                reader.ReadArrayStart(); // ignores name
                reader.Read();
                reader.Read();
                Assert.Throws<FormatException>(() => reader.ReadArrayStart()); // end of stream
            }


            // ReadEnd, implicitly AssertEnd
            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart, "a"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart, "b"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End)) )
            {
                reader.Read();
                reader.Read();
                reader.ReadEnd(); // ignores name
                reader.Read();
                Assert.Throws<FormatException>(() => reader.ReadEnd()); // token mismatch
                reader.ReadEnd(); // no name
                reader.Read();
                reader.Read();
                Assert.Throws<FormatException>(() => reader.ReadEnd()); // end of stream
            }


            // ReadValue, implicitly GetValue
            using( var reader = CreateOutputReader(
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, "a", "a"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, "b", "b"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, "c", "c"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ArrayStart),
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, null, "a"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End)) )
            {
                reader.Read();
                Test.OrdinalEquals("a", reader.ReadValue("a")); // matches name
                Test.OrdinalEquals("b", reader.ReadValue<string>()); // ignores name
                Assert.Throws<FormatException>(() => reader.ReadValue("X")); // name mismatch
                Assert.Throws<FormatException>(() => reader.ReadValue()); // token mismatch
                Test.OrdinalEquals("a", reader.ReadValue()); // no name
                reader.Read();
                reader.Read();
                Assert.Throws<FormatException>(() => reader.ReadValue()); // end of stream
            }
        }

        #endregion
    }
}
