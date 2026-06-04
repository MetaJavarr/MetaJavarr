using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.MetaTube;

namespace NzbDrone.Core.Test.MetadataSource.MetaTube
{
    [TestFixture]
    public class MetaTubeIdMapperFixture
    {
        [Test]
        public void should_return_same_positive_id_for_same_identity()
        {
            var first = MetaTubeIdMapper.ToMetadataId("javbus", "ipzz-562");
            var second = MetaTubeIdMapper.ToMetadataId("JAVBUS", "IPZZ-562");

            first.Should().Be(second);
            first.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_create_external_id()
        {
            MetaTubeIdMapper.ToExternalId("javbus", "ipzz-562").Should().Be("javbus:ipzz-562");
        }
    }
}
