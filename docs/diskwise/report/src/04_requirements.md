# 4. Existing Application Improvement Audit

## 4.1 Audit status and method

**No source code, build configuration, screenshots or module descriptions of the existing application were supplied.** The workspace was empty at the start of this engagement and remained so apart from the artefacts listed in §12. Consequently:

- **Nothing below is a report of what your application currently does.** Every "current behaviour" cell is marked **[PENDING VERIFICATION]**.
- What follows is a **target specification**: the behaviour each requirement demands, the data and interfaces it implies, its acceptance criteria, and the specific questions an engineer must answer by reading your code before touching it.
- Where a requirement is already satisfied in a conventional C/Win32 application (for example, an existing list view), the work is framed as *audit-and-extend*, not *replace*. §4.17 states the rule applied throughout.

**Estimated complexity** uses S (≤3 engineer-days), M (1–2 engineer-weeks), L (3–6 engineer-weeks), XL (>6 weeks) for a competent Windows C developer working in an established codebase. These are estimates, not measurements.

## 4.2 Traceability summary

| Req | Title | Priority | Complexity | Depends on | Implementation status |
|---|---|---|---|---|---|
| R1 | Professional application icon | P1 | S | — | **Delivered** (assets built, §12.1) |
| R2 | Unified tables & advanced multi-selection | P0 | L | R8, selection core | Specified; core **prototyped and tested** (§12.2) |
| R3 | Safe deletion & warning dialogs | P0 | L | R2, R9, gate core | Specified; gate **prototyped and tested** |
| R4 | Comprehensive application uninstallation | P0 | XL | R7, leftover engine, backup | Specified |
| R5 | Polish every existing module | P1 | M | R13 | Specified; **pending code/screenshot audit** |
| R6 | Right-click context menus | P1 | M | R2 | Specified |
| R7 | Drive selection | P1 | M | volume service | Specified |
| R8 | Table headers, sorting, columns | P1 | M | R2 | Specified |
| R9 | Pre-execution review for admin operations | P0 | M | R3 | Specified |
| R10 | Clean Run preview + Disk Intelligence map | P2 | XL | scanner core, R8 | Specified |
| R11 | Open-source reuse feasibility | — | S | — | **Delivered** (§7) |
| R12 | Advanced context-menu actions | P2 | M | R6, F-02/F-03 | Specified |
| R13 | Responsive dashboard & DPI | P1 | M | layout system | Specified |
| R14 | Redesigned settings | P2 | L | settings store | Specified |
| R15 | Research-driven additional features | P2–P3 | XL | §5 | **Delivered as proposals** (§5) |

## 4.3 R1 — Professional application icon

**Status: delivered.** Master SVG, dark/light/mono and small-size optical variants, 34 PNG exports, a 10-resolution ICO (16–256 px) and preview sheets exist in `assets/icons/`. Design rationale and sizing behaviour are in §8.1 and verification in §12.1.

| Acceptance criterion | Result |
|---|---|
| Distinctive silhouette, not confusable with CCleaner/Revo/WizTree iconography | [CONFIRMED] reviewed as a size ramp: arc + bars + shard reads as "disk + analysis + reclaim" |
| Editable vector master | [CONFIRMED] `assets/icons/svg/diskwise_master.svg` |
| Transparent PNG assets | [CONFIRMED] `assets/icons/png/` (34 files) |
| Windows ICO with standard sizes | [CONFIRMED] 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 |
| Legible at 16 px | [CONFIRMED] separate optical variant used for 16–24 px |
| Light and dark variants | [CONFIRMED] `diskwise_light.svg`, `diskwise_dark.svg` |
| Taskbar / title bar / Start menu / shortcut use | [PENDING VERIFICATION] requires embedding in the application's `.rc` and testing on a real Windows host |

**Remaining work.** (a) Add the ICO to the resource script as `IDI_APP_ICON` with `LoadIconMetric`/`LoadIconWithScaleDown` for DPI-correct selection. (b) Provide `AppUserModelID` so taskbar grouping and the jump list use the new icon rather than the host process icon. (c) Ship a 44×44 and 150×150 "targetsize" asset set for packaged/MSIX distribution if that path is chosen. (d) Do not ship the mono variant as the shell icon; it is for in-app and documentation use.

## 4.4 R2 — Unified tables and advanced multi-selection

