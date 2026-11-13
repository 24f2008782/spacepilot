# 9. Technical Architecture

## 9.1 Principles

1. **Preserve the existing application.** The architecture is expressed as modules that can be introduced behind existing UI code, not as a rewrite. Nothing here requires abandoning the current language, toolkit or build system.
2. **C11, no new large dependencies.** All system interaction through documented Win32/WDK-adjacent APIs: `CreateFileW`, `FSCTL_*`, `Msi*`, `PackageManager*`(COM), Restart Manager, `IFileOperation`. No third-party runtime is required; a small hash table and an arena allocator are the only infrastructure to write.
3. **Read-only by default.** Every module can run with zero write capability. Destruction is a separate subsystem with its own policy engine.
4. **Identity, not location.** Files are tracked by `FILE_ID_INFO`-style identity. Paths are presentation data.
5. **Measure, never assume.** Space savings are computed from free-space deltas; category estimates are always labelled as estimates.

## 9.2 What must be established by inspecting your code first

| Question | Why it matters | Where to look |
|---|---|---|
| GUI toolkit: raw Win32 common controls, MFC, custom Direct2D, or a third-party framework? | Determines whether the virtualised grid can be extended or must be introduced | Resource files, `WinMain`, class registrations |
| Is any grid already virtual (`LVS_OWNERDATA`)? | R8's performance budget depends on it | List-view creation flags |
| How is selection stored today (index, path, handle)? | R2 cannot be satisfied by index storage | Selection/notification handlers |
| Is deletion performed directly, via `SHFileOperation`, or `IFileOperation`? | Direct deletion cannot be made reversible without rewrite | Delete command paths |
| Is any code running elevated by manifest (`requireAdministrator`)? | Determines the broker design (§9.7) | Application manifest |
| Threading model: any worker threads, or UI-thread scanning? | Freeze fixes are architectural | Thread creation, message pumps |
| Settings persistence format and location | R14 requires atomic writes and migration | Registry vs INI vs JSON |
| Build system and CRT usage | Constrains library choices | `.vcxproj`/Makefile, `/MT` vs `/MD` |
| Existing module boundaries and file layout | Determines the feature-to-file map | Directory tree |
| Any existing icon/version resources | R1 integration | `.rc` file |

Until these are answered, every statement about *your* code in this report is **[PENDING VERIFICATION]**.

## 9.3 Module map

```
                         ┌──────────────────────────────┐
                         │        UI PRESENTATION       │
                         │  Shell · Grids · Map · Settings │
                         │  (virtualised models only)   │
                         └──────────────┬───────────────┘
                                        │ view models, commands
        ┌───────────────────────────────┼────────────────────────────────┐
        │                               │                                │
┌───────▼────────┐            ┌─────────▼──────────┐          ┌──────────▼─────────┐
│ SEARCH & FILTER│            │  RISK & POLICY     │          │  SETTINGS STORE    │
│ dual predicates│            │ classify · gate   │          │ atomic · versioned │
└───────┬────────┘            └─────────┬──────────┘          └──────────┬─────────┘
        │                               │                                │
┌───────▼───────────────────────────────▼────────────────────────────────▼─────────┐
│                           JOB ORCHESTRATOR (worker pool)                          │
│   cancellable tasks · progress batching · partial-result preservation             │
└───┬───────────┬────────────┬───────────────┬──────────────┬──────────────┬────────┘
    │           │            │               │              │              │
┌───▼────┐ ┌────▼─────┐ ┌────▼──────┐ ┌──────▼──────┐ ┌─────▼─────┐ ┌──────▼──────┐
│ FS     │ │ APP      │ │ OWNERSHIP │ │ REGISTRY    │ │ CLEANUP   │ │ DISK        │
│ SCANNER│ │ INVENTORY│ │ & LEFTOVER│ │ ANALYSER    │ │ EXECUTOR  │ │ VISUALISER  │
│ (3     │ │ (MSI,    │ │ ENGINE    │ │ (read/backup│ │ (quarantine│ │ (treemap,   │
│ backends)│ │ reg, MSIX│ │ (scoring) │ │  only)      │ │  · verify)│ │  tree)      │
└───┬────┘ │ winget,  │ └───────────┘ └─────────────┘ └─────┬─────┘ └─────────────┘
    │      │ launchers)│                                      │
┌───▼──────▼──────────────────────────────────────────────────▼─────────────────────┐
│ SYSTEM SERVICES: volumes · file identity · locks (RM) · elevation broker ·         │
│                  backup/quarantine vault · audit log · free-space measurement      │
└───────────────────────────────────────────────────────────────────────────────────┘
```

