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

            Assert.Null(version.GitCommit);
        }

        [Test]
        public static void GitCommitTests()
        {
            //// NOTE: the no git-commit option was already tested above

            const string withGitCommitJSON = @"{
  ""name"": ""Mechanical3 Unit Tests"",
  ""version"": "" test  version   "",
  ""totalBuildCount"": 1,
  ""versionBuildCount"": 0,
  ""lastBuildDate"": ""0001-01-01T00:00:00.0000000Z"",
  ""gitCommit"": ""6692ba6f7f091fd687b0780a96375fbd4166acf5""
}";
            const string nullGitCommitJSON = @"{
  ""name"": ""Mechanical3 Unit Tests"",
  ""version"": "" test  version   "",
  ""totalBuildCount"": 1,
  ""versionBuildCount"": 0,
  ""lastBuildDate"": ""0001-01-01T00:00:00.0000000Z"",
  ""gitCommit"": null
}";
            var version = new MechanicalVersion(withGitCommitJSON);
            Test.OrdinalEquals("6692ba6f7f091fd687b0780a96375fbd4166acf5", version.GitCommit);

            version = new MechanicalVersion(nullGitCommitJSON);
            Assert.Null(version.GitCommit);
        }
    }
}
