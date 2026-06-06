using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.MetaTube;
using NzbDrone.Core.MetadataSource.MetaTube;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.MetaTube
{
    [TestFixture]
    public class MetaTubeActressImportFixture : CoreTest<MetaTubeActressImport>
    {
        private const string BaseUrl = "https://metatube.example/v1";

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigFileProvider>()
                .SetupGet(c => c.MetaTubeUrl)
                .Returns(BaseUrl);

            Mocker.GetMock<IConfigFileProvider>()
                .SetupGet(c => c.MetaTubeToken)
                .Returns(string.Empty);

            Subject.Definition = new ImportListDefinition
            {
                Id = 7,
                Settings = new MetaTubeActressSettings
                {
                    ActressName = "Minami Aizawa",
                    Provider = "av-league",
                    ActorId = "minami-aizawa"
                }
            };
        }

        [Test]
        public void should_import_identity_based_actor_matches()
        {
            GivenActorDetailResponse();
            GivenActorMovieSearchResponse();

            var items = Subject.Fetch().Movies;

            items.Should().ContainSingle();
            items.Single().Title.Should().Contain("IPX-159");
            items.Single().TmdbId.Should().Be(MetaTubeIdMapper.ToMetadataId("javbus", "ipx-159"));

            VerifyGet<MetaTubeResponse<MetaTubeActorResource>>("/actors/av-league/minami-aizawa");
            VerifyGet<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=Minami%20Aizawa");
        }

        private void GivenActorDetailResponse()
        {
            const string json = @"{
              ""data"": {
                ""id"": ""minami-aizawa"",
                ""provider"": ""av-league"",
                ""name"": ""Minami Aizawa"",
                ""aliases"": [""Aizawa Minami""],
                ""images"": [""https://example.test/minami.jpg""]
              }
            }";

            GivenResponse<MetaTubeResponse<MetaTubeActorResource>>("/actors/av-league/minami-aizawa", json);
        }

        private void GivenActorMovieSearchResponse()
        {
            const string json = @"{
              ""data"": [
                {
                  ""id"": ""ipx-159"",
                  ""provider"": ""javbus"",
                  ""number"": ""IPX-159"",
                  ""title"": ""Matching Movie"",
                  ""release_date"": ""2024-01-02"",
                  ""actors"": [""Minami Aizawa""]
                },
                {
                  ""id"": ""ipx-999"",
                  ""provider"": ""javbus"",
                  ""number"": ""IPX-999"",
                  ""title"": ""Non Matching Movie"",
                  ""release_date"": ""2024-01-03"",
                  ""actors"": [""Different Actress""]
                }
              ]
            }";

            GivenResponse<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=Minami%20Aizawa", json);
        }

        private void GivenResponse<T>(string pathAndQuery, string json)
            where T : new()
        {
            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Get<T>(It.Is<HttpRequest>(r => r.Url.FullUri == $"{BaseUrl}{pathAndQuery}")))
                .Returns<HttpRequest>(r => new HttpResponse<T>(new HttpResponse(r, new HttpHeader { ContentType = HttpAccept.Json.Value }, json)));
        }

        private void VerifyGet<T>(string pathAndQuery)
            where T : new()
        {
            Mocker.GetMock<IHttpClient>()
                .Verify(c => c.Get<T>(It.Is<HttpRequest>(r => r.Url.FullUri == $"{BaseUrl}{pathAndQuery}")), Times.Once());
        }
    }
}
