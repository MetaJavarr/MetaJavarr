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
            GetMovieNumber(metadata).Should().Be("IPZZ-562");
            ShouldHaveDefaultTmdbRating(metadata.Ratings);

            var detail = Subject.GetMovieInfo(metadataId).Item1;

            detail.Title.Should().Be("IPZZ-562 Example Title");
            detail.TmdbId.Should().Be(metadataId);
            detail.ImdbId.Should().BeNullOrEmpty();
            detail.Website.Should().Contain("metatube");
            detail.Images.Should().Contain(i => i.CoverType == MediaCoverTypes.Poster);
            detail.Genres.Should().Contain("Drama");
            detail.Studio.Should().Be("Example Studio");
            GetMovieNumber(detail).Should().Be("IPZZ-562");
            ShouldHaveDefaultTmdbRating(detail.Ratings);

            VerifyAuthorizedGet<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=IPZZ-562");
            VerifyAuthorizedGet<MetaTubeResponse<MetaTubeMovieResource>>("/movies/javbus/ipzz-562");
        }

        [Test]
        public void should_use_javfree_cover_for_fc2hub_storage_covers()
        {
            GivenFc2HubSearchResponse();
            GivenFc2HubDetailResponse();

            var movie = Subject.SearchForNewMovie("FC2-PPV-3131319").Single();
            var metadataId = MetaTubeIdMapper.ToMetadataId("fc2hub", "1312343-3131319");
            const string javfreeCover = "https://cf.javfree.me/HLIC/FC2-PPV-3131319.jpg";

            movie.TmdbId.Should().Be(metadataId);
            movie.MovieMetadata.Value.Images.Should().OnlyContain(i => i.RemoteUrl == javfreeCover);

            var detail = Subject.GetMovieInfo(metadataId).Item1;

            detail.Images.Should().OnlyContain(i => i.RemoteUrl == javfreeCover);

            VerifyAuthorizedGet<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=FC2-PPV-3131319");
            VerifyAuthorizedGet<MetaTubeResponse<MetaTubeMovieResource>>("/movies/fc2hub/1312343-3131319");
        }

        [Test]
        public void should_use_dmm_images_for_javbus_covers_with_dmm_previews()
        {
            GivenJavBusDmmSearchResponse();
            GivenJavBusDmmDetailResponse();

            var movie = Subject.SearchForNewMovie("IDBD-894").Single();
            var metadataId = MetaTubeIdMapper.ToMetadataId("javbus", "idbd-894");
            const string dmmCover = "https://pics.dmm.co.jp/digital/video/idbd00894/idbd00894pl.jpg";
            const string dmmFanart = "https://pics.dmm.co.jp/digital/video/idbd00894/idbd00894jp-1.jpg";

            movie.TmdbId.Should().Be(metadataId);
            movie.MovieMetadata.Value.Images.Should().ContainSingle(i => i.CoverType == MediaCoverTypes.Poster && i.RemoteUrl == dmmCover);
            movie.MovieMetadata.Value.Images.Should().ContainSingle(i => i.CoverType == MediaCoverTypes.Fanart && i.RemoteUrl == dmmFanart);

            var detail = Subject.GetMovieInfo(metadataId).Item1;

            detail.Images.Should().ContainSingle(i => i.CoverType == MediaCoverTypes.Poster && i.RemoteUrl == dmmCover);
            detail.Images.Should().ContainSingle(i => i.CoverType == MediaCoverTypes.Fanart && i.RemoteUrl == dmmFanart);

            VerifyAuthorizedGet<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=IDBD-894");
            VerifyAuthorizedGet<MetaTubeResponse<MetaTubeMovieResource>>("/movies/javbus/idbd-894");
        }

        [Test]
        public void should_resolve_cast_credit_to_metatube_actor_identity()
        {
            GivenIpx159SearchResponse();
            GivenIpx159DetailResponse();
            GivenMinamiAizawaActorSearchResponse();

            var movie = Subject.SearchForNewMovie("IPX-159").Single();
            var metadataId = MetaTubeIdMapper.ToMetadataId("javbus", "ipx-159");

            movie.TmdbId.Should().Be(metadataId);

            var credits = Subject.GetMovieInfo(metadataId).Item2;
            var credit = credits.Single();

            credit.Name.Should().Be("Minami Aizawa");
            credit.CreditTmdbId.Should().Be("javbus:ipx-159:actor:av-league:minami-aizawa");
            credit.PersonTmdbId.Should().Be(MetaTubeIdMapper.ToMetadataId("av-league", "minami-aizawa"));
            credit.Images.Should().ContainSingle(i => i.RemoteUrl == "https://example.test/minami.jpg");

            VerifyAuthorizedGet<MetaTubeResponse<List<MetaTubeActorResource>>>("/actors/search?q=Minami%20Aizawa");
        }

        [Test]
        public void should_map_movie_from_metatube_website_identity()
        {
            GivenIpx159DetailResponse();

            var metadataId = MetaTubeIdMapper.ToMetadataId("javbus", "ipx-159");
            var metadata = Subject.MapMovieToTmdbMovie(new MovieMetadata
            {
                TmdbId = metadataId,
                Title = "IPX-159",
                Website = "metatube:javbus:ipx-159"
            });

            metadata.Should().NotBeNull();
            metadata.TmdbId.Should().Be(metadataId);
            metadata.Title.Should().Be("IPX-159 Example IPX Title");

            VerifyAuthorizedGet<MetaTubeResponse<MetaTubeMovieResource>>("/movies/javbus/ipx-159");
        }

        private static string GetMovieNumber(MovieMetadata metadata)
        {
            return metadata.GetType().GetProperty("Number")?.GetValue(metadata) as string;
        }

        private static void ShouldHaveDefaultTmdbRating(Ratings ratings)
        {
            ratings.Tmdb.Should().NotBeNull();
            ratings.Tmdb.Value.Should().Be(0);
            ratings.Tmdb.Votes.Should().Be(0);
            ratings.Tmdb.Type.Should().Be(RatingType.User);
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

        private void GivenFc2HubSearchResponse()
        {
            const string json = @"{
              ""data"": [
                {
                  ""id"": ""1312343-3131319"",
                  ""provider"": ""fc2hub"",
                  ""number"": ""FC2-3131319"",
                  ""title"": ""Example FC2 Title"",
                  ""summary"": ""Example summary"",
                  ""runtime"": 120,
                  ""release_date"": ""2022-11-18"",
                  ""cover_url"": ""https://storage72000.contents.fc2.com/file/383/38262697/1668762729.1.png"",
                  ""thumb_url"": ""https://storage72000.contents.fc2.com/file/383/38262697/1668762729.1.png"",
                  ""actors"": [],
                  ""genres"": [],
                  ""maker"": ""FC2""
                }
              ]
            }";

            GivenResponse<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=FC2-PPV-3131319", json);
        }

        private void GivenFc2HubDetailResponse()
        {
            const string json = @"{
              ""data"": {
                ""id"": ""1312343-3131319"",
                ""provider"": ""fc2hub"",
                ""number"": ""FC2-3131319"",
                ""title"": ""Example FC2 Title"",
                ""summary"": ""Example summary"",
                ""runtime"": 120,
                ""release_date"": ""2022-11-18"",
                ""cover_url"": ""https://storage72000.contents.fc2.com/file/383/38262697/1668762729.1.png"",
                ""thumb_url"": ""https://storage72000.contents.fc2.com/file/383/38262697/1668762729.1.png"",
                ""actors"": [],
                ""genres"": [],
                ""maker"": ""FC2""
              }
            }";

            GivenResponse<MetaTubeResponse<MetaTubeMovieResource>>("/movies/fc2hub/1312343-3131319", json);
        }

        private void GivenJavBusDmmSearchResponse()
        {
            const string json = @"{
              ""data"": [
                {
                  ""id"": ""idbd-894"",
                  ""provider"": ""JavBus"",
                  ""number"": ""IDBD-894"",
                  ""title"": ""Example JavBus Title"",
                  ""summary"": ""Example summary"",
                  ""runtime"": 120,
                  ""release_date"": ""2023-06-13"",
                  ""cover_url"": ""https://www.javbus.com/pics/cover/9s92_b.jpg"",
                  ""thumb_url"": ""https://www.javbus.com/pics/thumb/9s92.jpg"",
                  ""preview_images"": [""https://pics.dmm.co.jp/digital/video/idbd00894/idbd00894jp-1.jpg""],
                  ""actors"": [""Minami Aizawa""],
                  ""genres"": [""Drama""],
                  ""maker"": ""Idea Pocket""
                }
              ]
            }";

            GivenResponse<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=IDBD-894", json);
        }

        private void GivenJavBusDmmDetailResponse()
        {
            const string json = @"{
              ""data"": {
                ""id"": ""idbd-894"",
                ""provider"": ""JavBus"",
                ""number"": ""IDBD-894"",
                ""title"": ""Example JavBus Title"",
                ""summary"": ""Example summary"",
                ""runtime"": 120,
                ""release_date"": ""2023-06-13"",
                ""cover_url"": ""https://www.javbus.com/pics/cover/9s92_b.jpg"",
                ""thumb_url"": ""https://www.javbus.com/pics/thumb/9s92.jpg"",
                ""preview_images"": [""https://pics.dmm.co.jp/digital/video/idbd00894/idbd00894jp-1.jpg""],
                ""actors"": [""Minami Aizawa""],
                ""genres"": [""Drama""],
                ""maker"": ""Idea Pocket""
              }
            }";

            GivenResponse<MetaTubeResponse<MetaTubeMovieResource>>("/movies/javbus/idbd-894", json);
        }

        private void GivenIpx159SearchResponse()
        {
            const string json = @"{
              ""data"": [
                {
                  ""id"": ""ipx-159"",
                  ""provider"": ""javbus"",
                  ""number"": ""IPX-159"",
                  ""title"": ""Example IPX Title"",
                  ""summary"": ""Example summary"",
                  ""runtime"": 120,
                  ""release_date"": ""2018-01-02"",
                  ""cover_url"": ""https://example.test/ipx-cover.jpg"",
                  ""thumb_url"": ""https://example.test/ipx-thumb.jpg"",
                  ""actors"": [""Minami Aizawa""],
                  ""genres"": [""Drama""],
                  ""maker"": ""Idea Pocket""
                }
              ]
            }";

            GivenResponse<MetaTubeResponse<List<MetaTubeMovieResource>>>("/movies/search?q=IPX-159", json);
        }

        private void GivenIpx159DetailResponse()
        {
            const string json = @"{
              ""data"": {
                ""id"": ""ipx-159"",
                ""provider"": ""javbus"",
                ""number"": ""IPX-159"",
                ""title"": ""Example IPX Title"",
                ""summary"": ""Example summary"",
                ""runtime"": 120,
                ""release_date"": ""2018-01-02"",
                ""cover_url"": ""https://example.test/ipx-cover.jpg"",
                ""thumb_url"": ""https://example.test/ipx-thumb.jpg"",
                ""actors"": [""Minami Aizawa""],
                ""genres"": [""Drama""],
                ""maker"": ""Idea Pocket""
              }
            }";

            GivenResponse<MetaTubeResponse<MetaTubeMovieResource>>("/movies/javbus/ipx-159", json);
        }

        private void GivenMinamiAizawaActorSearchResponse()
        {
            const string json = @"{
              ""data"": [
                {
                  ""id"": ""minami-aizawa"",
                  ""provider"": ""av-league"",
                  ""name"": ""Minami Aizawa"",
                  ""aliases"": [""Aizawa Minami""],
                  ""images"": [""https://example.test/minami.jpg""]
                }
              ]
            }";

            GivenResponse<MetaTubeResponse<List<MetaTubeActorResource>>>("/actors/search?q=Minami%20Aizawa", json);
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
