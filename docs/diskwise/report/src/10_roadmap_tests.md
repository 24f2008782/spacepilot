# 10. Implementation Roadmap

## 10.1 Sequencing principle

The order is dictated by risk, not by visibility:

1. Nothing destructive ships before the **policy gate, identity re-validation and quarantine** exist.
2. Nothing claims accuracy before the **size model** (allocated vs logical, hard links, placeholders) is correct.
3. No module is "polished" before its **data model** is right, because polish on a wrong model is wasted work.

This deliberately inverts the order in which the requirements were listed: the disk map (R10) is P2, not because it is unimportant, but because building it before the size model would mean rebuilding it.

## 10.2 Dependency graph

```
                 ┌──────────────────────────────────────────────┐
   PHASE 0       │ P0-A  Policy gate + risk classes (R3 core)   │
  correctness    │ P0-B  Identity & selection model (R2 core)   │← prototyped, tested
                 │ P0-C  Size model: allocated/logical/links    │
                 │ P0-D  Quarantine vault + manifest + audit    │
                 └───────────────┬──────────────────────────────┘
                                 │
                 ┌───────────────▼──────────────────────────────┐
   PHASE 1       │ P1-A  Unified grid + dual search (R2, R8)    │
  usability      │ P1-B  Deletion dialogs + review modal (R3,R9)│
                 │ P1-C  Drive/scope service (R7)               │
                 │ P1-D  Context menus (R6)                     │
                 │ P1-E  Responsive shell + DPI (R13)           │
                 └───────────────┬──────────────────────────────┘
                                 │
        ┌────────────────────────┼───────────────────────────┐
        │                        │                           │
┌───────▼──────────┐   ┌─────────▼──────────┐   ┌────────────▼───────────┐
│ PHASE 2          │   │ PHASE 2            │   │ PHASE 2                │
│ Disk intelligence│   │ Application suite  │   │ System maintenance     │
│ R10 + F-02,F-05  │   │ R4 + F-12..F-16    │   │ F-07..F-11, F-24       │
│ (needs P0-C)     │   │ (needs P0-A/B/D)   │   │ (needs P1-B)           │
└───────┬──────────┘   └─────────┬──────────┘   └────────────┬───────────┘
        └────────────────────────┼───────────────────────────┘
                                 │
                 ┌───────────────▼──────────────────────────────┐
   PHASE 3       │ Settings (R14) · automation (F-23) · history  │
  depth          │ (F-26) · dev/VM tooling (F-20,F-21) · games   │
                 │ (F-25) · remaining §5 features               │
                 └──────────────────────────────────────────────┘
```

## 10.3 Consolidated work breakdown

Acceptance criteria are written out in the referenced sections; this table is the delivery index.

