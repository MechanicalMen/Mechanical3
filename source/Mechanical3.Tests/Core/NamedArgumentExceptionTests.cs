using System;
using Mechanical3.Core;
using NUnit.Framework;

namespace Mechanical3.Tests.Core
{
    [TestFixture(Category = "Core")]
    public static class NamedArgumentExceptionTests
    {
        [Test]
        public static void FromParameterTests()
        {
            var ex = NamedArgumentException.FromParameter("a");
            Assert.NotNull(ex);
            Assert.IsInstanceOf<ArgumentException>(ex);
            Test.OrdinalEquals(@"Invalid parameter: ""a""!", ex.Message);
            Assert.AreEqual(0, ex.Data.Count); // no data stored, only the message is different
            Assert.Null(ex.InnerException);

            // null parameter name
            ex = NamedArgumentException.FromParameter(paramName: null);
            Assert.NotNull(ex);
            Assert.IsInstanceOf<ArgumentException>(ex);
            Test.OrdinalEquals(@"Invalid parameter: """"!", ex.Message);

            // inner exception
            ex = NamedArgumentException.FromParameter("i", new OverflowException());
            Assert.NotNull(ex);
            Assert.IsInstanceOf<ArgumentException>(ex);
            Test.OrdinalEquals(@"Invalid parameter: ""i""!", ex.Message);
            Assert.NotNull(ex.InnerException);
            Assert.IsInstanceOf<OverflowException>(ex.InnerException);
        }
    }
}
