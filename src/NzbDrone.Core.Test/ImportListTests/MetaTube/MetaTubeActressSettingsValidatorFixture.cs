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
        public void name_only_actor_follow_should_not_validate()
        {
            var setting = new MetaTubeActressSettings
            {
                ActressName = "Minami Aizawa"
            };

            setting.Validate().IsValid.Should().BeFalse();
        }

        [Test]
        public void identity_based_actor_follow_should_validate()
        {
            var setting = new MetaTubeActressSettings
            {
                ActressName = "Minami Aizawa",
                Provider = "av-league",
                ActorId = "minami-aizawa"
            };

            setting.Validate().IsValid.Should().BeTrue();
        }
    }
}
