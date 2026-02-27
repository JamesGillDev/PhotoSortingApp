# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project follows Semantic Versioning.

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