**Current behaviour [PENDING VERIFICATION].** Unknown whether the existing modules share a grid control, whether selection is index-based, and whether selections survive filter changes. This requirement is the highest-risk item in Part 2 because a wrong selection model silently widens destructive scope.

### 4.4.1 Unified column schema

One shared table model, with columns declared per module and rendered by a single grid control:

| Column | Type | Notes |
|---|---|---|
| ☑ | checkbox | tri-state header (none / partial / all *visible*) |
| Name | text | primary sort, natural ordering |
| Type | enum | file / folder / registry key / app / package / service / task |
| Path | text (ellipsised) | middle-ellipsis, copyable, tooltip full path |
| Size | allocated bytes | right-aligned, IEC units, "Size on disk" toggle |
| Age (months) | integer | derived from chosen timestamp source |
| Depth | integer | folder depth from scan root |
| Created / Modified | datetime | localised, sortable |
| Last-used evidence | enum + detail | atime / prefetch / registry MRU / launcher log / none |
| Associated app | text | resolved owner, with confidence chip in leftovers |
| Risk | enum | Safe / Review / High / Protected |
| Recommended action | text | "Keep", "Move to Recycle Bin", "Quarantine", "Review" |

Shared rendering rules: row height 24 px at 100% DPI (scales with DPI, never fixed pixels), 8 px cell padding, header 28 px, alternating row tint at 3% opacity, focus ring visible, 1 px column separators at 8% opacity, selection uses a single accent fill with sufficient contrast in both themes.

### 4.4.2 Selection model (the part that must be correct)

- Selection is stored as a **set of stable identities** — `(volume serial, 128-bit file id)` from `FILE_ID_INFO` — **never row indices and never paths alone**. The reference core implements exactly this (`dw_key`), with hashing, tombstones and load-factor growth verified at 200 000 entries (§12.2).
- The view is a filter over the record set. `dw_sel_apply_view` computes, in one pass: visible rows, selected-and-visible count, selected-but-hidden count, selected-and-hidden byte total, and stale selections (§12.2).
- **Bulk verbs operate on "visible ∩ selected" only.** Invert affects only visible rows. "Select all matching" evaluates the *primary* filter across the whole result set, not the rendered page.
- Two search controls: **Search** (primary predicate over all records) and **Search within results** (secondary predicate over the current view). Both are composable and both are shown as removable chips above the grid.

### 4.4.3 Hidden-selection handling (mandatory)

- The bulk toolbar always shows `N selected · X GB`, split as `visible` and `hidden` when hidden > 0.
- When hidden selections exist, the toolbar renders an amber chip: *"3 selected items are hidden by the current filter — Review"*. Clicking it clears filters and scrolls to the first hidden selection.
- **Every destructive dialog enumerates hidden selections explicitly** and requires a separate acknowledgement before including them. Default is **exclude hidden**, not include.
- Selections that no longer exist after a rescan are marked **stale**, are never silently dropped, and block execution until the user reviews or clears them.

### 4.4.4 Required interactions

Ctrl+Click toggles · Shift+Click range-selects over the *visible order* · Ctrl+A selects visible · Ctrl+Shift+A deselect-all · Ctrl+I invert visible · context-menu entries mirroring each · space toggles focused row · Esc clears focus, not selection · selection persists across sorting, filtering, paging and rescan.

### 4.4.5 Acceptance criteria

1. Selecting rows, changing any filter, then returning to the unfiltered view preserves exactly the same identities (**verified in prototype**).
2. A bulk delete with 2 visible and 1 hidden selection offers to delete 2 by default and states that 1 is excluded.
3. "Select all matching" on a 2 000 000-row result selects every match, not only rendered rows; the count updates without blocking the UI for more than 100 ms.
4. Sorting never changes membership; the selected count is unchanged across sorts.
5. Invert affects only visible rows; records outside the view are untouched (**verified in prototype**).
6. After a rescan that deletes one selected file externally, the item is reported as stale, count includes it, and no destructive action proceeds until resolved (**verified in prototype**).
7. Selection count and size refresh within 50 ms of any change for up to 100 000 selections.

**Pending verification:** grid toolkit (common controls `SysListView32`, MFC, custom Direct2D), whether a virtual list (`LVS_OWNERDATA`) is already used, existing selection storage, and whether module-level filters share a predicate language.

## 4.5 R3 — Safe deletion and warning dialogs

**Current behaviour [PENDING VERIFICATION].** It is unknown whether the current application deletes directly, uses the shell (`IFileOperation`), or requires confirmation at all.

