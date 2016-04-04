using System.Text;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Xml;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores.Xml
{
    [TestFixture(Category = "DataStores")]
    public static class XmlFileFormatWriterTests
    {
        #region Private Methods

        private static string ToString( XmlFileFormatReaderTests.ReaderOutput[] outputs )
        {
            var sb = new StringBuilder();
            using( var writer = XmlFileFormatWriter.From(sb) )
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
        public static void ComplexXmlWriterTests()
        {
            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.ComplexXml_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.ComplexOutputs_Format3));
        }

        [Test]
        public static void SimpleXmlWriterTests()
        {
            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.SimpleXml_ArrayRoot_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SimpleOutput_ArrayRoot_Format3));

            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.SimpleXml_ValueRoot_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SimpleOutput_ValueRoot_Format3));

            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.SimpleXml_EmptyValueRoot_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SimpleOutput_EmptyValueRoot_Format3));

            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.SimpleXml_NullValueRoot_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SimpleOutput_NullValueRoot_Format3));

            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.SimpleXml_NestedObjects_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SimpleOutput_NestedObjects));

            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.SimpleXml_NestedArrays_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SimpleOutput_NestedArrays_Format3));
        }
    }
}
