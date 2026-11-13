# Changelog

All notable changes to SpacePilot are recorded here.

## [1.5.3] - 2026-11-12

### Fixed

- Scheduled runs now execute only Safe-class rules. Ticked manual rules are queued for review instead of being deleted silently.
- The review queue deduplicates repeated manual items.
- The Rules editor normalizes WinUI CR-only line endings before parsing and saving `rules.toml`.
- The Schedule toggle captures WinUI values on the UI thread before creating or removing the scheduled task.
- Clean preview planning is faster: file targets carry an `IsDirectory` hint, so known files skip `Directory.Exists`.
- Orphan-service reporting resolves `SystemRoot` and relative image paths, removing hundreds of false positives.
- Maintenance analysis surfaces DISM/VSS elevation failures instead of reporting misleading zero values.
- Developer-cache scanning no longer aborts on the first inaccessible folder. The scan is single-pass, runs off the UI thread, and avoids listing nested caches twice.

### Verified

- Release build: 0 warnings, 0 errors.
- Core tests: 107/107.
- Smoke: all 15 pages.
- Full UI suite: all checks pass, one foreground-only check skipped.
- Disk Map suite: all 19 checks pass.
- Fixture safety checks: 26/26.
- Clean preview: 1,297,124 files, 1,200,573 blocked, 96,551 actionable, about 75 seconds.

## [Unreleased]

### Planned

- Batch 3.1: Settings rewrite with 14 categories, search, atomic/versioned settings persistence, reset controls, and typed confirmation for dangerous settings.
