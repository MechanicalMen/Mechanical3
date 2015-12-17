using System;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    public static class FileLineInfoTests
    {
        private static void CheckParse( FileLineInfo info )
        {
            var parsed = FileLineInfo.Parse(info.ToString());
            Test.OrdinalEquals(info.File, parsed.File);
            Test.OrdinalEquals(info.Member, parsed.Member);
            Assert.AreEqual(info.Line, parsed.Line);
        }

        [Test]
        public static void DefaultParameterTest()
        {
            var info = FileLineInfo.Create();
            Test.OrdinalEquals("FileLineInfoTests.cs", info.File);
            Test.OrdinalEquals("DefaultParameterTest", info.Member);
            Assert.AreEqual(20, info.Line);
            Test.OrdinalEquals("   at DefaultParameterTest in FileLineInfoTests.cs:line 20", info.ToStackTraceLine());
            Test.OrdinalEquals("DefaultParameterTest;20;FileLineInfoTests.cs", info.ToString());
            CheckParse(info);
        }

        [Test]
        public static void BadParameterTest()
        {
            Assert.Throws<ArgumentException>(() => new FileLineInfo(null, "a", 1));
            Assert.Throws<ArgumentException>(() => new FileLineInfo("a", null, 1));
            Assert.Throws<ArgumentException>(() => new FileLineInfo("a", "b", -1));
            var info = new FileLineInfo(string.Empty, string.Empty, 0); // works

            Test.OrdinalEquals("   at ? in ?:line 0", info.ToStackTraceLine());
            Test.OrdinalEquals(";0;", info.ToString());
            CheckParse(info);

            info = default(FileLineInfo);
            Test.OrdinalEquals("   at ? in ?:line 0", info.ToStackTraceLine());
            Test.OrdinalEquals(";0;", info.ToString());
        }
    }
}
