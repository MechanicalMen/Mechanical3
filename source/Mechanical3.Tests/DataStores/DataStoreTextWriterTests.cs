using System.Text;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Xml;
using Mechanical3.Tests.DataStores.Xml;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores
{
    [TestFixture(Category = "DataStores")]
    public static class DataStoreTextWriterTests
    {
        #region Complex tests

        [Test]
        public static void ComplexTextWriterTests()
        {
            var sb = new StringBuilder();
            using( var writer = new DataStoreTextWriter(XmlFileFormatWriter.From(sb)) )
            {
                writer.WriteObjectStart();

                writer.Write("value_not_empty", "a");
                writer.Write("value_empty", string.Empty);
                writer.Write("value_null", (string)null);
                writer.WriteObjectStart("object_not_empty");
                writer.Write("a", "b");
                writer.WriteEnd();
                writer.WriteObjectStart("object_empty");
                writer.WriteEnd();

                writer.WriteArrayStart("as_array");
                writer.Write("a");
                writer.Write(string.Empty);
                writer.Write((string)null);
                writer.WriteObjectStart();
                writer.Write("a", "b");
                writer.WriteEnd();
                writer.WriteObjectStart();
                writer.WriteEnd();
                writer.WriteEnd();
                writer.WriteArrayStart("array_empty");
                writer.WriteEnd();

                writer.WriteEnd();
            }

            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.ComplexXml_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                sb.ToString());
        }

        #endregion

        #region Simple tests

        [Test]
        public static void SimpleTextWriterTests()
        {
            var sb = new StringBuilder();
            using( var writer = new DataStoreTextWriter(XmlFileFormatWriter.From(sb)) )
            {
                writer.WriteArrayStart();
                writer.Write("a");
                writer.Write("b");
                writer.WriteEnd();
            }
            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.SimpleXml_ArrayRoot_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                sb.ToString());


            sb.Clear();
            using( var writer = new DataStoreTextWriter(XmlFileFormatWriter.From(sb)) )
            {
                writer.WriteObjectStart();
                writer.WriteObjectStart("a");
                writer.WriteEnd();
                writer.WriteObjectStart("b");
                writer.WriteObjectStart("b");
                writer.WriteObjectStart("b");
                writer.WriteEnd();
                writer.WriteEnd();
                writer.WriteEnd();
                writer.WriteObjectStart("c");
                writer.WriteEnd();
                writer.WriteEnd();
            }
            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.SimpleXml_NestedObjects_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                sb.ToString());


            sb.Clear();
            using( var writer = new DataStoreTextWriter(XmlFileFormatWriter.From(sb)) )
            {
                writer.WriteArrayStart();
                writer.WriteArrayStart();
                writer.WriteEnd();
                writer.WriteArrayStart();
                writer.WriteArrayStart();
                writer.WriteArrayStart();
                writer.WriteEnd();
                writer.WriteEnd();
                writer.WriteEnd();
                writer.WriteArrayStart();
                writer.WriteEnd();
                writer.WriteEnd();
            }
            Test.OrdinalEquals(
                Test.ReplaceLineTerminators(XmlFileFormatReaderTests.SimpleXml_NestedArrays_Format3, DataStoreFileFormatWriterOptions.Default.NewLine),
                sb.ToString());
        }

        #endregion
    }
}
