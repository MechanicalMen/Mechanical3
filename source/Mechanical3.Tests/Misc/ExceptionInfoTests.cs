using System;
using System.Text;
using Mechanical3.Core;
using Mechanical3.DataStores;
using Mechanical3.DataStores.Xml;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class ExceptionInfoTests
    {
        private class TestException : Exception
        {
            public override string Message
            {
                get { return "test message"; }
            }
        }

        private static Exception ForceStackTrace( Exception ex )
        {
            try
            {
                throw ex;
            }
            catch( Exception e )
            {
                return e;
            }
        }

        [Test]
        public static void ConstructorTests()
        {
            //// NOTE: due to localization, the parts dealing with the Exception and AggregateException messages,
            ////       as well as the stack trace test may fail on non-english language computers.

            // simple exception, no stack trace
            var info = new ExceptionInfo(new TestException());
            Test.OrdinalEquals("Mechanical3.Tests.Misc.ExceptionInfoTests.TestException", info.Type);
            Test.OrdinalEquals("test message", info.Message);
            Assert.True(info.StackTrace.NullOrEmpty());
            Assert.AreEqual(0, info.Data.Length);
            Assert.Null(info.InnerException);
            Assert.AreEqual(0, info.InnerExceptions.Length);
            Test.OrdinalEquals(
                @"Type: Mechanical3.Tests.Misc.ExceptionInfoTests.TestException
Message: test message",
                info.ToString());


            // 1 inner exception, data on both
            info = new ExceptionInfo(
                new Exception(
                    "some exception",
                    new TestException().Store("inner", "a"))
                    .Store("outer", 1));
            Test.OrdinalEquals("Exception", info.Type);
            Assert.AreEqual(1, info.Data.Length);
            var testData = info.Data[0];
            Test.OrdinalEquals("outer", testData.Name);
            Test.OrdinalEquals("1", testData.Value);
            Test.OrdinalEquals("int", testData.ValueType);
            Assert.AreEqual(1, info.InnerExceptions.Length);
            Test.OrdinalEquals("Mechanical3.Tests.Misc.ExceptionInfoTests.TestException", info.InnerExceptions[0].Type);
            testData = info.InnerExceptions[0].Data[0];
            Test.OrdinalEquals("inner", testData.Name);
            Test.OrdinalEquals("a", testData.Value);
            Test.OrdinalEquals("string", testData.ValueType);
            Test.OrdinalEquals(
                @"Type: Exception
Message: some exception
Data:
  outer = 1


InnerException:
  Type: Mechanical3.Tests.Misc.ExceptionInfoTests.TestException
  Message: test message
  Data:
    inner = ""a""",
                info.ToString());


            // aggregate exception, with data on each child
            info = new ExceptionInfo(
                new AggregateException(
                    new TestException().Store("testDataName", "testDataValue1"),
                    new TestException().Store("testDataName", "testDataValue2")));
            Test.OrdinalEquals("AggregateException", info.Type);
            Assert.AreEqual(2, info.InnerExceptions.Length);
            Test.OrdinalEquals("Mechanical3.Tests.Misc.ExceptionInfoTests.TestException", info.InnerExceptions[1].Type);
            Assert.AreEqual(1, info.InnerExceptions[1].Data.Length);
            testData = info.InnerExceptions[1].Data[0];
            Test.OrdinalEquals("testDataName", testData.Name);
            Test.OrdinalEquals("testDataValue2", testData.Value);
            Test.OrdinalEquals("string", testData.ValueType);
            Test.OrdinalEquals(
                @"Type: AggregateException
Message: One or more errors occurred.


InnerExceptions[0]:
  Type: Mechanical3.Tests.Misc.ExceptionInfoTests.TestException
  Message: test message
  Data:
    testDataName = ""testDataValue1""


InnerExceptions[1]:
  Type: Mechanical3.Tests.Misc.ExceptionInfoTests.TestException
  Message: test message
  Data:
    testDataName = ""testDataValue2""",
                info.ToString());


            // simple exception with a stack trace captured
            info = new ExceptionInfo(ForceStackTrace(new Exception().Store("a", 1)));
            Test.OrdinalEquals("Exception", info.Type);
            Test.OrdinalEquals("Exception of type 'System.Exception' was thrown.", info.Message);
            Assert.False(info.StackTrace.NullOrEmpty());
            Assert.True(info.ToString().StartsWith(
                @"Type: Exception
Message: Exception of type 'System.Exception' was thrown.
Data:
  a = 1
StackTrace:
   at Mechanical3.Tests.Misc.ExceptionInfoTests.ForceStackTrace(Exception ex) in ", // NOTE: we cut things short, since the location of the repository will probably change for each user
                StringComparison.Ordinal));
        }

        [Test]
        public static void SerializationTests()
        {
            // null is serializable!
            SaveLoad(null);

            // no data or stack trace, or inner exceptions
            SaveLoad(
                new ExceptionInfo(
                    new TestException()));

            // a little bit of everything
            SaveLoad(
                new ExceptionInfo(
                    new AggregateException( // no data or stack trace, but has inner exceptions
                        new TestException().Store("a", 1).Store("b", string.Empty), // multiple data, no stack trace
                        ForceStackTrace(new TestException()), // stack trace, no data
                        ForceStackTrace(new Exception()).Store("x", (object)null)))); // both stack trace and data
        }

        private static void SaveLoad( ExceptionInfo infoToSave )
        {
            // save
            var sb = new StringBuilder();
            using( var writer = new DataStoreTextWriter(XmlFileFormatFactory.Default.CreateWriter(sb)) )
            {
                writer.WriteArrayStart();

                Assert.Throws<ArgumentNullException>(() => ExceptionInfo.Save(infoToSave, writer: null));
                ExceptionInfo.Save(infoToSave, writer);

                writer.WriteEnd();
            }

            // load
            ExceptionInfo infoLoaded;
            using( var reader = new DataStoreTextReader(XmlFileFormatFactory.Default.CreateReader(sb.ToString())) )
            {
                reader.ReadArrayStart();
                reader.AssertCanRead();

                Assert.Throws<ArgumentNullException>(() => ExceptionInfo.LoadFrom(reader: null));
                infoLoaded = ExceptionInfo.LoadFrom(reader);

                reader.ReadEnd();
            }

            // compare
            AssertEqual(infoToSave, infoLoaded);
        }

        private static void AssertEqual( ExceptionInfo info1, ExceptionInfo info2 )
        {
            if( info1.NullReference() )
            {
                Assert.Null(info2);
            }
            else
            {
                Test.OrdinalEquals(info1.Type, info2.Type);
                Test.OrdinalEquals(info1.Message, info2.Message);

                Assert.AreEqual(info1.Data.Length, info2.Data.Length);
                for( int i = 0; i < info1.Data.Length; ++i )
                    Assert.True(StringStateCollectionTests.Equals(info1.Data[i], info2.Data[i]));

                Assert.AreEqual(info1.InnerExceptions.Length, info2.InnerExceptions.Length);
                for( int i = 0; i < info1.InnerExceptions.Length; ++i )
                    AssertEqual(info1.InnerExceptions[i], info2.InnerExceptions[i]);
            }
        }
    }
}
