using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.App.Test
{
    [TestFixture]
    public class DockerfileFixture : TestBase
    {
        [Test]
        public void image_should_run_as_uid_1000_gid_1000()
        {
            var dockerfile = File.ReadAllLines(GetRepositoryFile("Dockerfile"));

            var runtimeUser = dockerfile
                .Select(line => line.Trim())
                .LastOrDefault(line => line.StartsWith("USER ", StringComparison.Ordinal));

            runtimeUser.Should().Be("USER 1000:1000");
        }

        private static string GetRepositoryFile(string fileName)
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, fileName);
                if (File.Exists(path))
                {
                    return path;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Could not find {fileName} from {TestContext.CurrentContext.TestDirectory}");
        }
    }
}
