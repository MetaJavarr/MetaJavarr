using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.MetaTube;

namespace NzbDrone.Core.Test.MetadataSource.MetaTube
{
    [TestFixture]
    public class MetaTubeMovieResourceFixture
    {
        [Test]
        public void should_deserialize_movie_detail_response()
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

            var response = JsonConvert.DeserializeObject<MetaTubeResponse<MetaTubeMovieResource>>(json);
            var resource = response.Data;

            resource.Provider.Should().Be("javbus");
            resource.Id.Should().Be("ipzz-562");
            resource.Number.Should().Be("IPZZ-562");
            resource.Title.Should().Be("Example Title");
            resource.CoverUrl.Should().Be("https://example.test/cover.jpg");
            resource.ThumbUrl.Should().Be("https://example.test/thumb.jpg");
            resource.Actors.Should().ContainSingle(a => a == "Example Actress");
            resource.Genres.Should().ContainSingle(g => g == "Drama");
            resource.Maker.Should().Be("Example Studio");
        }
    }
}
