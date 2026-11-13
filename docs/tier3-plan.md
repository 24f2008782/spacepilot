# Tier 3 plan: Settings, automation, history, specialists

| Field | Value |
|---|---|
| Date | 2026-11-05 |
| Starts after | Tier 2 (Batches 2.1–2.4), verified 2026-11-12 (v1.5.3) |
| Source | `playbook-v2.md` §6.3 (Tier 3 batches), §6.1 (features F-01..F-28, Tier 3 subset), `manual-review.md` deferred polish items, `plan-tier2-completion.md` |
| Platform | Windows only, WinUI 3 |
| Rule for this tier | Polish and automation only after the core is verified. Every destructive action keeps its review dialog. |

## 1. What Tier 3 must deliver

From the playbook §6.3 and the deferred Tier 2 polish list:

- A real Settings page with its own sidebar (14 categories), search, reset, typed confirmation for dangerous settings.
- Preview-first automation: scheduled runs execute Safe-class rules only; everything else goes to a review queue shown on next launch; undo history.
- Storage history / "what grew" (snapshots, shown on the Dashboard).
- Startup manager + orphans (disable rather than delete).
- VHDX compactor (WSL / Docker).
- Developer cache manager (`node_modules` / `target` finder).
- Game library analyser (Steam ACF, Epic manifests) — extend the Tier 2 inventory.
- Maintenance console (DISM → SFC order, CHKDSK, Optimize).
- Duplicate & similar finder (extend Duplicates page).
- Downloads triage.
- Browser cache analyser.
- Orphan services/tasks report (read-only first).
- MSI patch / Installer cache auditor.
- Cloud placeholder lens.
- Advanced submenu (R12) items.
- Tier 2 polish carried over: toast/timer for scans, filter layout, Leftovers grid auto-fit, Organize split preview, Backups folder picker + detail modals, Apps load perf + MSIX size, MFT fast scan completion (signing + admin wiring).

## 2. Batches

### Batch 3.1: Settings (R14) [playbook §4.16, §8.9]
Files (new unless marked):
- `App/Views/SettingsPage.xaml` + `.cs`: rewrite as a page with its own sidebar (14 categories) instead of the current flat list.
- `Core/Settings/SettingsStore.cs`: versioned, atomic save with `.bak`, per-field/per-category reset, corrupt-file fallback to defaults with a notice.
- `Core/Settings/SettingsSearch.cs`: search across all settings.
- Dangerous settings in a bordered "Advanced risk settings" box with typed confirmation the first time.

Categories:
1. General, 2. Appearance, 3. Scan, 4. Drives, 5. Cleanup rules, 6. Uninstall, 7. Registry safety, 8. Exclusions & protected, 9. Backups & recovery (vault drive, cap, retention), 10. Performance, 11. Notifications (low-space alert, default 60 GB), 12. Privacy & diagnostics, 13. Advanced (CLI, feature flags), 14. About & updates.

Approved decisions (recommended by the agent, accepted 2026-11-12):
- Categories with no backing setting stay as placeholders; Batch 3.1 does not invent new settings.
- Dangerous settings are custom `RulesPath`, clearing `ProtectedPaths`, and `VaultCapGb = 0`.
- Typed confirmation is one `ADVANCED` unlock per app session.
- The implementation records these decisions in code comments so a later change is deliberate.

Acceptance:
- Search finds any setting by name.
- Reset per field / per category / everything (double confirm).
- Corrupt `settings.json` falls back to defaults with a notice.
- Dangerous settings require typed confirmation once.

### Batch 3.2: Automation, history, startup manager
- F-23 Preview-first automation:
  - Scheduled runs execute Safe-class rules only; non-Safe items go to a review queue shown on next launch.
  - Undo history for every run.
  - `--clean-safe` keeps its current behaviour for caches.
