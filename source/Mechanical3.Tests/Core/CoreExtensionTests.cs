using Mechanical3.Core;
using NUnit.Framework;

namespace Mechanical3.Tests.Core
{
    public class CoreExtensionTests
    {
        [Test]
        public void ObjectExtensionTests()
        {
            var obj = new object();
            Assert.AreEqual(true, obj.NotNullReference());
            Assert.AreEqual(false, obj.NullReference());

            obj = null;
            Assert.AreEqual(false, obj.NotNullReference());
            Assert.AreEqual(true, obj.NullReference());
        }

        [Test]
        public void StringExtensionTests()
        {
            // NullOrEmpty
            Assert.True(((string)null).NullOrEmpty());
            Assert.True(string.Empty.NullOrEmpty());
            Assert.False(" a b ".NullOrEmpty());

            // NullOrLengthy
            Assert.True(((string)null).NullOrLengthy());
            Assert.True(string.Empty.NullOrLengthy());
            Assert.True(" ".NullOrLengthy());
            Assert.True(" a".NullOrLengthy());
            Assert.True("a ".NullOrLengthy());
            Assert.True(" a ".NullOrLengthy());
            Assert.False("a b".NullOrLengthy());

            // NullOrWhiteSpace
            Assert.True(((string)null).NullOrWhiteSpace());
            Assert.True(string.Empty.NullOrWhiteSpace());
            Assert.True(" ".NullOrWhiteSpace());
            Assert.False(" a ".NullOrWhiteSpace());
        }
    }
}
