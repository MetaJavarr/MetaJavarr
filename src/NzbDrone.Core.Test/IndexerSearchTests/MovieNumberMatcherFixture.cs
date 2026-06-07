using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class MovieNumberMatcherFixture : CoreTest
    {
        [TestCase("FC2-PPV-1517552", "FC2-1517552", "FC2-PPV-1517552")]
        [TestCase("IPZ-910-UC", "IPZ-910", "IPZ-910")]
        [TestCase("ipvr00167pl", "IPVR-167", "IPVR-167")]
        [TestCase("bbs2048.org@ipvr00128.part1", "IPVR-128", "IPVR-128")]
        public void should_match_movie_number_variants(string title, string movieNumber, string expectedMatch)
        {
            var movie = new Movie();
            movie.MovieMetadata.Value.Number = movieNumber;

            MovieNumberMatcher.TryGetMatch(title, movie, out var matchedNumber).Should().BeTrue();
            matchedNumber.Should().Be(expectedMatch);
        }
    }
}
