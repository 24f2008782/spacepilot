# 5. Additional Feature Recommendations

## 5.1 Selection rules used

A proposal was accepted only if it satisfies at least one of:

1. **Evidence-backed:** ≥ 1 first-hand report (T1) or official document (T2) in §3 demonstrates the problem.
2. **Engineering necessity:** a technical deficiency in the product's own safety or accuracy model (e.g. identity re-validation, audit log).

A proposal was rejected if it creates more risk than value (see §5.4). Requirement 15 asked for 20–30 features; **28 are proposed**, none of which duplicates R1–R14.

## 5.2 The 28 proposals

Priority uses P0–P3 from §10. Complexity: S/M/L/XL as in §4. "Usefulness" is a judgement (Critical / High / Medium) about how much it improves a real workflow — it is not a promise of adoption.

| ID | Feature | Problem solved | Evidence | Priority | Complexity | Usefulness | Key safety consideration | Depends on |
|---|---|---|---|---|---|---|---|---|
| F-01 | **Duplicate & similar-file finder** | Duplicated media and installers consuming tens of GB, with free tools capped at 10 deletions or absent | P-36; S76–S78 | P2 | L | High | Group-level confirmation: must choose which copy survives; never delete all members of a group; hard links and identical-by-name-but-different-content excluded | R2, F-05 size model |
| F-02 | **Large-file explorer with provenance** | Users cannot find the few files that actually matter among millions | P-01, P-29; S01, S59 | P2 | S | High | Read-only; "who owns this file" shown before any action | R8, R10 |
| F-03 | **Downloads triage** | Downloads folders accumulate for years and are the most common accidental-deletion target | P-31; S66, S64 | P2 | M | High | Default 90-day threshold; recycle-bin only; show what is still referenced by installers | R2, R3 |
| F-04 | **Browser cache & profile analyser** | Browser caches are large, per-profile, and opaque; cleanup pages do not attribute them | P-01, P-08; S01 | P3 | M | Medium | Must not touch bookmarks/passwords/history; cache-only deletion; browser must be closed or deletion deferred | F-06 |
| F-05 | **Storage reconciliation ledger** | Explorer, Settings and analysers disagree; totals never close | P-01, P-28; S01, S03, S59, S89 | **P0** | L | Critical | Read-only reporting; every unexplained byte must be named as a bucket, never silently dropped | R10, F-05 model |
| F-06 | **Temp & junk churn inspector** | Category totals are misleading; junk regrows in hours; the writer is never identified | P-08, P-10; S21, S23, S24, S25 | P1 | M | High | Per-writer attribution is read-only; deletion of an active writer's files is blocked | None |
| F-07 | **System-managed file inspector** (hiberfil, pagefile, swapfile) | Huge locked files with harmful community advice | P-02, P-03; S05–S09 | P2 | M | High | Never delete directly; expose `powercfg` reduced-size and pagefile caps; explain Fast Startup loss; revert path | R9 |
| F-08 | **Windows Update workspace** | Update cache bloat, stuck updates, delivery optimisation growth, "not enough space" loops | P-09, P-11, P-12, P-13; S15, S16, S17, S20, S67, S83 | P2 | L | High | Stop/restart services around the operation; verify the update engine afterwards; never present "8.63 GB reclaimed" without measurement | R9, F-05 |
| F-09 | **Component store analyser** | WinSxS panic; irreversible `/ResetBase` used casually | P-04; S10, S11, S12, S92 | P2 | M | High | `AnalyzeComponentStore` first; `/ResetBase` in the Irreversible confirmation class with typed confirmation | R9 |
| F-10 | **Previous installation (Windows.old) removal** | 20 GB+ folder users cannot delete; ownership hacks break servicing | P-05; S13, S14 | P1 | S | High | Route through supported removal; explicit rollback-loss warning; detect when Windows still considers the rollback window open | R9 |
| F-11 | **Shadow copy & restore point analyser** | System Volume Information consumes hundreds of GB; users delete their only backups | P-07; S49, S50, S51 | P2 | M | High | Frame as *backup data*; never default-select; show what restore points protect; cap-resize preferred over deletion | R9 |
| F-12 | **Leftover ownership engine** | Post-uninstall residue; competitors misidentify unrelated applications' files | P-17, P-18; S27–S31 | **P0** | XL | Critical | Confidence class A/B/C; *Unverified* excluded from bulk actions; shared-resource detection; evidence shown per item | R4 |
| F-13 | **Uninstall orchestrator** | Uninstallers fail, leave residue, or are missing; users juggle five tools | P-19, P-25; S32–S34, S93, S96 | P0 | XL | Critical | Native uninstaller first; elevation only where required; full report of removed/skipped/preserved | F-12, F-14 |
| F-14 | **Broken-entry repair** | Entries that cannot uninstall; "cannot find uninstall.exe" | P-19; S32, S33, S34 | P1 | M | High | Registry export before any removal; reinstall-then-uninstall offered first; entry removal is not the same as app removal | F-13 |
| F-15 | **Startup manager with orphan detection** | Phantom "Program" entries; dead paths; boot slowdown | P-20, P-32; S35 | P1 | M | High | Disable by default rather than delete; StartupApproved state preserved; measured launch cost, no invented "impact score" | None |
| F-16 | **Orphaned service & task report** | Vendor services and scheduled tasks survive uninstall | P-21; S36 | P2 | M | Medium | Read-only report first; dependency graph before removal; services marked protected if referenced by Windows components | F-13 |
| F-17 | **Lock & permission inspector** | "File is open in an uninstalled app"; access denied; ownership advice that breaks systems | P-26, P-27; S37–S42 | P1 | M | High | Restart Manager first; never auto-take ownership; delete-on-reboot only with consent; ACL view is read-only | None |
| F-18 | **MSI patch/cache auditor** | `C:\Windows\Installer` growth; orphaned patches; broken repair | P-06; S43, S44, S45 | P2 | L | Medium | MSI API only (never raw file deletion); identify orphans by product registration; quarantine rather than delete | F-13 |
| F-19 | **Packaged-app manager** | Store/MSIX apps that will not uninstall; `WindowsApps` ownership disasters | P-22; S46, S47, S48 | P1 | M | High | Supported APIs only; `WindowsApps` is Protected; never change ACLs | R7 |
| F-20 | **Virtual disk compactor** (WSL/Docker/Hyper-V) | VHDX grows and never shrinks; 68 GB host file for 12 GB guest | P-33; S52, S53, S54, S55 | P2 | M | High | Refuse while the distro/VM is running; compact only (never delete); verify integrity after; explain sparse-VHD prevention | R9 |
| F-21 | **Developer cache manager** | `node_modules`, npm/pip/nuget/cargo/gradle caches, Docker images | P-33; S56, S57, S58 | P2 | L | High | Per-tool documented prune commands; warn about rebuild cost; never delete project source | None |
| F-22 | **Cloud placeholder safety lens** | 44 GB counted as local usage; deleting a placeholder deletes the cloud copy | P-30; S61, S62 | P1 | M | High | Show allocated (0) and logical size separately; warn before deleting placeholders; detect "always keep on device" | F-05 |
| F-23 | **Preview-first automation & cleanup profiles** | Automation deletes wanted files; scope limited to the system drive | P-31; S64, S65, S66 | P1 | L | High | Scheduled jobs run in dry-run by default; produce a review queue; per-volume scope; full undo history | R3, R9 |
| F-24 | **Maintenance console** | SFC/DISM/CHKDSK/optimise used with wrong order and unreadable logs | P-14, P-15; S69, S70, S81, S82 | P2 | M | Medium | Documented order enforced; plain-language log extraction; no "repair" claims beyond tool output | R9 |
| F-25 | **Game library analyser** | Uninstalled games leaving stores of data; launcher-managed installs | P-23; S71, S72 | P2 | M | Medium | Read launcher manifests as authoritative; protect save games explicitly; never delete `steamapps` wholesale | R7 |
| F-26 | **Storage history & growth tracking** | No way to see what grew over time; recurring "space disappeared again" | P-01; S01, S03 | P3 | L | Medium | Local database only; snapshot diffs are read-only; clear retention control | R10 |
| F-27 | **Export & audit log** | Users and admins need a record of what a tool did; currently none | P-09, P-31; S17, S20, S64 | P1 | S | High | Redact user names in exported paths on request; audit log is append-only; export never includes file contents | R3 |
| F-28 | **Protected-path manager** | Users need to guard their own folders from bulk operations | P-27, P-31; S40, S47, S64 | P1 | S | High | Add-only guardrails by default; removing a built-in protection requires typed confirmation | R14 |

