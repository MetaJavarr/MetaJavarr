using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.AlternativeTitles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MovieTests.MovieServiceTests
{
    [TestFixture]
    public class FindByTitleFixture : CoreTest<MovieService>
    {
        private List<Movie> _candidates;

        [SetUp]
        public void Setup()
        {
            _candidates = Builder<Movie>.CreateListOfSize(3)
                                        .TheFirst(1)
                                        .With(x => x.MovieMetadata.Value.CleanTitle = "batman")
                                        .With(x => x.Year = 2000)
                                        .TheNext(1)
                                        .With(x => x.MovieMetadata.Value.CleanTitle = "batman")
                                        .With(x => x.Year = 1999)
                                        .TheRest()
                                        .With(x => x.MovieMetadata.Value.CleanTitle = "darkknight")
                                        .With(x => x.Year = 2008)
                                        .With(x => x.MovieMetadata.Value.AlternativeTitles = new List<AlternativeTitle>
                                        {
                                            new AlternativeTitle
                                            {
                                                CleanTitle = "batman"
                                            }
                                        })
                                        .Build()
                                        .ToList();
        }

        [Test]
        public void should_find_by_title_year()
        {
            var movie = Subject.FindByTitle(new List<string> { "batman" }, 2000, new List<string>(), _candidates);

            movie.Should().NotBeNull();
            movie.Year.Should().Be(2000);
        }

        [Test]
        public void should_find_candidates_by_alt_titles()
        {
            var movie = Subject.FindByTitle(new List<string> { "batman" }, 2008, new List<string>(), _candidates);
            movie.Should().NotBeNull();
            movie.Year.Should().Be(2008);
        }

        [Test]
        public void should_find_by_movie_number_from_candidates()
        {
            var movie = Builder<Movie>.CreateNew()
                                      .With(x => x.MovieMetadata.Value.Number = "FC2-1517552")
                                      .With(x => x.MovieMetadata.Value.CleanTitle = "unrelatedtitle")
                                      .Build();

            var result = Subject.FindByTitle(new List<string> { "FC2-PPV-1517552" }, null, new List<string>(), new List<Movie> { movie });

            result.Should().Be(movie);
        }

        [Test]
        public void should_find_by_title_using_movie_number_candidates()
        {
            var movie = Builder<Movie>.CreateNew()
                                      .With(x => x.MovieMetadata.Value.Number = "FC2-1517552")
                                      .With(x => x.MovieMetadata.Value.CleanTitle = "unrelatedtitle")
                                      .Build();

            Mocker.GetMock<IMovieRepository>()
                  .Setup(s => s.FindByTitles(It.IsAny<List<string>>()))
                  .Returns(new List<Movie>());

            Mocker.GetMock<IMovieRepository>()
                  .Setup(s => s.FindByMovieNumbers(It.Is<List<string>>(numbers => numbers.Contains("FC2-1517552"))))
                  .Returns(new List<Movie> { movie });

            var result = Subject.FindByTitle("FC2-PPV-1517552");

            result.Should().Be(movie);
        }
    }
}
