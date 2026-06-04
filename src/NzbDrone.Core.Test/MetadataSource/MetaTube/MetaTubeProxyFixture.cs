using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.MetaTube;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.MetaTube
{
    [TestFixture]
    public class MetaTubeProxyFixture : CoreTest<MetaTubeProxy>
    {
        private const string BaseUrl = "https://metatube.example/v1";
        private const string Token = "secret-token";

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigFileProvider>()
                .SetupGet(c => c.MetaTubeUrl)
                .Returns(BaseUrl);

            Mocker.GetMock<IConfigFileProvider>()
                .SetupGet(c => c.MetaTubeToken)
                .Returns(Token);

            Mocker.GetMock<IMovieService>()
                .Setup(c => c.FindByTmdbId(It.IsAny<int>()))
                .Returns((Movie)null);

            Mocker.GetMock<IMovieMetadataService>()
                .Setup(c => c.FindByTmdbId(It.IsAny<int>()))
                .Returns((MovieMetadata)null);
        }

        [Test]
        public void should_search_and_get_movie_detail_from_metatube()
        {
            GivenSearchResponse();
            GivenDetailResponse();

            var movie = Subject.SearchForNewMovie("IPZZ-562").Single();
            var metadata = movie.MovieMetadata.Value;
            var metadataId = MetaTubeIdMapper.ToMetadataId("javbus", "ipzz-562");

            movie.Title.Should().Be("IPZZ-562 Example Title");
            movie.TmdbId.Should().Be(metadataId);
            movie.ImdbId.Should().BeNullOrEmpty();
            metadata.Website.Should().Contain("metatube");
            metadata.Images.Should().Contain(i => i.CoverType == MediaCoverTypes.Poster);
            metadata.Genres.Should().Contain("Drama");
            metadata.Studio.Should().Be("Example Studio");

            var detail = Subject.GetMovieInfo(metadataId).Item1;

            detail.Title.Should().Be("IPZZ-562 Example Title");
            detail.TmdbId.Should().Be(metadataId);
            detail.ImdbId.Should().BeNullOrEmpty();
            detail.Website.Should().Contain("metatube");
            detail.Images.Should().Contain(i => i.CoverType == MediaCoverTypes.Poster);
            detail.Genres.Should().Contain("Drama");
            detail.Studio.Should().Be("Example Studio");

            VerifyAuthorizedGet<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=IPZZ-562");
            VerifyAuthorizedGet<MetaTubeResponse<MetaTubeMovieResource>>("/movies/javbus/ipzz-562");
        }

        private void GivenSearchResponse()
        {
            const string json = @"{
              ""data"": [
                {
                  ""id"": ""ipzz-562"",
                  ""provider"": ""javbus"",
                  ""number"": ""IPZZ-562"",
                  ""title"": ""Example Title"",
                  ""summary"": ""Example summary"",
                  ""runtime"": 120,
                  ""release_date"": ""2024-01-02"",
                  ""cover_url"": ""https://example.test/cover.jpg"",
                  ""thumb_url"": ""https://example.test/thumb.jpg"",
                  ""actors"": [""Example Actress""],
                  ""genres"": [""Drama""],
                  ""maker"": ""Example Studio""
                }
              ]
            }";

            GivenResponse<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=IPZZ-562", json);
        }

        private void GivenDetailResponse()
        {
            const string json = @"{
              ""data"": {
                ""id"": ""ipzz-562"",
                ""provider"": ""javbus"",
                ""number"": ""IPZZ-562"",
                ""title"": ""Example Title"",
                ""summary"": ""Example summary"",
                ""runtime"": 120,
                ""release_date"": ""2024-01-02"",
                ""cover_url"": ""https://example.test/cover.jpg"",
                ""thumb_url"": ""https://example.test/thumb.jpg"",
                ""actors"": [""Example Actress""],
                ""genres"": [""Drama""],
                ""maker"": ""Example Studio""
              }
            }";

            GivenResponse<MetaTubeResponse<MetaTubeMovieResource>>("/movies/javbus/ipzz-562", json);
        }

        private void GivenResponse<T>(string pathAndQuery, string json)
            where T : new()
        {
            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Get<T>(It.Is<HttpRequest>(r => MatchesRequest(r, pathAndQuery))))
                .Returns<HttpRequest>(r => new HttpResponse<T>(new HttpResponse(r, new HttpHeader { ContentType = HttpAccept.Json.Value }, json)));
        }

        private void VerifyAuthorizedGet<T>(string pathAndQuery)
            where T : new()
        {
            Mocker.GetMock<IHttpClient>()
                .Verify(c => c.Get<T>(It.Is<HttpRequest>(r => MatchesRequest(r, pathAndQuery))), Times.Once());
        }

        private static bool MatchesRequest(HttpRequest request, string pathAndQuery)
        {
            return request.Url.FullUri == $"{BaseUrl}{pathAndQuery}" &&
                   request.Headers.GetSingleValue("Authorization") == $"Bearer {Token}";
        }
    }
}
