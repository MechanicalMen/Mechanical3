using System;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class FileLineInfoTests
    {
        [Test]
        public static void DefaultParameterTest()
        {
            var info = FileLineInfo.Create();
            Test.OrdinalEquals("FileLineInfoTests.cs", info.File);
            Test.OrdinalEquals("DefaultParameterTest", info.Member);
            Assert.AreEqual(13, info.Line);
            Test.OrdinalEquals("  at DefaultParameterTest in FileLineInfoTests.cs:line 13", info.ToString());
        }

        [Test]
        public static void FilePathTrimmingTest()
        {
            var info = new FileLineInfo(@"a\b.cs", "member", 0);
            Test.OrdinalEquals("b.cs", info.File);

            info = new FileLineInfo(@"a:\b\c.cs", "member", 0);
            Test.OrdinalEquals("c.cs", info.File);


            info = new FileLineInfo(@"a/b.cs", "member", 0);
            Test.OrdinalEquals("b.cs", info.File);

            info = new FileLineInfo(@"/a/b.cs", "member", 0);
            Test.OrdinalEquals("b.cs", info.File);
        }

        [Test]
        public static void BadOrMissingParameterTest()
        {
            Assert.Throws<ArgumentException>(() => new FileLineInfo(null, null, null)); // no member
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileLineInfo("a", "b", -1)); // bad line number
            Assert.Throws<ArgumentNullException>(() => FileLineInfo.Create().ToString(sb: null)); // null StringBuilder

            // member only: round-trip
            var info = new FileLineInfo(null, " member  ", null);
            Test.OrdinalEquals("member", info.Member);
            Test.OrdinalEquals("  at member", info.ToString());

            // member + line: line number lost when converting to string
            info = new FileLineInfo(null, " member  ", 3);
            Assert.AreEqual(3, info.Line.Value);
            Test.OrdinalEquals("  at member", info.ToString());

            // member + file: round-trip
            info = new FileLineInfo("  file ", "member", null);
            Test.OrdinalEquals("file", info.File);
            Test.OrdinalEquals("  at member in file", info.ToString());

            // member + file + line: round-trip
            info = new FileLineInfo("  file ", "member", 2);
            Test.OrdinalEquals("file", info.File);
            Test.OrdinalEquals("  at member in file:line 2", info.ToString());
        }
    }
}
