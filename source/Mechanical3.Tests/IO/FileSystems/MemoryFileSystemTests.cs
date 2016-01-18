using Mechanical3.IO.FileSystems;
using NUnit.Framework;

namespace Mechanical3.Tests.IO.FileSystems
{
    [TestFixture(Category = "FileSystems")]
    public static class MemoryFileSystemTests
    {
        [Test]
        public static void MemoryFileSysTests()
        {
            using( var memoryFileSystem = new MemoryFileSystem() )
            {
                GenericFileSystemTests.GetPathsTests(memoryFileSystem);
                GenericFileSystemTests.CreateDeleteDirectoryTests(memoryFileSystem);
                GenericFileSystemTests.CreateWriteReadDeleteFileTests(memoryFileSystem);
                GenericFileSystemTests.ReadWriteFileTests(memoryFileSystem);
            }
        }
    }
}
