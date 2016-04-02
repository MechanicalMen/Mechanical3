using Mechanical3.DataStores;
using Mechanical3.DataStores.Xml;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores.Xml
{
    [TestFixture(Category = "DataStores")]
    public static class XmlFileFormatReaderTests
    {
        #region ReaderOutput

        internal class ReaderOutput
        {
            public bool Result { get; }
            public DataStoreToken Token { get; }
            public string Name { get; }
            public string Value { get; }

            internal ReaderOutput( bool result, DataStoreToken token, string name, string value )
            {
                this.Result = result;
                this.Token = token;
                this.Name = name;
                this.Value = value;
            }

            internal static ReaderOutput True( DataStoreToken token = default(DataStoreToken), string name = null, string value = null )
            {
                return new ReaderOutput(true, token, name, value);
            }

            internal static ReaderOutput False( DataStoreToken token = default(DataStoreToken), string name = null, string value = null )
            {
                return new ReaderOutput(false, token, name, value);
            }

            private static void AssertResultEquals( IDataStoreTextFileFormatReader reader, ReaderOutput expectedOutput )
            {
                Assert.NotNull(reader);
                Assert.NotNull(expectedOutput);

                DataStoreToken actualToken;
                string actualName, actualValue;
                bool actualResult = reader.TryReadToken(out actualToken, out actualName, out actualValue);

                Assert.AreEqual(expectedOutput.Result, actualResult);
                Assert.AreEqual(expectedOutput.Token, actualToken);
                Test.OrdinalEquals(expectedOutput.Name, actualName);
                Test.OrdinalEquals(expectedOutput.Value, actualValue);
            }

            internal static void AssertResultsEqual( IDataStoreTextFileFormatReader reader, ReaderOutput[] expectedOutputs )
            {
                Assert.NotNull(expectedOutputs);

                foreach( var output in expectedOutputs )
                    AssertResultEquals(reader, output);
            }
        }

        #endregion

        #region Complex tests

        private const string ComplexXml_Format3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<DataStore formatVersion=""3"" type=""object"">
    <value_not_empty>a</value_not_empty>
    <value_empty type=""value""></value_empty>
    <value_null />

    <object_not_empty type=""object"">
        <a>b</a>
    </object_not_empty>
    <object_empty type=""object""></object_empty>

    <as_array type=""array"">
        <i>a</i>
        <i type=""value""></i>
        <i />
        <i type=""object"">
            <a>b</a>
        </i>
        <i type=""object""></i>
    </as_array>
    <array_empty type=""array""></array_empty>
</DataStore>";

        private static readonly ReaderOutput[] ComplexOutputs_Format3 = new ReaderOutput[]
        {
            ReaderOutput.True(DataStoreToken.ObjectStart, "DataStore"),

            ReaderOutput.True(DataStoreToken.Value, "value_not_empty", "a"),
            ReaderOutput.True(DataStoreToken.Value, "value_empty", string.Empty),
            ReaderOutput.True(DataStoreToken.Value, "value_null", null),
            ReaderOutput.True(DataStoreToken.ObjectStart, "object_not_empty"),
            ReaderOutput.True(DataStoreToken.Value, "a", "b"),
            ReaderOutput.True(DataStoreToken.End, "object_not_empty"),
            ReaderOutput.True(DataStoreToken.ObjectStart, "object_empty"),
            ReaderOutput.True(DataStoreToken.End, "object_empty"),

            ReaderOutput.True(DataStoreToken.ArrayStart, "as_array"),
            ReaderOutput.True(DataStoreToken.Value, "i", "a"),
            ReaderOutput.True(DataStoreToken.Value, "i", string.Empty),
            ReaderOutput.True(DataStoreToken.Value, "i", null),
            ReaderOutput.True(DataStoreToken.ObjectStart, "i"),
            ReaderOutput.True(DataStoreToken.Value, "a", "b"),
            ReaderOutput.True(DataStoreToken.End, "i"),
            ReaderOutput.True(DataStoreToken.ObjectStart, "i"),
            ReaderOutput.True(DataStoreToken.End, "i"),
            ReaderOutput.True(DataStoreToken.End, "as_array"),
            ReaderOutput.True(DataStoreToken.ArrayStart, "array_empty"),
            ReaderOutput.True(DataStoreToken.End, "array_empty"),

            ReaderOutput.True(DataStoreToken.End, "DataStore"),
            ReaderOutput.False(),
            ReaderOutput.False(),
            ReaderOutput.False(),
        };

        [Test]
        public static void ComplexXmlTests()
        {
            ReaderOutput.AssertResultsEqual(
                XmlFileFormatReader.FromXml(ComplexXml_Format3),
                ComplexOutputs_Format3);
        }

        #endregion

        #region Small tests

        private const string SmallXml_ArrayRoot_Format3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<DataStore formatVersion=""3"" type=""array"">
    <i>a</i>
    <i>b</i>
</DataStore>";

        // testing different root names here as well
        private const string SmallXml_ValueRoot_Format3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<value_not_empty formatVersion=""3"">a</value_not_empty>";
        private const string SmallXml_EmptyValueRoot_Format3 = @"<value_empty formatVersion=""3"" type=""value""></value_empty>";
        private const string SmallXml_NullValueRoot_Format3 = @"<value_null formatVersion=""3"" />";

        private const string SmallXml_NestedObjects_Format3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<DataStore formatVersion=""3"" type=""object"">
    <a type=""object""></a>
    <b type=""object"">
        <b type=""object"">
            <b type=""object""></b>
        </b>
    </b>
    <c type=""object""></c>
</DataStore>";

        private const string SmallXml_NestedArrays_Format3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<DataStore formatVersion=""3"" type=""array"">
    <i type=""array""></i>
    <i type=""array"">
        <i type=""array"">
            <i type=""array""></i>
        </i>
    </i>
    <i type=""array""></i>
</DataStore>";

        private static readonly ReaderOutput[] SmallOutput_ArrayRoot_Format3 = new ReaderOutput[]
        {
            ReaderOutput.True(DataStoreToken.ArrayStart, "DataStore"),
            ReaderOutput.True(DataStoreToken.Value, "i", "a"),
            ReaderOutput.True(DataStoreToken.Value, "i", "b"),
            ReaderOutput.True(DataStoreToken.End, "DataStore"),
            ReaderOutput.False(),
        };

        private static readonly ReaderOutput[] SmallOutput_ValueRoot_Format3 = new ReaderOutput[]
        {
            ReaderOutput.True(DataStoreToken.Value, "value_not_empty", "a"),
            ReaderOutput.False(),
        };

        private static readonly ReaderOutput[] SmallOutput_EmptyValueRoot_Format3 = new ReaderOutput[]
        {
            ReaderOutput.True(DataStoreToken.Value, "value_empty", string.Empty),
            ReaderOutput.False(),
        };

        private static readonly ReaderOutput[] SmallOutput_NullValueRoot_Format3 = new ReaderOutput[]
        {
            ReaderOutput.True(DataStoreToken.Value, "value_null", null),
            ReaderOutput.False(),
        };

        private static readonly ReaderOutput[] SmallOutput_NestedObjects_Format3 = new ReaderOutput[]
        {
            ReaderOutput.True(DataStoreToken.ObjectStart, "DataStore"),
            ReaderOutput.True(DataStoreToken.ObjectStart, "a"),
            ReaderOutput.True(DataStoreToken.End, "a"),
            ReaderOutput.True(DataStoreToken.ObjectStart, "b"),
            ReaderOutput.True(DataStoreToken.ObjectStart, "b"),
            ReaderOutput.True(DataStoreToken.ObjectStart, "b"),
            ReaderOutput.True(DataStoreToken.End, "b"),
            ReaderOutput.True(DataStoreToken.End, "b"),
            ReaderOutput.True(DataStoreToken.End, "b"),
            ReaderOutput.True(DataStoreToken.ObjectStart, "c"),
            ReaderOutput.True(DataStoreToken.End, "c"),
            ReaderOutput.True(DataStoreToken.End, "DataStore"),
            ReaderOutput.False(),
        };

        private static readonly ReaderOutput[] SmallOutput_NestedArrays_Format3 = new ReaderOutput[]
        {
            ReaderOutput.True(DataStoreToken.ArrayStart, "DataStore"),
            ReaderOutput.True(DataStoreToken.ArrayStart, "i"),
            ReaderOutput.True(DataStoreToken.End, "i"),
            ReaderOutput.True(DataStoreToken.ArrayStart, "i"),
            ReaderOutput.True(DataStoreToken.ArrayStart, "i"),
            ReaderOutput.True(DataStoreToken.ArrayStart, "i"),
            ReaderOutput.True(DataStoreToken.End, "i"),
            ReaderOutput.True(DataStoreToken.End, "i"),
            ReaderOutput.True(DataStoreToken.End, "i"),
            ReaderOutput.True(DataStoreToken.ArrayStart, "i"),
            ReaderOutput.True(DataStoreToken.End, "i"),
            ReaderOutput.True(DataStoreToken.End, "DataStore"),
            ReaderOutput.False(),
        };

        [Test]
        public static void SmallXmlTests()
        {
            ReaderOutput.AssertResultsEqual(
                XmlFileFormatReader.FromXml(SmallXml_ArrayRoot_Format3),
                SmallOutput_ArrayRoot_Format3);

            ReaderOutput.AssertResultsEqual(
                XmlFileFormatReader.FromXml(SmallXml_ValueRoot_Format3),
                SmallOutput_ValueRoot_Format3);

            ReaderOutput.AssertResultsEqual(
                XmlFileFormatReader.FromXml(SmallXml_EmptyValueRoot_Format3),
                SmallOutput_EmptyValueRoot_Format3);

            ReaderOutput.AssertResultsEqual(
                XmlFileFormatReader.FromXml(SmallXml_NullValueRoot_Format3),
                SmallOutput_NullValueRoot_Format3);

            ReaderOutput.AssertResultsEqual(
                XmlFileFormatReader.FromXml(SmallXml_NestedObjects_Format3),
                SmallOutput_NestedObjects_Format3);

            ReaderOutput.AssertResultsEqual(
                XmlFileFormatReader.FromXml(SmallXml_NestedArrays_Format3),
                SmallOutput_NestedArrays_Format3);
        }

        #endregion
    }
}