### 4.5.1 Risk classification

| Class | Definition | Default action | Confirmation |
|---|---|---|---|
| **Safe Candidate** | Disposable by construction *and* verified: temp files older than a policy age, caches with a documented rebuild path, quarantineable artefacts, files inside a scanned job's own work area | Quarantine or Recycle Bin | Single explicit confirm |
| **Review Required** | Ownership or usage uncertain: last-access older than threshold but atime unreliable, no application attribution, ambiguous path | Recycle Bin only | Confirm + per-item acknowledgement above 20 items |
| **High Risk** | Likely in use or shared: hard links > 1, file locked, cloud placeholder, recently modified, inside an application's active install tree | Blocked by default; individual override only | Strong confirmation (typed word set by policy) |
| **Protected** | Windows-critical or OS-owned: `%SystemRoot%` except supported servicing paths, `WindowsApps`, `System Volume Information`, boot/system files, any path on the protected list | **Never deletable from the product** | Not offered; explanation shown |

### 4.5.2 Confirmation dialog content (mandatory fields)

Every deletion opens a review dialog stating: item count (visible/hidden split) · total estimated reclaim **and** measured free-space baseline · affected applications with confidence · recent-use evidence per grouping · highest risk class present · whether an application may stop working · reversibility (Recycle Bin / quarantine vault / permanent — with retention window) · elevation required (yes/no and why) · exclusions that will not be touched · and an explicit statement that heuristic classification **cannot prove** a file is unused.

### 4.5.3 Execution pipeline

`plan → re-validate → classify → stage → execute → verify → record`

1. **Plan:** freeze the job (identity set + metadata snapshot) and compute the predicted reclaim.
2. **Re-validate immediately before acting:** re-query each identity (`GetFileInformationByHandleEx`/`FILE_ID_INFO`), compare with the snapshot. Identity mismatch, missing file, new lock, or newly-changed size ⇒ that item is **skipped and reported**, never "best-effort deleted".
3. **Classify** through the policy gate, which returns `ALLOW`/`WARN`/`BLOCK` plus machine-readable reasons (**implemented and verified**, §12.2).
4. **Stage:** move to the quarantine vault (same volume, atomic rename, fallback to copy+verify for cross-volume), or to the Recycle Bin via `IFileOperation` with `FOFX_RECYCLEONDELETE`.
5. **Execute** with bounded concurrency (2–4 workers), `FILE_FLAG_BACKUP_SEMANTICS` where required for directories, and handle sharing violations by queuing a delete-on-reboot only with explicit consent.
6. **Verify:** confirm absence, recompute free space, reconcile predicted vs actual.
7. **Record** the manifest (JSONL) with the reason codes for every item.

### 4.5.4 Acceptance criteria

1. Zero deletions occur without an explicit confirmation; there is no "remember my choice" that skips the dialog for High Risk items.
2. Changing a file between scan and delete (replace with a different file at the same path) results in a **skip with reason**, not a deletion (**verified rule in prototype**).
3. A junction or symlink selected for deletion removes the link only; the target is never traversed (**verified rule**).
4. A hard-linked file produces a warning that includes the link count and the other paths when obtainable.
5. A cloud placeholder produces a warning that deletion may propagate to the cloud provider.
6. A locked file is reported with the holding process name and offers delete-on-reboot instead of failing silently.
7. Recycle-Bin deletion is distinguishable in the UI and in the audit log from permanent deletion.
8. Cancel mid-operation leaves the application consistent, records partial completion, and the verification pass reports exactly which items were acted on.
9. Predicted reclaim is within ±5% of measured, or the discrepancy is explained (cluster size, sparse files, delayed allocation).

## 4.6 R4 — Comprehensive application uninstallation

**Current behaviour [PENDING VERIFICATION].** Discovery sources, uninstall invocation method and any existing leftover logic are unknown.

### 4.6.1 Orchestration stages

