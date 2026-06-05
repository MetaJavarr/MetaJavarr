# MetaTube Number Folder Naming Design

## Context

MetaJavarr currently maps a MetaTube movie with `number = "IPZZ-562"` and `title = "Example Title"` into the display title `IPZZ-562 Example Title`. Add-movie flows normally submit a root folder without an explicit movie path, so `AddMovieService` asks `IBuildFileNames.GetMovieFolder` to build the folder from the active `NamingConfig.MovieFolderFormat`. The default format is `{Movie CleanTitle}`, which creates a folder such as `IPZZ-562 Example Title`.

The desired behavior is a hard cutover: new add-movie folder names must use the MetaTube 番号 only, for example `IPZZ-562`, while display titles remain `IPZZ-562 Example Title`.

## Decision

Store the MetaTube number as first-class movie metadata and expose it through naming tokens.

Add `MovieMetadata.Number` and populate it from `MetaTubeMovieResource.Number.Trim()` when MetaTube metadata is mapped. Keep `MovieMetadata.Title` and `MovieMetadata.OriginalTitle` unchanged as the combined display title.

Add organizer tokens:

- `{Movie Number}`: the raw stored number.
- `{Movie CleanNumber}`: the number passed through existing title cleanup rules.

Use `{Movie CleanNumber}` for the hard-cutover default movie folder format and standard movie filename format.

## Data Flow

1. MetaTube search/detail responses deserialize `number`.
2. `MetaTubeProxy.MapMovie` stores the trimmed number in `MovieMetadata.Number`.
3. Add-movie receives the selected metadata and refreshes it through `AddSkyhookData`.
4. `AddMovieService.SetPropertiesAndValidate` builds `Path` with `IBuildFileNames.GetMovieFolder` when the request did not provide a path.
5. `FileNameBuilder.GetMovieFolder` resolves `{Movie CleanNumber}` from `MovieMetadata.Number`, cleans it with existing filename rules, and combines it with the root folder.

Result:

```text
/root/IPZZ-562/IPZZ-562.ext
```

The movie title shown in the UI remains:

```text
IPZZ-562 Example Title
```

## Migration

Add a new database migration after `242_add_movie_keywords`:

- Add nullable `Number` to `MovieMetadata`.
- Update `NamingConfig.MovieFolderFormat` and `NamingConfig.StandardMovieFormat` to `{Movie CleanNumber}`.

This is intentionally not backward compatible. Existing custom naming formats are overwritten as part of the hard cutover.

Existing rows cannot reliably recover a number without parsing title text or re-querying MetaTube. The migration leaves `MovieMetadata.Number` null for existing rows. Future refreshes should populate it from MetaTube.

## Error Handling

If `MovieMetadata.Number` is empty, `{Movie Number}` and `{Movie CleanNumber}` resolve to an empty string. Existing component cleanup already skips empty path components. Add-movie validation will continue to reject invalid or empty paths through the current validators.

No parsing fallback from title is added, because parsing would create ambiguous behavior and hide missing source metadata.

## Tests

Focused regression tests should cover:

- MetaTube mapping preserves display title as `IPZZ-562 Example Title` and stores `Number` as `IPZZ-562`.
- `FileNameBuilder` resolves `{Movie Number}` and `{Movie CleanNumber}`.
- Default naming produces `IPZZ-562/IPZZ-562.ext` when `MovieMetadata.Number` is `IPZZ-562`.
- `AddMovieService` creates a movie path under the root folder using the number-only folder.

Verification should follow the project workflow: reproduce the current folder-name behavior first, add the focused regression test, then run the focused fixture in Google Cloud Shell with the Docker SDK image.
