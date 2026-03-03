# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project follows Semantic Versioning.

## [1.6.2] - 2026-03-03

### Fixed
- Normalized top filter row control heights for a consistent, professional layout:
  - `Search`
  - `Person ID`
  - `Animal ID`
  - `Location ID`
  - `Theme`
  - `Apply Filters`
  - `Clear Filters`

### Release Readiness
- `v1.6.2` is validated, published locally, and marked ready for public GitHub release.

## [1.6.1] - 2026-03-03

### Added
- New `Save Tags` action beside `Remove Tag` in the Inspector `Tags` section.
- Explicit hard-save tag embedding workflow:
  - run `Scan Environment / Event / Scene Tags`
  - click `Save Tags` to force-embed current tags into file metadata for all selected media

### Changed
- Added a dedicated metadata-embed service path for tags (`EmbedTagsIntoFileAsync`) so users can explicitly persist tags to file metadata on demand without changing the tag set.

### Release Readiness
- `v1.6.1` is validated, published locally, and marked ready for public GitHub release.

## [1.6.0] - 2026-03-03

### Added
- Draggable workspace pane resizing between `Library Filters`, `Gallery`, and `Inspector`.
- `media:photo` / `media:video` context labels in environment/event/scene scanning.

### Changed
- `Library Filters` and `Inspector` section titles are now pinned so they remain visible while pane content scrolls.
- Context scanning is now media-aware and evidence-based:
  - video context labels are inferred from filename/folder/metadata evidence instead of timestamp defaults
  - event keyword coverage expanded (concert/live/performance/tour/music-video/travel/sports/business variants)
  - repeated context scans now replace prior auto-generated context tags with the latest inferred set

### Fixed
- Reduced false-positive video context tagging (for example `shot:photo`, timestamp-only `season:*`, timestamp-only `time:*`).

### Release Readiness
- `v1.6.0` is validated, published locally, and marked ready for public GitHub release.

## [1.5.2] - 2026-03-02

### Added
- Stronger cross-PC media noise exclusion heuristics for scan and browse pipelines:
  - dot-prefixed directories (for example `.codex`, `.cache`)
  - common dev/build/cache folders (`bin`, `obj`, `publish`, `artifacts`, `node_modules`, cache/thumbnail folders)
  - likely web project folders (`wwwroot`, `website`, `web site`) and common generated web-asset name patterns

### Changed
- Default album selection after refresh and clear now uses `All Media` instead of auto-selecting a year, reducing accidental hidden results.
- Top filter controls (`Search`, `Person ID`, `Animal ID`, `Location ID`, `Theme`) were normalized to consistent widths for cleaner layout.

### Fixed
- Fixed combo/dropdown hover color overlay causing bright system highlight flashes in dark theme.
- Fixed top header spacing so `Photo Workspace` and subtitle text are visually separated.
- Improved context-menu theming consistency and readability.

### Release Readiness
- `v1.5.2` is validated, published locally, and marked ready for public GitHub release.

## [1.5.1] - 2026-03-02

### Added
- Right-click media context menu in both gallery layouts with quick actions:
  - `Open File`
  - `Open File Location`
  - `Save Media IDs`
  - `Move`, `Copy`, `Duplicate`
  - `Fix Location`
- New media-type toggles (`Images`, `Videos`) beside `Sort` for faster gallery filtering.
- `Exclude system/app media` checkbox under `Root Folder` to reduce noisy scan/query results from OS and tool folders.

### Changed
- Scan and browse pipelines now honor system-folder exclusion consistently in albums, folder filters, and search results.
- Status and filter text now consistently refer to `media` when both photos and videos are in scope.

### Fixed
- `Folder Filter` selection now immediately reloads results and no longer appears unresponsive.
- Reduced duplicate auto-reload calls during filter resets/refreshes to prevent UI lock-up behavior.

## [1.5.0] - 2026-03-02

### Added
- Video indexing support for common desktop formats (`.mp4`, `.mov`, `.m4v`, `.avi`, `.mkv`, `.wmv`, `.webm`) alongside existing photo formats.
- Year-first browsing UX with a dedicated `Year Folders` list in the filter pane.
- Organizer plan targeting by selected year album (example: select `Year 2016`, then build/apply plan for 2016 only).
- Location ID support end-to-end:
  - top-bar `Location ID` filter
  - selected-item details display
  - manual Location ID input/save
  - persisted to catalog and Windows metadata fields
- `Videos` smart album for quick access to indexed videos.

### Changed
- Organizer default strategy now places media into `Year` folders by default.
- Main workspace layout was refactored for clearer grouping, spacing, and reduced visual clutter.
- Startup guide content now reflects year-folder workflow and photo+video support.
- Save/open wording was generalized to media/file terminology where applicable.