| Stage | Sources / APIs | Output |
|---|---|---|
| **1. Inventory** | `MsiEnumProductsEx` + `MsiGetProductInfoEx`; `HKLM/HKCU …\Uninstall` (both 32/64 views); `PackageManager` APIs for MSIX; WinGet via COM/CLI bridge; launcher manifests (Steam ACF, Epic JSON) | Canonical app record with provenance per field |
| **2. Classify** | Installer type (MSI product code, registered uninstaller path, packaged, portable, broken) | Uninstall strategy |
| **3. Pre-flight** | Restart Manager, running processes from install path, dependent services, pending reboot flags, elevation requirement | Blockers and warnings |
| **4. Execute** | MSI: `MsiInstallProduct` with `REMOVE=ALL` (never `msiexec /x` string parsing) · registered: `CreateProcess` on the declared command with correct elevation · packaged: `PackageManager::RemovePackageAsync` · WinGet: `winget uninstall --id <exact>` | Exit status, duration, captured logs |
| **5. Diff leftovers** | Snapshot before/after: install tree, `%ProgramData%`, per-user `%LocalAppData%`/`%Roaming%`, services, tasks, Run keys, shortcuts, file associations, firewall rules, MSI cache references | Candidate set with evidence |
| **6. Score ownership** | Signals described in §9.5 | Confidence class A/B/C, never a boolean |
| **7. Review** | Preview tree with per-item size, evidence, confidence | User decisions |
| **8. Remove** | Quarantine-first through the R3 pipeline; registry keys exported (`.reg`) before removal | Manifest + restore bundle |
| **9. Verify** | Re-scan for residue; report removed / skipped / failed / preserved | Final report |

### 4.6.2 Non-negotiable rules

- The application's own uninstaller **must** be used first, always.
- Name similarity alone is **never** sufficient to remove a registry key or a folder. If no stronger signal exists, the item is *Unverified* and excluded from bulk actions.
- Shared runtimes (VC++ redistributables, .NET, WebView2), shared COM registrations, and anything under a different product's install root are marked **shared** and preserved unless the vendor's own uninstaller removed them.
- Registry modification requires an export first, and the exported file is included in the job's restore bundle.
- No claim of "complete removal" is ever made in the UI. The report says what was removed and what was deliberately preserved.

### 4.6.3 Acceptance criteria

1. MSI, registered-executable, packaged and WinGet-managed applications each uninstall through their native mechanism, with exit codes surfaced.
2. Applications installed on a non-system drive uninstall correctly, and the leftover scan covers the actual install volume.
3. A broken entry (missing uninstaller) is detected before execution and routed to the repair path, not attempted blindly.
4. A shared registry value referenced by two products is never removed when only one is uninstalled.
5. The final report distinguishes removed / skipped / failed / preserved, with a reason per item.
6. Every registry key removed has a matching `.reg` export in the restore bundle; the bundle can be applied to restore state.
7. Uninstalling a 5 000-file application produces a leftover candidate list in under 60 s on NVMe and does not block the UI.

## 4.7 R5 — Polish every existing module

**Current behaviour [PENDING VERIFICATION].** This requirement cannot be specified concretely without the existing screens.

### 4.7.1 Audit checklist to apply to each existing page

| Dimension | What "good" means | Evidence of a gap |
|---|---|---|
| Empty state | Explains *why* the page is empty and what to do next; offers the primary action as a button | Blank grid; unchanged "0 items" label |
| Loading state | Skeleton or determinate progress with the current path/count; cancel available | Frozen window or indeterminate spinner after 2 s |
| Error state | Human sentence + cause + next step + copy-details | Raw `HRESULT` or silent failure |
| Keyboard | Tab order, mnemonics, F5 refresh, Del guarded, Ctrl+A/C, Esc dismiss | Mouse-only actions |
| Bulk actions | Disabled until selection exists; label includes count | Always-enabled buttons |
| Consistency | Same toolbar position, same button order, same confirm wording, same risk colours | Per-page variations |
| Density | 24 px rows at 100%, consistent padding, no clipped text | Mixed row heights |
| Focus | Visible focus ring; focus restored after dialogs | Focus lost to nowhere |
| Navigation | Same back/forward semantics; no dead ends | Modal traps |

### 4.7.2 Rule for existing functionality

Existing modules are **audited first and extended second**. A replacement is permitted only when the existing implementation cannot satisfy an acceptance criterion in R2/R3/R8 (for example, an index-based selection store must be replaced because it cannot express identity-persistent selection). Any replacement must keep the module's user-visible capabilities; removing one requires written justification in the pull request.

### 4.7.3 Acceptance criteria

1. Every page has explicit empty, loading, error and success states, each reachable in a test harness.
2. No operation blocks the UI thread for more than 100 ms; long operations show progress and can be cancelled.
3. Every action available via mouse is reachable via keyboard, verified by keyboard-only walkthrough of each page.
4. All confirmation strings come from a single string table keyed by risk class — no ad-hoc wording.
5. A cold start of each module shows a usable skeleton within 300 ms even while a scan is running.

