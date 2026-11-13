# DiskWise — Windows Storage Intelligence & Application Management Suite

**Master research, product specification, architecture and implementation report**

| Field | Value |
|---|---|
| Document date | 2026-11-04 |
| Prepared for | Owner of an existing Windows cleanup application written primarily in C |
| Scope | Part 1 research (Nov 2022 – Nov 2026), Part 2 requirements 1–15, Part 3 safety, Parts 4–9 architecture/performance/competition/roadmap/testing, Part 10 report |
| Working product name | **DiskWise** (provisional; see §8.1 for naming note) |
| Source-code access | **None.** No repository, URL, archive or local directory of the existing application was supplied, and the workspace was empty at the start of this engagement. |
| Code changes to your application | **None.** Nothing in your existing codebase was inspected, modified, compiled or tested. §12 states exactly what is required to begin. |
| Artefacts actually produced | Icon system (SVG masters, PNG set, multi-resolution ICO), a tested C reference core for selection + deletion-gate logic, and this report. All are verifiable in the workspace. |

## Status legend used throughout

| Marker | Meaning |
|---|---|
| **[CONFIRMED]** | Directly observed, executed, or sourced from official documentation / first-hand reports cited in Appendix A. |
| **[PROPOSED]** | A design recommendation produced by this engagement. Not implemented, not user-tested. |
| **[PENDING VERIFICATION]** | Cannot be settled without the application's source, build system, or screenshots. Must be checked before implementation. |
| **[VENDOR CLAIM]** | A statement made by a commercial vendor about its own product. Not independently verified here. |

## How to read this document

§1–§3 are evidence. §4–§5 convert evidence into work. §6–§9 are analysis and design. §10–§12 are delivery. Appendix A is the source index and is the authority for every citation of the form `[S01]`. If a section makes a claim about your code, it will say **[PENDING VERIFICATION]** instead of guessing.

---

# 1. Executive Summary

## 1.1 The situation in one paragraph

Windows users are not short of cleaning tools; they are short of **tools that can explain themselves**. Across roughly 90 primary and secondary sources gathered for this report, the same failure pattern recurs in different costumes: the operating system reports storage the user cannot see, third-party utilities report numbers that do not reconcile with the drive, "leftover" removers guess by name similarity and occasionally guess wrong in a destructive way, and cleanup automation deletes files the user wanted. The strongest product opportunity is therefore not "clean more aggressively". It is **account for storage precisely, justify every recommendation with evidence, and make every destructive action reversible or refusable**.

## 1.2 What this engagement actually produced

| Deliverable | State | Evidence |
|---|---|---|
| Research corpus: 36 problem families, 99 indexed sources, tiered by evidence quality | Complete | §3, Appendix A |
| Mapping of all 15 requirements to discrete engineering work with acceptance criteria | Complete (specification only) | §4 |
| 26 additional research-driven feature proposals | Complete (specification only) | §5 |
| Competitor matrix and licensing analysis | Complete | §6, §7 |
| UI/UX blueprint, architecture, performance targets, roadmap, test plan | Complete (design) | §8–§11 |
| Icon system: vector masters (light/dark/mono/small-size), PNG exports, 10-size ICO | **Built and verified** | `assets/icons/`, §12.1 |
| Selection + deletion-gate reference core in portable C11, 39 behavioural checks | **Built, compiled, tested; 39/39 pass under ASan + UBSan** | `prototype/`, §12.2 |
| Changes to your existing application | **Not performed — no source access** | §12.3 |

## 1.3 The five strongest product opportunities

**1. Storage reconciliation instead of another pie chart.** Users repeatedly report that Explorer, Settings, and their disk analyzer disagree about how much space is used [S01][S03][S04][S59]. The disagreements are frequently legitimate and explainable: hard links counted twice, allocated size versus logical length, OneDrive placeholders with no local data, shadow copies, and NTFS metadata that directory enumeration cannot see [S59][S61][S89]. A product that shows *why the numbers differ* — a reconciliation ledger with named, measured buckets — converts the single most common support question into a trust-building feature. Microsoft itself shipped a misreporting bug in this exact area (the 24H2 "8.63 GB" cleanup figure) and had to call it a reporting error [S17][S20].

**2. Evidence-scored leftover ownership, not name matching.** The most damaging documented failures in competing tools come from similarity heuristics: a user removing Affinity Designer was offered files belonging to Ableton Live [S30]. Sysinternals-style "everything that looks like the name" is exactly the wrong model. Multi-signal ownership — MSI component registration, product codes, install paths, publisher signatures, file identity recorded at uninstall time — with an explicit *Unverified* bucket that is never bulk-deleted, is both safer and more defensible than competitors.

