# Verification record - SpacePilot 1.6.0

**Date:** 2026-11-13
**Scope:** non-admin release pass
**Platform:** Windows 11, x64  
**Build:** Release, framework-dependent, `win-x64`

## Verdict

**Accepted for the non-admin release pass.** No release-blocking functional or safety defect remains.

Tier 2 final verification is complete. Batch 3.1 adds the Settings rewrite and safer settings persistence. The remaining items are documented limitations, not blockers for this pass.

## Build and tests

| Check | Result |
|---|---|
| Release build (`CDClean.App`) | 0 warnings, 0 errors |
| Core tests | 112/112 passed |
| Smoke test | all 15 pages opened |
| Full UI suite | all checks passed; 1 foreground-only check skipped |
| Disk Map UI suite | all 19 checks passed |
| Fixture safety checks | 26/26 passed |

## Fixture safety checks

All destructive checks used synthetic folders. Real user data was not modified.

| Scenario | Result |
|---|---|
| Schedule weekly on | `CDClean-Weekly` created; UI reported `Weekly clean set for 10:00.` |
| Schedule weekly off | `CDClean-Weekly` removed; UI reported `Turned off.` |
| Rules save | UI reported `Saved.`; saved file is LF-only and both fixture rules survived |
| P0 safe/manual scheduled run | Safe fixture deleted; manual fixture kept; `1 manual item(s) queued for review.` |
| Review queue | Contained the manual fixture path and rule name |
| Maintenance analysis without elevation | WinSxS and VSS reported `unavailable — Requires administrator`, not zero |
| Orphan services | 8 reported, down from 424; known `1394ohci`, `ACPI`, and `AFD` false positives gone |
| Developer caches | 2,477 caches found; `node_modules` path present; UI stayed responsive |

## Settings (Batch 3.1)

| Check | Result |
|---|---|
| 14-category sidebar | Present |
| Search `vault` | Finds Vault size cap; result opens Backups & recovery |
| Search `protected` | Finds Protected paths |
| Multi-word search `low space` | Finds Low-space warning |
| Reset field/category/all | Core tests pass; UI reset controls wired |
| Atomic save and `.bak` | Core tests pass; second save keeps the previous file |
| Corrupt file | Core test falls back to defaults with a notice; focused UI check confirmed the notice is visible; real settings were backed up and restored |
| Newer schema | Core test falls back to defaults with a notice |
| Advanced unlock | Wrong word keeps Unlock disabled; `ADVANCED` unlocks the session |
| Dangerous changes | Custom `RulesPath`, clearing `ProtectedPaths`, and `VaultCapGb = 0` are guarded |

## Clean preview

| Metric | Result |
|---|---|
| Files scanned | 1,297,124 |
| Blocked by safety checks and unticked rules | 1,200,573 |
| Actionable items | 96,551 |
| Review button | `Clean 96,551 items` |
| Plan time | 51.0 s |
| Total preview | about 75 s |
| Scan | about 23 s |
| File-facts phase | 48.6 s |

The preview is 4.7x faster than the earlier 352.3 s plan measurement, but 1.3 million rows are still a large UI Automation surface.

## Cleanup

- App data was backed up before fixture verification and restored afterward.
- No fixture paths remain in `settings.json`.
- No review-queue file remains from the fixture run.
- No CDClean process remains.
- No `CDClean-Weekly` or `CDClean-Daily` scheduled task remains.
- Fixture folders and temporary release folders were removed.

## Known limitations

- The foreground-only `Shift+F10` row menu is not covered by the background UI test.
- External UI Automation or screen-reader traversal of the full 1.3M-row Clean preview can be slow. The in-app preview is usable, but the table is not virtualized for automation traversal.
- The developer-cache scan matches broad `bin`, `obj`, and `dist` folders. Result-quality tuning is planned for Tier 3.
- Admin/UAC tasks, Fast Scan/MFT, real-data destructive flows, and code signing are outside this pass.
- The release is not code-signed. Windows SmartScreen may warn on first launch.

## Internal evidence

The full internal report and test logs contain machine-specific paths and are not committed. The committed summary above is the public record.
