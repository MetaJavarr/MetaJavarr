# MetaTube Number Folder Naming Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make add-movie folder and file naming use the MetaTube `number` field only while keeping the display title unchanged.

**Architecture:** Store the MetaTube number as `MovieMetadata.Number`, expose it through `{Movie Number}` and `{Movie CleanNumber}` naming tokens, and hard-cut active/default naming config to `{Movie CleanNumber}`. Add regression tests before production code and verify the focused fixtures in Google Cloud Shell with Docker.

**Tech Stack:** C#/.NET 8, NUnit, FluentAssertions, FluentMigrator, React/TypeScript naming settings UI.

---

### Task 1: Add Red Regression Tests

**Files:**
- Modify: `src/NzbDrone.Core.Test/MetadataSource/MetaTube/MetaTubeProxyFixture.cs`
- Modify: `src/NzbDrone.Core.Test/OrganizerTests/FileNameBuilderTests/FileNameBuilderFixture.cs`
- Modify: `src/NzbDrone.Core.Test/OrganizerTests/GetMovieFolderFixture.cs`
- Modify: `src/NzbDrone.Core.Test/MovieTests/AddMovieFixture.cs`
- Create: `src/NzbDrone.Core.Test/OrganizerTests/FileNameValidationFixture.cs`

- [ ] **Step 1: Add reflection helpers for tests that must compile before `MovieMetadata.Number` exists**

```csharp
private static string GetMovieNumber(MovieMetadata metadata)
{
    return metadata.GetType().GetProperty("Number")?.GetValue(metadata) as string;
}

private static void SetMovieNumber(MovieMetadata metadata, string number)
{
    metadata.GetType().GetProperty("Number")?.SetValue(metadata, number);
}
```

- [ ] **Step 2: Update MetaTube mapping test**

Add assertions to `should_search_and_get_movie_detail_from_metatube`:

```csharp
GetMovieNumber(metadata).Should().Be("IPZZ-562");
GetMovieNumber(detail).Should().Be("IPZZ-562");
```

- [ ] **Step 3: Update default naming test**

Change `default_naming_should_write_jellyfin_jav_movie_layout` so the movie title remains display text and the number drives layout:

```csharp
_movie.Title = "IPZZ-562 Example Title";
_movie.Year = 2024;
SetMovieNumber(_movie.MovieMetadata.Value, "IPZZ-562");

var fileName = Subject.BuildFileName(_movie, _movieFile);
var folder = Subject.GetMovieFolder(_movie);

Path.Combine(folder, fileName + ".ext")
    .Should().Be(Path.Combine("IPZZ-562", "IPZZ-562.ext"));
```

- [ ] **Step 4: Add explicit folder token tests**

Add cases to `GetMovieFolderFixture`:

```csharp
[TestCase("{Movie Number}", "IPZZ-562")]
[TestCase("{Movie CleanNumber}", "IPZZ-562")]
public void should_replace_movie_number_tokens(string format, string expected)
{
    _namingConfig.MovieFolderFormat = format;

    var movie = new Movie { Title = "IPZZ-562 Example Title", Year = 2024 };
    SetMovieNumber(movie.MovieMetadata.Value, "IPZZ-562");

    Subject.GetMovieFolder(movie).Should().Be(expected);
}
```

- [ ] **Step 5: Add add-movie path regression**

Add a test to `AddMovieFixture` proving `AddMovieService` builds the path from refreshed metadata:

```csharp
[Test]
public void should_build_path_from_movie_number_when_available()
{
    var rootFolder = @"C:\Test\Movies";
    var newMovie = new Movie
    {
        TmdbId = 1,
        RootFolderPath = rootFolder
    };

    _fakeMovie.Title = "IPZZ-562 Example Title";
    SetMovieNumber(_fakeMovie, "IPZZ-562");

    GivenValidMovie(newMovie.TmdbId);

    Mocker.GetMock<IBuildFileNames>()
          .Setup(s => s.GetMovieFolder(It.IsAny<Movie>(), null))
          .Returns<Movie, NamingConfig>((c, n) => GetMovieNumber(c.MovieMetadata.Value) ?? c.Title);

    Mocker.GetMock<IAddMovieValidator>()
          .Setup(s => s.Validate(It.IsAny<Movie>()))
          .Returns(new ValidationResult());

    var movie = Subject.AddMovie(newMovie);

    movie.Title.Should().Be("IPZZ-562 Example Title");
    movie.Path.Should().Be(Path.Combine(rootFolder, "IPZZ-562"));
}
```

