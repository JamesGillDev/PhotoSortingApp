# PhotoSortingApp

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-512BD4)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![EF%20Core](https://img.shields.io/badge/EF%20Core-8.0-5C2D91)](https://learn.microsoft.com/ef/core/)
[![Version](https://img.shields.io/badge/Version-1.6.0-brightgreen)](./CHANGELOG.md)
[![License](https://img.shields.io/badge/License-BLS%201.1-blue.svg)](./LICENSE.md)

PhotoSortingApp is a local-first Windows desktop media catalog and safe organizer planner.

It is designed for large unsorted libraries (10,000+ photos/videos), with incremental indexing, metadata extraction, thumbnail caching, and read-only-first safety.

## Core Principles

- Local-first: no cloud dependency in v1
- Safety-first file operations: scan/index is read-only
- Organizer apply requires explicit confirmation and logs every move
- Incremental performance for large folders

## Features

- Root folder selection + rescanning
- Recursive indexing for photos and videos:
  - photos: `.jpg`, `.jpeg`, `.png`, `.heic`, `.webp`, `.gif`, `.bmp`, `.tif`, `.tiff`
  - videos: `.mp4`, `.mov`, `.m4v`, `.avi`, `.mkv`, `.wmv`, `.webm`
- Metadata extraction:
  - EXIF `DateTimeOriginal` when available
  - fallback to file timestamps
- Incremental updates by file size + last write time
- Index progress with cancellation
- `Scan This PC` option:
  - scans fixed local drives in safe mode
  - excludes likely system/program locations (`Windows`, `Program Files`, `ProgramData`, `AppData`, etc.) to reduce non-user image noise
  - excludes common dev/build/cache/web-asset paths to reduce icons/thumbnails/background art noise on different PCs
- Gallery with thumbnail cache (`App_Data/ThumbCache`)
- Gallery layout options:
  - Tiles (default)
  - List
- Adjustable workspace panes with drag resize splitters between:
  - `Library Filters`
  - `Gallery`
  - `Inspector`
- Pinned section headers for `Library Filters` and `Inspector` so titles remain visible while scrolling.
- Tile layout wraps within the viewport and scrolls vertically (no side scrolling)
- Multiple photo selection in gallery (Ctrl/Shift multi-select)
- Sort options:
  - date taken
  - date indexed
  - file size
  - file name
- Quick media-type toggles beside `Sort`:
  - Images
  - Videos
- Theme options:
  - System (matches Windows app theme)
  - Light
  - Dark
- Startup quick-start guide popup on launch with concise usage instructions
- Filters:
  - date range
  - date source
  - folder subpath
  - tokenized, case-insensitive filename / notes / tags search
  - person ID search
  - animal ID search
  - location ID search
  - exclude system/app media toggle for cleaner scan/query results
- Smart Albums:
  - All Media
  - Videos
  - By Month
  - By Year
  - Unknown Date
  - Recently Added
  - Possible Duplicates
- Year-first browsing:
  - dedicated `Year Folders` list in the filter pane
  - selecting a year lets organizer planning target that year only
- Duplicate detection toggle (SHA-256 only when enabled)
- Rename assistant for selected photos:
  - metadata-informed name suggestions (date/camera/folder/tags)
  - AI-powered smart rename analysis (optional) for setting/season/holiday/shot type/subjects
  - manual rename input with one-click apply
  - batch "Smart Rename Selected" action for multi-selected photos
- Identity scan tool for selected photos:
  - detects and stores `PeopleCsv` IDs
  - detects and stores `AnimalsCsv` IDs
  - portrait-friendly heuristic fallback when AI is not configured
  - auto-applies context tags from scan analysis so detected labels are immediately searchable
  - manual multi-ID assignment (`Save IDs`) applies across selected photos
  - merges scanned/manual IDs with existing IDs to avoid losing previous labels
  - explicit save confirmation popups for `Save IDs` / `Save Image`
  - searchable from the top filter bar
- Context scan tool for selected photos:
  - adds structured tags for environment, event, holiday, season, time-of-day, shot type, subjects, and scene hints (scenery/artwork)
  - supports both AI-assisted and heuristic analysis paths
  - uses media-aware evidence gating to reduce false tags on videos
  - replaces prior auto-generated context tags when rescanning so stale labels do not accumulate
- Tag management on selected photos:
  - add tags
  - edit existing tags
  - remove tags
- Streamlined selected-photo file actions:
  - right-click media context menu in gallery items
  - open file
  - open file location
  - save media IDs
  - move
  - copy
  - duplicate
  - location repair for missing files (searches scan root)
- Infinite in-session undo history:
  - undo last change
  - undo any selected change from history dropdown
- Dry-run organizer plan:
  - Year folder strategy (default)
  - optional Year/Month folder strategy in service rules
  - can target only the selected year album (for example `2016`)
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

## Publish and Run Locally (No IDE)

Framework-dependent publish (requires installed .NET 8 Desktop Runtime on target machine):

```powershell
dotnet publish src/PhotoSortingApp.App/PhotoSortingApp.App.csproj -c Release -r win-x64 --self-contained false -o artifacts/publish/win-x64
```

Run:

```powershell
.\artifacts\publish\win-x64\PhotoSortingApp.App.exe
```

## Share to Another Windows Computer

1. Publish the app (command above).
2. Copy the full `artifacts/publish/win-x64` folder to the other computer.
3. Ensure the other computer has the .NET 8 Desktop Runtime installed (if framework-dependent publish is used).
4. Launch `PhotoSortingApp.App.exe`.

Notes:

- App data (catalog DB, thumbnails, organizer logs) is created under `App_Data` next to the executable.
- For a fully standalone package, use `--self-contained true` when publishing (larger output).

## Smart Rename AI Configuration (Optional)

Set these environment variables before launching the app to enable vision-based smart rename and identity detection:

```powershell
$env:OPENAI_API_KEY="your_api_key"
$env:OPENAI_MODEL="gpt-4o-mini"          # optional, default: gpt-4o-mini
$env:OPENAI_BASE_URL="https://api.openai.com/v1"  # optional
```

If `OPENAI_API_KEY` is not set, the app uses local heuristics for rename suggestions and people/animal ID detection.

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

- Current release version: `1.6.0`
- Release history and iteration notes: [`CHANGELOG.md`](./CHANGELOG.md)
- Build version metadata source: `Directory.Build.props`

Public release readiness:
- `v1.6.0` is validated and ready for public release.

Current release iteration history:

- `v1.0.0` (2026-02-25): Initial public release.
- `v1.0.1` (2026-02-26): Added centralized version metadata and public release iteration tracking.
- `v1.1.0` (2026-02-27): Added rename assistant, persistent tag management, direct move/copy/duplicate/repair actions, and in-session undo history.
- `v1.2.0` (2026-02-27): Added gallery multi-select, AI-assisted smart rename batching, person/animal identity scan storage+search, and tile wrapping improvements.
- `v1.3.0` (2026-02-27): Added batch identity ID assignment, context category scanning tags (environment/event/holiday/scenery/artwork), standardized rename guidance, and duplicate-group header spacing fixes.
- `v1.3.1` (2026-02-27): Added explicit Save Image and double-click open actions, fixed top-bar clipping, and fixed Windows file-metadata persistence for identity/tag updates.
- `v1.4.0` (2026-02-27): Added startup quick-start popup, fixed published EXE icon startup issue, improved portrait fallback detection, auto-applied scan context tags, and strengthened case-insensitive tokenized search.
- `v1.5.0` (2026-03-02): Added video indexing support, year-first browsing and organizer targeting, location IDs, and overall UI/UX layout cleanup.
- `v1.5.1` (2026-03-02): Added gallery right-click context actions (including Open File Location), added Images/Videos and system-media exclusion toggles, and fixed folder filter reload lock-up behavior.
- `v1.5.2` (2026-03-02): Tightened cross-PC noise exclusions (system/cache/web assets), fixed dropdown hover color behavior, aligned top filter control sizing, fixed title/subtitle spacing, and defaulted refresh/clear to `All Media`.
- `v1.6.0` (2026-03-03): Added draggable pane resizing with pinned section headers, and upgraded context/environment scanning with media-aware evidence-based tagging plus false-positive reduction for videos.

Tag and publish a new GitHub release iteration:

```powershell
git tag v1.6.0
git push origin main
git push origin v1.6.0
```

Then create a GitHub Release from the matching tag (for example `v1.6.0`) and copy the matching section from `CHANGELOG.md` into the release notes.

## AI Extension Points (v1 Stubs)

- `ISemanticSearchService`
- `IFaceClusterService`

`ISemanticSearchService` and `IFaceClusterService` are currently implemented as empty services so AI can be added later without architecture rewrites.

## Known Limitations

- HEIC decoding/metadata depends on local Windows codec support.
- Thumbnail generation uses `System.Drawing.Common` and is Windows-targeted.
- Video preview thumbnails are currently not generated (videos are still indexed, searchable, and auto-organized by year).
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