**3. A deletion pipeline that is safe by construction.** The industry norm is confirm-then-delete. The safer norm is: plan → re-validate identity → classify risk → stage to a quarantine vault → delete → verify → log. NTFS gives us the primitives to do this properly: 128-bit file IDs for identity re-checks before deletion, allocation size, link counts, reparse tags, and the Restart Manager for lock detection. My reference core verifies these rules mechanically and it is already passing 39 checks (§12.2), including the case that silently destroys data in naive implementations: **selections the user cannot see because a filter hides them** (§8.6).

**4. One integrated workflow where users currently chain five disconnected tools.** The observed workflow in the research is: install a disk analyzer to find the culprit → install an uninstaller to remove the app → run Disk Cleanup for the system junk → hunt `%TEMP%` manually → look up a WinSxS article and run DISM → discover WSL's virtual disk never shrank and run `diskpart` [S52][S53][S54][S59][S83]. Each hop is a place where a non-expert user applies the wrong tool to the wrong category. Integration — with each module able to hand a selected item to the correct specialist workflow — is a genuine, defensible differentiator.

**5. Preview-first, reversible automation.** Storage Sense is documented to operate on the system drive only, and users report it deleting content they wanted [S64][S65][S66]. The correct design is not "no automation" but **automation with a dry-run, a review queue, a per-drive scope, and an undo history**. That is achievable with a quarantine vault and a manifest per job.

## 1.4 The five most serious risks

| Risk | Why it matters | Mitigation committed in this design |
|---|---|---|
| **Leftover false positives** | Deleting another application's data or a shared runtime is the fastest way to lose a customer permanently. Documented competitor failure: [S30]. | Evidence scoring with confidence levels; *Unverified* bucket never eligible for bulk action; registry keys never removed on name resemblance; hard-linked and shared files detected via link count (§9.5). |
| **Elevation becomes a security hole** | An elevated cleaner that accepts commands from an unelevated UI is a privilege-escalation primitive. | Split-process broker: unelevated UI, elevated worker, strictly typed command set (no arbitrary path/command execution), named pipe restricted to the interactive session, caller verification by SID and session (§9.7). |
| **Registry "cleanup"** | No evidence that broad registry cleaning improves performance; high potential to break COM/installer state. | Registry module is **read/analyse/backup only**, limited to verified product-owned keys; no "registry optimisation" claims; restore path mandatory (§3.5, Appendix C). |
| **Raw NTFS scanning interacts with security software** | Direct `$MFT` reads and USN enumeration can trip EDR heuristics, and require administrator rights. | Documented-API-only implementation, signed binary, explicit consent for the fast backend, automatic fallback to directory enumeration, and a manifest declaring no network access (§9.4.4). |
| **Silent destructive scope creep** | "Select all" that includes filtered-out rows is a data-loss generator. | Selection is keyed by stable file identity, never by row index; hidden-selection accounting is mandatory in every destructive dialog; the reference core proves the accounting (§12.2). |

## 1.5 Bottom line

The requirements in Part 2 are sound and mutually reinforcing: unified tables and true multi-selection (R2) only become safe when paired with the risk-classified confirmation dialog (R3) and the pre-execution review modal (R9); the disk map (R10) is only honest when it reconciles with the volume rather than inflating totals with hard links. The recommended delivery order is therefore **safety and correctness first (P0), then table/search/selection usability (P1), then the disk map and the ownership engine (P2)** — deliberately *not* the order in which the visual features were requested. A detailed, dependency-aware sequence is in §10.

---

# 2. Research Methodology

## 2.1 Timing and process

| Item | Detail |
|---|---|
| Research window requested | November 2022 → November 2026, weighted to 2024–2026 |
| Research performed | 2026-11-04, in five parallel batches (40 discrete queries, ~90 retained sources) |
| Selection rule | Retain a source if it describes a *symptom, failed attempt, or capability gap* — not merely a product recommendation |
| Grouping method | Symptoms clustered into families; a family was accepted only if at least two independent sources, or one first-hand report plus one authoritative document, supported it |
| Contradiction handling | Conflicting advice recorded (e.g. pagefile disabling: [S08][S09]); environmental differences noted rather than resolved into a false universal rule |
| Safety-sensitive claims | Cross-checked against Microsoft documentation where available: WinSxS [S10], Storage Sense [S65], component cleanup [S92], 24H2 reporting bug [S20] |

## 2.2 Platforms reached, and not reached