| ID | Description | Problem solved | User impact | Pri | Cplx | Dependencies | Safety considerations | Acceptance criteria |
|---|---|---|---|---|---|---|---|---|
| R1 | Icon system | Product identity; unprofessional default icon | Trust, recognisability | P1 | S | — | None | §4.3 (delivered) |
| R2 | Unified tables + multi-selection | Wrong selections, lost selections, no bulk work | Core daily workflow | **P0** | L | R8, identity model | Hidden-selection accounting; identity-keyed selection | §4.4.5 |
| R3 | Safe deletion + dialogs | Irreversible mistakes, unclear warnings | Confidence in every action | **P0** | L | R9, gate, vault | Risk classes; identity re-check; quantum of confirmation | §4.5.4 |
| R4 | Uninstall orchestration | Incomplete removal; uninstall failures | Reclaim space, clean reinstalls | **P0** | XL | R7, F-12, vault | Ownership proof; shared resources; registry export | §4.6.3 |
| R5 | Module polish | Inconsistent, confusing UI | Perceived quality | P1 | M | R13 | No destructive change | §4.7.3 |
| R6 | Context menus | Discoverability; Explorer workarounds | Speed | P1 | M | R2 | No silent destructive verbs; conditional availability | §4.8.3 |
| R7 | Drive selection | Wrong scope, silent widening | Predictability | P1 | M | volume service | Never silently widen scope | §4.9.4 |
| R8 | Headers, sorting, columns | Unsorted, unresizable tables | Usability at scale | P1 | M | virtualised grid | Sorting must not alter selection | §4.10 |
| R9 | Pre-execution review | One-click admin actions | Informed consent | **P0** | M | R3 | Typed confirmation for irreversible ops | §4.11.3 |
| R10 | Clean Run preview + Disk Map | No way to see where space went | The headline capability | P2 | XL | P0-C, R8 | Read-only scanning; no bypass of R3 | §4.12.3 |
| R11 | Licence/OSS study | Legal risk | Avoids GPL contamination | — | S | — | — | Delivered §7 |
| R12 | Advanced context actions | Power-user workflows | Depth | P2 | M | R6 | No destructive entry in advanced submenu | §4.14 |
| R13 | Responsive/DPI | Broken layouts, multi-monitor | Basic quality | P1 | M | — | None | §4.15 |
| R14 | Settings redesign | Unfindable settings, unsafe defaults | Control | P2 | L | settings store | Dangerous settings separated | §4.16.3 |
| R15 | Research-driven features | See §5 | See §5 | P2–P3 | XL | varies | Per-row in §5.2 | §5.5 |
| F-01 | Duplicate finder | Duplicated media | Space recovered safely | P2 | L | R2, size model | Choose survivor; never delete whole group | §5.2 |
| F-02 | Large-file explorer | Finding the few big items | Fast wins | P2 | S | R8, R10 | Read-only | §5.2 |
| F-03 | Downloads triage | Accumulated downloads | Common cleanup | P2 | M | R2, R3 | Age floor; recycle-bin default | §5.2 |
| F-04 | Browser cache analyser | Opaque browser data | Space | P3 | M | F-06 | Never touch credentials/history | §5.2 |
| F-05 | Reconciliation ledger | Numbers that never close | Trust | **P0** | L | R10 | Every unexplained byte named | §5.2 |
| F-06 | Temp churn inspector | Junk that regrows | Root-cause view | P1 | M | — | Writer attribution read-only | §5.2 |
| F-07 | System-managed files | hiberfil/pagefile | Space + understanding | P2 | M | R9 | Never delete directly | §5.2 |
| F-08 | Update workspace | Update cache/stuck updates | Space, reliability | P2 | L | R9 | Service stop/restart; verify | §5.2 |
| F-09 | Component store analyser | WinSxS | Space | P2 | M | R9 | `/ResetBase` irreversible class | §5.2 |
| F-10 | Windows.old removal | Rollback folder | Large space | P1 | S | R9 | Rollback loss warning | §5.2 |
| F-11 | Shadow copy analyser | VSS growth | Space + backup honesty | P2 | M | R9 | Backup framing; never default-selected | §5.2 |
| F-12 | Ownership engine | False-positive leftovers | Safety | **P0** | XL | R4 | Confidence classes; Unverified excluded | §5.2 |
| F-13 | Uninstall orchestrator | Fragmented uninstall | One workflow | **P0** | XL | F-12, F-14 | Native first; report everything | §5.2 |
| F-14 | Broken-entry repair | Un-uninstallable apps | Recover control | P1 | M | F-13 | Export before removal | §5.2 |
| F-15 | Startup manager | Phantom entries | Boot clarity | P1 | M | — | Disable before delete | §5.2 |
| F-16 | Orphan services/tasks | Vendor residue | Cleanliness | P2 | M | F-13 | Read-only report first | §5.2 |
| F-17 | Lock & permission inspector | Undeletable files | Unblocks users | P1 | M | — | No auto-ownership | §5.2 |
| F-18 | MSI patch auditor | Windows\Installer growth | Space | P2 | L | F-13 | MSI API; quarantine | §5.2 |
| F-19 | Packaged-app manager | Store app failures | Control | P1 | M | R7 | Protected paths; supported APIs | §5.2 |
| F-20 | VHDX compactor | WSL/Docker growth | Tens of GB without deletion | P2 | M | R9 | Refuse while running; compact only | §5.2 |
| F-21 | Dev cache manager | node_modules, caches | Developer space | P2 | L | — | Documented prune commands only | §5.2 |
| F-22 | Cloud placeholder lens | Miscounted OneDrive | Correct decisions | P1 | M | F-05 | Warn about cloud propagation | §5.2 |
| F-23 | Preview-first automation | Destructive automation | Safe scheduling | P1 | L | R3, R9 | Dry-run default; undo history | §5.2 |
| F-24 | Maintenance console | SFC/DISM/optimise misuse | Reliability | P2 | M | R9 | Correct order enforced | §5.2 |
| F-25 | Game library analyser | Launcher residue | Space | P2 | M | R7 | Protect saves | §5.2 |
| F-26 | Storage history | Recurring growth | Insight | P3 | L | R10 | Local-only | §5.2 |
| F-27 | Export & audit log | No record of actions | Accountability | P1 | S | R3 | Append-only; redaction | §5.2 |
| F-28 | Protected-path manager | User guardrails | Safety | P1 | S | R14 | Add-only by default | §5.2 |

