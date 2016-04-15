using System;
using System.IO;
using Mechanical3.IO.FileSystems;
using NUnit.Framework;

namespace Mechanical3.Tests.IO.FileSystems
{
    public static class GenericFileSystemTests
    {
        public static void CreateDeleteDirectoryTests( IFileSystem fileSystem )
        {
            Assert.NotNull(fileSystem);

            // test invalid arguments
            Assert.Throws<ArgumentException>(() => fileSystem.CreateDirectory(null));
            Assert.Throws<ArgumentException>(() => fileSystem.CreateDirectory(FilePath.FromFileName("a")));
            Assert.Throws<ArgumentException>(() => fileSystem.Delete(null));

            // create directories
            var directoryPath = FilePath.From("a/b/");
            fileSystem.CreateDirectory(directoryPath);
            Assert.True(fileSystem.Exists(directoryPath));
            Assert.True(fileSystem.Exists(directoryPath.Parent));

            // delete directories
            fileSystem.Delete(directoryPath.Parent);
            Assert.False(fileSystem.Exists(directoryPath.Parent));
            Assert.False(fileSystem.Exists(directoryPath));
        }

        public static void CreateWriteReadDeleteFileTests( IFileSystem fileSystem )
        {
            Assert.NotNull(fileSystem);

            // test invalid arguments
            Assert.Throws<ArgumentException>(() => fileSystem.CreateFile(null, overwriteIfExists: false));
            Assert.Throws<ArgumentException>(() => fileSystem.CreateFile(FilePath.FromDirectoryName("a"), overwriteIfExists: true));
            Assert.Throws<ArgumentException>(() => fileSystem.ReadFile(null));
            Assert.Throws<ArgumentException>(() => fileSystem.ReadFile(FilePath.FromDirectoryName("a")));
            if( fileSystem.SupportsGetFileSize )
            {
                Assert.Throws<ArgumentException>(() => fileSystem.GetFileSize(null));
                Assert.Throws<ArgumentException>(() => fileSystem.GetFileSize(FilePath.FromDirectoryName("a")));
                Assert.Throws<FileNotFoundException>(() => fileSystem.GetFileSize(FilePath.FromFileName("a")));
            }

            // create file
            var filePath = FilePath.From("a/b");
            using( var stream = fileSystem.CreateFile(filePath, overwriteIfExists: false) )
            {
                Assert.True(fileSystem.Exists(filePath));
                Assert.True(fileSystem.Exists(filePath.Parent));

                // write
                Assert.True(stream.CanWrite);
                stream.WriteByte(53);
            }

            // check file size
            if( fileSystem.SupportsGetFileSize )
            {
                Assert.AreEqual(1L, fileSystem.GetFileSize(filePath));
                Assert.Throws<ArgumentException>(() => fileSystem.GetFileSize(filePath.Parent));
            }

            // read file
            using( var stream = fileSystem.ReadFile(filePath) )
            {
                Assert.True(stream.CanRead);
                Assert.AreEqual(53, stream.ReadByte());

                if( stream.CanSeek )
                {
                    Assert.AreEqual(1L, stream.Position);
                    Assert.AreEqual(stream.Position, stream.Length);
                }
            }

            // try to overwrite
            Assert.Throws<IOException>(() => fileSystem.CreateFile(filePath, overwriteIfExists: false));
            Assert.Throws<ArgumentException>(() => fileSystem.CreateFile(filePath.Parent, overwriteIfExists: false));
            Assert.Throws<ArgumentException>(() => fileSystem.CreateFile(filePath.Parent, overwriteIfExists: true));
            fileSystem.CreateFile(filePath, overwriteIfExists: true).Close();

            // read again
            using( var stream = fileSystem.ReadFile(filePath) )
                Assert.AreEqual(-1, stream.ReadByte());

            // delete file
            fileSystem.Delete(filePath);
            Assert.False(fileSystem.Exists(filePath));
            Assert.True(fileSystem.Exists(filePath.Parent));
            fileSystem.Delete(filePath.Parent);
        }

        public static void GetPathsTests( IFileSystem fileSystem )
        {
            // invalid arguments
            Assert.Throws<ArgumentException>(() => fileSystem.GetPaths(FilePath.FromFileName("a")));

            // create entries
            fileSystem.CreateFile(FilePath.From("a/b/c"), overwriteIfExists: false).Close();
            fileSystem.CreateDirectory(FilePath.From("a/d/"));
            fileSystem.CreateFile(FilePath.From("e"), overwriteIfExists: true).Close();
            fileSystem.CreateDirectory(FilePath.From("f/"));

            // test results
            Test.AssertAreEqual(
                new string[] { "a/", "e", "f/" },
                fileSystem.GetPaths());

            Test.AssertAreEqual(
                new string[] { "a/b/", "a/d/" },
                fileSystem.GetPaths(FilePath.From("a/")));

            Test.AssertAreEqual(
                new string[] { "a/b/c" },
                fileSystem.GetPaths(FilePath.From("a/b/")));

            Test.AssertAreEqual(
                new string[0],
                fileSystem.GetPaths(FilePath.From("a/d/")));

            // delete files and directories
            fileSystem.DeleteAllFrom();
        }

        public static void ReadWriteFileTests( IFileSystem fileSystem )
        {
            Assert.NotNull(fileSystem);

            // test invalid arguments
            Assert.Throws<ArgumentException>(() => fileSystem.ReadWriteFile(null));
            Assert.Throws<ArgumentException>(() => fileSystem.ReadWriteFile(FilePath.FromDirectoryName("a")));

            // create file
            var filePath = FilePath.From("a");
            using( var stream = fileSystem.ReadWriteFile(filePath) )
            {
                Assert.True(fileSystem.Exists(filePath));

                // write
                Assert.True(stream.CanRead);
                Assert.True(stream.CanWrite);
                stream.WriteByte(53);
            }

            // check file size
            if( fileSystem.SupportsGetFileSize )
                Assert.AreEqual(1L, fileSystem.GetFileSize(filePath));

            // read, then write file
            using( var stream = fileSystem.ReadWriteFile(filePath) )
            {
                Assert.True(stream.CanRead);
                Assert.True(stream.CanWrite);

                if( stream.CanSeek )
                {
                    Assert.AreEqual(0L, stream.Position);
                    Assert.AreEqual(1L, stream.Length);
                }

                Assert.AreEqual(53, stream.ReadByte());
                stream.WriteByte(7);

                if( stream.CanSeek )
                    Assert.AreEqual(2L, stream.Position);
            }

            // delete file
            fileSystem.Delete(filePath);
        }
    }
}
