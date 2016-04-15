using System;
using Mechanical3.IO.FileSystems;
using NUnit.Framework;

namespace Mechanical3.Tests.IO.FileSystems
{
    [TestFixture(Category = "FileSystems")]
    public static class SubdirectoryFileSystemTests
    {
        [Test]
        public static void GenericSubdirectoryFileSystemTests()
        {
            using( var memoryFileSystem = new MemoryFileSystem() )
            {
                var subdirFS = new SubdirectoryFileSystem(memoryFileSystem, FilePath.From("a/b/"));
                GenericFileSystemTests.GetPathsTests(subdirFS);
                GenericFileSystemTests.CreateDeleteDirectoryTests(subdirFS);
                GenericFileSystemTests.CreateWriteReadDeleteFileTests(subdirFS);
                GenericFileSystemTests.ReadWriteFileTests(subdirFS);
            }
        }

        [Test]
        public static void SubdirectoryFileSysSpecificTests()
        {
            Assert.Throws<ArgumentNullException>(() => new SubdirectoryFileSystem(null, FilePath.FromDirectoryName("a")));
            Assert.Throws<ArgumentException>(() => new SubdirectoryFileSystem(new MemoryFileSystem(), null));
            Assert.Throws<ArgumentException>(() => new SubdirectoryFileSystem(new MemoryFileSystem(), FilePath.FromFileName("a")));

            using( var memoryFileSystem = new MemoryFileSystem() )
            {
                var subdirFS = new SubdirectoryFileSystem(memoryFileSystem, FilePath.From("a/b/"));

                // ToHostPath
                Test.OrdinalEquals("a/b/c", subdirFS.ToHostPath(FilePath.FromFileName("c")));

                // GetPaths
                using( var stream = subdirFS.CreateFile(FilePath.From("x/y"), overwriteIfExists: false) )
                    stream.WriteByte(3);
                Test.AssertAreEqual(new string[] { "x/" }, subdirFS.GetPaths());
                Test.AssertAreEqual(new string[] { "x/y" }, subdirFS.GetPaths(FilePath.From("x/")));

                // created in subdirFS --> turns up in memoryFS
                using( var stream = memoryFileSystem.ReadFile(FilePath.From("a/b/x/y")) )
                {
                    Assert.AreEqual(1, stream.Length);
                    Assert.AreEqual(3, stream.ReadByte());
                }

                // create in memoryFS --> turns up in subdirFS
                memoryFileSystem.CreateDirectory(FilePath.From("a/b/z/"));
                Assert.True(subdirFS.Exists(FilePath.From("z/")));
            }
        }
    }
}