## 4.8 R6 — Right-click context menus

**Current behaviour [PENDING VERIFICATION].**

### 4.8.1 Action matrix

| Action | File | Folder | Leftover | App | Registry key | Notes |
|---|---|---|---|---|---|---|
| Open | ✔ | ✔ | ✔ (if exists) | ✔ (launch) | — | Default verb via `IContextMenu` or `ShellExecuteEx` |
| Open with… | ✔ | — | ✔ | — | — | `OpenWith` dialog |
| Show in Explorer | ✔ | ✔ | ✔ | ✔ (install dir) | — | `SHOpenFolderAndSelectItems` |
| Delete… | ✔ | ✔ | ✔ | — | — | Opens R3 dialog; never immediate |
| Move to Recycle Bin | ✔ | ✔ | ✔ | — | — | `IFileOperation` |
| Quarantine | ✔ | ✔ | ✔ | — | — | Internal vault |
| Properties | ✔ | ✔ | ✔ | ✔ | ✔ | `SHObjectProperties` / own sheet |
| Copy path / name / details | ✔ | ✔ | ✔ | ✔ | ✔ | Details = TSV row |
| Analyse folder size | ✔ | ✔ | ✔ | ✔ | — | Hands to Disk Map (F-02/R10) |
| Find duplicates here | — | ✔ | — | — | — | F-01 |
| Check if in use | ✔ | ✔ | ✔ | — | — | Restart Manager (F-17) |
| Inspect permissions | ✔ | ✔ | ✔ | — | ✔ | Read-only ACL view |
| Inspect registry association | ✔ | ✔ | ✔ | ✔ | — | Opens registry inspector at the key |
| View associated application | ✔ | ✔ | ✔ | — | — | Owner resolution with evidence |
| Select similar items | ✔ | ✔ | ✔ | ✔ | — | Applies a derived predicate to the current view |
| Add to exclusions | ✔ | ✔ | ✔ | ✔ | ✔ | Writes to protected list (F-28) |
| Open command prompt here | — | ✔ | — | — | — | Elevated variant marked |

### 4.8.2 Rules

- **Conditional availability:** an action is present only if it can succeed. No greyed-out "Delete" on Protected items — instead the item shows a *why protected* explanation entry.
- **Grouping:** primary verbs first; destructive verbs in a separated group with a divider; advanced verbs in a submenu ("Advanced ▸"). Aim for ≤ 9 top-level entries.
- **Keyboard:** `Shift+F10`/menu key opens at the focused row; arrow navigation; Enter activates; Esc closes and restores focus to the grid.
- **Multi-selection:** commands that only make sense for one item (Properties, Open with) operate on the focused item and are labelled accordingly; commands that work on sets operate on all selected visible items and state the count.

### 4.8.3 Acceptance criteria

1. No menu entry can silently affect items outside the current selection.
2. Every destructive entry routes through R3 and shows the same dialog as the toolbar path.
3. Menu construction time is under 20 ms for 100 000 selected rows (verbs are computed once, not per row).
4. Accessibility: menu items expose names and states to UI Automation (verified with Inspect/Narrator).

## 4.9 R7 — Drive selection

**Current behaviour [PENDING VERIFICATION].**

### 4.9.1 Volume service

Enumerate with `GetLogicalDriveStringsW` + `GetVolumeInformationW` + `GetDiskFreeSpaceExW` + `GetDriveTypeW`; per volume expose: letter/mount point, label, file system, drive type (fixed/removable/network/CD), total/free bytes, cluster size, `FILE_SUPPORTS_HARD_LINKS` and `FILE_PERSISTENT_ACLS` capability flags, and health/TRIM capability where available through documented interfaces.

### 4.9.2 Scope rules

| Module | Scope semantics |
|---|---|
| Disk Map | One or more volumes **or** folders; each target gets its own scan job |
| Old Files / Duplicates / Large Files | Multi-select volumes + folder inclusion/exclusion; saved as a named scope |
| System Cleanup | Per-volume where the operation is volume-local; system-wide items explicitly labelled |
| Apps | **Two independent scopes:** *install locations* (where binaries live, may be any volume) and *metadata* (registry and package state, effectively always the system volume). The UI must state that "apps on D:" still have registry entries on C: |
| Leftovers | Follows the uninstall target's install volume plus the system volume for registry residue |

