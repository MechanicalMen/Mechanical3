using System;
using System.Collections.Generic;
using Mechanical3.Core;
using Mechanical3.DataStores;
using Mechanical3.IO.FileSystems;
using NUnit.Framework;

namespace Mechanical3.Tests.DataStores
{
    public static class TestData
    {
        #region TextReaderOutput

        public partial class TextReaderOutput
        {
            public bool Result { get; }
            public DataStoreToken Token { get; }
            public string _Name { get; }
            public int _Index { get; }
            public string Value { get; }
            public FilePath Path { get; }

            private TextReaderOutput( bool result, DataStoreToken token, string name, int index, string value, FilePath path )
            {
                this.Result = result;
                this.Token = token;
                this._Name = name;
                this._Index = index;
                this.Value = value;
                this.Path = path;
            }

            public static TextReaderOutput StartOrEnd( DataStoreToken token )
            {
                if( token == DataStoreToken.Value )
                    throw new ArgumentException();

                return new TextReaderOutput(
                    result: true,
                    token: token,
                    name: null,
                    index: -1,
                    value: null,
                    path: null);
            }

            public static TextReaderOutput True( DataStoreToken token, string name, FilePath path, string value = null )
            {
                if( !Enum.IsDefined(typeof(DataStoreToken), token)
                 || !DataStore.IsValidName(name)
                 || path.NullReference()
                 || (token != DataStoreToken.Value && value.NotNullReference()) )
                    throw new ArgumentException();

                return new TextReaderOutput(true, token, name, -1, value, path);
            }

            public static TextReaderOutput True( DataStoreToken token, int index, FilePath path, string value = null )
            {
                if( !Enum.IsDefined(typeof(DataStoreToken), token)
                 || index < 0
                 || path.NullReference()
                 || (token != DataStoreToken.Value && value.NotNullReference()) )
                    throw new ArgumentException();

                return new TextReaderOutput(true, token, null, index, value, path);
            }

            public static TextReaderOutput False()
            {
                return new TextReaderOutput(
                    result: false,
                    token: default(DataStoreToken),
                    name: null,
                    index: -1,
                    value: null,
                    path: null);
            }
        }

        #endregion

        #region FileFormatReaderOutput

        public partial class FileFormatReaderOutput
        {
            public bool Result { get; }
            public DataStoreToken Token { get; }
            public string Name { get; }
            public string Value { get; }

            private FileFormatReaderOutput( bool result, DataStoreToken token, string name, string value )
            {
                this.Result = result;
                this.Token = token;
                this.Name = name;
                this.Value = value;
            }

            public static FileFormatReaderOutput StartOrEnd( DataStoreToken token )
            {
                if( token == DataStoreToken.Value )
                    throw new ArgumentException();

                return new FileFormatReaderOutput(
                    result: true,
                    token: token,
                    name: null,
                    value: null);
            }

            public static FileFormatReaderOutput True( DataStoreToken token, string name = null, string value = null )
            {
                return new FileFormatReaderOutput(true, token, name, value);
            }

            public static FileFormatReaderOutput False()
            {
                return new FileFormatReaderOutput(false, default(DataStoreToken), null, null);
            }

            public static FileFormatReaderOutput From( TextReaderOutput output, string nullNameReplacement )
            {
                if( output.Result )
                    return new FileFormatReaderOutput(output.Result, output.Token, output._Name ?? nullNameReplacement, output.Value);
                else
                    return new FileFormatReaderOutput(output.Result, output.Token, output._Name, output.Value);
            }

            public static FileFormatReaderOutput From( FileFormatReaderOutput output, string nullNameReplacement )
            {
                if( output.Result )
                    return new FileFormatReaderOutput(output.Result, output.Token, output.Name ?? nullNameReplacement, output.Value);
                else
                    return output;
            }
        }

        #endregion

        #region FileFormatReaderOutput.Reader

        public partial class FileFormatReaderOutput
        {
            public class Reader : DisposableObject, IDataStoreTextFileFormatReader
            {
                #region Private Fields

                private FileFormatReaderOutput[] outputs;
                private int index = 0;

                #endregion

                #region Constructor

                public Reader( params FileFormatReaderOutput[] items )
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
        }

        #endregion

        #region FileFormatReaderOutput.Writer

        public partial class FileFormatReaderOutput
        {
            public class Writer : DisposableObject, IDataStoreTextFileFormatWriter
            {
                #region Private Fields

                private readonly List<FileFormatReaderOutput> outputs;

                #endregion

                #region Constructor

                public Writer()
                {
                    this.outputs = new List<FileFormatReaderOutput>();
                }

                #endregion

                #region IDisposableObject

