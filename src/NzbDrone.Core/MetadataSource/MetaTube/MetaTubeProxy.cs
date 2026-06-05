using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Collections;
using NzbDrone.Core.Movies.Credits;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.MetadataSource.MetaTube
{
    public class MetaTubeProxy : IProvideMovieInfo, ISearchForNewMovie
    {
        private const string WebsitePrefix = "metatube:";

        private readonly IHttpClient _httpClient;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IMovieService _movieService;
        private readonly IMovieMetadataService _movieMetadataService;
        private readonly Logger _logger;
        private readonly Dictionary<int, string> _metadataIdentities;

        public MetaTubeProxy(IHttpClient httpClient,
                             IConfigFileProvider configFileProvider,
                             IMovieService movieService,
                             IMovieMetadataService movieMetadataService,
                             Logger logger)
        {
            _httpClient = httpClient;
            _configFileProvider = configFileProvider;
            _movieService = movieService;
            _movieMetadataService = movieMetadataService;
            _logger = logger;
            _metadataIdentities = new Dictionary<int, string>();
        }

        public List<Movie> SearchForNewMovie(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return new List<Movie>();
            }

            try
            {
                var request = BuildRequest("movies/search");
                request.Url = request.Url.AddQueryParam("q", title.Trim());

                var response = _httpClient.Get<MetaTubeResponse<List<MetaTubeMovieResource>>>(request);
                var resources = response.Resource.Data ?? new List<MetaTubeMovieResource>();

                return resources
                    .Where(r => r.Provider.IsNotNullOrWhiteSpace() && r.Id.IsNotNullOrWhiteSpace())
                    .Select(MapSearchResult)
                    .ToList();
            }
            catch (WebException ex)
            {
                _logger.Warn(ex);
                throw;
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex);
                throw;
            }
        }

        public Tuple<MovieMetadata, List<Credit>> GetMovieInfo(int tmdbId)
        {
            if (!TryResolveIdentity(tmdbId, out var provider, out var id))
            {
                throw new MovieNotFoundException(tmdbId, "Movie with MetaTube metadata id {0} was not found.", tmdbId);
            }

            var resource = GetMovieResource(provider, id);
            var metadata = MapMovie(resource);
            var credits = MapCredits(resource).ToList();

            return new Tuple<MovieMetadata, List<Credit>>(metadata, credits);
        }

        public MovieMetadata GetMovieByImdbId(string imdbId)
        {
            return null;
        }

        public MovieCollection GetCollectionInfo(int tmdbId)
        {
            throw new MovieNotFoundException(tmdbId, "MetaTube collections are not supported.");
        }

        public List<MovieMetadata> GetBulkMovieInfo(List<int> tmdbIds)
        {
            var movies = new List<MovieMetadata>();

            foreach (var tmdbId in tmdbIds)
            {
                try
                {
                    movies.Add(GetMovieInfo(tmdbId).Item1);
                }
                catch (MovieNotFoundException)
                {
                }
            }

            return movies;
        }

        public List<MovieMetadata> GetTrendingMovies()
        {
            return new List<MovieMetadata>();
        }

        public List<MovieMetadata> GetPopularMovies()
        {
            return new List<MovieMetadata>();
        }

        public HashSet<int> GetChangedMovies(DateTime startTime)
        {
            return new HashSet<int>();
        }

        public MovieMetadata MapMovieToTmdbMovie(MovieMetadata movie)
        {
            if (movie == null)
            {
                return null;
            }

            try
            {
                if (movie.TmdbId > 0)
                {
                    var existing = _movieMetadataService.FindByTmdbId(movie.TmdbId);

                    return existing ?? GetMovieInfo(movie.TmdbId).Item1;
                }

                var year = movie.Year > 1900 ? $" {movie.Year}" : string.Empty;
                return SearchForNewMovie(movie.Title + year).FirstOrDefault()?.MovieMetadata.Value;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Couldn't map movie {0} to a movie on MetaTube. It will not be added.", movie.Title);
                return null;
            }
        }

        private Movie MapSearchResult(MetaTubeMovieResource resource)
        {
            RememberIdentity(resource);

            var metadataId = MetaTubeIdMapper.ToMetadataId(resource.Provider, resource.Id);
            var existing = _movieService.FindByTmdbId(metadataId);

            if (existing != null)
            {
                return existing;
            }

            return new Movie { MovieMetadata = MapMovie(resource) };
        }

        private MetaTubeMovieResource GetMovieResource(string provider, string id)
        {
            var request = BuildRequest($"movies/{provider}/{id}");
            var response = _httpClient.Get<MetaTubeResponse<MetaTubeMovieResource>>(request);

            if (response.HasHttpError)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new MovieNotFoundException(MetaTubeIdMapper.ToMetadataId(provider, id));
                }

                throw new HttpException(request, response);
            }

            if (response.Resource.Data == null)
            {
                throw new MovieNotFoundException(MetaTubeIdMapper.ToMetadataId(provider, id), "MetaTube returned an empty movie detail response.");
            }

            RememberIdentity(response.Resource.Data);

            return response.Resource.Data;
        }

        private MovieMetadata MapMovie(MetaTubeMovieResource resource)
        {
            RememberIdentity(resource);

            var title = BuildTitle(resource);
            var metadataId = MetaTubeIdMapper.ToMetadataId(resource.Provider, resource.Id);
            var releaseDate = ParseReleaseDate(resource.ReleaseDate);

            var movie = new MovieMetadata
            {
                TmdbId = metadataId,
                ImdbId = null,
                Title = title,
                OriginalTitle = title,
                CleanTitle = title.CleanMovieTitle(),
                SortTitle = MovieTitleNormalizer.Normalize(title, metadataId),
                CleanOriginalTitle = title.CleanMovieTitle(),
                Overview = resource.Summary,
                Website = WebsitePrefix + MetaTubeIdMapper.ToExternalId(resource.Provider, resource.Id),
                InCinemas = releaseDate,
                PhysicalRelease = releaseDate,
                DigitalRelease = releaseDate,
                Year = releaseDate?.Year ?? 0,
                Runtime = resource.Runtime ?? 0,
                Studio = resource.Maker,
                Genres = resource.Genres ?? new List<string>(),
                Keywords = new List<string>(),
                Images = MapImages(resource).ToList(),
                Recommendations = new List<int>(),
                Ratings = new Ratings
                {
                    Tmdb = new RatingChild
                    {
                        Type = RatingType.User
                    }
                },
                Status = releaseDate.HasValue && releaseDate.Value <= DateTime.UtcNow ? MovieStatusType.Released : MovieStatusType.Announced,
                OriginalLanguage = Language.Japanese
            };

            return movie;
        }

        private static IEnumerable<Credit> MapCredits(MetaTubeMovieResource resource)
        {
            return (resource.Actors ?? new List<string>())
                .Where(a => a.IsNotNullOrWhiteSpace())
                .Select((actor, index) => new Credit
                {
                    Name = actor,
                    CreditTmdbId = $"{MetaTubeIdMapper.ToExternalId(resource.Provider, resource.Id)}:actor:{index}",
                    PersonTmdbId = MetaTubeIdMapper.ToMetadataId("actor", actor),
                    Type = CreditType.Cast,
                    Character = actor,
                    Order = index
                });
        }

        private static IEnumerable<MediaCover.MediaCover> MapImages(MetaTubeMovieResource resource)
        {
            if (resource.CoverUrl.IsNotNullOrWhiteSpace())
            {
                yield return new MediaCover.MediaCover(MediaCoverTypes.Poster, resource.CoverUrl);
            }

            if (resource.ThumbUrl.IsNotNullOrWhiteSpace())
            {
                yield return new MediaCover.MediaCover(MediaCoverTypes.Fanart, resource.ThumbUrl);
            }
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

        private HttpRequest BuildRequest(string resource)
        {
            var request = new HttpRequestBuilder(_configFileProvider.MetaTubeUrl)
                .Resource(resource)
                .Build();

            var token = _configFileProvider.MetaTubeToken;

            if (token.IsNotNullOrWhiteSpace())
            {
                request.Headers.Set("Authorization", $"Bearer {token}");
            }

            request.AllowAutoRedirect = true;

            return request;
        }

        private void RememberIdentity(MetaTubeMovieResource resource)
        {
            if (resource.Provider.IsNullOrWhiteSpace() || resource.Id.IsNullOrWhiteSpace())
            {
                return;
            }

            _metadataIdentities[MetaTubeIdMapper.ToMetadataId(resource.Provider, resource.Id)] = MetaTubeIdMapper.ToExternalId(resource.Provider, resource.Id);
        }

        private bool TryResolveIdentity(int metadataId, out string provider, out string id)
        {
            if (_metadataIdentities.TryGetValue(metadataId, out var identity) &&
                TryParseIdentity(identity, out provider, out id))
            {
                return true;
            }

            var movie = _movieService.FindByTmdbId(metadataId);

            if (TryParseWebsite(movie?.MovieMetadata.Value.Website, out provider, out id))
            {
                _metadataIdentities[metadataId] = MetaTubeIdMapper.ToExternalId(provider, id);
                return true;
            }

            var metadata = _movieMetadataService.FindByTmdbId(metadataId);

            if (TryParseWebsite(metadata?.Website, out provider, out id))
            {
                _metadataIdentities[metadataId] = MetaTubeIdMapper.ToExternalId(provider, id);
                return true;
            }

            provider = null;
            id = null;
            return false;
        }

        private static bool TryParseWebsite(string website, out string provider, out string id)
        {
            provider = null;
            id = null;

            if (website.IsNullOrWhiteSpace() || !website.StartsWith(WebsitePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return TryParseIdentity(website.Substring(WebsitePrefix.Length), out provider, out id);
        }

        private static bool TryParseIdentity(string identity, out string provider, out string id)
        {
            provider = null;
            id = null;

            if (identity.IsNullOrWhiteSpace())
            {
                return false;
            }

            var parts = identity.Split(':');

            if (parts.Length != 2 || parts[0].IsNullOrWhiteSpace() || parts[1].IsNullOrWhiteSpace())
            {
                return false;
            }

            provider = parts[0];
            id = parts[1];
            return true;
        }

        private static DateTime? ParseReleaseDate(string releaseDate)
        {
            if (!DateTime.TryParse(releaseDate, out var parsed))
            {
                return null;
            }

            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }
    }
}