### 4.9.3 Behaviour rules

- Refresh re-enumerates volumes and reconciles open scans: a disappeared volume cancels its job with a clear message, never a crash.
- Removable media is scanned only when explicitly selected; a volume with `DRIVE_REMOVABLE` shows a warning before a long scan.
- The preferred scope persists per module and is shown in the status bar ("Scanning D:, E: · 2 folders excluded").
- **Never silently widen scope.** If a saved scope references a missing drive, the module opens with the drive missing and requires confirmation, rather than defaulting to the system drive.

### 4.9.4 Acceptance criteria

1. Scanning two volumes reports combined totals and per-volume rows.
2. Disconnecting a USB drive during a scan produces a clean cancellation with a message naming the volume.
3. A folder that is a reparse point into a different volume is not followed by default; the UI explains why.
4. The Apps module can list an application installed on D: while showing the registry location on C:.
5. Scope changes never mutate an in-flight job's target set.

## 4.10 R8 — Table headers and sorting

**Current behaviour [PENDING VERIFICATION].**

- **Headers:** visible at all times, always legible, with sort indicator (▲/▼/multi-sort numeral), resize grip, and a right-click header menu (hide/show columns, reset widths, auto-fit, choose sort).
- **Sorting:** single-column by default; multi-column via Ctrl+Click on header (max 3 keys); stable sort; nulls last; size sorts on allocated bytes; name sorts with `CompareStringEx` `SORT_DIGITSASNUMBERS` and `LINGUISTIC_IGNORECASE`.
- **Persistence:** column order, widths, visibility, sort keys and per-module defaults saved per user in the settings store; a "Reset table layout" action exists.
- **Rendering:** virtualised (`LVS_OWNERDATA` or a custom virtual model) with a fixed-height row cache; never allocate one window per row.
- **Selection invariance:** sorting must not clear selections; after a sort, the selected identities and the scroll anchor are preserved.

**Acceptance criteria:** 1 000 000-row table sorts within 400 ms using a precomputed key array; column resize reflows without flicker; hidden columns are reflected in export; sorting during an active scan does not interrupt the scan; restoring layout after restart reproduces widths exactly.

## 4.11 R9 — Pre-execution review for administrative and cleanup operations

**Current behaviour [PENDING VERIFICATION].** Some modules may execute administrative commands immediately on "Run".

### 4.11.1 Mandatory modal fields

Operation name and one-sentence effect · exact commands/APIs that will run (expandable, copyable) · components/files/settings affected (with per-category estimated size) · **measured** free-space baseline and predicted saving · required privilege and why · estimated duration with the basis for the estimate · reversibility and recovery method · restart requirement (none / likely / required) · known risks in plain language · exclusions · and a *What will not happen* line (explicitly: no other drives, no user documents outside the scope, no registry keys without backup).

### 4.11.2 Confirmation classes

| Class | Examples | Confirmation |
|---|---|---|
| Standard | Temp cleanup, Recycle Bin empty, DO cache clear | Confirm button |
| Elevated | Component store cleanup, `SoftwareDistribution` reset, previous-installation removal | Confirm + elevation prompt + explicit acknowledgement of consequence |
| Irreversible | `DISM /ResetBase`, permanent delete, VSS deletion | Type-to-confirm string (`RESETBASE`) + consequence checklist ticked |
| Blocked | Protected paths, unverified registry keys, anything failing a safety prerequisite | Executable path disabled with reason |

### 4.11.3 Acceptance criteria

1. No administrative operation executes from a single click; every path passes through the review modal.
2. The modal's predicted saving is replaced by a measured result after execution, and both values are stored in the audit log.
3. Irreversible operations require the typed confirmation; paste-into-field is allowed but the string is not prefilled.
4. If a prerequisite fails at execution time (e.g. elevation declined), the operation aborts cleanly and reports why.
5. The exact command line shown to the user matches the one executed (byte-for-byte in the log).

## 4.12 R10 — Clean Run preview and Disk Intelligence (Disk Map)

**Current behaviour [PENDING VERIFICATION].** Whether a Clean Run module exists and what it does is unknown.

### 4.12.1 Clean Run upgrade

- Runs the full category model in **preview mode** by default: nothing is deleted until the user reviews.
- Two search controls: search by name/path and filter by category/age/size.
- Per-category expandable list with item counts, measured sizes, and per-item risk.
- "Explain this category" panel: what it is, what rebuilds it, what breaks if removed, and the documented Microsoft behaviour that governs it (e.g. Storage Sense scope [S65]).
- Dry-run report export (CSV/JSON) before any execution.