| Platform | Status | Notes |
|---|---|---|
| Reddit (r/Windows11, r/Windows10, r/techsupport, r/WindowsHelp, r/sysadmin, r/software, r/docker, r/wsl2, r/webdev, r/node, r/Steam, r/DataHoarder, r/pcgamingtechsupport, r/WindowsApps, r/antivirus, r/privacy) | Reached | Thread content retrieved through a search layer; read as text excerpts, not rendered pages. Exact post timestamps are frequently unavailable through this route and are marked accordingly. |
| Microsoft Learn / Microsoft Q&A / Microsoft Support | Reached | Treated as authoritative for documented behaviour; Q&A answers are treated as community opinion unless flagged by Microsoft staff. |
| GitHub (issues, discussions, repositories, licences) | Reached | Used for licensing facts and for first-hand bug reports (e.g. OneDrive placeholder accounting [S61]). |
| Super User / Stack Exchange | Reached | Used for technical explanations and for the `%windir%\Installer` safety ruling [S44]. |
| Vendor documentation (WizTree EULA/features, TreeSize KB, BCUninstaller README) | Reached | Treated as **[VENDOR CLAIM]** except where licence text is quoted directly. |
| **X / Twitter** | **Not reachable** | No public post or reply content could be retrieved through the available tooling. **Evidence gap declared.** All X-derived claims are absent rather than fabricated. |
| Authenticated forums (Ten Forums thread pages beyond excerpts), some vendor forums | Partially reachable | Excerpt-level only. |
| Benchmarks on real hardware | **Not performed** | No Windows host available in this environment. §9.9 contains *targets*, explicitly not measured results. |

## 2.3 Evidence tiers

| Tier | Definition | Usage rule in this report |
|---|---|---|
| **T1** | First-hand user report: Reddit thread, Microsoft Q&A question, Super User question, GitHub issue | May be used to establish that a problem is real and to characterise its symptoms. Never used alone to establish a root cause. |
| **T2** | Official documentation or vendor acknowledgement (Microsoft Learn/Support, known-issue notice) | May establish documented behaviour and correct procedure. |
| **T3** | Technical press with named reporting (e.g. Windows Latest, PCMag, ExtremeTech, Neowin) | May establish that a defect occurred and was acknowledged; not used for root-cause claims beyond what is quoted. |
| **T4** | Vendor/SEO/tutorial content, content farms, AI-generated answer pages | Used **only** as corroboration of a procedure already supported by T1–T3, never as sole support for a claim. |

## 2.4 Queries executed (condensed)

Storage mystery: `Windows 11 storage space disappearing no files found` · `unexplained drive usage Windows 11` · `free space missing Windows 10 11 volume` · `Storage Sense deleted my downloaded files`. System-managed files: `hiberfil.sys huge disable`, `pagefile.sys 64GB RAM disable`, `WinSxS huge can I delete`, `Windows.old TrustedInstaller`, `C:\Windows\Installer huge safe delete`, `System Volume Information huge vssadmin`. Updates: `SoftwareDistribution safe delete`, `Delivery Optimization cache 60GB`, `Windows 11 24H2 8.63GB undeletable`, `24H2 needs 20GB free`, `DISM StartComponentCleanup resetbase`. Junk: `%TEMP% refilling`, `temporary files shows 40GB deletes 500MB`, `Disk Cleanup deprecated Storage Sense`. Uninstall: `leftover files after uninstall`, `Revo Uninstaller safe reddit`, `uninstaller missing registry`, `WindowsApps access denied uninstall`, `uninstalled game still taking space`, `orphaned services scheduled tasks`, `phantom startup entries`. Files: `cannot delete file in use LockHunter`, `take ownership TrustedInstaller`, `delete huge number of files Explorer`, `duplicate photos Windows`. Analysis tools: `WizTree vs TreeSize MFT`, `hardlinks double counting disk analyzer`, `WinDirStat slow freeze`, `OneDrive placeholder size`, `Everything MFT index`. Developer/VM: `WSL2 ext4.vhdx compact`, `Docker Desktop vhdx`, `node_modules 70GB`. Platform: `WMIC removed Windows 11`, `SSD TRIM optimize drives`. Licensing: `WinDirStat GPL`, `Bulk Crap Uninstaller Apache`, `WizTree EULA`, `TreeSize Free limitations`.

## 2.5 Explicit limitations of this research

1. **No telemetry, no benchmarks.** Nothing here measures your application's current speed, memory use, or crash rate.
2. **Thread dates are approximate.** Where a date could not be confirmed it is shown as a range inferred from the thread's content (e.g. "≥2024, references 24H2"), not as a precise timestamp.
3. **Sentiment is not quantified.** Upvote counts, view counts and engagement statistics were not reliably available and are therefore never quoted.
4. **Market-share and adoption claims are avoided.** Competitor strengths are described from documentation and community commentary, not from sales data.
5. **Windows Insider builds move.** Any statement about WMIC removal [S79], Disk Cleanup's status [S91] or Storage Settings behaviour should be re-verified against the specific build you support before it becomes a hard dependency.
6. **One first-hand source can be wrong.** A user report establishes that a symptom was experienced, not that the user's diagnosis was correct. Cause columns therefore separate "documented cause" from "community speculation".

---
