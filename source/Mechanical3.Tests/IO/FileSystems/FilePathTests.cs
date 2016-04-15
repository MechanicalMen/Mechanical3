using System;
using System.IO;
using Mechanical3.Core;
using Mechanical3.IO.FileSystems;
using NUnit.Framework;

namespace Mechanical3.Tests.IO.FileSystems
{
    [TestFixture(Category = "FileSystems")]
    public static class FilePathTests
    {
        [Test]
        public static void IsValidNameTests()
        {
            Assert.False(FilePath.IsValidName(null));
            Assert.False(FilePath.IsValidName(string.Empty));

            Assert.True(FilePath.IsValidName("a"));
            Assert.True(FilePath.IsValidName("."));
            Assert.True(FilePath.IsValidName(" "));
            Assert.True(FilePath.IsValidName("\n"));
            Assert.True(FilePath.IsValidName(new string('a', count: 255)));

            Assert.False(FilePath.IsValidName(new string('a', count: 256)));
            Assert.False(FilePath.IsValidName("/"));
            Assert.False(FilePath.IsValidName("a/a"));
            Assert.False(FilePath.IsValidName("/a"));
            Assert.False(FilePath.IsValidName("a/"));
        }

        [Test]
        public static void IsValidPathTests()
        {
            Action<bool, string> pathTest = ( expectedResult, path ) =>
            {
                if( expectedResult )
                {
                    Assert.True(FilePath.IsValidPath(path));
                    Assert.NotNull(FilePath.From(path));
                }
                else
                {
                    Assert.False(FilePath.IsValidPath(path));
                    Assert.Throws<ArgumentException>(() => FilePath.From(path));
                }
            };

            pathTest(false, null);
            pathTest(false, string.Empty);

            pathTest(true, "a");
            pathTest(true, "a/./\n/ ");

            pathTest(true, new string('a', count: 255));
            pathTest(false, new string('a', count: 256));

            pathTest(false, "/");
            pathTest(false, "///");
            pathTest(false, "a//b");
            pathTest(false, "/a");

            pathTest(true, "a/");
            pathTest(true, "a/a/a/");
        }

        [Test]
        public static void IsDirectoryTests()
        {
            Assert.False(FilePath.From("a").IsDirectory);
            Assert.False(FilePath.From("a/b").IsDirectory);

            Assert.True(FilePath.From("a/").IsDirectory);
            Assert.True(FilePath.From("a/b/").IsDirectory);
        }

        [Test]
        public static void NameTests()
        {
            Test.OrdinalEquals("a", FilePath.From("a").Name);
            Test.OrdinalEquals("a", FilePath.From("a/").Name);
            Test.OrdinalEquals("b", FilePath.From("a/b").Name);
            Test.OrdinalEquals("b", FilePath.From("a/b/").Name);
            Test.OrdinalEquals("a.b", FilePath.From("a.b").Name);
            Test.OrdinalEquals("a.b", FilePath.From("a.b/").Name);
        }

        [Test]
        public static void ExtensionTests()
        {
            Test.OrdinalEquals(string.Empty, Path.GetExtension("a"));
            Test.OrdinalEquals(string.Empty, FilePath.From("a").Extension);

            Test.OrdinalEquals(string.Empty, Path.GetExtension(Path.Combine("a", "b")));
            Test.OrdinalEquals(string.Empty, FilePath.From("a/b").Extension);

            Test.OrdinalEquals(".txt", Path.GetExtension("a.txt"));
            Test.OrdinalEquals(".txt", FilePath.From("a.txt").Extension);

            Test.OrdinalEquals(".gz", Path.GetExtension("a.tar.gz"));
            Test.OrdinalEquals(".gz", FilePath.From("a.tar.gz").Extension);

            Test.OrdinalEquals(".txt", Path.GetExtension(Path.Combine("a.a", "b.txt")));
            Test.OrdinalEquals(".txt", FilePath.From("a.a/b.txt").Extension);

            Test.OrdinalEquals(string.Empty, Path.GetExtension("a.txt" + Path.DirectorySeparatorChar));
            Test.OrdinalEquals(string.Empty, FilePath.From("a.txt/").Extension);
        }

        [Test]
        public static void NameWithoutExtensionTests()
        {
            Test.OrdinalEquals("a", FilePath.From("a").NameWithoutExtension);
            Test.OrdinalEquals("a", FilePath.From("a/").NameWithoutExtension);
            Test.OrdinalEquals("b", FilePath.From("a/b").NameWithoutExtension);
            Test.OrdinalEquals("b", FilePath.From("a/b/").NameWithoutExtension);

            Test.OrdinalEquals("a", FilePath.From("a.b").NameWithoutExtension);
            Test.OrdinalEquals("a.b", FilePath.From("a.b/").NameWithoutExtension);

            Test.OrdinalEquals("a.tar", Path.GetFileNameWithoutExtension("a.tar.gz"));
            Test.OrdinalEquals("a.tar", FilePath.From("a.tar.gz").NameWithoutExtension);
        }