### 4.12.2 Disk Map module

| Element | Specification |
|---|---|
| Targets | Volume(s) or folder(s), multi-select (R7) |
| Tree | Virtualised folder tree with inclusive/exclusive size, item count, % of parent; expand-on-demand |
| Treemap | Squarified treemap, cushion shading optional; click selects; hover shows path and size; zoom into node; breadcrumb to zoom out |
| Largest files/folders | Top-N table using the R2/R8 grid, with "show in map" linkage |
| File types | Extension/type distribution with colour legend, click filters the map |
| Size model | Allocated (default) vs logical toggle; hard links counted once; placeholders shown as 0 on disk with logical size; slack shown separately |
| Filters | Age, modified date, depth, size threshold, extension, include/exclude patterns |
| Search | Within scan results only (no filesystem access), instant |
| Linked selection | Selecting in tree highlights in treemap and vice versa; largest-file row highlights its map cell |
| Progress | Determinate by item count where possible; current path; items/s; elapsed; pause/cancel/refresh |
| Safety | **Read-only.** Deletion is only reachable through explicit commands which route into R3. Scanning never modifies the volume |
| Export | CSV of tree/files, PNG of treemap, JSON of the full node list |

### 4.12.3 Acceptance criteria

1. Full scan of a 1 TB NVMe NTFS volume completes in ≤ 60 s with the MFT backend and ≤ 8 min with the fallback on the reference hardware defined in §9.9.
2. Treemap renders 50 000 nodes at ≥ 30 fps during zoom and pan.
3. Totals reconcile with `GetDiskFreeSpaceExW` within 1% when metadata and unmovable system areas are accounted for, and the difference is itemised.
4. Cancel returns within 300 ms and preserves partial results marked "incomplete".
5. Scanning is verifiably read-only: no file timestamps change (verified by comparing atime/mtime before/after on a sample set).
6. Pause/resume works, and refreshing after external changes marks changed subtrees.

## 4.13 R11 — Open-source reuse feasibility

Delivered as §7. Summary: **no third-party source code is required or recommended.** WinDirStat is GPL (viral for a closed-source product); BCUninstaller is Apache-2.0 but .NET (incompatible with a C codebase at the source level, usable only as a process-level integration); WizTree is proprietary with an EULA that forbids reverse engineering. The recommended path is an independent implementation against documented Windows APIs, with ideas (not code) taken from public documentation.

## 4.14 R12 — Advanced context-menu improvements

Delivered as an *extension* of R6 with a two-level structure: top-level verbs for everyday actions, and an **Advanced ▸** submenu containing: Folder size analysis · Find duplicates here · Find similar files · Inspect permissions · Open elevated command prompt here · Analyse associated application · Check whether file is active/locked · View parent directories · Find other copies of this file · Show storage impact (allocated vs logical, links, streams). Rules: never more than nine top-level items; advanced items require a double-level admit to their own help text; any action that performs a long scan opens a job in the relevant module rather than blocking the menu.

**Acceptance criteria:** the advanced submenu never contains an entry that silently performs a destructive action; "Find other copies" reports hard links by identity (not name); "Storage impact" shows allocation, logical size, link count and alternate data streams where readable.

## 4.15 R13 — Responsive dashboard and screen-size handling

**Current behaviour [PENDING VERIFICATION].**

| Concern | Specification |
|---|---|
| Per-monitor DPI | `PerMonitorV2` awareness in the manifest; handle `WM_DPICHANGED` by resizing the top-level window to the suggested rect and re-laying out child controls; never scale a window by bitmap-stretching |
| Layout | Grid-based layout engine with breakpoints: ≥1600 px three columns, 1200–1599 two columns, <1200 single column with collapsible sidebar; sidebar collapses to icon rail below 1100 px |
| Tables | Column priority system: low-priority columns hide at narrow widths rather than producing horizontal scroll for the whole page; horizontal scroll remains available for the grid body |
| Multi-monitor | Drag between monitors with different DPI re-renders at the new scale without relayout jumps; window position restore is per-monitor and clamped to visible work area |
| Theme | Light/dark via `AppsUseLightTheme` detection plus manual override; all custom-drawn surfaces use a theme token set; verify contrast ≥ 4.5:1 for text |
| Dashboard | Drive selector at top; metrics (used/free/reclaimable/recommended) recompute on drive switch; no metric may become stale after a switch |
| Minimum size | 1024×640 usable; below that, a message rather than a broken layout |

