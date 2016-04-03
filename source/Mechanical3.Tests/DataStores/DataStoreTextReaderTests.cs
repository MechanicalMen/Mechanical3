using System;
using System.IO;
using Mechanical3.Core;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Xml;
using Mechanical3.IO.FileSystems;
using Mechanical3.Tests.DataStores.Xml;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores
{
    [TestFixture(Category = "DataStores")]
    public static class DataStoreTextReaderTests
    {
        #region ReaderOutput

        internal class ReaderOutput
        {
            internal const string NullValue = "BA3FE4D2-F97C-11E5-B700-32F5E97DB0C8";

            public bool Result { get; }
            public DataStoreToken Token { get; }
            public string _Name { get; }
            public int _Index { get; }
            public bool HasValue { get; }
            public string Value { get; }
            public FilePath Path { get; }

            internal ReaderOutput( bool result, DataStoreToken token, string name, int index, string value, FilePath path )
            {
                this.Result = result;
                this.Token = token;
                this._Name = name;
                this._Index = index;
                this.HasValue = value.NotNullReference();
                this.Value = string.Equals(value, NullValue, StringComparison.Ordinal) ? null : value;
                this.Path = path;
            }

            internal static ReaderOutput Name( DataStoreToken token, string name, FilePath path, string value = null )
            {
                return new ReaderOutput(true, token, name, -1, value, path);
            }

            internal static ReaderOutput Index( DataStoreToken token, int index, FilePath path, string value = null )
            {
                return new ReaderOutput(true, token, null, index, value, path);
            }

            internal static ReaderOutput False( DataStoreToken token = default(DataStoreToken), string name = null, int index = -1, string value = null, FilePath path = null )
            {
                return new ReaderOutput(false, token, name, index, value, path);
            }

            private static void AssertResultEquals( DataStoreTextReader reader, ReaderOutput expectedOutput )
            {
                Assert.NotNull(reader);
                Assert.NotNull(expectedOutput);

                bool actualResult = reader.Read();
                Assert.AreEqual(expectedOutput.Result, actualResult);

                if( expectedOutput.Result )
                    Assert.AreEqual(expectedOutput.Token, reader.Token);
                else
                    Assert.Throws<InvalidOperationException>(() => reader.Token.ToString());

                if( expectedOutput._Name.NullReference() )
                    Assert.Throws<InvalidOperationException>(() => reader.Name.NotNullReference());
                else
                    Test.OrdinalEquals(expectedOutput._Name, reader.Name);

                if( expectedOutput._Index == -1 )
                    Assert.Throws<InvalidOperationException>(() => reader.Index.ToString());
                else
                    Assert.AreEqual(expectedOutput._Index, reader.Index);

                if( !expectedOutput.HasValue )
                    Assert.Throws<InvalidOperationException>(() => reader.Value.NotNullReference());
                else
                    Test.OrdinalEquals(expectedOutput.Value, reader.Value);

                if( expectedOutput.Path.NullReference() )
                    Assert.Throws<InvalidOperationException>(() => reader.Path.NotNullReference());
                else
                    Assert.True(expectedOutput.Path == reader.Path);
            }

            internal static void AssertResultsEqual( DataStoreTextReader reader, ReaderOutput[] expectedOutputs )
            {
                Assert.NotNull(expectedOutputs);

                using( reader )
                {
                    foreach( var output in expectedOutputs )
                        AssertResultEquals(reader, output);
                }
            }
        }

        #endregion

        #region OutputFileFormatReader

        private class OutputFileFormatReader : DisposableObject, IDataStoreTextFileFormatReader
        {
            #region Private Fields

            private XmlFileFormatReaderTests.ReaderOutput[] outputs;
            private int index = 0;

            #endregion

            #region Constructor

            internal OutputFileFormatReader( params XmlFileFormatReaderTests.ReaderOutput[] items )
            {
                if( items.NullReference() )
                    throw new ArgumentNullException(nameof(items)).StoreFileLine();

                foreach( var i in items )
                {
                    if( i.NullReference() )
                        throw new ArgumentException("Null item found!").StoreFileLine();
                }

                this.outputs = items;
            }

            #endregion

            #region IDataStoreTextFileFormatReader

            public bool HasLineInfo()
            {
                return false;
            }

            public int LineNumber
            {
                get { throw new NotSupportedException().StoreFileLine(); }
            }

            public int LinePosition
            {
                get { throw new NotSupportedException().StoreFileLine(); }
            }

            public bool TryReadToken( out DataStoreToken token, out string name, out string value )
            {
                if( index >= outputs.Length )
                {
                    token = default(DataStoreToken);
                    name = null;
                    value = null;
                    return false;
                }
                else
                {
                    var curr = this.outputs[this.index++];
                    token = curr.Token;
                    name = curr.Name;
                    value = curr.Value;
                    return curr.Result;
                }
            }

            #endregion
        }

        #endregion

        #region Complex tests

        private static readonly ReaderOutput[] ComplexOutputs = new ReaderOutput[]
        {
            new ReaderOutput(true, DataStoreToken.ObjectStart, null, -1, null, null),

            ReaderOutput.Name(DataStoreToken.Value, "value_not_empty", FilePath.FromFileName("value_not_empty"), "a"),
            ReaderOutput.Name(DataStoreToken.Value, "value_empty", FilePath.FromFileName("value_empty"), string.Empty),
            ReaderOutput.Name(DataStoreToken.Value, "value_null", FilePath.FromFileName("value_null"), ReaderOutput.NullValue),
            ReaderOutput.Name(DataStoreToken.ObjectStart, "object_not_empty", FilePath.FromDirectoryName("object_not_empty")),
            ReaderOutput.Name(DataStoreToken.Value, "a", FilePath.From("object_not_empty/a"), "b"),
            ReaderOutput.Name(DataStoreToken.End, "object_not_empty", FilePath.FromDirectoryName("object_not_empty")),
            ReaderOutput.Name(DataStoreToken.ObjectStart, "object_empty", FilePath.FromDirectoryName("object_empty")),
            ReaderOutput.Name(DataStoreToken.End, "object_empty", FilePath.FromDirectoryName("object_empty")),

            ReaderOutput.Name(DataStoreToken.ArrayStart, "as_array", FilePath.FromDirectoryName("as_array")),
            ReaderOutput.Index(DataStoreToken.Value, 0, FilePath.From("as_array/0"), "a"),
            ReaderOutput.Index(DataStoreToken.Value, 1, FilePath.From("as_array/1"), string.Empty),
            ReaderOutput.Index(DataStoreToken.Value, 2, FilePath.From("as_array/2"), ReaderOutput.NullValue),
            ReaderOutput.Index(DataStoreToken.ObjectStart, 3, FilePath.From("as_array/3/")),
            ReaderOutput.Name(DataStoreToken.Value, "a", FilePath.From("as_array/3/a"), "b"),
            ReaderOutput.Index(DataStoreToken.End, 3, FilePath.From("as_array/3/")),
            ReaderOutput.Index(DataStoreToken.ObjectStart, 4, FilePath.From("as_array/4/")),
            ReaderOutput.Index(DataStoreToken.End, 4, FilePath.From("as_array/4/")),
            ReaderOutput.Name(DataStoreToken.End, "as_array", FilePath.FromDirectoryName("as_array")),
            ReaderOutput.Name(DataStoreToken.ArrayStart, "array_empty", FilePath.FromDirectoryName("array_empty")),
            ReaderOutput.Name(DataStoreToken.End, "array_empty", FilePath.FromDirectoryName("array_empty")),

            new ReaderOutput(true, DataStoreToken.End, null, -1, null, null),
            ReaderOutput.False(),
            ReaderOutput.False(),
            ReaderOutput.False(),
        };

        [Test]
        public static void ComplexTextReaderTests()
        {
            ReaderOutput.AssertResultsEqual(
                new DataStoreTextReader(
                    XmlFileFormatReader.FromXml(
                        XmlFileFormatReaderTests.ComplexXml_Format3)),
                ComplexOutputs);
        }

        #endregion

        #region Simple Tests

        [Test]
        public static void SimpleTextReaderTests()
        {
            // accessing before the first Read
            using( var reader = new DataStoreTextReader(XmlFileFormatReader.FromXml(XmlFileFormatReaderTests.ComplexXml_Format3)) )
            {
                Assert.Throws<InvalidOperationException>(() => reader.Token.ToString());
                Assert.Throws<InvalidOperationException>(() => reader.Name.NotNullReference());
                Assert.Throws<InvalidOperationException>(() => reader.Index.ToString());
                Assert.Throws<InvalidOperationException>(() => reader.Value.NotNullReference());
                Assert.Throws<InvalidOperationException>(() => reader.Path.NotNullReference());
            }


            // empty data store
            var emptyFileOutputs = new XmlFileFormatReaderTests.ReaderOutput[0];
            using( var reader = new DataStoreTextReader(new OutputFileFormatReader(emptyFileOutputs)) )
            {
                Assert.Throws<FormatException>(() => reader.Read());
            }


            // unexpected end of file
            var eofFileOutputs = new XmlFileFormatReaderTests.ReaderOutput[] {
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.ArrayStart),
            };
            using( var reader = new DataStoreTextReader(new OutputFileFormatReader(eofFileOutputs)) )
            {
                Assert.True(reader.Read());
                Assert.Throws<EndOfStreamException>(() => reader.Read());
            }


            // multiple root nodes
            var multiFileOutputs = new XmlFileFormatReaderTests.ReaderOutput[] {
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.ArrayStart),
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.End),
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.ObjectStart),
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.End),
            };
            using( var reader = new DataStoreTextReader(new OutputFileFormatReader(multiFileOutputs)) )
            {
                Assert.True(reader.Read());
                Assert.True(reader.Read());
                Assert.Throws<FormatException>(() => reader.Read());
            }


            // invalid name
            var nameFileOutputs = new XmlFileFormatReaderTests.ReaderOutput[] {
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.ObjectStart),
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.Value, new string('a', count: 1000), value: "b"),
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.End),
            };
            using( var reader = new DataStoreTextReader(new OutputFileFormatReader(nameFileOutputs)) )
            {
                Assert.True(reader.Read());
                Assert.Throws<FormatException>(() => reader.Read());
            }


            // invalid starting token
            var startFileOutput1 = new XmlFileFormatReaderTests.ReaderOutput[] {
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.Value, "a", "b"),
            };
            using( var reader = new DataStoreTextReader(new OutputFileFormatReader(startFileOutput1)) )
            {
                Assert.Throws<FormatException>(() => reader.Read());
            }

            var startFileOutput2 = new XmlFileFormatReaderTests.ReaderOutput[] {
                XmlFileFormatReaderTests.ReaderOutput.True(DataStoreToken.End),
            };
            using( var reader = new DataStoreTextReader(new OutputFileFormatReader(startFileOutput2)) )
            {
                Assert.Throws<FormatException>(() => reader.Read());
            }
        }

        #endregion
    }
}
