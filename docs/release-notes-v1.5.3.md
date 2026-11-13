# SpacePilot 1.5.3

**Release date:** 2026-11-12  
**Type:** Safety and reliability fix release  
**License:** MIT

## Highlights

This release closes the non-admin safety and reliability defects found in the 1.5.3 verification pass. The biggest change is scheduled-run safety: a scheduled run now executes only rules marked `safe = true`. Ticked manual rules are queued for review instead of being deleted silently.

## Fixed

- **Scheduled-run safety (P0).** Safe rules run; manual rules are queued for review. The queue is deduplicated.
- **Rules editor save.** WinUI's CR-only line endings are normalized before parsing and saving `rules.toml`.
- **Schedule toggle.** Values are captured on the UI thread before the task is created or removed.
- **Clean preview performance.** File targets carry an `IsDirectory` hint, so known files skip `Directory.Exists`. The plan time dropped from 352.3 s to 51.0 s in verification.
- **Orphan services.** `SystemRoot` and relative image paths are resolved correctly. The count dropped from 424 to 8, with known false positives removed.
- **Maintenance analysis.** DISM and VSS elevation failures are shown as `unavailable — Requires administrator` instead of misleading zeroes.
- **Developer caches.** The scan no longer aborts on the first inaccessible folder. It is single-pass, runs off the UI thread, and avoids nested duplicate caches.
- **UI test compatibility.** The full Clean preview remains available; the test caches elements and locates the review dialog through its popup window.

## Verification

- Release build: 0 warnings, 0 errors.
- Core tests: 107/107.
- Smoke: all 15 pages.
- Full UI suite: all checks pass; one foreground-only check skipped.
- Disk Map suite: all 19 checks pass.
- Fixture safety checks: 26/26.
- Clean preview: 1,297,124 files, 1,200,573 blocked, 96,551 actionable, about 75 seconds.

## Known limitations

- The release is not code-signed. SmartScreen may warn.
- The full Clean preview is still a large UI Automation surface.
- Developer-cache results include broad build-output matches; tuning is planned.
- Admin/UAC tasks and real-data destructive flows are outside the non-admin verification pass.

## Install

1. Download `SpacePilot-1.5.3-x64.zip`.
2. Extract it to a folder you control.
3. Install the [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) if it is not already present.
4. Run `CDClean.exe`.

See [verification.md](verification.md) for the detailed record.
