using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.ImportListMovies;
using NzbDrone.Core.MetadataSource.MetaTube;

namespace NzbDrone.Core.ImportLists.MetaTube
{
    public class MetaTubeActressImportProxy
    {
        private const string WebsitePrefix = "metatube:";

        private readonly IHttpClient _httpClient;
        private readonly IConfigFileProvider _configFileProvider;

        public MetaTubeActressImportProxy(IHttpClient httpClient, IConfigFileProvider configFileProvider)
        {
            _httpClient = httpClient;
            _configFileProvider = configFileProvider;
        }

        public List<ImportListMovie> Fetch(MetaTubeActressSettings settings)
        {
            var request = BuildRequest(settings.ActressName);
            var response = _httpClient.Get<MetaTubeResponse<List<MetaTubeMovieResource>>>(request);
            var resources = response.Resource.Data ?? new List<MetaTubeMovieResource>();

            return resources
                .Where(r => MatchesActress(r, settings.ActressName))
                .Select(MapMovie)
                .ToList();
        }

        private HttpRequest BuildRequest(string actressName)
        {
            var request = new HttpRequestBuilder(_configFileProvider.MetaTubeUrl)
                .Resource("movies/search")
                .AddQueryParam("q", actressName)
                .Accept(HttpAccept.Json)
                .Build();

            var token = _configFileProvider.MetaTubeToken;

            if (token.IsNotNullOrWhiteSpace())
            {
                request.Headers.Set("Authorization", $"Bearer {token}");
            }

            request.AllowAutoRedirect = true;

            return request;
        }

        private static bool MatchesActress(MetaTubeMovieResource resource, string actressName)
        {
            return resource.Actors != null &&
                   resource.Actors.Any(actor => actor.Equals(actressName, StringComparison.OrdinalIgnoreCase));
        }

        private static ImportListMovie MapMovie(MetaTubeMovieResource resource)
        {
            var movie = new ImportListMovie
            {
                Title = BuildTitle(resource),
                TmdbId = MetaTubeIdMapper.ToMetadataId(resource.Provider, resource.Id),
                Year = ParseReleaseYear(resource.ReleaseDate)
            };

            movie.MovieMetadata.Value.Website = WebsitePrefix + MetaTubeIdMapper.ToExternalId(resource.Provider, resource.Id);

            return movie;
        }

        private static string BuildTitle(MetaTubeMovieResource resource)
        {
            if (resource.Number.IsNotNullOrWhiteSpace() && resource.Title.IsNotNullOrWhiteSpace())
            {
                return $"{resource.Number.Trim()} {resource.Title.Trim()}";
            }

            if (resource.Number.IsNotNullOrWhiteSpace())
            {
                return resource.Number.Trim();
            }

            return resource.Title?.Trim();
        }

        private static int ParseReleaseYear(string releaseDate)
        {
            return DateTime.TryParse(releaseDate, out var parsed) ? parsed.Year : 0;
        }
    }
}
