using System;
using System.Globalization;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class LocalizedStringComparerTests
    {
        [Test]
        public static void LocalizedComparerTest()
        {
            string str1 = "id"; // in turkish 'i' and 'I' are different letters (like 'y' and 'i').
            string str2 = "ID";
            Assert.False(StringComparer.Ordinal.Equals(str1, str2));

            // case-sensitive
            Assert.False(new LocalizedStringComparer(CultureInfo.GetCultureInfo("en-US"), CompareOptions.None).Equals(str1, str2));
            Assert.False(new LocalizedStringComparer(CultureInfo.GetCultureInfo("tr-TR"), CompareOptions.None).Equals(str1, str2));

            // case insensitive
            Assert.True(new LocalizedStringComparer(CultureInfo.GetCultureInfo("en-US"), CompareOptions.IgnoreCase).Equals(str1, str2));
            Assert.False(new LocalizedStringComparer(CultureInfo.GetCultureInfo("tr-TR"), CompareOptions.IgnoreCase).Equals(str1, str2));
        }
    }
}