## 10.4 Phase plan

| Phase | Goal | Contents | Exit criteria (all must pass) | Estimate |
|---|---|---|---|---|
| **0 — Correctness foundations** | Make destruction safe and measurement honest | Policy gate + risk classes; identity-keyed selection; size model (allocated/logical/links/placeholders); quarantine vault + manifests + audit log; harness with a synthetic tree and trap files | 100% of destructive paths pass the gate unit tests; no direct `DeleteFile` remains in destructive flows; vault restore verified on 3 scenarios; size model reconciles with `GetDiskFreeSpaceExW` on two file systems | 4–6 weeks |
| **1 — Usability core** | Make the existing app usable and trustworthy | Unified grid + dual search; deletion dialogs + review modal; drive/scope service; context menus; responsive shell/DPI; empty/loading/error states | Selection survives filter/sort/rescan in UI tests; hidden-selection banner verified; 150% DPI layouts verified; keyboard-only walkthrough passes | 5–7 weeks |
| **2 — Capability build-out** | Deliver the differentiating modules | Disk Map + ledger; uninstall suite + ownership engine; system maintenance modules | Scan targets met on reference hardware; ownership engine precision measured on a labelled corpus (target: zero class-A false positives in the test set); no maintenance action bypasses R9 | 10–14 weeks |
| **3 — Depth** | Automation, history, specialist domains | Settings overhaul; automation/profiles; storage history; dev/VM/game tooling; remaining §5 features | Automation dry-run → review → execute → undo cycle verified; settings migration tested from the previous format | 8–12 weeks |

**A note on "P0" scope.** Phase 0 is not a placeholder for "cleanup". It is the phase in which the product stops being able to destroy data irreversibly, and every later phase depends on it. If capacity is constrained, cut features from Phase 2 — never from Phase 0.

## 10.5 Cut lines if capacity is short

| If you must cut | Cut this | Keep this |
|---|---|---|
| Disk map | Treemap visuals; ship the tree + largest-files table (F-02) first | F-05 ledger, because it is the trust differentiator |
| Uninstall suite | Leftover *removal*; ship inventory + native uninstall + report | F-12 scoring, so nothing unsafe is ever offered |
| Maintenance modules | Everything except Windows.old and component analysis | R9 modal |
| Automation | Scheduling | Manual review-queue workflow |

## 10.6 Definition of done (per feature)

1. Behaviour matches the acceptance criteria in §4 or §5.
2. Destructive paths have a dry-run and are covered by an automated test in the synthetic harness.
3. Every new UI surface has empty, loading, error and permission-denied states.
4. The feature appears in the audit log with the reason codes it used.
5. Strings are localisation-ready (no concatenation), and every heuristic is described as a heuristic in the UI.
6. The "what this cannot do" text exists and is reachable without a tooltip.

---

# 11. Testing and Security Checklist

## 11.1 Test environment

- **Destructive tests run only inside a disposable VM** with a snapshot restore point; never on a developer's working machine and never on a user's machine in a build under test.
- **Synthetic tree generator** produces: deep nesting (200+ levels), long paths (> 260 chars), 1 M small files, sparse files, compressed files, hard-link farms, junctions and symlinks (including a junction pointing at `C:\Windows`), ACL-denied directories, files locked by a helper process, cloud-placeholder stubs (simulated with recall attributes where possible), zero-byte files, files with alternate data streams, and filenames with emoji/CJK/RTL text.
- **Fault injection:** remove the USB volume mid-scan; fill the disk during quarantine staging; kill the worker process; revoke privileges mid-operation; corrupt the settings file; corrupt a manifest.

## 11.2 UI tests

