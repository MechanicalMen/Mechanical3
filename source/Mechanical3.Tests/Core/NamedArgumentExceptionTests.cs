using System;
using Mechanical3.Core;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Core
{
    [TestFixture(Category = "Core")]
    public static class NamedArgumentExceptionTests
    {
        [Test]
        public static void FromTests()
        {
            var ex = NamedArgumentException.From("a");
            Assert.NotNull(ex);
            Assert.IsInstanceOf<ArgumentException>(ex);
            Test.OrdinalEquals(@"Invalid parameter: ""a""!", ex.Message);
            Assert.AreEqual(0, ex.Data.Count); // no data stored, only the message is different
            Assert.Null(ex.InnerException);

            // null parameter name
            ex = NamedArgumentException.From(paramName: null);
            Assert.NotNull(ex);
            Assert.IsInstanceOf<ArgumentException>(ex);
            Test.OrdinalEquals(@"Invalid parameter: """"!", ex.Message);

            // inner exception
            ex = NamedArgumentException.From("i", new OverflowException());
            Assert.NotNull(ex);
            Assert.IsInstanceOf<ArgumentException>(ex);
            Test.OrdinalEquals(@"Invalid parameter: ""i""!", ex.Message);
            Assert.NotNull(ex.InnerException);
            Assert.IsInstanceOf<OverflowException>(ex.InnerException);
        }

        [Test]
        public static void StoreTests()
        {
            var srcPos = FileLineInfo.Current();

            var ex = NamedArgumentException.Store("a", "b", (Exception)null, srcPos.File, srcPos.Member, srcPos.Line);
            Assert.NotNull(ex);
            Assert.IsInstanceOf<ArgumentException>(ex);
            Test.OrdinalEquals(@"Invalid parameter: ""a""!", ex.Message);
            var data = ex.GetStoredData();
            var storedParam = data.FirstOrNullable(s => string.Equals(s.Name, "a", StringComparison.Ordinal)).Value;
            Test.OrdinalEquals(storedParam.Value, "b");
            Test.OrdinalEquals(storedParam.ValueType, "string");
            Test.OrdinalEquals(srcPos.ToString(), data.GetPartialStackTrace());

            // null, empty or lengthy parameter name
            Assert.Throws<ArgumentException>(() => NamedArgumentException.Store(null, 5));
            Assert.Throws<ArgumentException>(() => NamedArgumentException.Store(string.Empty, 5));
            Assert.Throws<ArgumentException>(() => NamedArgumentException.Store(" ", 5));
            Assert.Throws<ArgumentException>(() => NamedArgumentException.Store("x ", 5));
            Assert.Throws<ArgumentException>(() => NamedArgumentException.Store(" x", 5));

            // inner exception
            ex = NamedArgumentException.Store("i", 5, new OverflowException());
            Assert.NotNull(ex);
            Assert.IsInstanceOf<ArgumentException>(ex);
            Test.OrdinalEquals(@"Invalid parameter: ""i""!", ex.Message);
            Assert.NotNull(ex.InnerException);
            Assert.IsInstanceOf<OverflowException>(ex.InnerException);
        }
    }
}
