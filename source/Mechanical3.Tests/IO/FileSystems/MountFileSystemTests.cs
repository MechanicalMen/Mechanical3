using System;
using Mechanical3.IO.FileSystems;
using NUnit.Framework;

namespace Mechanical3.Tests.IO.FileSystems
{
    [TestFixture(Category = "FileSystems")]
    public static class MountFileSystemTests
    {
        [Test]
        public static void MountTests()
        {
            using( var mfs = new MountFileSystem() )
            {
                // bad parameters
                Assert.Throws<ArgumentNullException>(() => mfs.Mount(null, new MemoryFileSystem()));
                Assert.Throws<ArgumentNullException>(() => mfs.Mount(FilePath.FromDirectoryName("test"), null));
                Assert.Throws<ArgumentException>(() => mfs.Mount(FilePath.FromFileName("test"), new MemoryFileSystem()));

                // can only mount once
                mfs.Mount(FilePath.FromDirectoryName("test"), new MemoryFileSystem());
                Assert.Throws<ArgumentException>(() => mfs.Mount(FilePath.FromDirectoryName("test"), new MemoryFileSystem()));

                // may not be part of a previous mount path
                mfs.Mount(FilePath.From("a/b/c/d/"), new MemoryFileSystem());
                Assert.Throws<ArgumentException>(() => mfs.Mount(FilePath.From("a/b/"), new MemoryFileSystem()));

                // sharing part of the path is OK
                mfs.Mount(FilePath.From("a/b/x/"), new MemoryFileSystem());
            }
        }

        [Test]
        public static void MountFileSystemSpecificTests()
        {
            // ToHostPath
            using( var mfs = new MountFileSystem() )
            {
                var memfs = new MemoryFileSystem();
                mfs.Mount(FilePath.From("a/"), memfs);
                Test.OrdinalEquals("b/c", mfs.ToHostPath(FilePath.From("a/b/c")));
            }

            // GetPaths
            using( var mfs = new MountFileSystem() )
            {
                var memfs = new MemoryFileSystem();
                mfs.Mount(FilePath.From("a/b/"), memfs);
                memfs.CreateFile(FilePath.From("x/y"), overwriteIfExists: false).Close();

                memfs = new MemoryFileSystem();
                mfs.Mount(FilePath.From("a/c/"), memfs);
                memfs.CreateFile(FilePath.From("z"), overwriteIfExists: false).Close();

                memfs = new MemoryFileSystem();
                mfs.Mount(FilePath.From("d/"), memfs);
                memfs.CreateDirectory(FilePath.From("w/"));

                Test.AssertAreEqual(
                    new string[] { "a/", "d/" },
                    mfs.GetPaths());

                Test.AssertAreEqual(
                    new string[] { "a/b/", "a/c/" },
                    mfs.GetPaths(FilePath.From("a/")));

                Test.AssertAreEqual(
                    new string[] { "a/b/x/" },
                    mfs.GetPaths(FilePath.From("a/b/")));

                Test.AssertAreEqual(
                    new string[] { "a/b/x/y" },
                    mfs.GetPaths(FilePath.From("a/b/x/")));

                Test.AssertAreEqual(
                    new string[] { "a/c/z" },
                    mfs.GetPaths(FilePath.From("a/c/")));

                Test.AssertAreEqual(
                    new string[] { "d/w/" },
                    mfs.GetPaths(FilePath.From("d/")));
            }

            // files and directories can not be created outside of mounts
            using( var mfs = new MountFileSystem() )
            {
                Assert.Throws<InvalidOperationException>(() => mfs.CreateFile(FilePath.From("a/b"), overwriteIfExists: true));
                Assert.Throws<InvalidOperationException>(() => mfs.CreateDirectory(FilePath.From("c/")));

                mfs.Mount(FilePath.From("x/y/"), new MemoryFileSystem());
                Assert.Throws<InvalidOperationException>(() => mfs.CreateFile(FilePath.From("x/z"), overwriteIfExists: true));
                Assert.Throws<InvalidOperationException>(() => mfs.CreateDirectory(FilePath.From("x/z/")));
            }

            // existing mounts can not be deleted
            using( var mfs = new MountFileSystem() )
            {
                mfs.Mount(FilePath.From("a/b/"), new MemoryFileSystem());
                Assert.Throws<InvalidOperationException>(() => mfs.Delete(FilePath.From("a/")));
                Assert.Throws<InvalidOperationException>(() => mfs.Delete(FilePath.From("a/b/")));
            }

            // disposing mountFS, disposes mounted memoryFS
            var memoryFS = new MemoryFileSystem();
            var mountFS = new MountFileSystem();
            mountFS.Mount(FilePath.FromDirectoryName("test"), memoryFS);
            Assert.False(memoryFS.IsDisposed);
            mountFS.Dispose();
            Assert.True(memoryFS.IsDisposed);
        }

        [Test]
        public static void GenericMountFileSystemTests()
        {
            using( var mfs = new MountFileSystem() )
            {
                var fs = new MemoryFileSystem();
                mfs.Mount(FilePath.FromDirectoryName("a"), fs);

                fs = new MemoryFileSystem();
                mfs.Mount(FilePath.From("b/c/"), fs);

                Action<IFileSystem> runGenericTests = fileSys =>
                {
                    GenericFileSystemTests.GetPathsTests(fileSys);
                    GenericFileSystemTests.CreateDeleteDirectoryTests(fileSys);
                    GenericFileSystemTests.CreateWriteReadDeleteFileTests(fileSys);
                    GenericFileSystemTests.ReadWriteFileTests(fileSys);
                };

                runGenericTests(new SubdirectoryFileSystem(mfs, FilePath.From("a/")));
                runGenericTests(new SubdirectoryFileSystem(mfs, FilePath.From("b/c/")));
            }
        }
    }
}