        [Test]
        public static void ParentTests()
        {
            Assert.False(FilePath.From("a").HasParent);
            Assert.Null(FilePath.From("a").Parent);
            Assert.False(FilePath.From("a/").HasParent);
            Assert.Null(FilePath.From("a/").Parent);

            Assert.True(FilePath.From("a/b").HasParent);
            Test.OrdinalEquals("a/", FilePath.From("a/b").Parent.ToString());
            Assert.True(FilePath.From("a/b/c").HasParent);
            Test.OrdinalEquals("a/b/", FilePath.From("a/b/c").Parent.ToString());
        }

        [Test]
        public static void RootTests()
        {
            Test.OrdinalEquals(FilePath.From("a/").ToString(), FilePath.From("a/b/c").Root.ToString()); // root is indirect parent
            Test.OrdinalEquals(FilePath.From("a/").ToString(), FilePath.From("a/b/c/").Root.ToString());
            Test.OrdinalEquals(FilePath.From("a/").ToString(), FilePath.From("a/b/").Root.ToString()); // root is direct parent
            Test.OrdinalEquals(FilePath.From("a/").ToString(), FilePath.From("a/b").Root.ToString());
            Test.OrdinalEquals(FilePath.From("a/").ToString(), FilePath.From("a/").Root.ToString()); // root is the same as the path
            Test.OrdinalEquals(FilePath.From("a").ToString(), FilePath.From("a").Root.ToString());
        }

        [Test]
        public static void CombineTests()
        {
            Test.OrdinalEquals("a/b", (FilePath.From("a/") + FilePath.From("b")).ToString());
            Test.OrdinalEquals("a/b/", (FilePath.From("a/") + FilePath.From("b/")).ToString());

            Assert.Throws<NullReferenceException>(() => (FilePath.From("a/") + (FilePath)null).NotNullReference());
            Assert.Throws<NullReferenceException>(() => ((FilePath)null + FilePath.From("b")).NotNullReference());
            Assert.Throws<InvalidOperationException>(() => (FilePath.From("a") + FilePath.From("b")).NotNullReference());
        }

        [Test]
        public static void ToFileOrDirectoryPathTests()
        {
            Test.OrdinalEquals(FilePath.From("a").ToString(), FilePath.From("a").ToFilePath().ToString());
            Test.OrdinalEquals(FilePath.From("a/").ToString(), FilePath.From("a").ToDirectoryPath().ToString());

            Test.OrdinalEquals(FilePath.From("a").ToString(), FilePath.From("a/").ToFilePath().ToString());
            Test.OrdinalEquals(FilePath.From("a/").ToString(), FilePath.From("a/").ToDirectoryPath().ToString());
        }

        [Test]
        public static void EqualityTests()
        {
            Assert.True(FilePath.From("a").Equals(FilePath.From("a")));
            Assert.False(FilePath.From("a").Equals(FilePath.From("A")));
            Assert.False(FilePath.From("a").Equals(FilePath.From("a/")));
            Assert.False(FilePath.From("a").Equals(FilePath.From("b")));
            Assert.False(FilePath.From("a").Equals(FilePath.From("aa")));
            Assert.True(FilePath.From("a/b").Equals(FilePath.From("a/b")));
            Assert.False(FilePath.From("a/").Equals(FilePath.From("a/b")));
        }

        [Test]
        public static void IsParentOrAncestorOfTests()
        {
            // parent or ancestor of self is always false
            // (independent of whether the path points to a file or directory)
            Assert.False(FilePath.From("a").IsParentOf(FilePath.From("a")));
            Assert.False(FilePath.From("a").IsAncestorOf(FilePath.From("a")));
            Assert.False(FilePath.From("a").IsParentOf(FilePath.From("a/")));
            Assert.False(FilePath.From("a").IsAncestorOf(FilePath.From("a/")));
            Assert.False(FilePath.From("a/").IsParentOf(FilePath.From("a")));
            Assert.False(FilePath.From("a/").IsAncestorOf(FilePath.From("a")));
            Assert.False(FilePath.From("a/").IsParentOf(FilePath.From("a/")));
            Assert.False(FilePath.From("a/").IsAncestorOf(FilePath.From("a/")));

            // direct parents are always ancestors
            Assert.True(FilePath.From("a/").IsParentOf(FilePath.From("a/b"))); // direct parent of file
            Assert.True(FilePath.From("a/").IsAncestorOf(FilePath.From("a/b")));
            Assert.False(FilePath.From("a").IsParentOf(FilePath.From("a/b")));
            Assert.False(FilePath.From("a").IsAncestorOf(FilePath.From("a/b")));
            Assert.True(FilePath.From("a/b/").IsParentOf(FilePath.From("a/b/c/"))); // direct parent of directory
            Assert.True(FilePath.From("a/b/").IsAncestorOf(FilePath.From("a/b/c/")));
            Assert.False(FilePath.From("a/b").IsParentOf(FilePath.From("a/b/c")));
            Assert.False(FilePath.From("a/b").IsAncestorOf(FilePath.From("a/b/c")));

            // indirect parents are only ancestors
            Assert.False(FilePath.From("a/").IsParentOf(FilePath.From("a/b/c")));
            Assert.True(FilePath.From("a/").IsAncestorOf(FilePath.From("a/b/c")));

            // names from the middle of a path don't count as anything
            Assert.False(FilePath.From("b").IsParentOf(FilePath.From("a/b/c")));
            Assert.False(FilePath.From("b").IsAncestorOf(FilePath.From("a/b/c")));
            Assert.False(FilePath.From("b/").IsParentOf(FilePath.From("a/b/c")));
            Assert.False(FilePath.From("b/").IsAncestorOf(FilePath.From("a/b/c")));
        }

