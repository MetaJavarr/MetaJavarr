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
            var actor = FetchActor(settings);
            var actorNames = GetActorNames(actor, settings).ToList();
            var response = _httpClient.Get<MetaTubeResponse<List<MetaTubeMovieResource>>>(BuildMovieSearchRequest(actorNames.First()));
            var resources = response.Resource.Data ?? new List<MetaTubeMovieResource>();

            return resources
                .Select(r => GetActorMatchedMovie(r, actorNames))
                .Where(r => r != null)
                .Select(MapMovie)
                .GroupBy(m => m.TmdbId)
                .Select(g => g.First())
                .ToList();
        }

        private MetaTubeActorResource FetchActor(MetaTubeActressSettings settings)
        {
            var response = _httpClient.Get<MetaTubeResponse<MetaTubeActorResource>>(BuildActorRequest(settings));

            if (response.Resource.Data == null)
            {
                throw new InvalidOperationException("MetaTube returned an empty actor detail response.");
            }

            return response.Resource.Data;
        }

        private HttpRequest BuildActorRequest(MetaTubeActressSettings settings)
        {
            return BuildRequest($"actors/{settings.Provider.Trim()}/{settings.ActorId.Trim()}").Build();
        }

        private HttpRequest BuildMovieSearchRequest(string actorName)
        {
            return BuildRequest("movies/search")
                .AddQueryParam("q", actorName)
                .Build();
        }

        private HttpRequest BuildMovieDetailRequest(MetaTubeMovieResource resource)
        {
            return BuildRequest($"movies/{resource.Provider.Trim()}/{resource.Id.Trim()}").Build();
        }

        private HttpRequestBuilder BuildRequest(string resource)
        {
            var requestBuilder = new HttpRequestBuilder(_configFileProvider.MetaTubeUrl)
                .Resource(resource)
                .Accept(HttpAccept.Json);

            var token = _configFileProvider.MetaTubeToken;

            if (token.IsNotNullOrWhiteSpace())
            {
                requestBuilder.SetHeader("Authorization", $"Bearer {token}");
            }

            requestBuilder.AllowAutoRedirect = true;

            return requestBuilder;
        }

        private static IEnumerable<string> GetActorNames(MetaTubeActorResource actor, MetaTubeActressSettings settings)
        {
            if (actor.Name.IsNotNullOrWhiteSpace())
            {
                yield return actor.Name.Trim();
            }

            if (settings.ActressName.IsNotNullOrWhiteSpace())
            {
                yield return settings.ActressName.Trim();
            }

            foreach (var alias in actor.Aliases ?? Array.Empty<string>())
            {
                if (alias.IsNotNullOrWhiteSpace())
                {
                    yield return alias.Trim();
                }
            }
        }

        private static bool MatchesActor(MetaTubeMovieResource resource, IEnumerable<string> actorNames)
        {
            if (resource == null)
            {
                return false;
            }

            var names = new HashSet<string>(actorNames, StringComparer.OrdinalIgnoreCase);

            return resource.Actors != null &&
                   resource.Actors.Any(actor => actor.IsNotNullOrWhiteSpace() && names.Contains(actor.Trim()));
        }

        private MetaTubeMovieResource GetActorMatchedMovie(MetaTubeMovieResource resource, IEnumerable<string> actorNames)
        {
            if (HasActorMetadata(resource))
            {
                return MatchesActor(resource, actorNames) ? resource : null;
            }

            var detail = FetchMovie(resource);

            return MatchesActor(detail, actorNames) ? detail : null;
        }

        private MetaTubeMovieResource FetchMovie(MetaTubeMovieResource resource)
        {
            var response = _httpClient.Get<MetaTubeResponse<MetaTubeMovieResource>>(BuildMovieDetailRequest(resource));

            return response.Resource.Data;
        }

        private static bool HasActorMetadata(MetaTubeMovieResource resource)
        {
            return resource.Actors != null &&
                   resource.Actors.Any(actor => actor.IsNotNullOrWhiteSpace());
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