| # | Module | Responsibility | Key interfaces |
|---|---|---|---|
| 1 | **FS Scanner** | Enumerate, size and identity-annotate a scope; produce an immutable scan tree | `scan_start(scope, opts) → job`, `scan_cancel`, `scan_snapshot` |
| 2 | **App Inventory** | Discover installed apps from all sources with provenance | `apps_enumerate()`, `app_uninstall_plan(id)` |
| 3 | **Ownership & Leftover Engine** | Score candidate residue against an uninstalled product | `leftover_scan(product_evidence) → candidates[]` |
| 4 | **Registry Analyser** | Read-only key enumeration within product scope; export before change | `reg_export(key, path)`, `reg_ownership_evidence(key)` |
| 5 | **Risk & Policy** | Classification, gating, protected-path matching | `policy_eval(item, context) → verdict + reasons[]` |
| 6 | **Cleanup Executor** | Plan → revalidate → stage → execute → verify → record | `job_plan(items)`, `job_execute(plan)` |
| 7 | **Backup & Recovery** | Quarantine vault, manifests, restore | `vault_stage(item)`, `vault_restore(job_id)` |
| 8 | **Disk Visualiser** | Tree aggregation, squarified layout, hit-testing | `treemap_layout(nodes, rect) → rects[]` |
| 9 | **Search & Filter** | Composable predicates, indexed evaluation | `filter_compile(expr)`, `filter_apply(view, expr)` |
| 10 | **Settings** | Typed, versioned, atomic persistence | `settings_get/set`, `settings_reset` |

**Dependency rule:** UI depends on view models only. Destructive modules depend on Risk & Policy, never the reverse. The Registry Analyser cannot call the Cleanup Executor directly; it can only propose items.

## 9.4 File-system scanner

### 9.4.1 Backends

| Backend | Mechanism | Requirements | Used when |
|---|---|---|---|
| A — USN/`$MFT` | `CreateFileW("\\\\.\\C:", …)`, `FSCTL_ENUM_USN_DATA` (and `FSCTL_GET_NTFS_FILE_RECORD` where needed) | NTFS, elevated, volume handle | Default for fixed NTFS volumes when elevated |
| B — `FileIdBothDirectoryInfo` | `GetFileInformationByHandleEx` | Any volume, no elevation | Non-NTFS, unelevated, or if A fails |
| C — `FindFirstFileExW` (`FindExInfoBasic`) | Classic enumeration | Any volume | Network paths, FAT/exFAT without IDs, fallback |

Each backend produces the same `dw_node` records; the UI displays which backend produced the result and the known under-reporting (for example: no unreadable directories, no metadata buckets).

### 9.4.2 Node record and memory strategy

```c
typedef struct {
    uint64_t id_hi, id_lo;      /* FILE_ID_INFO.FileId          */
    uint32_t vol_serial;        /* FILE_ID_INFO.VolumeSerialNumber */
    uint32_t parent;            /* index into node array        */
    uint64_t alloc_size;        /* AllocationSize               */
    uint64_t logical_size;      /* EndOfFile                    */
    uint32_t attr;              /* FILE_ATTRIBUTE_*             */
    uint32_t nlink;             /* NumberOfLinks                */
    uint32_t name_off;          /* into string arena            */
    uint16_t name_len;
    uint8_t  backend;           /* A/B/C                        */
} dw_node;                      /* 56 bytes incl. padding       */
```

