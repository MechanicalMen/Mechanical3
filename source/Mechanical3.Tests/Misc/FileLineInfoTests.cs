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
            var info = FileLineInfo.Current();
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
            // member
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileLineInfo(null, "member", -1));

            // member, file
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileLineInfo("  file ", "member", -1));

            // member, line
            var info = new FileLineInfo(null, " member  ", 0);
            Test.OrdinalEquals("member", info.Member);
            Test.OrdinalEquals("  at member", info.ToString());

            // member, file, line
            info = new FileLineInfo("  file ", "member", 2);
            Test.OrdinalEquals("file", info.File);
            Test.OrdinalEquals("  at member in file:line 2", info.ToString());

            // file
            Assert.Throws<ArgumentException>(() => new FileLineInfo("  file ", null, -1));

            // line
            Assert.Throws<ArgumentException>(() => new FileLineInfo(null, null, 0));

            // file, line
            Assert.Throws<ArgumentException>(() => new FileLineInfo("  file ", null, 0));

            // neither
            Assert.Throws<ArgumentException>(() => new FileLineInfo(null, null, -1));

            // ToString(...)
            Assert.Throws<ArgumentNullException>(() => FileLineInfo.Current().ToString(sb: null));
        }
    }
}
