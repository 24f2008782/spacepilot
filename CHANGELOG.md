# Changelog

All notable changes to SpacePilot are recorded here.

## [1.6.0] - 2026-11-13

### Added

- Settings page with a 14-category sidebar, multi-term search across setting names, descriptions, and keywords, and direct navigation to a result's category.
- Per-field, per-category, and full reset controls. Reset all requires two confirmations; dangerous resets also require the session `ADVANCED` unlock.
- Atomic, versioned settings persistence. Saves write a temporary file, swap it in with `File.Replace`, and keep the previous file as `settings.json.bak`.
- Corrupt or newer-schema settings files fall back to defaults with a notice and are left untouched until the next save.
- A bordered Advanced risk box for a custom `RulesPath`, clearing `ProtectedPaths`, and setting `VaultCapGb` to `0`. Typing `ADVANCED` unlocks these changes for the current app session.
- Protected-path add/remove controls and a read-only vault-cap summary.

### Changed

- Settings schema version is now `1`; existing 1.5.x files load without migration.
- Empty Settings categories are explicit placeholders rather than invented controls.
- Settings saves and resets apply the audit-redaction flag immediately, without requiring a restart.
- The UI test suite now searches for flyout controls in the app's popup windows, fixing the Filters check in background mode.

### Verified

- Release build: 0 warnings, 0 errors.
- Core tests: 112/112.
- Smoke: all 15 pages.
- Full UI suite: all checks pass, one foreground-only check skipped.
- Disk Map suite: all 19 checks pass.
- Fixture safety checks: 26/26.

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