- Nodes live in a contiguous, growable array (arena) — never a pointer-linked tree. Parent indices keep the structure serialisable and cache-friendly.
- Names live in a separate UTF-16 arena; a node is only 56 bytes, so 10 M files ≈ 560 MB of node data before names — above the default ceiling, which is why the scanner supports **depth-limited** and **size-threshold** modes and streams completed subtrees to disk for very large volumes.
- Directory aggregation (inclusive sizes, item counts) is computed once at scan completion in **reverse enumeration order** (children always follow parents in a USN stream), making roll-up a single O(n) pass with no recursion.
- Hard-link deduplication uses an open-addressing set keyed by `(vol_serial, id)`, mirroring the tested selection core.

### 9.4.3 Correctness rules

Long paths via `\\?\` prefix; reparse points detected by attribute and classified by `FSCTL_GET_REPARSE_POINT` (do not follow by default); DOS 8.3 short names excluded from totals; ADS size reported separately (readable via `FindFirstStreamW`) and never silently added; unreadable directories recorded as nodes with `ERROR_ACCESS_DENIED` so the ledger can name them; cluster slack computed as `sum(round_up(logical, cluster) - alloc)` where meaningful.

### 9.4.4 Interaction with security software

Code-sign; request elevation explicitly with a stated reason; keep scanning read-only; never enumerate beyond scope; declare no-network capability; detect blocking (`ERROR_ACCESS_DENIED` on the volume handle) and fall back to backend B without argument; document allow-listing for enterprise deployment. Never attempt to bypass AV or tamper protection.

## 9.5 Ownership and classification engine

The engine answers one question: *what evidence do we have that this artefact belongs to the product being removed?* It never returns a boolean.

| Signal | Weight | Source | Notes |
|---|---|---|---|
| MSI component/product registration references the path | Strong | `MsiEnumComponentsEx`, component → path mapping | Authoritative for MSI products |
| Path is inside the product's recorded install directory | Strong | Uninstall key `InstallLocation`, MSI source dir | Root must be the recorded one, not a similar name |
| Uninstaller created the folder (before/after diff during this session) | Strong | Filesystem snapshot diff | Only available if uninstall was run by us |
| Registry value data references the product's executable path | Strong | Read-only registry scan of product keys | Value *data*, not key name resemblance |
| Publisher/company string matches the product's publisher **and** path contains a unique product token | Medium | `FileVersionInfo`/signature | Token must be ≥ 4 chars and not a generic word |
| File name shares a long prefix with the product name | Weak | Name heuristic | Never sufficient alone |
| Icon/embedded version resource matches | Weak | Resource read | Corroboration only |
| Timing proximity to the uninstall | Weak | MFT `$STANDARD_INFORMATION` timestamps | Never sufficient alone |

**Confidence classes:** **A** = at least one strong signal, no contradicting signal → eligible for bulk action after preview. **B** = medium only → individual selection, default unselected. **C** = weak/ambiguous → listed as *Unverified*, excluded from all bulk verbs, clearly labelled "we cannot establish ownership".

**Shared-resource guards.** Never propose for removal: files with `NumberOfLinks > 1`; anything under a different product's install root; redistributable runtimes (`VCRUNTIME*`, `.NET`, `WebView2`) unless the vendor's uninstaller removed them; COM registrations referenced by other CLSIDs; keys under `HKEY_CLASSES_ROOT` unless the exact extension association is owned by the product; anything in the Protected list.

**Registry policy (Appendix C).** Read-only analysis by default. Export (`.reg`) before any approved removal. Scope limited to: the product's own `Uninstall` key, its `Software\<Publisher>\<Product>` subtree, its services, its `Run` values, its file-association entries. Everything else is out of scope by design — there is no "scan the whole registry and clean it" mode.

## 9.6 Risk scoring

A transparent additive model with hard overrides:

```
score = base(category)                         // e.g. temp file 0, app data 10
      + age_penalty(last_used, configured_days) // 0..20
      + location_weight(path)                   // user profile 0, ProgramData 5, ProgramFiles 15
      + sharing_weight(nlink>1 ? 20 : 0)
      + lock_weight(locked ? 25 : 0)
      + cloud_weight(placeholder ? 15 : 0)
      - evidence_bonus(strong_ownership ? 15 : 0)