**Acceptance criteria:** 100/125/150/175/200% DPI tested at 1920×1080, 2560×1440, 3840×2160; maximised and restored states preserve column widths; dragging between a 100% and a 200% monitor produces no clipped text; drive switch updates every dashboard metric within 200 ms; the app starts correctly on a monitor with a different DPI than the system default.

## 4.16 R14 — Redesigned settings

**Current behaviour [PENDING VERIFICATION].**

### 4.16.1 Structure

Dedicated settings page with a left sidebar of 14 categories, a search box filtering settings by name and description, and a content pane using consistent control groups (switch with inline description, dropdown, numeric field with validation and unit, path picker, list editor with add/remove/reorder).

| # | Category | Representative settings |
|---|---|---|
| 1 | General | Language, start page, confirm-on-exit, default module |
| 2 | Appearance | Theme (system/light/dark), density, accent, treemap palette, reduce motion |
| 3 | Scan Preferences | Fast MFT scan toggle, fallback behaviour, follow reparse points (default off), include hidden/system, max depth, exclusion of network paths |
| 4 | Drive Preferences | Default scope per module, include removable media, network path policy |
| 5 | Cleanup Rules | Per-category age thresholds, "never touch" age floor, Recycle Bin vs permanent default, quarantine retention |
| 6 | Application Uninstallation | Use native uninstaller (locked on), leftover scan depth, registry export before change, packaged-app handling |
| 7 | Registry Safety | Read-only mode (default), require export, allowed-key scope, protected key list |
| 8 | Exclusions and Protected Paths | Paths/patterns/extensions, application-owned roots, inherited Windows protection list (read-only view) |
| 9 | Backups and Recovery | Quarantine location and size cap, retention, restore UI entry, verify-before-restore |
| 10 | Performance | Worker thread count, I/O priority, memory ceiling for scan trees, UI update rate |
| 11 | Notifications | Completion, errors, scheduled-job results, tray behaviour |
| 12 | Privacy and Diagnostics | Local-only processing statement, opt-in diagnostics, log redaction, clear logs |
| 13 | Advanced | Command-line interface, job files, export formats, feature flags, reset-all with double confirm |
| 14 | About and Updates | Version, build hash, licence, update channel, third-party notices (including the licences reviewed in §7) |

### 4.16.2 Rules

- **Dangerous settings are visually separated:** anything that widens destructive scope lives under a bordered "Advanced risk settings" area with an explanatory line and requires an extra confirmation on first enablement.
- **Validation is immediate** and states the constraint ("Threshold must be at least 7 days; 1 day risks deleting active caches").
- Every setting has a **Reset to default** at both field and category level, and "Reset all" is double-confirmed.
- **Persistence is atomic:** write to a temporary file, `FlushFileBuffers`, `MoveFileEx` with `MOVEFILE_REPLACE_EXISTING` + `MOVEFILE_WRITE_THROUGH`; keep one previous version; if the file is corrupt, fall back to defaults and tell the user rather than failing to start.
- **Documented effect is verifiable:** each setting that changes behaviour is listed in the acceptance tests with an observable outcome.

### 4.16.3 Acceptance criteria

1. Every setting round-trips across restart; a corrupted settings file falls back safely with a notice.
2. Changing a scan preference takes effect on the next scan and never mid-scan.
3. A search for "quarantine" surfaces every related setting across categories.
4. Dangerous settings are unreachable by keyboard focus order without the risk explanation being read (the explanation is adjacent, not a tooltip).
5. Settings are per-user unless explicitly marked machine-wide, and machine-wide changes state their privilege requirement.

## 4.17 R15 — Research-driven additional features

Delivered as §5 (28 proposals), each tied to a problem family in §3 and each with priority, complexity, dependencies and acceptance criteria.

## 4.18 Cross-cutting rule applied in this audit

> **Audit first, extend second, replace only with written justification.**

Concretely: before implementing any requirement, the engineer records (a) which existing files implement the behaviour, (b) what the current selection/search/settings model is, and (c) whether the existing implementation can meet the acceptance criteria by extension. Replacement is allowed when the existing model is structurally incompatible — most likely for index-based selection and for any direct-delete code path.

---