- [ ] **Step 6: Add filename validation tests**

Create `FileNameValidationFixture`:

```csharp
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
```

- [ ] **Step 7: Run red tests in Cloud Shell**

Run the focused Core test project in Cloud Shell with Docker:

```bash
gcloud cloud-shell ssh --command '
set -euo pipefail
rm -rf /tmp/metajavarr-test
mkdir -p /tmp/metajavarr-nuget-cache
git clone --depth 1 --branch develop https://github.com/MetaJavarr/MetaJavarr.git /tmp/metajavarr-test
'

git diff --binary HEAD | gcloud cloud-shell ssh --command '
set -euo pipefail
cd /tmp/metajavarr-test
git apply -
'

gcloud cloud-shell ssh --command '
set -euo pipefail
cd /tmp/metajavarr-test
docker run --rm \
  -u "$(id -u):$(id -g)" \
  -e HOME=/tmp \
  -e DOTNET_CLI_HOME=/tmp \
  -e NUGET_PACKAGES=/tmp/nuget-cache \
  -v "$PWD":/src \
  -v /tmp/metajavarr-nuget-cache:/tmp/nuget-cache \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:8.0.421 \
  sh -lc "dotnet restore src/NzbDrone.Core.Test/Radarr.Core.Test.csproj --disable-parallel --verbosity minimal && dotnet test src/NzbDrone.Core.Test/Radarr.Core.Test.csproj --filter \"FullyQualifiedName~MetaTubeProxyFixture|FullyQualifiedName~FileNameBuilderFixture|FullyQualifiedName~GetMovieFolderFixture|FullyQualifiedName~AddMovieFixture|FullyQualifiedName~FileNameValidationFixture\" --no-restore --logger \"console;verbosity=normal\""
'
```

Expected: FAIL with assertions showing missing `Number` mapping/tokens/defaults.

### Task 2: Implement Movie Number Metadata And Tokens

**Files:**
- Modify: `src/NzbDrone.Core/Movies/MovieMetadata.cs`
- Modify: `src/NzbDrone.Core/MetadataSource/MetaTube/MetaTubeProxy.cs`
- Modify: `src/NzbDrone.Core/Organizer/FileNameBuilder.cs`
- Modify: `src/NzbDrone.Core/Organizer/FileNameSampleService.cs`
- Modify: `src/NzbDrone.Core/Organizer/FileNameValidation.cs`
- Modify: `frontend/src/Settings/MediaManagement/Naming/NamingModal.tsx`

- [ ] **Step 1: Add `Number` to movie metadata**

```csharp
public string Number { get; set; }
```

Place it near `Title` in `MovieMetadata`.

- [ ] **Step 2: Map MetaTube number**

In `MetaTubeProxy.MapMovie`, set:

```csharp
Number = resource.Number?.Trim(),
```

- [ ] **Step 3: Add number naming tokens**

In `FileNameBuilder.AddMovieTokens`, add:

```csharp
tokenHandlers["{Movie Number}"] = m => Truncate(movie.MovieMetadata.Value.Number, m.CustomFormat) ?? string.Empty;
tokenHandlers["{Movie CleanNumber}"] = m => Truncate(CleanTitle(movie.MovieMetadata.Value.Number ?? string.Empty), m.CustomFormat);
```

- [ ] **Step 4: Update movie token regex and validation**

Update `MovieTitleRegex` so `Movie Number` and `Movie CleanNumber` count as movie identity tokens:

```csharp
public static readonly Regex MovieTitleRegex = new Regex(@"(?<token>\{(?:Movie)(?<separator>[- ._])(?:Clean)?(?:OriginalTitle|Title(?:The)?|Number)(?::(?<customFormat>[a-z0-9|-]+))?\})",
                                                                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
```

Update `ValidMovieFormatValidator` to accept any movie identity token or original file token:

```csharp
return FileNameBuilder.MovieTitleRegex.IsMatch(value) ||
       FileNameValidation.OriginalTokenRegex.IsMatch(value);
```

