using System;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    public static class FileLineInfoTests
    {
        [Test]
        public static void DefaultParameterTest()
        {
            var info = FileLineInfo.Create();
            Assert.True(string.Equals("FileLineInfoTests.cs", info.File, StringComparison.Ordinal));
            Assert.True(string.Equals("DefaultParameterTest", info.Member, StringComparison.Ordinal));
            Assert.AreEqual(12, info.Line);
            Assert.True(string.Equals("   at DefaultParameterTest in FileLineInfoTests.cs:line 12", info.ToString(), StringComparison.Ordinal));
        }

        [Test]
        public static void BadParameterTest()
        {
            Assert.Throws<ArgumentException>(() => new FileLineInfo(null, "a", 1));
            Assert.Throws<ArgumentException>(() => new FileLineInfo("a", null, 1));
            Assert.Throws<ArgumentException>(() => new FileLineInfo("a", "b", -1));
            var info = new FileLineInfo(string.Empty, string.Empty, 0); // works

            Assert.True(string.Equals("   at ? in ?:line 0", info.ToString(), StringComparison.Ordinal));
        }
    }
}
