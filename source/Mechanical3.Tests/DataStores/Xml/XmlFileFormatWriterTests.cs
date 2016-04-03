using System.IO;
using System.Text;
using Mechanical3.Core;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Xml;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores.Xml
{
    [TestFixture(Category = "DataStores")]
    public static class XmlFileFormatWriterTests
    {
        #region Private Methods

        private static string ReplaceLineTerminators( string input, string newLine )
        {
            var sb = new StringBuilder();
            using( var reader = new StringReader(input) )
            {
                string line;
                while( (line = reader.ReadLine()).NotNullReference() )
                {
                    if( sb.Length != 0 )
                        sb.Append(newLine);

                    sb.Append(line);
                }
            }
            return sb.ToString();
        }

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
                ReplaceLineTerminators(XmlFileFormatReaderTests.ComplexXml_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.ComplexOutputs_Format3));
        }

        [Test]
        public static void SimpleXmlWriterTests()
        {
            Test.OrdinalEquals(
                ReplaceLineTerminators(XmlFileFormatReaderTests.SmallXml_ArrayRoot_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SmallOutput_ArrayRoot_Format3));

            Test.OrdinalEquals(
                ReplaceLineTerminators(XmlFileFormatReaderTests.SmallXml_ValueRoot_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SmallOutput_ValueRoot_Format3));

            Test.OrdinalEquals(
                ReplaceLineTerminators(XmlFileFormatReaderTests.SmallXml_EmptyValueRoot_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SmallOutput_EmptyValueRoot_Format3));

            Test.OrdinalEquals(
                ReplaceLineTerminators(XmlFileFormatReaderTests.SmallXml_NullValueRoot_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SmallOutput_NullValueRoot_Format3));

            Test.OrdinalEquals(
                ReplaceLineTerminators(XmlFileFormatReaderTests.SmallXml_NestedObjects_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SmallOutput_NestedObjects));

            Test.OrdinalEquals(
                ReplaceLineTerminators(XmlFileFormatReaderTests.SmallXml_NestedArrays_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                ToString(XmlFileFormatReaderTests.SmallOutput_NestedArrays_Format3));
        }
    }
}
