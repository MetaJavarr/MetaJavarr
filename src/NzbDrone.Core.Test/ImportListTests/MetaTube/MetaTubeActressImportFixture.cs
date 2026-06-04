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
                    ActressName = "Example Actress"
                }
            };
        }

        [Test]
        public void should_import_only_exact_actress_matches()
        {
            GivenSearchResponse();

            var items = Subject.Fetch().Movies;

            items.Should().ContainSingle();
            items.Single().Title.Should().Contain("IPZZ-562");
            items.Single().TmdbId.Should().Be(MetaTubeIdMapper.ToMetadataId("javbus", "ipzz-562"));
        }

        private void GivenSearchResponse()
        {
            const string json = @"{
              ""data"": [
                {
                  ""id"": ""ipzz-562"",
                  ""provider"": ""javbus"",
                  ""number"": ""IPZZ-562"",
                  ""title"": ""Matching Movie"",
                  ""release_date"": ""2024-01-02"",
                  ""actors"": [""Example Actress""]
                },
                {
                  ""id"": ""ipzz-999"",
                  ""provider"": ""javbus"",
                  ""number"": ""IPZZ-999"",
                  ""title"": ""Non Matching Movie"",
                  ""release_date"": ""2024-01-03"",
                  ""actors"": [""Different Actress""]
                }
              ]
            }";

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Get<MetaTubeResponse<List<MetaTubeMovieResource>>>(It.Is<HttpRequest>(r => r.Url.FullUri == $"{BaseUrl}/movies/search?q=Example%20Actress")))
                .Returns<HttpRequest>(r => new HttpResponse<MetaTubeResponse<List<MetaTubeMovieResource>>>(new HttpResponse(r, new HttpHeader { ContentType = HttpAccept.Json.Value }, json)));
        }
    }
}