## 5.3 Grouping into delivery themes

| Theme | Features | Why grouped |
|---|---|---|
| **Trust & accounting** | F-05, F-06, F-22, F-27 | Answers "why does the number not match?" and produces a record. Delivers the product's central differentiator |
| **Safe removal** | F-12, F-13, F-14, F-18, F-19 | All depend on the ownership engine and the R3 deletion pipeline |
| **System maintenance** | F-07, F-08, F-09, F-10, F-11, F-24 | Administrative; all need R9's review modal and elevation |
| **Everyday cleanup** | F-01, F-02, F-03, F-04, F-23 | High-frequency user workflows |
| **Specialist domains** | F-15, F-16, F-17, F-20, F-21, F-25 | Each needs domain-specific command integration |
| **Scale & history** | F-26 | Depends on a stable scan database |

## 5.4 Candidates evaluated and rejected or deferred

| Candidate | Decision | Reason |
|---|---|---|
| Registry optimiser / "registry cleaner" | **Rejected** | No evidence of benefit in the corpus; several reports of harm [S30][S95]. Conflicting with the registry policy in P-24 and Appendix C |
| RAM cleaner / "free memory" button | **Rejected** | Engineering nonsense as a product feature; Windows memory management is not improved by trimming working sets; damages trust |
| One-click "boost" / "speed up PC" | **Rejected** | Unverifiable claims; contradicts the evidence-first positioning; creates pressure to over-delete |
| Disk wiper / partition editor | **Rejected for this product** | Part 3.3 classifies these as separate high-risk workflows; they do not belong in a cleanup suite |
| File shredder (secure erase) | **Deferred** | Legitimate need, but SSD wear-levelling and TRIM make overwrite guarantees weak; would need careful claims; not evidence-driven in this corpus |
| Recycle Bin inspector as a separate module | **Merged** into R2/R3 UI | Duplicate surface area; better delivered as a scope in the cleanup module |
| Storage health / SMART panel | **Deferred to P3** | Valuable but tangential; requires `IOCTL_STORAGE_*` work and per-vendor reliability counters; no strong evidence pull in this corpus |
| Browser extension/history cleaner | **Rejected** | Privacy-sensitive data with weak user benefit and high support cost |
| Cloud upload of scan results for "cloud analysis" | **Rejected** | Violates the privacy stance in §3.4; local-only by default |
| Auto-delete without review | **Rejected** | Directly contradicted by the P-31 evidence |

## 5.5 Acceptance criteria template for feature delivery

Every feature in §5.2 must ship with: (1) a read-only or dry-run mode first; (2) an entry in the audit log; (3) a mapping to at least one §3 problem family in the release notes; (4) an automated test that exercises the destructive path against a synthetic tree; and (5) a documented "what this cannot do" statement in the UI help — because every feature in this table is a heuristic, and the product's credibility depends on saying so.

---
