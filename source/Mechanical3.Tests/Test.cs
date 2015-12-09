using System;
using NUnit.Framework;

namespace Mechanical3.Tests
{
    public static class Test
    {
        public static void OrdinalEquals( string x, string y )
        {
            Assert.True(string.Equals(x, y, StringComparison.Ordinal));
        }
    }
}
