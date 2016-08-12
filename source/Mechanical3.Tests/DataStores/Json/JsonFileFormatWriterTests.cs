using System.Text;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Json;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores.Json
{
    [TestFixture(Category = "DataStores")]
    public static class JsonFileFormatWriterTests
    {
        #region Private Methods

        private static string ToString( TestData.FileFormatReaderOutput[] outputs )
        {
            var sb = new StringBuilder();
            using( var writer = JsonFileFormatFactory.Default.CreateWriter(sb) )
            {
                foreach( var output in outputs )
                {
                    if( output.Result )
                        writer.WriteToken(output.Token, output.Name, output.Value, valueType: null);
                }
            }
            return sb.ToString();
        }

        #endregion

        [Test]
        public static void ComplexJsonWriterTests()
        {
            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(JsonFileFormatReaderTests.ComplexJson, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(JsonFileFormatReaderTests.ComplexOutputs));
        }

        [Test]
        public static void SimpleJsonWriterTests()
        {
            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(JsonFileFormatReaderTests.SimpleJson_ArrayRoot, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(TestData.FileFormatReaderOutput.SimpleOutput_ArrayRoot));

            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(JsonFileFormatReaderTests.SimpleJson_NestedObjects, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(TestData.FileFormatReaderOutput.SimpleOutput_NestedObjects));

            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(JsonFileFormatReaderTests.SimpleJson_NestedArrays, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(TestData.FileFormatReaderOutput.SimpleOutput_NestedArrays));
        }
    }
}