- F-26 Storage history / "what grew":
  - Snapshot of drive used-space + top folders after each scan, stored under `%LOCALAPPDATA%\CDClean\scans\`.
  - Dashboard shows "what grew since last scan" (diff).
  - Retention limit; never uploaded.
- F-15 Startup manager + orphans:
  - List startup entries (Run/RunOnce, scheduled tasks, services).
  - Disable rather than delete; measured cost only.
  - Orphan detection (entries pointing at missing executables).

Acceptance:
- Scheduled run never deletes non-Safe items silently.
- Review queue appears on next launch with counts.
- "What grew" diff is shown on the Dashboard after a second scan.

### Batch 3.3: Specialists (VHDX, dev caches, games, maintenance console)
- F-20 VHDX compactor (WSL / Docker): list `.vhdx` files, show size, refuse while running, compact only with confirmation.
- F-21 Developer cache manager: `node_modules` / `target` / `__pycache__` finder with size, documented prune commands (never touch source), uv/pip/npm prune.
- F-25 Game library analyser: extend Tier 2 Steam/Epic inventory with save protection, size per game, library folder sizes.
- F-24 Maintenance console: enforce DISM → SFC → CHKDSK → Optimize order; read-only first.

Acceptance:
- VHDX compaction refuses while the VM/container is running.
- `node_modules` finder never touches source files.
- Game saves are protected by default.

### Batch 3.4: Finders and analysers
- F-01 Duplicate & similar finder: extend `Finders.Duplicates` (size → SHA-256) with image similarity (perceptual hash) for media; pick which copy survives; never delete a whole group; skip hard links.
- F-03 Downloads triage: 90-day floor; vault/recycle only.
- F-04 Browser cache analyser: cache only; browser must be closed; Chrome/Brave/Edge/Firefox.
- F-16 Orphan services/tasks report: read-only first.
- F-18 MSI patch / Installer cache auditor: `C:\Windows\Installer` analysis, MSI API only, vault before removal.

Acceptance:
- Duplicates never deletes the last copy of a group.
- Browser cache removal requires the browser to be closed.
- MSI cache removal is vaulted and reversible.

## 3. Tier 2 polish carried into Tier 3

From `manual-review.md` deferred items:
1. Toast/timer (10 s backward countdown + line animation) for every scan (leftovers, old files, duplicates, disk map, apps).
2. Filter row layout (single/two lines instead of horizontal scroll) on Disk Map.
3. Leftovers grid auto-fit to viewport.
4. Organize split preview: left = destination paths, right = per-category counts.
5. Backups: allow changing backup folder to any drive (UI picker) + detail modals for Delete/Restore/Prune.
6. Apps: optimize 10–20 s load (cache winget inventory with TTL, or skip when not needed); MSIX size discovery (research `StorageFolder`/`PackageManager`).
7. MFT fast scan completion: code signing, admin re-launch wiring, in-process vs elevated helper streaming.
8. Ledger view polish.
9. System page: show logs on the right side for all admin tasks.
10. Clean: permanent-deletion items shown in red in the review modal.

## 4. Tier 2 open items closed by Tier 3

- VSS resize admin task (behind review dialog).
- Advanced submenu (R12) items.
- Lock inspector (already exists via Restart Manager).

## 5. Performance targets (playbook §9.9, §10.6)

| Case | Target |
|---|---|
| Cold start to dashboard | ≤ 1.5 s |
| 1 M files, NVMe, MFT backend | ≤ 45 s (1 TB ≤ 60 s) |
| Treemap relayout, 5k rectangles | ≤ 2 ms |
| Grid, 1 M rows | 60 fps scroll; sort ≤ 400 ms; filter keystroke ≤ 120 ms |
| Memory ceiling | 256/512/1024 MB setting; spill large trees to disk |

Tier 3 must not regress these. RAM and CPU usage stay minimal: streaming scans, chunked node store, 8-worker cap, no all-folders dictionary.

## 6. Cut lines if time is short (playbook §10.5)

| If you must cut | Cut this | Keep this |
|---|---|---|
| Settings | Per-field reset, search | Atomic save, corrupt-file fallback |
| Automation | Scheduling | Manual review queue |
| Specialists | Everything except Windows.old and WinSxS analysis | The review dialog |
| Polish | Toast timers, filter layout, organize split | Ledger, unreadable tab |

## 7. Test plan

- Core tests (xUnit) on synthetic fixtures in `%TEMP%`.
- Background UI checks (`tools\uitest.ps1`, `tools\uitest-diskmap.ps1`).
- Smoke test (`tools\smoke.ps1`) opens every page.
- Admin/UAC tests listed as "not verified" until run elevated.
- New tests per batch: settings round-trip, automation review queue, VHDX refuse-while-running, downloads 90-day floor, browser-closed check, MSI cache vault round-trip.

## 8. Effort estimate

Tier 3 is 4 batches. The playbook estimates Tiers 0–3 combined at 27–39 weeks for one full-time developer; Tier 3 is the last of those four tiers. Tiers 4–5 add another 8–14 weeks (publishing, Linux/macOS). For a solo developer, the cut lines in §6 matter more than the estimates.