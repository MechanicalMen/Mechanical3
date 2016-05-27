using System.Linq;
using Mechanical3.DataStores;
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
            var outputWriter = new TestData.FileFormatReaderOutput.Writer();
            using( var writer = new DataStoreTextWriter(outputWriter) )
            {
                writer.WriteObjectStart();

                writer.WriteValue("value_not_empty", "a");
                writer.WriteValue("value_empty", string.Empty);
                writer.WriteValue("value_null", (string)null);
                writer.WriteObjectStart("object_not_empty");
                writer.WriteValue("a", "b");
                writer.WriteEnd();
                writer.WriteObjectStart("object_empty");
                writer.WriteEnd();

                writer.WriteArrayStart("as_array");
                writer.WriteValue("a");
                writer.WriteValue(string.Empty);
                writer.WriteValue((string)null);
                writer.WriteObjectStart();
                writer.WriteValue("a", "b");
                writer.WriteEnd();
                writer.WriteObjectStart();
                writer.WriteEnd();
                writer.WriteEnd();
                writer.WriteArrayStart("array_empty");
                writer.WriteEnd();

                writer.WriteEnd();
            }

            TestData.AssertEquals(
                outputWriter.ToArray(),
                TestData.TextReaderOutput.ComplexOutputs.Select(o => TestData.FileFormatReaderOutput.From(o, nullNameReplacement: null)).ToArray());
        }

        #endregion

        #region Simple tests

        [Test]
        public static void SimpleTextWriterTests()
        {
            var outputWriter = new TestData.FileFormatReaderOutput.Writer();
            using( var writer = new DataStoreTextWriter(outputWriter) )
            {
                writer.WriteArrayStart();
                writer.WriteValue("a");
                writer.WriteValue("b");
                writer.WriteEnd();
            }
            TestData.AssertEquals(
                outputWriter.ToArray(),
                TestData.FileFormatReaderOutput.SimpleOutput_ArrayRoot);


            outputWriter = new TestData.FileFormatReaderOutput.Writer();
            using( var writer = new DataStoreTextWriter(outputWriter) )
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
            TestData.AssertEquals(
                outputWriter.ToArray(),
                TestData.FileFormatReaderOutput.SimpleOutput_NestedObjects);


            outputWriter = new TestData.FileFormatReaderOutput.Writer();
            using( var writer = new DataStoreTextWriter(outputWriter) )
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
            TestData.AssertEquals(
                outputWriter.ToArray(),
                TestData.FileFormatReaderOutput.SimpleOutput_NestedArrays);
        }

        #endregion
    }
}