| ID | Scenario | Expected | Pass criterion |
|---|---|---|---|
| UI-01 | Select 10 rows, apply a filter hiding 4, open delete | Dialog states 6 visible / 4 hidden; hidden excluded by default | Both numbers correct; hidden not selected in payload |
| UI-02 | Select, sort by size, verify selection | Same identities selected | Count unchanged; highlighted rows follow the items |
| UI-03 | Invert selection on a filtered view | Only visible rows inverted | Outside-view items unchanged (prototype-verified) |
| UI-04 | "Select all matching" on 2 M rows | All matches selected, count accurate | Completes ≤ 2 s, UI responsive |
| UI-05 | Rescan removes a selected file | Item marked stale; destructive action blocked until reviewed | Stale count shown; no silent drop (prototype-verified) |
| UI-06 | Context menu on Protected item | Destructive verbs absent; "why protected" present | No greyed destructive items |
| UI-07 | Keyboard-only walkthrough of each page | Full parity with mouse | No mouse-only action |
| UI-08 | 200% DPI, maximised, second monitor | No clipping; columns adapt | Visual diff approved |
| UI-09 | Drag window between 100% and 200% monitors | Re-render, no layout jump | No clipped text after 3 drags |
| UI-10 | Corrupt settings file, start app | Defaults loaded with notice | No crash; user informed |
| UI-11 | Review modal content for each operation class | All mandatory fields present | Field checklist 100% |
| UI-12 | Typed confirmation for irreversible op | Cannot proceed without exact token | Paste allowed, prefill absent |

## 11.3 Scanning tests

| ID | Scenario | Expected | Pass criterion |
|---|---|---|---|
| SC-01 | NTFS volume, backend A | Completes, totals reconcile | Within 1% of free-space arithmetic with metadata buckets itemised |
| SC-02 | Force backend C | Completes slower with correct totals | Same reconciliation |
| SC-03 | Junction to `C:\Windows` inside scope | Not followed | Target never enumerated |
| SC-04 | Hard-link farm (100 links, 1 file) | Counted once | Total equals file size, not ×100 |
| SC-05 | Sparse file 10 GB, 1 MB allocated | Allocated shown as 1 MB; logical 10 GB | Both columns correct |
| SC-06 | OneDrive placeholder | Allocated 0, logical = size, warning flag | Deletion warns about propagation |
| SC-07 | ACL-denied directory | Recorded as unreadable node with code | Ledger lists it; no silent omission |
| SC-08 | 200+ level nesting | No stack overflow (iterative traversal) | Completes |
| SC-09 | Path > 260 chars | Enumerated correctly | Present in results |
| SC-10 | USB removed mid-scan | Clean cancellation naming the volume | No crash; partial results marked incomplete |
| SC-11 | Scan cancelled at 50% | Returns ≤ 300 ms; partial results preserved | No orphan threads |
| SC-12 | Scan while a second scan runs on the same volume | Serialised or refused with message | No interleaved corruption |
| SC-13 | FAT32/exFAT volume | Backend B/C used; limits disclosed | UI states backend and limitations |
| SC-14 | 4 M files | Memory under ceiling; graceful degradation | No OOM; progress accurate |

## 11.4 Deletion tests

| ID | Scenario | Expected | Pass criterion |
|---|---|---|---|
| DE-01 | Recycle Bin deletion | Item in Recycle Bin; restore works | Restored file identical (hash) |
| DE-02 | Quarantine deletion | Item staged; manifest written; restore works | Hash match; manifest complete |
| DE-03 | Permanent deletion | Typed confirmation required | No bypass path exists |
| DE-04 | File replaced between scan and delete | Skipped with `IDENTITY_CHANGED` | Not deleted (gate prototype-verified) |
| DE-05 | File deleted externally before job | Skipped with `GONE` | Job completes with report |
| DE-06 | Locked file | Reported with holder; delete-on-reboot offered | No silent failure |
| DE-07 | Protected path | Blocked at plan time and again at execution | Two independent blocks |
| DE-08 | Hard-linked file | Warned with link count | Deleting one link leaves data intact |
| DE-09 | Junction selected | Link removed, target untouched | Target tree byte-identical |
| DE-10 | Disk full during quarantine staging | Job aborts cleanly; nothing deleted | Manifest records partial staging; user informed |
| DE-11 | Cancel mid-job | Executed items listed; staged items restorable | Report matches audit log exactly |
| DE-12 | Worker killed mid-job | On restart, job marked incomplete; vault intact | Restore still works |
| DE-13 | Measured vs predicted reclaim | Reported both | Deviation > 5% explained |
| DE-14 | Audit log completeness | Every item has a reason code | Log joins to manifest 1:1 |

