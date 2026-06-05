using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Update;

namespace NzbDrone.Core.Test.UpdateTests
{
    public class UpdatePackageProviderFixture : CoreTest<UpdatePackageProvider>
    {
        [Test]
        public void should_not_check_radarr_update_service_for_latest_update()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(c => c.Get<UpdatePackageAvailable>(It.IsAny<HttpRequest>()))
                  .Throws(new AssertionException("MetaJavarr must not query Radarr update packages."));

            Subject.GetLatestUpdate("develop", new Version(3, 0)).Should().BeNull();

            Mocker.GetMock<IHttpClient>()
                  .Verify(c => c.Get<UpdatePackageAvailable>(It.IsAny<HttpRequest>()), Times.Never());
        }

        [Test]
        public void should_not_check_radarr_update_service_for_recent_updates()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(c => c.Get<List<UpdatePackage>>(It.IsAny<HttpRequest>()))
                  .Throws(new AssertionException("MetaJavarr must not query Radarr update history."));

            Subject.GetRecentUpdates("develop", new Version(3, 0), new Version(2, 0)).Should().BeEmpty();

            Mocker.GetMock<IHttpClient>()
                  .Verify(c => c.Get<List<UpdatePackage>>(It.IsAny<HttpRequest>()), Times.Never());
        }
    }
}