### Fixed
- Preview loading now safely skips non-image media and gracefully handles decode failures.
- Thumbnail generation now explicitly skips non-image files to avoid unnecessary processing/errors.
- Smart rename AI path now avoids image payload calls for videos and uses heuristic fallback text.

## [1.4.0] - 2026-02-27

### Added
- Startup quick-start popup window with concise usage guidance shown at launch.
- App icon asset and packaging updates so a custom icon appears in the executable and app window.
- Auto-apply of context detection labels during `Scan People / Animals` so portrait/environment/event tags become searchable without extra manual tagging.
- Explicit success confirmation dialogs for `Save IDs` and `Save Image`.

### Changed
- Search/filter matching now uses case-insensitive tokenized matching for filename, notes, tags, person IDs, and animal IDs (for example `cece` matches `CeCe`).
- Heuristic identity fallback improved for portrait-style photos when AI vision is unavailable, reducing empty detection results on local-only setups.

### Fixed
- Fixed published EXE startup failure caused by invalid runtime window-icon URI (`TypeConverterMarkupExtension` startup exception).
- Identity/context scan status reporting now includes context-tag application counts.

## [1.3.1] - 2026-02-27

### Added
- Dedicated `Save Image` action in the selected-photo panel so manual identity edits can be explicitly saved for one photo.
- Double-click to open the selected image with the system default image viewer.
- File metadata persistence now writes identity/context details to Windows properties (`Subject`, `Comments`, and `Tags/Keywords` where supported).

### Changed
- Enter key in People/Animal ID inputs now triggers image save behavior to reduce missed manual edits.
- Duplicate Groups header spacing and top-bar control sizing were adjusted to prevent clipped text on common window widths.
- Save status messaging now clarifies that metadata is written to Windows file property fields when supported by the file type/handler.

### Fixed
- Metadata persistence now initializes COM on the calling thread before property-store writes, fixing cases where ID/tag changes were saved in the catalog but not embedded into file properties.

## [1.3.0] - 2026-02-27

### Added
- Manual identity assignment now supports comma-separated multi-ID input for both people and animals, with save applied across all selected photos.
- New context scanning action for selected photos that can add structured tags for environment, event, holiday, season, time-of-day, shot type, subjects, and scene hints (including scenery/artwork).
- Context scan preview text on selected photo details to show what labels are currently inferred.

### Changed
- Identity scan now merges detected IDs with existing IDs and avoids creating redundant undo entries when no changes are detected.
- Duplicate Groups header layout spacing was adjusted so the `Refresh` button no longer overlaps nearby text.
- Smart rename suggestions and batch smart rename continue using the standardized timestamp-first format for more specific filenames.

## [1.2.0] - 2026-02-27

### Added
- Gallery multi-select support (Ctrl/Shift selection) with selected-count feedback.
- Batch `Smart Rename Selected` workflow using smart image analysis.
- Person and animal identity scan for selected photos, with persistence in `PeopleCsv` and `AnimalsCsv`.
- Person/animal filter inputs in the top bar for searchable identity lookup.
- New EF Core migration `AddPeopleAndAnimalsCsv` for identity storage columns.

### Changed
- Rename assistant now consumes structured smart analysis (setting/season/holiday/time-of-day/shot type/subjects).
- Tile view wrapping now targets vertical-only scrolling behavior without horizontal gallery scrolling.
- Dark/light disabled control colors were adjusted for stronger text readability.
- Scan pipeline now de-duplicates discovered file paths before insert to avoid SQLite unique-constraint save failures on overlapping traversal results.

## [1.1.0] - 2026-02-27

### Added
- Rename assistant for selected photos with metadata-based name suggestions (date/camera/folder/tag-informed).
- Manual rename workflow for selected photos directly in the app.
- Persistent tag management (add/edit/remove) stored in the catalog database (`TagsCsv`).
- Streamlined file actions for selected photos:
  - move to folder
  - copy to folder
  - duplicate in place
  - repair missing location references by searching within the scan root
- Session-based infinite undo history for photo edits, including:
  - undo last change
  - undo a selected change from a dropdown history
- New EF Core migration `AddPhotoTagsCsv` for tag persistence.

### Changed
- Search now includes stored tags in addition to filename and notes.
- App services now use concrete data-backed tagging and photo edit services.

## [1.0.1] - 2026-02-26

### Added
- Centralized release version metadata via `Directory.Build.props` so build artifacts carry consistent `Version`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion`.
- Public release iteration documentation for GitHub tagging and release notes workflow in `README.md`.

### Changed
- Established explicit release iteration tracking for public versions on GitHub.

## [1.0.0] - 2026-02-25

### Added
- Initial public release of PhotoSortingApp v1.
- Local-first photo indexing, metadata extraction, gallery browsing, smart albums, duplicate detection, and dry-run organizer planning.
