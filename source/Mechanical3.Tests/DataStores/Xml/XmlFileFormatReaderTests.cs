using System;
using System.Linq;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Xml;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores.Xml
{
    [TestFixture(Category = "DataStores")]
    public static class XmlFileFormatReaderTests
    {
        static XmlFileFormatReaderTests()
        {
            ComplexOutputs_Format3 = TestData.TextReaderOutput.ComplexOutputs.Select(o => TestData.FileFormatReaderOutput.From(o, nullNameReplacement: "i")).ToArray();
            ComplexOutputs_Format3[0] = TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart, name: "DataStore");
            ComplexOutputs_Format3[ComplexOutputs_Format3.Length - 4] = TestData.FileFormatReaderOutput.True(DataStoreToken.End, name: "DataStore");
        }

        #region Complex tests

        internal static readonly TestData.FileFormatReaderOutput[] ComplexOutputs_Format3;
        internal const string ComplexXml_Format3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
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

        private const string ComplexXml_Format2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
    <NameOfRootDataStoreObject>
        <value_not_empty>a</value_not_empty>
        <value_empty />

        <object_not_empty>
            <a>b</a>
        </object_not_empty>
        <object_empty></object_empty>
    </NameOfRootDataStoreObject>
</root>";

        private static readonly TestData.FileFormatReaderOutput[] ComplexOutputs_Format2 = new TestData.FileFormatReaderOutput[]
        {
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart, "NameOfRootDataStoreObject"),

                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, "value_not_empty", "a"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, "value_empty", string.Empty),

                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart, "object_not_empty"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.Value, "a", "b"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End, "object_not_empty"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.ObjectStart, "object_empty"),
                TestData.FileFormatReaderOutput.True(DataStoreToken.End, "object_empty"),

                TestData.FileFormatReaderOutput.True(DataStoreToken.End, "NameOfRootDataStoreObject"),
                TestData.FileFormatReaderOutput.False(),
                TestData.FileFormatReaderOutput.False(),
                TestData.FileFormatReaderOutput.False(),
        };

        [Test]
        public static void ComplexXmlReaderTests()
        {
            TestData.AssertEquals(
                XmlFileFormatReader.FromXml(ComplexXml_Format3),
                ToXmlOutputs(TestData.TextReaderOutput.ComplexOutputs.Select(o => TestData.FileFormatReaderOutput.From(o, nullNameReplacement: "i")).ToArray(), rootName: "DataStore"));

            TestData.AssertEquals(
                XmlFileFormatReader.FromXml(ComplexXml_Format2),
                ComplexOutputs_Format2);
        }

        #endregion

        #region Simple tests

        internal const string SimpleXml_ArrayRoot_Format3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<DataStore formatVersion=""3"" type=""array"">
  <i>a</i>
  <i>b</i>
</DataStore>";

        internal const string SimpleXml_NestedObjects_Format3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<DataStore formatVersion=""3"" type=""object"">
  <a type=""object""></a>
  <b type=""object"">
    <b type=""object"">
      <b type=""object""></b>
    </b>
  </b>
  <c type=""object""></c>
</DataStore>";

        // testing different root name here as well
        internal const string SimpleXml_NestedArrays_Format3 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<DataStore formatVersion=""3"" type=""array"">
  <i type=""array""></i>
  <i type=""array"">
    <i type=""array"">
      <i type=""array""></i>
    </i>
  </i>
  <i type=""array""></i>
</DataStore>";

        private const string SimpleXml_NestedObjects_Format2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <DataStore>
    <a></a>
    <b>
      <b>
        <b></b>
      </b>
    </b>
    <c></c>
  </DataStore>
</root>";

        private static TestData.FileFormatReaderOutput[] ToXmlOutputs( TestData.FileFormatReaderOutput[] outputs, string rootName )
        {
            outputs = outputs.Select(op => TestData.FileFormatReaderOutput.From(op, nullNameReplacement: "i")).ToArray();

            var o = outputs[0];
            outputs[0] = TestData.FileFormatReaderOutput.True(o.Token, rootName, o.Value);

            int index = outputs.Select(( op, i ) => Tuple.Create(op, i)).Reverse().Where(t => t.Item1.Result).First().Item2; // the last non-false index
            o = outputs[index];
            outputs[index] = TestData.FileFormatReaderOutput.True(o.Token, rootName, o.Value);

            return outputs;
        }

        [Test]
        public static void SimpleXmlReaderTests()
        {
            TestData.AssertEquals(
                XmlFileFormatReader.FromXml(SimpleXml_ArrayRoot_Format3),
                ToXmlOutputs(TestData.FileFormatReaderOutput.SimpleOutput_ArrayRoot, rootName: "DataStore"));

            TestData.AssertEquals(
                XmlFileFormatReader.FromXml(SimpleXml_NestedObjects_Format3),
                ToXmlOutputs(TestData.FileFormatReaderOutput.SimpleOutput_NestedObjects, rootName: "DataStore"));

            TestData.AssertEquals(
                XmlFileFormatReader.FromXml(SimpleXml_NestedArrays_Format3.Replace("DataStore", "RandomName")), // different root name test
                ToXmlOutputs(TestData.FileFormatReaderOutput.SimpleOutput_NestedArrays, rootName: "RandomName"));


            TestData.AssertEquals(
                XmlFileFormatReader.FromXml(SimpleXml_NestedObjects_Format2),
                ToXmlOutputs(TestData.FileFormatReaderOutput.SimpleOutput_NestedObjects, rootName: "DataStore"));
        }

        #endregion
    }
}