                /// <summary>
                /// Called when the object is being disposed of. Inheritors must call base.OnDispose to be properly disposed.
                /// </summary>
                /// <param name="disposing">If set to <c>true</c>, release both managed and unmanaged resources; otherwise release only the unmanaged resources.</param>
                protected override void OnDispose( bool disposing )
                {
                    if( disposing )
                    {
                        //// dispose-only (i.e. non-finalizable) logic
                        //// (managed, disposable resources you own)

                        this.outputs.Add(FileFormatReaderOutput.False());
                    }

                    //// shared cleanup logic
                    //// (unmanaged resources)


                    base.OnDispose(disposing);
                }

                #endregion

                #region IDataStoreTextFileFormatWriter

                public void WriteToken( DataStoreToken token, string name, string value, Type valueType )
                {
                    this.ThrowIfDisposed();

                    this.outputs.Add(new FileFormatReaderOutput(true, token, name, value));
                }

                #endregion

                public FileFormatReaderOutput[] ToArray()
                {
                    // can be invoked, even after being disposed
                    return this.outputs.ToArray();
                }
            }
        }

        #endregion

        #region AssertEquals

        public static void AssertEquals( DataStoreTextReader reader, TextReaderOutput expectedOutput )
        {
            Assert.NotNull(reader);
            Assert.NotNull(expectedOutput);

            bool actualResult = reader.Read();
            Assert.AreEqual(expectedOutput.Result, actualResult);

            if( expectedOutput.Result )
            {
                Assert.AreEqual(expectedOutput.Token, reader.Token);

                if( expectedOutput._Name.NullReference() )
                    Assert.Throws<InvalidOperationException>(() => reader.Name.NotNullReference());
                else
                    Test.OrdinalEquals(expectedOutput._Name, reader.Name);

                if( expectedOutput._Index == -1 )
                    Assert.Throws<InvalidOperationException>(() => reader.Index.ToString());
                else
                    Assert.AreEqual(expectedOutput._Index, reader.Index);

                if( expectedOutput.Token != DataStoreToken.Value )
                    Assert.Throws<InvalidOperationException>(() => reader.Value.NotNullReference());
                else
                    Test.OrdinalEquals(expectedOutput.Value, reader.Value);

                if( expectedOutput.Path.NullReference() )
                    Assert.Throws<InvalidOperationException>(() => reader.Path.NotNullReference());
                else
                    Assert.True(expectedOutput.Path == reader.Path);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => reader.Token.ToString());
                Assert.Throws<InvalidOperationException>(() => reader.Name.NotNullReference());
                Assert.Throws<InvalidOperationException>(() => reader.Index.ToString());
                Assert.Throws<InvalidOperationException>(() => reader.Value.NotNullReference());
                Assert.Throws<InvalidOperationException>(() => reader.Path.NotNullReference());
            }
        }

        public static void AssertEquals( DataStoreTextReader reader, TextReaderOutput[] expectedOutputs )
        {
            Assert.NotNull(expectedOutputs);

            using( reader )
            {
                foreach( var output in expectedOutputs )
                    AssertEquals(reader, output);
            }
        }


        public static void AssertEquals( IDataStoreTextFileFormatReader reader, FileFormatReaderOutput expectedOutput )
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

        public static void AssertEquals( IDataStoreTextFileFormatReader reader, FileFormatReaderOutput[] expectedOutputs )
        {
            Assert.NotNull(expectedOutputs);

            using( reader )
            {
                foreach( var output in expectedOutputs )
                    AssertEquals(reader, output);
            }
        }

        public static void AssertEquals( FileFormatReaderOutput o1, FileFormatReaderOutput o2 )
        {
            Assert.NotNull(o1);
            Assert.NotNull(o2);

            Assert.AreEqual(o1.Result, o2.Result);
            Assert.AreEqual(o1.Token, o2.Token);
            Test.OrdinalEquals(o1.Name, o2.Name);
            Test.OrdinalEquals(o1.Value, o2.Value);
        }

        public static void AssertEquals( FileFormatReaderOutput[] actualOutputs, FileFormatReaderOutput[] expectedOutputs )
        {
            Assert.NotNull(actualOutputs);
            Assert.NotNull(expectedOutputs);

            int count = Math.Min(actualOutputs.Length, expectedOutputs.Length);
            for( int i = 0; i < count; ++i )
                AssertEquals(actualOutputs[i], expectedOutputs[i]);

            if( actualOutputs.Length != expectedOutputs.Length )
            {
                Assert.False(actualOutputs[count - 1].Result);
                Assert.False(expectedOutputs[count - 1].Result);
                if( actualOutputs.Length > expectedOutputs.Length )
                {
                    for( int i = count; i < actualOutputs.Length; ++i )
                        Assert.False(actualOutputs[i].Result);
                }
                else
                {
                    for( int i = count; i < expectedOutputs.Length; ++i )
                        Assert.False(expectedOutputs[i].Result);
                }
            }
        }

        #endregion

        #region Complex Data