## 11.5 Uninstallation tests

| ID | Scenario | Expected | Pass criterion |
|---|---|---|---|
| UN-01 | MSI product | Removed via MSI API; exit code surfaced | Product gone from inventory; MSI cache consistent |
| UN-02 | Registered executable uninstaller | Runs with elevation only if declared | Exit code and log captured |
| UN-03 | MSIX packaged app | Removed via PackageManager | Package absent; `WindowsApps` untouched manually |
| UN-04 | App installed on D: | Install volume scanned for residue; registry on C: reported | Both scopes correct |
| UN-05 | Broken entry (uninstaller missing) | Detected pre-flight; repair path offered | No blind execution |
| UN-06 | Shared registry value (two products) | Preserved when one is removed | Second product still functions |
| UN-07 | Shared runtime (VC++ redist) | Preserved unless vendor uninstaller removed it | Runtime intact |
| UN-08 | Leftover classification | Class A/B/C assigned with evidence | Sampled 100 items reviewed manually; zero class-A false positives |
| UN-09 | Registry export before removal | `.reg` present in restore bundle | Import restores key |
| UN-10 | Permission-denied residue | Reported as skipped, not silently ignored | Report accurate |
| UN-11 | Reinstall after cleanup | Application reinstalls cleanly | No conflicting state |

## 11.6 Reliability tests

| ID | Scenario | Expected | Pass criterion |
|---|---|---|---|
| RL-01 | Cancel every long-running operation | Clean cancel everywhere | No hang; no leak |
| RL-02 | Kill the process during a scan | No partial file written as if complete | Snapshot marked invalid |
| RL-03 | Low memory (simulated allocation failures) | Graceful degradation, no crash | Error surfaced; state consistent |
| RL-04 | System restart after an interrupted job | Recovery prompt; vault restorable | Restore verified |
| RL-05 | 8-hour soak with repeated scans | Stable memory | Growth < 5% over 8 h |
| RL-06 | Two app instances | Second instance focuses the first; no concurrent destructive jobs | No duplicate execution |

## 11.7 Security checklist

| # | Control | Requirement | Verification |
|---|---|---|---|
| SEC-01 | Least privilege | UI runs at medium IL; elevation per operation | Process Explorer integrity level |
| SEC-02 | IPC hardening | Worker accepts only a closed command set; no arbitrary paths or command lines; pipe ACL restricted; client SID/session checked; remote clients rejected | Code review + a malicious client harness |
| SEC-03 | Path canonicalisation | Every path resolved with `GetFullPathNameW` + `GetFinalPathNameByHandleW` before policy evaluation | Unit tests with `..`, junctions, UNC, short names |
| SEC-04 | TOCTOU resistance | Identity re-checked at execution, not only at plan time | DE-04 |
| SEC-05 | Reparse-point safety | Never traverse links into unrelated locations; delete link not target | DE-09, SC-03 |
| SEC-06 | No silent ACL changes | Ownership/ACL modification never automatic; explicit, scoped, logged | Code search for `SetNamedSecurityInfo` |
| SEC-07 | Registry integrity | Export before change; no broad key removal; protected keys blocked | UN-06, UN-09 |
| SEC-08 | Quarantine vault integrity | Vault inaccessible to other users; hash on stage and restore | ACL check + hash test |
| SEC-09 | Log hygiene | No file contents; optional path redaction; logs not world-readable | Inspection of log files |
| SEC-10 | Network silence | No outbound connections by default; verify with a firewall rule during test | Firewall log |
| SEC-11 | Update integrity (if added) | Signed manifests; signature verified before applying | Deliberately corrupt a manifest |
| SEC-12 | AV coexistence | No tamper-tool behaviour; graceful fallback if blocked | Test on a machine with Defender + a third-party EDR |
| SEC-13 | Command construction | No shell string concatenation for external tools; argument arrays only | Code search for `system(`, `WinExec` |
| SEC-14 | Cancellation is not a bypass | Cancel cannot leave a job in a state where UI claims nothing happened while items were staged | DE-11 |

## 11.8 Performance regression protocol

Record for every build: build hash, file count, volume type and size, backend used, cache state (warm/cold), wall time, peak working set, and UI frame timing during scan. Compare against the previous build with the same parameters. A regression of more than 15% on any §9.9 target blocks the release until explained.

---

