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

        private enum NameMatch
        {
            HasName_Matches,  // token has name, member has same name
            HasName_Ignored,  // token has name, members has null
            HasName_BadMatch, // token has name, member has different name
            NoName_Ignored,   // token has no name, member has null
            NoName_BadMatch   // token has no name, member has a name
        }

        private static void TestMember( Action<DataStoreTextReader, string> memberAction, DataStoreToken token, NameMatch nameMatch, bool expectException, string value = null )
        {
            bool hasName;
            switch( nameMatch )
            {
            case NameMatch.HasName_Matches:
            case NameMatch.HasName_Ignored:
            case NameMatch.HasName_BadMatch:
                hasName = true;
                break;
            case NameMatch.NoName_Ignored:
            case NameMatch.NoName_BadMatch:
                hasName = false;
                break;
            default:
                throw new ArgumentException().StoreFileLine();
            }

            string name = hasName ? "a" : null;
            var tokens = new TestData.FileFormatReaderOutput[] {
                TestData.FileFormatReaderOutput.StartOrEnd(hasName ? DataStoreToken.ObjectStart : DataStoreToken.ArrayStart),
                TestData.FileFormatReaderOutput.True(token, name, value),
                TestData.FileFormatReaderOutput.StartOrEnd(DataStoreToken.End)
            }.ToList();
            bool extraStartToken = token == DataStoreToken.End;
            if( extraStartToken )
                tokens.Insert(1, TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart, name));
            bool extraEndToken = token == DataStoreToken.ObjectStart || token == DataStoreToken.ArrayStart;
            if( extraEndToken )
                tokens.Insert(2, TestData.FileFormatReaderOutput.True(DataStoreToken.End, name));

            using( var reader = CreateOutputReader(tokens.ToArray()) )
            {
                reader.Read();

                if( extraStartToken )
                    reader.Read();

                switch( nameMatch )
                {
                case NameMatch.HasName_Ignored:
                case NameMatch.NoName_Ignored:
                    name = null;
                    break;
                case NameMatch.HasName_BadMatch:
                case NameMatch.NoName_BadMatch:
                    name = "b";
                    break;
                default:
                    break;
                }

                if( expectException )
                    Assert.Throws<FormatException>(() => memberAction(reader, name));
                else
                    memberAction(reader, name);

                if( extraEndToken )
                    reader.Read();

                reader.Read(); // end of stream
                Assert.Throws<FormatException>(() => memberAction(reader, name)); // members should not be accessible after the stream has ended
            }
        }

        [Test]
        public static void ExtendedTextReaderMemberTests()
        {
            Action<DataStoreTextReader, string> member;

            // AssertCanRead
            member = ( reader, name ) => reader.AssertCanRead(name);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Matches, expectException: false);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Ignored, expectException: false);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_BadMatch, expectException: true);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_Ignored, expectException: false);
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.NoName_BadMatch, expectException: true);


            // ReadObjectStart, implicitly AssertObjectStart
            member = ( reader, name ) => reader.ReadObjectStart(name);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_Matches, expectException: false);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_Ignored, expectException: false);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_BadMatch, expectException: true);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_Ignored, expectException: false);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_BadMatch, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Matches, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Ignored, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_BadMatch, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_Ignored, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_BadMatch, expectException: true);


            // ReadArrayStart, implicitly AssertArrayStart
            member = ( reader, name ) => reader.ReadArrayStart(name);
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.HasName_Matches, expectException: false);
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.HasName_Ignored, expectException: false);
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.HasName_BadMatch, expectException: true);
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.NoName_Ignored, expectException: false);
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.NoName_BadMatch, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Matches, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Ignored, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_BadMatch, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_Ignored, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_BadMatch, expectException: true);


            // ReadEnd, implicitly AssertEnd
            member = ( reader, name ) => reader.ReadEnd(); // name ignored
            TestMember(member, DataStoreToken.End, NameMatch.HasName_Matches, expectException: false);
            TestMember(member, DataStoreToken.End, NameMatch.HasName_Ignored, expectException: false);
            TestMember(member, DataStoreToken.End, NameMatch.HasName_BadMatch, expectException: false);
            TestMember(member, DataStoreToken.End, NameMatch.NoName_Ignored, expectException: false);
            TestMember(member, DataStoreToken.End, NameMatch.NoName_BadMatch, expectException: false);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Matches, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Ignored, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_BadMatch, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_Ignored, expectException: true);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_BadMatch, expectException: true);


            // ReadValue, implicitly GetValue
            member = ( reader, name ) => Test.OrdinalEquals(null, reader.ReadValue(name));
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Matches, expectException: false, value: null);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Ignored, expectException: false, value: null);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_BadMatch, expectException: true, value: null);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_Ignored, expectException: false, value: null);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_BadMatch, expectException: true, value: null);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_Matches, expectException: true, value: null);
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.HasName_Ignored, expectException: true, value: null);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_BadMatch, expectException: true, value: null);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_Ignored, expectException: true, value: null);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_BadMatch, expectException: true, value: null);
            member = ( reader, name ) => { if( !string.Equals("a", reader.ReadValue(name), StringComparison.Ordinal) ) throw new FormatException("Value does not match!"); };
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Matches, expectException: true, value: "b");
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Ignored, expectException: true, value: "b");
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_BadMatch, expectException: true, value: "b");
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_Ignored, expectException: true, value: "b");
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_BadMatch, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_Matches, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.HasName_Ignored, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_BadMatch, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_Ignored, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_BadMatch, expectException: true, value: "b");


            // ReadNull, implicitly AssertNull
            member = ( reader, name ) => reader.ReadNull(name);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Matches, expectException: false, value: null);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Ignored, expectException: false, value: null);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_BadMatch, expectException: true, value: null);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_Ignored, expectException: false, value: null);
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_BadMatch, expectException: true, value: null);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_Matches, expectException: true, value: null);
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.HasName_Ignored, expectException: true, value: null);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_BadMatch, expectException: true, value: null);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_Ignored, expectException: true, value: null);
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_BadMatch, expectException: true, value: null);
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Matches, expectException: true, value: "b");
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_Ignored, expectException: true, value: "b");
            TestMember(member, DataStoreToken.Value, NameMatch.HasName_BadMatch, expectException: true, value: "b");
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_Ignored, expectException: true, value: "b");
            TestMember(member, DataStoreToken.Value, NameMatch.NoName_BadMatch, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_Matches, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ArrayStart, NameMatch.HasName_Ignored, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.HasName_BadMatch, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_Ignored, expectException: true, value: "b");
            TestMember(member, DataStoreToken.ObjectStart, NameMatch.NoName_BadMatch, expectException: true, value: "b");
        }

        #endregion
    }
}
