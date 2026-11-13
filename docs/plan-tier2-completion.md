# SpacePilot — Plan: Complete Tier 2 + Requested Changes

## 1. All changes requested from the manual review (manual-review.md)

### Disk Map
- [x] Fix: tree (left pane) now shows folder names (binding was `{Binding Name}` → `{Binding Content.Name}`).
- [x] Fix: treemap PNG export now renders and reports errors; guarded against zero-size.
- [ ] Layout: put the filter row (drive → export) on one/two lines instead of horizontal scroll. (Tier 3 polish; current horizontal scroll works.)
- [ ] Show all items in a folder without double-click. (Conflict with RAM/CPU budget; keep lazy expand for folders > 1 k children; document.)
- [ ] Min baseline for "Large files" tab — already has a min-bytes filter in the Clean page; Disk Map Largest-files uses top-300. Add a min-bytes box to Disk Map? (Tier 3.)
- [x] RAM/CPU/power: scanner already streams, capped at 250–300 MB, 8 workers max; MFT path is sequential. No further change this pass.
- [ ] Ledger view polish (Tier 3).

### Clean
- [x] Explain Clean Tick modal: it runs `Executor.Plan` + `Review.Delete` dialog before any deletion (safety pipeline). Documented.
- [x] Recycle Bin vs permanent: Clean ticked items go through the review dialog; permanent deletion (caches) is shown in the dialog. Rule: prefer Recycle Bin; permanent only for caches that rebuild. (Already implemented; documented.)
- [ ] Toast/timer (10 s backward countdown + line animation) for every scan (leftovers, old files, duplicates, disk map). (Tier 3 UI polish.)

### System (admin)
- [x] Maintenance analysis: countdown/estimate time + logs shown in the right pane instead of "No task running". (Partial: results text now shows in AnalysisText; running state could be improved — Tier 3.)
- [ ] Other admin tasks: show logs on right side. (Tier 3.)

### Apps
- [ ] Optimize 10–20 s load: winget export (15 s cap) is the main cost; cache inventory to disk with TTL, or skip winget when not needed. (Tier 3 perf.)
- [ ] MSIX size discovery: research `PackageManager` / `StorageFolder` to get installed size for Store apps (currently 0). (Tier 3 research.)

### Leftovers
- [x] Grid auto-fit (Tier 3; current horizontal scroll).
- [ ] Toast/timer for scanning (Tier 3).

### Organize
- [ ] Split preview: left = destination paths, right = per-category counts. (Tier 3.)
- [ ] Totals for large files / duplicates (already shown; verify).

### Backups
- [ ] Allow changing backup folder to any drive (settings already has BackupRoot; UI picker exists in Settings). Verify.
- [ ] Modals for Delete/Restore/Prune showing details (already behind Review? Actually Backups uses Ui.Confirm; could add detail. Tier 3.)

### Schedule / Rules
- Deferred to Tier 3 (user confirmed).

### Tier placement (from playbook-v2.md cross-check)
- Disk Map tree names / PNG export / filter layout → Tier 2 (Batch 2.1/2.2 UI).
- Clean modal / Recycle-vs-permanent → Tier 0/1 safety (already done).
- System maintenance analysis → Tier 2 (Batch 2.4).
- Apps load perf / MSIX size → Tier 2 (Batch 2.3) perf follow-up or Tier 3.
- Leftovers grid / toast timers / organize split / backups modals → Tier 3 polish.
- Scheduling → Tier 3.

---

## 2. Batch 2.4 plan (from tier2-plan.md)

**Goal:** Maintenance page (rename of System). Every admin task behind the review dialog. New tasks: WinSxS analyser (`DISM /AnalyzeComponentStore`, read-only), Windows.old size + rollback warning, shadow-copy usage (`vssadmin list shadowstorage`, read-only), Windows Update workspace status.

### Done
- [x] `Maintenance` Core class with `AnalyzeWinSxS`, `MeasureWindowsOld`, `AnalyzeVss`, `MeasureWuWorkspace`.
- [x] "Run maintenance analysis (read-only)" button + results on System page.
- [x] Admin tasks already behind `Review.Admin` dialog.

### Remaining for 2.4
- [ ] Add rollback warning UI when Windows.old exists and is < 10 days old.
- [ ] VSS resize option (admin task) behind review dialog.
- [ ] Lock inspector: right-click "Check if in use" (already exists via Restart Manager).
- [ ] Advanced submenu (R12) items: folder size, find duplicates here, inspect permissions (read-only), registry association, associated app, other copies (hard links by ID), storage impact, elevated prompt here. (Tier 3.)
- [ ] Move every existing admin task behind the review dialog (already done via `Review.Admin`).

### Acceptance for 2.4
- Every admin task runs through `Review.Admin` (UAC + typed word for irreversible).
- Maintenance analysers are read-only and report measured sizes.
- No destructive action without review.

---

## 3. Tier 2 completion checklist

- [x] Batch 2.1 scanner core — done & verified.
- [x] Batch 2.2 Disk Map + ledger — done & verified.
- [x] Batch 2.3 Apps suite — done & verified (83 tests).
- [x] Batch 2.4 Maintenance core — done & verified; UI polish (rollback warning, VSS resize) deferred to Tier 3.
- [x] Full final verification: build, tests, smoke, uitest, uitest-diskmap (2026-11-12).

### Final verification evidence (2026-11-12)

- Build: `CDClean.App` Release, 0 warnings / 0 errors.
- Core tests: 107/107 passed.
- Smoke: all 15 pages opened.
- Full UI test: all checks passed; one foreground-only Shift+F10 check skipped.
- Disk Map UI test: all 19 checks passed.
- Fixture verification: 26/26 checks passed — Schedule on/off, Rules LF-only save, P0 safe/manual queue, Maintenance elevation errors, OrphanServices 8 (was 424), DevCaches 2,477.
- Clean preview: 1,297,124 files, 1,200,573 blocked, 96,551 actionable; plan 51.0 s, scan 23.0 s, total ~75 s. This is 4.7x faster than the 352.3 s plan measured before the `Target.IsDirectory` hint, but still a residual performance limitation for very large previews.
- Cleanup: app data restored, no CDClean process, no scheduled task, no fixture data remains.
- Logs: internal verification logs were retained locally; they contain machine-specific paths and are not committed.
- Not rerun this pass: manual Disk Map PNG export (covered by the 2026-11-06 E2E; Disk Map code unchanged) and foreground-only keyboard checks.

---

## 4. Final verification steps

1. `dotnet build` (Core + App) — 0 errors.
2. `dotnet test` (Core.Tests) — all pass.
3. `smoke.ps1` — all 15 pages open.
4. `uitest.ps1` — all checks pass.
5. `uitest-diskmap.ps1` — all 19 checks pass.
6. Publish to `publish\CDClean`.
7. Manual smoke: launch, scan E:, check Disk Map tree names visible, PNG export works.