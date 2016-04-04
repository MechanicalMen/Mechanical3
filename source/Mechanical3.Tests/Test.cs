using System;
using System.IO;
using System.Text;
using Mechanical3.Core;
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
    }
}
