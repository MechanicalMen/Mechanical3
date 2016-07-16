using System;
using System.Text;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Xml;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class StringStateTests
    {
        [Test]
        public static void ConstructorTests()
        {
            // bad constructor parameters
            Assert.Throws<ArgumentException>(() => new StringState(name: null, value: "y", valueType: "Z"));
            Assert.Throws<ArgumentException>(() => new StringState(name: string.Empty, value: "y", valueType: "z"));
            Assert.Throws<ArgumentException>(() => new StringState(name: " x", value: "y", valueType: "z"));
            Assert.Throws<ArgumentException>(() => new StringState(name: "x ", value: "y", valueType: "z"));

            Assert.Throws<ArgumentException>(() => new StringState(name: "x", value: "y", valueType: string.Empty));
            Assert.Throws<ArgumentException>(() => new StringState(name: "x", value: "y", valueType: " z"));
            Assert.Throws<ArgumentException>(() => new StringState(name: "x", value: "y", valueType: "z "));

            // valid parameters
            var state = new StringState(name: "x", value: "y", valueType: "z");
            Test.OrdinalEquals("x", state.Name);
            Test.OrdinalEquals("y", state.Value);
            Test.OrdinalEquals("y", state.DisplayValue);
            Test.OrdinalEquals("z", state.ValueType);
            Test.OrdinalEquals("x=y", state.ToString());

            state = StringState.From("test2", 3.14);
            Test.OrdinalEquals("test2", state.Name);
            Test.OrdinalEquals("3.14d", state.Value);
            Test.OrdinalEquals("3.14d", state.DisplayValue);
            Test.OrdinalEquals("double", state.ValueType);
            Test.OrdinalEquals("test2=3.14d", state.ToString());

            state = StringState.From("test3", (NUnit.Framework.TestAttribute)null);
            Test.OrdinalEquals("test3", state.Name);
            Test.OrdinalEquals(null, state.Value);
            Test.OrdinalEquals("null", state.DisplayValue);
            Test.OrdinalEquals("NUnit.Framework.TestAttribute", state.ValueType);
            Test.OrdinalEquals("test3=null", state.ToString());

            state = StringState.From("test4", "value");
            Test.OrdinalEquals("test4", state.Name);
            Test.OrdinalEquals("value", state.Value);
            Test.OrdinalEquals(@"""value""", state.DisplayValue);
            Test.OrdinalEquals("string", state.ValueType);
            Test.OrdinalEquals(@"test4=""value""", state.ToString());
        }

        [Test]
        public static void SerializationTests()
        {
            var stateWritten = StringState.From("a", 5);

            // save
            var sb = new StringBuilder();
            using( var writer = new DataStoreTextWriter(XmlFileFormatFactory.Default.CreateWriter(sb)) )
            {
                writer.WriteArrayStart();

                Assert.Throws<ArgumentNullException>(() => stateWritten.SaveTo(null));
                Assert.Throws<ArgumentException>(() => default(StringState).SaveTo(writer));
                stateWritten.SaveTo(writer);

                writer.WriteEnd();
            }

            // load
            StringState stateRead;
            using( var reader = new DataStoreTextReader(XmlFileFormatFactory.Default.CreateReader(sb.ToString())) )
            {
                reader.ReadArrayStart();
                reader.AssertCanRead();

                Assert.Throws<ArgumentNullException>(() => StringState.LoadFrom(null));
                stateRead = StringState.LoadFrom(reader);

                reader.ReadEnd();
            }

            // compare
            Test.OrdinalEquals(stateWritten.Name, stateRead.Name);
            Test.OrdinalEquals(stateWritten.Value, stateRead.Value);
            Test.OrdinalEquals(stateWritten.ValueType, stateRead.ValueType);
        }
    }
}