Update the validation message to:

```csharp
protected override string GetDefaultMessageTemplate() => "Must contain movie title, movie number, or Original Title/Filename";
```

Update the folder validation message to:

```csharp
protected override string GetDefaultMessageTemplate() => "Must contain movie title or movie number";
```

- [ ] **Step 5: Add sample number**

In `FileNameSampleService`, set:

```csharp
Number = "IPZZ-562",
```

- [ ] **Step 6: Add number tokens to the naming UI**

In `NamingModal.tsx`, add to `movieTokens`:

```tsx
{ token: '{Movie Number}', example: 'IPZZ-562' },
{ token: '{Movie CleanNumber}', example: 'IPZZ-562' },
```

### Task 3: Hard-Cut Naming Defaults And Database Config

**Files:**
- Modify: `src/NzbDrone.Core/Organizer/NamingConfig.cs`
- Create: `src/NzbDrone.Core/Datastore/Migration/243_add_movie_number.cs`

- [ ] **Step 1: Change defaults**

Set both defaults to number-only naming:

```csharp
MovieFolderFormat = "{Movie CleanNumber}",
StandardMovieFormat = "{Movie CleanNumber}",
```

- [ ] **Step 2: Add migration**

Create migration `243_add_movie_number.cs`:

```csharp
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(243)]
    public class add_movie_number : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("MovieMetadata").AddColumn("Number").AsString().Nullable();
            Execute.Sql("UPDATE \"NamingConfig\" SET \"MovieFolderFormat\" = '{Movie CleanNumber}', \"StandardMovieFormat\" = '{Movie CleanNumber}'");
        }
    }
}
```

### Task 4: Verify And Review

**Files:**
- All files modified above.

- [ ] **Step 1: Run focused tests in Cloud Shell**

Run the same Cloud Shell Docker command from Task 1 after applying the full patch.

Expected: PASS for the focused fixtures.

- [ ] **Step 2: Inspect diff**

Run:

```bash
git diff --stat
git diff -- src/NzbDrone.Core/Movies/MovieMetadata.cs src/NzbDrone.Core/MetadataSource/MetaTube/MetaTubeProxy.cs src/NzbDrone.Core/Organizer/FileNameBuilder.cs src/NzbDrone.Core/Organizer/NamingConfig.cs
git diff -- src/NzbDrone.Core.Test/MetadataSource/MetaTube/MetaTubeProxyFixture.cs src/NzbDrone.Core.Test/OrganizerTests/FileNameBuilderTests/FileNameBuilderFixture.cs src/NzbDrone.Core.Test/OrganizerTests/GetMovieFolderFixture.cs src/NzbDrone.Core.Test/MovieTests/AddMovieFixture.cs
```

Expected: changes match the spec, with no title-parsing fallback.

- [ ] **Step 3: Commit implementation**

Run:

```bash
git add docs/superpowers/plans/2026-06-05-metatube-number-folder.md \
  src/NzbDrone.Core/Movies/MovieMetadata.cs \
  src/NzbDrone.Core/MetadataSource/MetaTube/MetaTubeProxy.cs \
  src/NzbDrone.Core/Organizer/FileNameBuilder.cs \
  src/NzbDrone.Core/Organizer/FileNameSampleService.cs \
  src/NzbDrone.Core/Organizer/FileNameValidation.cs \
  src/NzbDrone.Core/Organizer/NamingConfig.cs \
  src/NzbDrone.Core/Datastore/Migration/243_add_movie_number.cs \
  src/NzbDrone.Core.Test/MetadataSource/MetaTube/MetaTubeProxyFixture.cs \
  src/NzbDrone.Core.Test/OrganizerTests/FileNameBuilderTests/FileNameBuilderFixture.cs \
  src/NzbDrone.Core.Test/OrganizerTests/GetMovieFolderFixture.cs \
  src/NzbDrone.Core.Test/MovieTests/AddMovieFixture.cs \
  src/NzbDrone.Core.Test/OrganizerTests/FileNameValidationFixture.cs \
  frontend/src/Settings/MediaManagement/Naming/NamingModal.tsx
git commit -m "feat: use metatube number for movie naming"
```