        public partial class TextReaderOutput
        {
            public static readonly TextReaderOutput[] ComplexOutputs = new TextReaderOutput[]
            {
                TextReaderOutput.StartOrEnd(DataStoreToken.ObjectStart),

                TextReaderOutput.True(DataStoreToken.Value, "value_not_empty", FilePath.FromFileName("value_not_empty"), "a"),
                TextReaderOutput.True(DataStoreToken.Value, "value_empty", FilePath.FromFileName("value_empty"), string.Empty),
                TextReaderOutput.True(DataStoreToken.Value, "value_null", FilePath.FromFileName("value_null"), null),
                TextReaderOutput.True(DataStoreToken.ObjectStart, "object_not_empty", FilePath.FromDirectoryName("object_not_empty")),
                TextReaderOutput.True(DataStoreToken.Value, "a", FilePath.From("object_not_empty/a"), "b"),
                TextReaderOutput.True(DataStoreToken.End, "object_not_empty", FilePath.FromDirectoryName("object_not_empty")),
                TextReaderOutput.True(DataStoreToken.ObjectStart, "object_empty", FilePath.FromDirectoryName("object_empty")),
                TextReaderOutput.True(DataStoreToken.End, "object_empty", FilePath.FromDirectoryName("object_empty")),

                TextReaderOutput.True(DataStoreToken.ArrayStart, "as_array", FilePath.FromDirectoryName("as_array")),
                TextReaderOutput.True(DataStoreToken.Value, 0, FilePath.From("as_array/0"), "a"),
                TextReaderOutput.True(DataStoreToken.Value, 1, FilePath.From("as_array/1"), string.Empty),
                TextReaderOutput.True(DataStoreToken.Value, 2, FilePath.From("as_array/2"), null),
                TextReaderOutput.True(DataStoreToken.ObjectStart, 3, FilePath.From("as_array/3/")),
                TextReaderOutput.True(DataStoreToken.Value, "a", FilePath.From("as_array/3/a"), "b"),
                TextReaderOutput.True(DataStoreToken.End, 3, FilePath.From("as_array/3/")),
                TextReaderOutput.True(DataStoreToken.ObjectStart, 4, FilePath.From("as_array/4/")),
                TextReaderOutput.True(DataStoreToken.End, 4, FilePath.From("as_array/4/")),
                TextReaderOutput.True(DataStoreToken.End, "as_array", FilePath.FromDirectoryName("as_array")),
                TextReaderOutput.True(DataStoreToken.ArrayStart, "array_empty", FilePath.FromDirectoryName("array_empty")),
                TextReaderOutput.True(DataStoreToken.End, "array_empty", FilePath.FromDirectoryName("array_empty")),

                TextReaderOutput.StartOrEnd(DataStoreToken.End),
                TextReaderOutput.False(),
                TextReaderOutput.False(),
                TextReaderOutput.False(),
            };
        }

        #endregion

        #region Simple Data Sets

        public partial class FileFormatReaderOutput
        {
            public static readonly FileFormatReaderOutput[] SimpleOutput_ArrayRoot = new FileFormatReaderOutput[]
            {
                StartOrEnd(DataStoreToken.ArrayStart),
                True(DataStoreToken.Value, value: "a"),
                True(DataStoreToken.Value, value: "b"),
                StartOrEnd(DataStoreToken.End),
                False(),
            };

            public static readonly FileFormatReaderOutput[] SimpleOutput_NestedObjects = new FileFormatReaderOutput[]
            {
                StartOrEnd(DataStoreToken.ObjectStart),
                True(DataStoreToken.ObjectStart, "a"),
                True(DataStoreToken.End, "a"),
                True(DataStoreToken.ObjectStart, "b"),
                True(DataStoreToken.ObjectStart, "b"),
                True(DataStoreToken.ObjectStart, "b"),
                True(DataStoreToken.End, "b"),
                True(DataStoreToken.End, "b"),
                True(DataStoreToken.End, "b"),
                True(DataStoreToken.ObjectStart, "c"),
                True(DataStoreToken.End, "c"),
                StartOrEnd(DataStoreToken.End),
                False(),
            };

            public static readonly FileFormatReaderOutput[] SimpleOutput_NestedArrays = new FileFormatReaderOutput[]
            {
                StartOrEnd(DataStoreToken.ArrayStart),
                True(DataStoreToken.ArrayStart),
                True(DataStoreToken.End),
                True(DataStoreToken.ArrayStart),
                True(DataStoreToken.ArrayStart),
                True(DataStoreToken.ArrayStart),
                True(DataStoreToken.End),
                True(DataStoreToken.End),
                True(DataStoreToken.End),
                True(DataStoreToken.ArrayStart),
                True(DataStoreToken.End),
                StartOrEnd(DataStoreToken.End),
                False(),
            };
        }

        #endregion
    }
}
