using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.Test.OrganizerTests
{
    [TestFixture]
    public class FileNameValidationFixture
    {
        [Test]
        public void should_accept_movie_clean_number_as_movie_format()
        {
            var validator = new MovieFormatValidator();

            validator.Validate(new ValidationSubject { Format = "{Movie CleanNumber}" }).IsValid.Should().BeTrue();
        }

        [Test]
        public void should_accept_movie_clean_number_as_movie_folder_format()
        {
            var validator = new MovieFolderFormatValidator();

            validator.Validate(new ValidationSubject { Format = "{Movie CleanNumber}" }).IsValid.Should().BeTrue();
        }

        private class ValidationSubject
        {
            public string Format { get; set; }
        }

        private class MovieFormatValidator : AbstractValidator<ValidationSubject>
        {
            public MovieFormatValidator()
            {
                RuleFor(c => c.Format).ValidMovieFormat();
            }
        }

        private class MovieFolderFormatValidator : AbstractValidator<ValidationSubject>
        {
            public MovieFolderFormatValidator()
            {
                RuleFor(c => c.Format).ValidMovieFolderFormat();
            }
        }
    }
}
