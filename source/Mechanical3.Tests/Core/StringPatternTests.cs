using System;
using System.Globalization;
using Mechanical3.Core;
using NUnit.Framework;

namespace Mechanical3.Tests.Core
{
    [TestFixture(Category = "Core")]
    public static class StringPatternTests
    {
        [Test]
        public static void LiteralMatchTests()
        {
            // ordinal, case sensitive
            Assert.True(StringPattern.IsMatch("xyz", "xyz", StringComparison.Ordinal));
            Assert.False(StringPattern.IsMatch("xyz", "xyZ", StringComparison.Ordinal));
            Assert.False(StringPattern.IsMatch("xyZ", "xyz", StringComparison.Ordinal));

            // ordinal, case insensitive
            Assert.True(StringPattern.IsMatch("xyz", "xyz", StringComparison.OrdinalIgnoreCase));
            Assert.True(StringPattern.IsMatch("xyz", "Xyz", StringComparison.OrdinalIgnoreCase));
            Assert.True(StringPattern.IsMatch("Xyz", "xyz", StringComparison.OrdinalIgnoreCase));

            // culture specific, case sensitive
            var culture = CultureInfo.GetCultureInfo("hu-HU");
            Assert.True(StringPattern.IsMatch("aáb", "aáb", culture, CompareOptions.None));
            Assert.False(StringPattern.IsMatch("aáb", "aÁb", culture, CompareOptions.None));
            Assert.False(StringPattern.IsMatch("aÁb", "aáb", culture, CompareOptions.None));

            // culture specific, case insensitive
            Assert.True(StringPattern.IsMatch("aáb", "aáb", culture, CompareOptions.IgnoreCase));
            Assert.True(StringPattern.IsMatch("aáb", "aÁb", culture, CompareOptions.IgnoreCase));
            Assert.True(StringPattern.IsMatch("aÁb", "aáb", culture, CompareOptions.IgnoreCase));
        }

        private static void ExclamationMarkPatterns( char ch )
        {
            Assert.True(StringPattern.IsMatch("xyz", $"{ch}yz", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyz", $"x{ch}z", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyz", $"xy{ch}", StringComparison.Ordinal));

            Assert.True(StringPattern.IsMatch("xyz", $"{ch}y{ch}", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyz", $"{ch}{ch}z", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyz", $"x{ch}{ch}", StringComparison.Ordinal));

            Assert.True(StringPattern.IsMatch("xyz", $"{ch}{ch}{ch}", StringComparison.Ordinal));
        }

        [Test]
        public static void ExclamationMarkTests()
        {
            ExclamationMarkPatterns('!');
        }

        private static void QuestionMarkPatterns( char ch )
        {
            Assert.True(StringPattern.IsMatch("xyz", $"{ch}xyz", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyz", $"xyz{ch}", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyz", $"{ch}xyz{ch}", StringComparison.Ordinal));

            Assert.True(StringPattern.IsMatch("xyz", $"x{ch}y{ch}z", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyz", $"x{ch}{ch}z", StringComparison.Ordinal));
        }

        [Test]
        public static void QuestionMarkTests()
        {
            // match existing text character
            ExclamationMarkPatterns('?');

            // match missing text character
            QuestionMarkPatterns('?');
        }

        [Test]
        public static void StarTests()
        {
            // match existing text character
            ExclamationMarkPatterns('*');

            // match missing text character
            QuestionMarkPatterns('*');

            // match multiple existing characters
            Assert.True(StringPattern.IsMatch("xyz", "x*", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyz", "*z", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyz", "*", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch("xyyyz", "x*z", StringComparison.Ordinal));
        }

        [Test]
        public static void EscapeTests()
        {
            Assert.True(StringPattern.IsMatch(@"x*z", @"x\*z", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch(@"xy?", @"xy\?", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch(@"!yz", @"\!yz", StringComparison.Ordinal));
            Assert.True(StringPattern.IsMatch(@"\", @"\\", StringComparison.Ordinal));
        }
    }
}
