using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.ImportLists.MetaTube;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.MetaTube
{
    [TestFixture]
    public class MetaTubeActressSettingsValidatorFixture : CoreTest
    {
        [Test]
        public void empty_actress_name_should_not_validate()
        {
            var setting = new MetaTubeActressSettings
            {
                ActressName = string.Empty
            };

            setting.Validate().IsValid.Should().BeFalse();
        }

        [Test]
        public void actress_name_should_validate()
        {
            var setting = new MetaTubeActressSettings
            {
                ActressName = "Example Actress"
            };

            setting.Validate().IsValid.Should().BeTrue();
        }
    }
}
