using System;
using Mechanical3.Misc;
using Mechanical3.Core;
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

        private static Exception CreateStackTraceException()
        {
            try
            {
                throw new AssertionException("stack trace exception");
            }
            catch( Exception exception )
            {
                return exception;
            }
        }

        [Test]
        public static void ExceptionInfoTest()
        {
            var info = new ExceptionInfo(new TestException());
            Test.OrdinalEquals("Mechanical3.Tests.Misc.ExceptionInfoTests.TestException", info.Type);
            Test.OrdinalEquals("test message", info.Message);
            Assert.Null(info.StackTrace);
            Assert.AreEqual(0, info.Data.Length);
            Assert.Null(info.InnerException);
            Assert.AreEqual(0, info.InnerExceptions.Length);
            Test.OrdinalEquals(
                @"Type: Mechanical3.Tests.Misc.ExceptionInfoTests.TestException
Message: test message",
                info.ToString());


            info = new ExceptionInfo(
                new AggregateException(
                    CreateStackTraceException(),
                    new TestException().Store("testDataName", "testDataValue")));
            Test.OrdinalEquals("AggregateException", info.Type);
            Assert.AreEqual(2, info.InnerExceptions.Length);
            Test.OrdinalEquals("NUnit.Framework.AssertionException", info.InnerExceptions[0].Type);
            Assert.NotNull(info.InnerExceptions[0].StackTrace);
            Test.OrdinalEquals("Mechanical3.Tests.Misc.ExceptionInfoTests.TestException", info.InnerExceptions[1].Type);
            Assert.AreEqual(1, info.InnerExceptions[1].Data.Length);
            var testData = info.InnerExceptions[1].Data[0];
            Test.OrdinalEquals("testDataName", testData.Name);
            Test.OrdinalEquals("testDataValue", testData.Value);
            Test.OrdinalEquals("string", testData.ValueType);
            Test.OrdinalEquals(
                @"Type: AggregateException
Message: One or more errors occurred.


InnerExceptions[0]:
  Type: NUnit.Framework.AssertionException
  Message: stack trace exception
  StackTrace:
   at Mechanical3.Tests.Misc.ExceptionInfoTests.CreateStackTraceException() in ExceptionInfoTests.cs:line 23


InnerExceptions[1]:
  Type: Mechanical3.Tests.Misc.ExceptionInfoTests.TestException
  Message: test message
  Data:
    testDataName = testDataValue",
                info.ToString());
        }
    }
}
