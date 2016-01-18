using System;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class MechanicalVersionTests
    {
        [Test]
        public static void EmbeddedResourceTest()
        {
            var version = new MechanicalVersion(typeof(MechanicalVersionTests).Assembly, "Mechanical3.Tests.testVersion.json");

            Assert.True(string.Equals("Mechanical3 Unit Tests", version.Name, StringComparison.Ordinal));
            Assert.True(string.Equals(" test  version   ", version.Version, StringComparison.Ordinal));
            Assert.AreEqual(1, version.TotalBuildCount);
            Assert.AreEqual(0, version.VersionBuildCount);

            var expectedDate = new DateTime(0, DateTimeKind.Utc);
            Assert.AreEqual(expectedDate.Ticks, version.LastBuildDate.Ticks);
            Assert.AreEqual(expectedDate.Kind, version.LastBuildDate.Kind);
        }
    }
}
