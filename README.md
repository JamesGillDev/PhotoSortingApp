# PhotoSortingApp

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-512BD4)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![EF%20Core](https://img.shields.io/badge/EF%20Core-8.0-5C2D91)](https://learn.microsoft.com/ef/core/)
[![Version](https://img.shields.io/badge/Version-1.1.0-brightgreen)](./CHANGELOG.md)
[![License](https://img.shields.io/badge/License-BLS%201.1-blue.svg)](./LICENSE.md)

PhotoSortingApp is a local-first Windows desktop photo catalog and safe organizer planner.

It is designed for large unsorted libraries (10,000+ photos), with incremental indexing, metadata extraction, thumbnail caching, and read-only-first safety.

## Core Principles

- Local-first: no cloud dependency in v1
- Safety-first file operations: scan/index is read-only
- Organizer apply requires explicit confirmation and logs every move
- Incremental performance for large folders

## Features

- Root folder selection + rescanning
- Recursive indexing for `.jpg`, `.jpeg`, `.png`, `.heic`
- Metadata extraction:
  - EXIF `DateTimeOriginal` when available
  - fallback to file timestamps
- Incremental updates by file size + last write time
- Index progress with cancellation
- `Scan This PC` option:
  - scans fixed local drives in safe mode
  - excludes likely system/program locations (`Windows`, `Program Files`, `ProgramData`, `AppData`, etc.) to reduce non-user image noise
- Gallery with thumbnail cache (`App_Data/ThumbCache`)
- Gallery layout options:
  - Tiles (default)
  - List
- Sort options:
  - date taken
  - date indexed
  - file size
  - file name
- Theme options:
  - System (matches Windows app theme)
  - Light
  - Dark
- Filters:
  - date range
  - date source
  - folder subpath
  - filename / notes / tags search
- Smart Albums:
  - All Photos
  - By Month
  - By Year
  - Unknown Date
  - Recently Added
  - Possible Duplicates
- Duplicate detection toggle (SHA-256 only when enabled)
- Rename assistant for selected photos:
  - metadata-informed name suggestions (date/camera/folder/tags)
  - manual rename input with one-click apply
- Tag management on selected photos:
  - add tags
  - edit existing tags
  - remove tags
- Streamlined selected-photo file actions:
  - move
  - copy
  - duplicate
  - location repair for missing files (searches scan root)
- Infinite in-session undo history:
  - undo last change
  - undo any selected change from history dropdown
- Dry-run organizer plan:
  - Year/Month folder strategy
  - preview + operation logging
  - optional `Apply Plan` execution with confirmation

## Solution Layout

- `PhotoSortingApp.sln`
- `src/PhotoSortingApp.App` (WPF UI, MVVM)
- `src/PhotoSortingApp.Domain` (POCOs + enums)
- `src/PhotoSortingApp.Core` (metadata/thumbnail infrastructure + interfaces)
- `src/PhotoSortingApp.Data` (EF Core SQLite, scan/query/duplicate/plan services)

## Requirements

- Windows
- .NET SDK 8.x

## Build and Run

Run these commands from the repository root (`PhotoSorting_EditorApp`):

```powershell
dotnet restore PhotoSortingApp.sln
dotnet build PhotoSortingApp.sln
dotnet run --project .\src\PhotoSortingApp.App\PhotoSortingApp.App.csproj
```

This launches the Windows desktop app window (WPF).

If you run `dotnet run` from `PhotoSorting_EditorApp/PhotoSorting_EditorApp`, you'll start the sample web host and see `Hello World` instead.

Troubleshooting:

- If you see a popup saying the app "requires the Windows App Runtime Version 1.5", that is from a stale/other WinAppSDK process. This WPF project does not require Windows App SDK to run.
- From repo root, close any running `PhotoSortingApp.App.exe` instances, then run:

```powershell
dotnet clean .\src\PhotoSortingApp.App\PhotoSortingApp.App.csproj
dotnet run --project .\src\PhotoSortingApp.App\PhotoSortingApp.App.csproj
```

## Database and Migrations

Install local EF CLI tool once:

```powershell
dotnet new tool-manifest --force
dotnet tool install dotnet-ef --version 8.0.11
```

Apply migrations:

```powershell
dotnet tool run dotnet-ef database update --project src/PhotoSortingApp.Data/PhotoSortingApp.Data.csproj --startup-project src/PhotoSortingApp.App/PhotoSortingApp.App.csproj
```

Create a new migration:

```powershell
dotnet tool run dotnet-ef migrations add <MigrationName> --project src/PhotoSortingApp.Data/PhotoSortingApp.Data.csproj --startup-project src/PhotoSortingApp.App/PhotoSortingApp.App.csproj --output-dir Migrations
```

## Release Build (Desktop)

```powershell
dotnet publish src/PhotoSortingApp.App/PhotoSortingApp.App.csproj -c Release -r win-x64 --self-contained false -o artifacts/publish/win-x64
```

## Versioning and Public GitHub Releases

- Current release version: `1.1.0`
- Release history and iteration notes: [`CHANGELOG.md`](./CHANGELOG.md)
- Build version metadata source: `Directory.Build.props`

Current release iteration history:

- `v1.0.0` (2026-02-25): Initial public release.
- `v1.0.1` (2026-02-26): Added centralized version metadata and public release iteration tracking.
- `v1.1.0` (2026-02-27): Added rename assistant, persistent tag management, direct move/copy/duplicate/repair actions, and in-session undo history.

Tag and publish a new GitHub release iteration:

```powershell
git tag v1.1.0
git push origin main
git push origin v1.1.0
```

Then create a GitHub Release from the `v1.1.0` tag and copy the matching section from `CHANGELOG.md` into the release notes.

## AI Extension Points (v1 Stubs)

- `ISemanticSearchService`
- `IFaceClusterService`

`ISemanticSearchService` and `IFaceClusterService` are currently implemented as empty services so AI can be added later without architecture rewrites.

## Known Limitations

- HEIC decoding/metadata depends on local Windows codec support.
- Thumbnail generation uses `System.Drawing.Common` and is Windows-targeted.
- Undo history is session-based and not persisted after app restart.
- Notes editing UI is not yet implemented.

## Roadmap

- v2: Persisted multi-session undo/redo journal for file operations
- v3: Local semantic search + tagging adapters
- v4: Face clustering + guided “photo detective” workflow

## License

This project is licensed under the **Business Source License 1.1 (BLS)**.
See [LICENSE.md](./LICENSE.md) for full terms.

- Additional Use Grant: None
- Change Date: 2029-01-01
- Change License: Apache License 2.0
