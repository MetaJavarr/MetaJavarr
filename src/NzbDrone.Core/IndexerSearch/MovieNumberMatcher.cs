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
        private static readonly Regex Fc2TitleNumberRegex = new Regex(@"\bFC2(?:[-_. ]?PPV)?[-_. ]?(?<id>\d{4,8})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex JavNumberRegex = new Regex(@"^(?<prefix>[A-Z]{2,6})-(?<id>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex JavTitleNumberRegex = new Regex(@"(?<prefix>[A-Z]{2,6})[-_. ]?0*(?<id>\d{2,7})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            var movieNumberCandidates = GetNumberCandidates(movieNumber).ToList();

            foreach (var candidate in movieNumberCandidates)
            {
                if (title.IndexOf(candidate, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    matchedNumber = candidate;
                    return true;
                }
            }

            var titleNumberCandidates = GetNumberCandidatesFromTitle(title).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            foreach (var candidate in movieNumberCandidates)
            {
                if (titleNumberCandidates.Contains(candidate))
                {
                    matchedNumber = candidate;
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<string> GetNumberCandidatesFromTitle(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return Enumerable.Empty<string>();
            }

            var candidates = new List<string>();

            foreach (Match match in Fc2TitleNumberRegex.Matches(title))
            {
                candidates.AddRange(GetNumberCandidates($"FC2-{match.Groups["id"].Value}"));
            }

            foreach (Match match in JavTitleNumberRegex.Matches(title))
            {
                var prefix = match.Groups["prefix"].Value.ToUpperInvariant();

                if (prefix == "PPV")
                {
                    continue;
                }

                var rawId = match.Groups["id"].Value;
                var trimmedId = rawId.TrimStart('0');

                if (trimmedId.IsNullOrWhiteSpace())
                {
                    trimmedId = "0";
                }

                candidates.Add($"{prefix}-{rawId}");
                candidates.Add($"{prefix}-{trimmedId}");
            }

            return candidates
                .Where(candidate => candidate.IsNotNullOrWhiteSpace())
                .Distinct(StringComparer.InvariantCultureIgnoreCase);
        }

        public static IEnumerable<string> GetNumberCandidates(string number)
        {
            number = number?.Trim();

            if (number.IsNullOrWhiteSpace())
            {
                return Enumerable.Empty<string>();
            }

            var normalizedNumber = NormalizeJavNumber(number);

            return new[] { number, normalizedNumber, GetSearchNumber(normalizedNumber) }
                .Where(candidate => candidate.IsNotNullOrWhiteSpace())
                .Distinct(StringComparer.InvariantCultureIgnoreCase);
        }

        private static string NormalizeJavNumber(string number)
        {
            var match = JavNumberRegex.Match(number);

            if (!match.Success)
            {
                return number;
            }

            var id = match.Groups["id"].Value.TrimStart('0');

            if (id.IsNullOrWhiteSpace())
            {
                id = "0";
            }

            return $"{match.Groups["prefix"].Value.ToUpperInvariant()}-{id}";
        }
    }
}
