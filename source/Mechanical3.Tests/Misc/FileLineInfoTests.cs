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
            Test.OrdinalEquals("   at DefaultParameterTest in FileLineInfoTests.cs:line 13", info.ToString());
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
            Action<FileLineInfo, string> roundTripTest = ( fileLineInfo, expectedString ) =>
            {
                Test.OrdinalEquals(expectedString, fileLineInfo.ToString());

                var parsedInfo = StackTraceInfo.From(fileLineInfo.ToString()).Frames[0];
                Test.OrdinalEquals(fileLineInfo.File, parsedInfo.File);
                Test.OrdinalEquals(fileLineInfo.Member, parsedInfo.Member);
                Assert.AreEqual(fileLineInfo.Line.HasValue, parsedInfo.Line.HasValue);
                if( fileLineInfo.Line.HasValue )
                    Assert.AreEqual(fileLineInfo.Line.Value, parsedInfo.Line.Value);
            };

            Assert.Throws<ArgumentException>(() => new FileLineInfo(null, null, null)); // no member
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileLineInfo("a", "b", -1)); // bad line number

            // member only: round-trip
            var info = new FileLineInfo(null, " member  ", null);
            Test.OrdinalEquals("member", info.Member);
            roundTripTest(info, "   at member");

            // member + line: line number lost when converting to string
            info = new FileLineInfo(null, " member  ", 3);
            Assert.AreEqual(3, info.Line.Value);
            Test.OrdinalEquals("   at member", info.ToString());

            // member + file: round-trip
            info = new FileLineInfo("  file ", "member", null);
            Test.OrdinalEquals("file", info.File);
            roundTripTest(info, "   at member in file");

            // member + file + line: round-trip
            info = new FileLineInfo("  file ", "member", 2);
            Test.OrdinalEquals("file", info.File);
            roundTripTest(info, "   at member in file:line 2");
        }
    }
}
