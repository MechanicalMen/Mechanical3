using System;
using System.IO;
using Mechanical3.IO.FileSystems;
using NUnit.Framework;

namespace Mechanical3.Tests.IO.FileSystems
{
    [TestFixture(Category = "FileSystems")]
    public static class DirectoryFileSystemTests
    {
        [Test]
        public static void DirectoryFileSysTests()
        {
            // create an empty, temporary directory
            var tempPath = Path.GetTempPath();
            while( true )
            {
                var guid = Guid.NewGuid().ToString("N");
                if( !Directory.Exists(Path.Combine(tempPath, guid)) )
                {
                    tempPath = Path.Combine(tempPath, guid);
                    Directory.CreateDirectory(tempPath);
                    break;
                }
            }

            try
            {
                // do tests
                var directoryFileSystem = new DirectoryFileSystem(tempPath);
                GenericFileSystemTests.GetPathsTests(directoryFileSystem);
                GenericFileSystemTests.CreateDeleteDirectoryTests(directoryFileSystem);
                GenericFileSystemTests.CreateWriteReadDeleteFileTests(directoryFileSystem);
                GenericFileSystemTests.ReadWriteFileTests(directoryFileSystem);
            }
            finally
            {
                // remove temporary directory
                Directory.Delete(tempPath, recursive: true);
            }
        }
    }
}