class = PROTECTED  if protected_path(path) or system_attr and policy
      = HIGH       if score >= 60 or lock or nlink>1 or cloud
      = REVIEW     if score >= 25
      = SAFE       if score < 25 and has_rebuild_path(category)
```

Weights are configurable but the **overrides are not**: locks, protected paths, identity mismatch and hard links cannot be scored down by configuration. Rationale: these are the four conditions that produce irreversible surprises.

## 9.7 Privilege model

```
   ┌────────────────────────────┐        ┌────────────────────────────────┐
   │  DiskWise UI (medium IL)   │  IPC   │  DiskWise Worker (high IL)     │
   │  all rendering, all policy │◄──────►│  executes an enumerated        │
   │  evaluation, all dialogs   │ named  │  command set only              │
   └────────────────────────────┘  pipe  └────────────────────────────────┘
```

- The worker accepts a **closed set of typed operations** (`OP_STAGE_ITEMS`, `OP_DELETE_STAGED`, `OP_DISM_CLEANUP`, `OP_RESET_UPDATER`, `OP_COMPACT_VHD`, …). No operation takes an arbitrary command string.
- Paths are pre-validated by the UI against the protected list and re-validated inside the worker (never trust the client).
- Pipe security: `PIPE_REJECT_REMOTE_CLIENTS`, an ACL granting only the interactive user SID and Administrators, and a check of the client's session id and integrity level before executing.
- Elevation is requested **per operation**, not at startup, so ordinary browsing never runs elevated.
- If the existing application already requires administrator at launch, this is a P0 architectural change: it converts the entire product into an attack surface.

## 9.8 Data and persistence

| Store | Format | Location | Durability rules |
|---|---|---|---|
| Settings | Versioned JSON (or existing format + schema version) | `%LocalAppData%\DiskWise\settings.json` | Write temp → `FlushFileBuffers` → `MoveFileEx` with `MOVEFILE_REPLACE_EXISTING` and `MOVEFILE_WRITE_THROUGH`; keep `.bak` |
| Scan snapshots (for history/diff) | Binary node dump, zstd-optional | `%LocalAppData%\DiskWise\scans\` | Retention policy; never uploaded |
| Cleanup jobs / manifests | JSONL + restore bundle (ZIP of `.reg` exports and a file manifest) | `%LocalAppData%\DiskWise\jobs\<id>\` | Append-only; integrity check on restore |
| Audit log | JSONL, append-only, rotation | `%LocalAppData%\DiskWise\audit\` | Redact user name on request; no file contents ever |
| Quarantine vault | Files preserved with original metadata in a sidecar | `%LocalAppData%\DiskWise\vault\` (configurable) | Size cap; FIFO eviction with notice; restore verifies hash |

Privacy: local-only processing is the default and is stated in the UI. Diagnostics are opt-in and, when enabled, exclude file paths by default.

## 9.9 Performance targets

**These are targets derived from the architecture, not measurements.** No Windows hardware was available in this environment, so no benchmark claim is made. Validate on the reference machine before publishing any figure.

| Scenario | Target | Measurement method |
|---|---|---|
| Cold start to interactive dashboard | ≤ 1.5 s | Event to `WM_PAINT` of first frame |
| NTFS scan, 1 M files, NVMe, backend A | ≤ 45 s | Wall clock, warm cache and cold cache reported separately |
| Same scan, backend C fallback | ≤ 10 min | Same |
| Scan, 4 M files, NVMe, backend A | ≤ 4 min, ≤ 900 MB RSS | Working set peak |
| Treemap layout, 5 000 visible nodes | ≤ 2 ms per relayout | Instrumented |
| Grid scroll, 1 M rows | 60 fps, no allocation in the paint path | Frame timing |
| Sort 1 M rows, single key | ≤ 400 ms | Instrumented |
| Filter keystroke → visible update | ≤ 120 ms | Input to first frame |
| Quarantine move, 5 000 small files | ≥ 500 files/s, UI responsive at 10 Hz progress | Throughput log |
| Cancellation latency | ≤ 300 ms to acknowledged, partial results preserved | Instrumented |
| Memory ceiling | Configurable 256/512/1024 MB with graceful degradation (stream subtrees to disk) | Working set |

Never publish a benchmark without stating file count, volume type, cache state, backend and build hash.

## 9.10 Threading and responsiveness

- One coordinator thread per job; worker pool sized `max(2, min(8, cores/2))`; no UI-thread I/O.
- UI updates are **batched**: workers append to a lock-free ring; the UI drains at 10 Hz. This keeps a 4 M-file scan from generating 4 M messages.
- Cancellation: `atomic_bool` checked every 256 items plus an event handle for blocking calls; every backend has a cancellation branch that leaves the tree consistent and marked incomplete.
- Destructive jobs are **single-threaded per manifest** with optional parallel staging, because ordering and failure attribution matter more than throughput.

## 9.11 Build, packaging and compatibility

| Aspect | Recommendation |
|---|---|
| Toolchain | Keep the existing one (MSVC expected). C11 with `/W4 /WX` in CI for new modules only, to avoid a flag-day on legacy code |
| Build system | Keep existing; add new modules as sources rather than introducing CMake wholesale |
| Architecture | x64 primary, ARM64 build from the same sources; avoid inline asm and architecture assumptions |
| Minimum OS | Windows 10 22H2 and Windows 11 23H2+ recommended; detect features rather than version-check |
| Elevation | `asInvoker` + per-operation elevation via the broker |
| Signing | Authenticode required before distribution; mention in the manifest that the app makes no network connections |
| Update channel | Optional; if added, verify signatures and never auto-update a running elevated worker |

## 9.12 Tradeoffs accepted

| Decision | Benefit | Cost |
|---|---|---|
| Identity-keyed selection instead of row indices | Correct persistence across filters/rescans | Bigger memory footprint, need for a hash set |
| Quarantine-first deletion by default | Reversibility, auditability | Disk must hold the data temporarily; cross-volume staging needs copy+hash |
| USN backend with fallback | 10–50× faster scans on NTFS | Requires elevation; more code paths to test |
| No registry optimiser | Avoids the highest-consequence failure mode | A competitor feature is deliberately absent; must be explained in the UI |
| Confidence classes instead of booleans | Honest and safe | More UI surface; users must make more decisions |
| Split-process broker | Least privilege, smaller attack surface | IPC complexity; more failure modes to test |

## 9.13 What to change versus preserve in the existing application

| Preserve | Extend | Replace only with justification |
|---|---|---|
| Existing module navigation and feature set | Scan engines (add backends behind the current interface) | Index-based selection store |
| Existing settings location if user-visible | Deletion paths (wrap in the pipeline) | Direct `DeleteFile` calls in destructive flows |
| Existing branding and icon slots | Tables (virtualisation, unified model) | Any UI-thread scanning |
| Existing installer/update mechanism | Dialogs (R3/R9 content) | Raw registry deletion code, if any |
| Existing CLI (if any) | Settings page structure | — |

---