        [Test]
        public static void GetChildFromTests()
        {
            Assert.Throws<InvalidOperationException>(() => FilePath.FromFileName("a").GetChildFrom(FilePath.From("a/b")));
            Assert.Throws<ArgumentNullException>(() => FilePath.FromDirectoryName("a").GetChildFrom(null));

            // not an ancestor
            Test.OrdinalEquals(null, FilePath.From("a/").GetChildFrom(FilePath.From("b"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("a/").GetChildFrom(FilePath.From("b/"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("a/").GetChildFrom(FilePath.From("b/c"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("a/").GetChildFrom(FilePath.From("b/c/"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("a/b/").GetChildFrom(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("a/b/").GetChildFrom(FilePath.From("b/"))?.ToString());

            // "same" path (still not an ancestor)
            Test.OrdinalEquals(null, FilePath.From("a/").GetChildFrom(FilePath.From("a"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("a/").GetChildFrom(FilePath.From("a/"))?.ToString());

            // direct parent
            Test.OrdinalEquals("a/b", FilePath.From("a/").GetChildFrom(FilePath.From("a/b"))?.ToString());
            Test.OrdinalEquals("a/b/", FilePath.From("a/").GetChildFrom(FilePath.From("a/b/"))?.ToString());
            Test.OrdinalEquals("a/b/c", FilePath.From("a/b/").GetChildFrom(FilePath.From("a/b/c"))?.ToString());
            Test.OrdinalEquals("a/b/c/", FilePath.From("a/b/").GetChildFrom(FilePath.From("a/b/c/"))?.ToString());

            // indirect parent
            Test.OrdinalEquals("a/b/", FilePath.From("a/").GetChildFrom(FilePath.From("a/b/c"))?.ToString());
            Test.OrdinalEquals("a/b/", FilePath.From("a/").GetChildFrom(FilePath.From("a/b/c/"))?.ToString());
            Test.OrdinalEquals("a/b/c/", FilePath.From("a/b/").GetChildFrom(FilePath.From("a/b/c/d"))?.ToString());
            Test.OrdinalEquals("a/b/c/", FilePath.From("a/b/").GetChildFrom(FilePath.From("a/b/c/d/"))?.ToString());
        }

        [Test]
        public static void RemoveAncestorTests()
        {
            Assert.Throws<InvalidOperationException>(() => FilePath.From("a/b").RemoveAncestor(FilePath.FromFileName("a")));
            Assert.Throws<ArgumentNullException>(() => FilePath.From("a/b").RemoveAncestor(null));

            // not an ancestor
            Test.OrdinalEquals(null, FilePath.From("b").RemoveAncestor(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("b/").RemoveAncestor(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("b/c").RemoveAncestor(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("b/c/").RemoveAncestor(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("a/").RemoveAncestor(FilePath.From("a/b/"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("b/").RemoveAncestor(FilePath.From("a/b/"))?.ToString());

            // "same" path (still not an ancestor)
            Test.OrdinalEquals(null, FilePath.From("a").RemoveAncestor(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals(null, FilePath.From("a/").RemoveAncestor(FilePath.From("a/"))?.ToString());

            // direct parent
            Test.OrdinalEquals("b", FilePath.From("a/b").RemoveAncestor(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals("b/", FilePath.From("a/b/").RemoveAncestor(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals("c", FilePath.From("a/b/c").RemoveAncestor(FilePath.From("a/b/"))?.ToString());
            Test.OrdinalEquals("c/", FilePath.From("a/b/c/").RemoveAncestor(FilePath.From("a/b/"))?.ToString());

            // indirect parent
            Test.OrdinalEquals("b/c", FilePath.From("a/b/c").RemoveAncestor(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals("b/c/", FilePath.From("a/b/c/").RemoveAncestor(FilePath.From("a/"))?.ToString());
            Test.OrdinalEquals("c/d", FilePath.From("a/b/c/d").RemoveAncestor(FilePath.From("a/b/"))?.ToString());
            Test.OrdinalEquals("c/d/", FilePath.From("a/b/c/d/").RemoveAncestor(FilePath.From("a/b/"))?.ToString());
        }
    }
}
