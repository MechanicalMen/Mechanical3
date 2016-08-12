using System.Linq;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Json;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores.Json
{
    [TestFixture(Category = "DataStores")]
    public static class JsonFileFormatReaderTests
    {
        #region Complex tests

        internal static readonly TestData.FileFormatReaderOutput[] ComplexOutputs = TestData.TextReaderOutput.ComplexOutputs.Select(o => TestData.FileFormatReaderOutput.From(o, nullNameReplacement: null)).ToArray();
        internal const string ComplexJson = @"{
  ""FormatVersion"": 2,
  ""DataStore"": {
    ""value_not_empty"": ""a"",
    ""value_empty"": """",
    ""value_null"": null,
    ""object_not_empty"": {
      ""a"": ""b""
    },
    ""object_empty"": {},
    ""as_array"": [
      ""a"",
      """",
      null,
      {
        ""a"": ""b""
      },
      {}
    ],
    ""array_empty"": []
  }
}";

        [Test]
        public static void ComplexJsonReaderTests()
        {
            TestData.AssertEquals(
                JsonFileFormatFactory.Default.CreateReader(ComplexJson),
                ToJsonOutputs(ComplexOutputs));
        }

        #endregion

        #region Simple tests

        internal const string SimpleJson_ArrayRoot = @"{
  ""FormatVersion"": 2,
  ""DataStore"": [
    ""a"",
    ""b""
  ]
}";

        internal const string SimpleJson_NestedObjects = @"{
  ""FormatVersion"": 2,
  ""DataStore"": {
    ""a"": {},
    ""b"": {
      ""b"": {
        ""b"": {}
      }
    },
    ""c"": {}
  }
}";

        // testing different root name here as well
        internal const string SimpleJson_NestedArrays = @"{
  ""FormatVersion"": 2,
  ""DataStore"": [
    [],
    [
      [
        []
      ]
    ],
    []
  ]
}";

        private static TestData.FileFormatReaderOutput[] ToJsonOutputs( TestData.FileFormatReaderOutput[] outputs )
        {
            outputs = outputs.Select(o =>
            {
                if( o.Token == DataStoreToken.End )
                    return TestData.FileFormatReaderOutput.True(o.Token, name: null, value: o.Value);
                else
                    return o;
            }).ToArray();
            return outputs;
        }

        [Test]
        public static void SimpleJsonReaderTests()
        {
            TestData.AssertEquals(
                JsonFileFormatFactory.Default.CreateReader(SimpleJson_ArrayRoot),
                ToJsonOutputs(TestData.FileFormatReaderOutput.SimpleOutput_ArrayRoot));

            TestData.AssertEquals(
                JsonFileFormatFactory.Default.CreateReader(SimpleJson_NestedObjects),
                ToJsonOutputs(TestData.FileFormatReaderOutput.SimpleOutput_NestedObjects));

            TestData.AssertEquals(
                JsonFileFormatFactory.Default.CreateReader(SimpleJson_NestedArrays),
                ToJsonOutputs(TestData.FileFormatReaderOutput.SimpleOutput_NestedArrays));
        }

        #endregion
    }
}
