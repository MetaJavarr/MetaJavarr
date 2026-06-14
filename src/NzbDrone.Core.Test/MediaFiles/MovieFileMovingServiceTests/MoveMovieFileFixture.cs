using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MovieFileMovingServiceTests
{
    [TestFixture]
    public class MoveMovieFileFixture : CoreTest<MovieFileMovingService>
    {
        private Movie _movie;
        private MovieFile _movieFile;
        private LocalMovie _localMovie;

        [SetUp]
        public void Setup()
        {
            _movie = Builder<Movie>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\Movies\Movie".AsOsAgnostic())
                                     .Build();

            _movieFile = Builder<MovieFile>.CreateNew()
                                               .With(f => f.Path = null)
                                               .With(f => f.RelativePath = @"File.avi")
                                               .Build();

            _localMovie = Builder<LocalMovie>.CreateNew()
                                                 .With(l => l.Movie = _movie)
                                                 .Build();

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildFileName(It.IsAny<Movie>(), It.IsAny<MovieFile>(), null, It.IsAny<List<CustomFormat>>()))
                  .Returns("File Name");

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildFilePath(It.IsAny<Movie>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(@"C:\Test\Movies\Movie\File Name.avi".AsOsAgnostic());

            var rootFolder = @"C:\Test\Movies\".AsOsAgnostic();

            Mocker.GetMock<IRootFolderService>()
                .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>(), null))
                .Returns(rootFolder);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(rootFolder))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(It.IsAny<string>()))
                  .Returns(true);
        }

        [Test]
        public void should_catch_UnauthorizedAccessException_during_folder_inheritance()
        {
            WindowsOnly();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.InheritFolderPermissions(It.IsAny<string>()))
                  .Throws<UnauthorizedAccessException>();

            Subject.MoveMovieFile(_movieFile, _localMovie);
        }

        [Test]
        public void should_catch_InvalidOperationException_during_folder_inheritance()
        {
            WindowsOnly();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.InheritFolderPermissions(It.IsAny<string>()))
                  .Throws<InvalidOperationException>();

            Subject.MoveMovieFile(_movieFile, _localMovie);
        }

        [Test]
        public void should_notify_on_movie_folder_creation()
        {
            Subject.MoveMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<MovieFolderCreatedEvent>(It.Is<MovieFolderCreatedEvent>(p =>
                      p.MovieFolder.IsNotNullOrWhiteSpace())), Times.Once());
        }

        [Test]
        public void should_not_notify_if_movie_folder_already_exists()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_movie.Path))
                  .Returns(true);

            Subject.MoveMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<MovieFolderCreatedEvent>(It.Is<MovieFolderCreatedEvent>(p =>
                      p.MovieFolder.IsNotNullOrWhiteSpace())), Times.Never());
        }

        [Test]
        public void should_append_jellyfin_part_suffix_when_moving_other_video_files()
        {
            _localMovie.OtherVideoFiles = true;
            _localMovie.Path = @"C:\Test\Unsorted\FC2-PPV-3308060\hhd800.com@FC2-PPV-3308060_1.mp4".AsOsAgnostic();
            _movie.MovieMetadata.Value.Number = "FC2-3308060";
            _movieFile.Path = _localMovie.Path;

            Subject.MoveMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IBuildFileNames>()
                  .Verify(v => v.BuildFilePath(_movie, "File Name-part1", ".mp4"), Times.Once());
        }

        [Test]
        public void should_append_jellyfin_part_suffix_when_source_has_site_prefix_and_dot_part()
        {
            _localMovie.OtherVideoFiles = true;
            _localMovie.Path = @"C:\Test\Unsorted\ipvr00167pl\fbzip.com@ipvr00167.part2.mp4".AsOsAgnostic();
            _movie.MovieMetadata.Value.Number = "IPVR-167";
            _movieFile.Path = _localMovie.Path;

            Subject.MoveMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IBuildFileNames>()
                  .Verify(v => v.BuildFilePath(_movie, "File Name-part2", ".mp4"), Times.Once());
        }

        [Test]
        public void should_not_append_jellyfin_part_suffix_from_tracker_site_prefix_number()
        {
            _localMovie.OtherVideoFiles = true;
            _localMovie.Path = @"C:\Test\Unsorted\IPZZ-562\hhd800.com@IPZZ-562.mp4".AsOsAgnostic();
            _movie.MovieMetadata.Value.Number = "IPZZ-562";
            _movieFile.Path = _localMovie.Path;

            Subject.MoveMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IBuildFileNames>()
                  .Verify(v => v.BuildFilePath(_movie, "File Name", ".mp4"), Times.Once());
        }

        [Test]
        public void should_not_append_jellyfin_part_suffix_when_part_matches_movie_number()
        {
            _localMovie.OtherVideoFiles = true;
            _localMovie.Path = @"C:\Test\Unsorted\IPZZ-562\hhd800.com@IPZZ-562-part562.mp4".AsOsAgnostic();
            _movie.MovieMetadata.Value.Number = "IPZZ-562";
            _movieFile.Path = _localMovie.Path;

            Subject.MoveMovieFile(_movieFile, _localMovie);

            Mocker.GetMock<IBuildFileNames>()
                  .Verify(v => v.BuildFilePath(_movie, "File Name", ".mp4"), Times.Once());
        }
    }
}
