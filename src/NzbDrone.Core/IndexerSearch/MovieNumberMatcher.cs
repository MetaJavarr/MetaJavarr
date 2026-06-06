using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Movies;

namespace NzbDrone.Core.IndexerSearch
{
    public static class MovieNumberMatcher
    {
        private static readonly Regex Fc2NumberRegex = new Regex(@"^FC2-(?<id>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string GetSearchNumber(Movie movie)
        {
            return GetSearchNumber(movie?.MovieMetadata?.Value?.Number);
        }

        public static string GetSearchNumber(string number)
        {
            number = number?.Trim();

            if (number.IsNullOrWhiteSpace())
            {
                return number;
            }

            var fc2Match = Fc2NumberRegex.Match(number);

            if (fc2Match.Success)
            {
                return $"FC2-PPV-{fc2Match.Groups["id"].Value}";
            }

            return number;
        }

        public static bool TryGetMatch(string title, Movie movie, out string matchedNumber)
        {
            matchedNumber = null;
            var movieNumber = movie?.MovieMetadata?.Value?.Number;

            if (title.IsNullOrWhiteSpace() || movieNumber.IsNullOrWhiteSpace())
            {
                return false;
            }

            foreach (var candidate in GetNumberCandidates(movieNumber))
            {
                if (title.IndexOf(candidate, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    matchedNumber = candidate;
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> GetNumberCandidates(string number)
        {
            number = number?.Trim();

            if (number.IsNullOrWhiteSpace())
            {
                return Enumerable.Empty<string>();
            }

            return new[] { number, GetSearchNumber(number) }
                .Where(candidate => candidate.IsNotNullOrWhiteSpace())
                .Distinct(StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
