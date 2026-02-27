# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project follows Semantic Versioning.

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
