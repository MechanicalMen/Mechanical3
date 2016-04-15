using System;
using System.IO;
using System.Linq;
using System.Text;
using Mechanical3.Core;
using Mechanical3.IO.FileSystems;
using NUnit.Framework;

namespace Mechanical3.Tests
{
    public static class Test
    {
        public static void OrdinalEquals( string x, string y )
        {
            Assert.True(string.Equals(x, y, StringComparison.Ordinal));
        }

        public static string ReplaceLineTerminators( string input, string newLine )
        {
            var sb = new StringBuilder();
            using( var reader = new StringReader(input) )
            {
                string line;
                while( (line = reader.ReadLine()).NotNullReference() )
                {
                    if( sb.Length != 0 )
                        sb.Append(newLine);

                    sb.Append(line);
                }
            }
            return sb.ToString();
        }

        public static void AssertAreEqual( string[] expected, string[] actual, StringComparer comparer )
        {
            Assert.AreEqual(expected.Length, actual.Length);

            for( int i = 0; i < expected.Length; ++i )
            {
                Assert.IsTrue(comparer.Equals(expected[i], actual[i]));
            }
        }

        public static void AssertAreEqual( string[] expected, FilePath[] actual, bool sort = true )
        {
            if( sort )
            {
                expected = expected?.OrderBy(str => str, FilePath.Comparer).ToArray();
                actual = actual?.OrderBy(p => p.ToString(), FilePath.Comparer).ToArray();
            }

            AssertAreEqual(expected, actual?.Select(p => p.ToString()).ToArray(), FilePath.Comparer);
        }
    }
}
