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

## Contents

- **[1. Executive Summary](#1-executive-summary)**
  - [1.1 The situation in one paragraph](#11-the-situation-in-one-paragraph)
  - [1.2 What this engagement actually produced](#12-what-this-engagement-actually-produced)
  - [1.3 The five strongest product opportunities](#13-the-five-strongest-product-opportunities)
  - [1.4 The five most serious risks](#14-the-five-most-serious-risks)
  - [1.5 Bottom line](#15-bottom-line)
- **[2. Research Methodology](#2-research-methodology)**
  - [2.1 Timing and process](#21-timing-and-process)
  - [2.2 Platforms reached, and not reached](#22-platforms-reached-and-not-reached)
  - [2.3 Evidence tiers](#23-evidence-tiers)
  - [2.4 Queries executed (condensed)](#24-queries-executed-condensed)
  - [2.5 Explicit limitations of this research](#25-explicit-limitations-of-this-research)
- **[3. Real-World Windows Cleanup Problems](#3-real-world-windows-cleanup-problems)**
  - [3.1 How to read these tables](#31-how-to-read-these-tables)
  - [3.2 Storage accounting and system-managed files](#32-storage-accounting-and-system-managed-files)
  - [3.3 Updates, junk and maintenance](#33-updates-junk-and-maintenance)
  - [3.4 Applications, uninstall and leftovers](#34-applications-uninstall-and-leftovers)
  - [3.5 Files, permissions, developer and virtualisation](#35-files-permissions-developer-and-virtualisation)
  - [3.6 Ten families examined in detail](#36-ten-families-examined-in-detail)
  - [3.7 Cross-cutting findings](#37-cross-cutting-findings)
  - [3.8 Gap analysis: what the market does not do](#38-gap-analysis-what-the-market-does-not-do)
- **[4. Existing Application Improvement Audit](#4-existing-application-improvement-audit)**
  - [4.1 Audit status and method](#41-audit-status-and-method)
  - [4.2 Traceability summary](#42-traceability-summary)
  - [4.3 R1 — Professional application icon](#43-r1-professional-application-icon)
  - [4.4 R2 — Unified tables and advanced multi-selection](#44-r2-unified-tables-and-advanced-multi-selection)
  - [4.5 R3 — Safe deletion and warning dialogs](#45-r3-safe-deletion-and-warning-dialogs)
  - [4.6 R4 — Comprehensive application uninstallation](#46-r4-comprehensive-application-uninstallation)
  - [4.7 R5 — Polish every existing module](#47-r5-polish-every-existing-module)
  - [4.8 R6 — Right-click context menus](#48-r6-right-click-context-menus)
  - [4.9 R7 — Drive selection](#49-r7-drive-selection)
  - [4.10 R8 — Table headers and sorting](#410-r8-table-headers-and-sorting)
  - [4.11 R9 — Pre-execution review for administrative and cleanup operations](#411-r9-pre-execution-review-for-administrative-and-cleanup-operations)
  - [4.12 R10 — Clean Run preview and Disk Intelligence (Disk Map)](#412-r10-clean-run-preview-and-disk-intelligence-disk-map)
  - [4.13 R11 — Open-source reuse feasibility](#413-r11-open-source-reuse-feasibility)
  - [4.14 R12 — Advanced context-menu improvements](#414-r12-advanced-context-menu-improvements)
  - [4.15 R13 — Responsive dashboard and screen-size handling](#415-r13-responsive-dashboard-and-screen-size-handling)
  - [4.16 R14 — Redesigned settings](#416-r14-redesigned-settings)
  - [4.17 R15 — Research-driven additional features](#417-r15-research-driven-additional-features)
  - [4.18 Cross-cutting rule applied in this audit](#418-cross-cutting-rule-applied-in-this-audit)
- **[5. Additional Feature Recommendations](#5-additional-feature-recommendations)**
  - [5.1 Selection rules used](#51-selection-rules-used)
  - [5.2 The 28 proposals](#52-the-28-proposals)
  - [5.3 Grouping into delivery themes](#53-grouping-into-delivery-themes)
  - [5.4 Candidates evaluated and rejected or deferred](#54-candidates-evaluated-and-rejected-or-deferred)
  - [5.5 Acceptance criteria template for feature delivery](#55-acceptance-criteria-template-for-feature-delivery)
- **[6. Competitor Comparison](#6-competitor-comparison)**
  - [6.1 Ground rules for this section](#61-ground-rules-for-this-section)
  - [6.2 Comparison matrix](#62-comparison-matrix)
  - [6.3 What the matrix actually says](#63-what-the-matrix-actually-says)
  - [6.4 Where DiskWise should compete — and where it should not](#64-where-diskwise-should-compete-and-where-it-should-not)
  - [6.5 Integration patterns that are lawful and useful](#65-integration-patterns-that-are-lawful-and-useful)
- **[7. WizTree and Open-Source Feasibility](#7-wiztree-and-open-source-feasibility)**
  - [7.1 WizTree: what may and may not be done](#71-wiztree-what-may-and-may-not-be-done)
  - [7.2 The open-source candidates](#72-the-open-source-candidates)
  - [7.3 Recommended approach: independent implementation on documented APIs](#73-recommended-approach-independent-implementation-on-documented-apis)
- **[8. UI/UX Redesign Blueprint](#8-uiux-redesign-blueprint)**
  - [8.1 Identity: name, mark and brand caveat](#81-identity-name-mark-and-brand-caveat)
  - [8.2 Design tokens](#82-design-tokens)
  - [8.3 Application shell](#83-application-shell)
  - [8.4 Dashboard](#84-dashboard)
  - [8.5 Unified table (Old Files / Leftovers / Apps share one control)](#85-unified-table-old-files-leftovers-apps-share-one-control)
  - [8.6 Deletion confirmation dialog](#86-deletion-confirmation-dialog)
  - [8.7 Pre-execution review modal (Requirement 9)](#87-pre-execution-review-modal-requirement-9)
  - [8.8 Disk Map (Disk Intelligence)](#88-disk-map-disk-intelligence)
  - [8.9 Settings](#89-settings)
  - [8.10 Apps module (Requirement 4 surface)](#810-apps-module-requirement-4-surface)
  - [8.11 Leftover review and Clean Run preview](#811-leftover-review-and-clean-run-preview)
  - [8.12 Context menu structure](#812-context-menu-structure)
  - [8.13 Keyboard model](#813-keyboard-model)
  - [8.14 Component inventory to build or standardise](#814-component-inventory-to-build-or-standardise)
- **[9. Technical Architecture](#9-technical-architecture)**
  - [9.1 Principles](#91-principles)
  - [9.2 What must be established by inspecting your code first](#92-what-must-be-established-by-inspecting-your-code-first)
  - [9.3 Module map](#93-module-map)
  - [9.4 File-system scanner](#94-file-system-scanner)
  - [9.5 Ownership and classification engine](#95-ownership-and-classification-engine)
  - [9.6 Risk scoring](#96-risk-scoring)
  - [9.7 Privilege model](#97-privilege-model)
  - [9.8 Data and persistence](#98-data-and-persistence)
  - [9.9 Performance targets](#99-performance-targets)
  - [9.10 Threading and responsiveness](#910-threading-and-responsiveness)
  - [9.11 Build, packaging and compatibility](#911-build-packaging-and-compatibility)
  - [9.12 Tradeoffs accepted](#912-tradeoffs-accepted)
  - [9.13 What to change versus preserve in the existing application](#913-what-to-change-versus-preserve-in-the-existing-application)
- **[10. Implementation Roadmap](#10-implementation-roadmap)**
  - [10.1 Sequencing principle](#101-sequencing-principle)
  - [10.2 Dependency graph](#102-dependency-graph)
  - [10.3 Consolidated work breakdown](#103-consolidated-work-breakdown)
  - [10.4 Phase plan](#104-phase-plan)
  - [10.5 Cut lines if capacity is short](#105-cut-lines-if-capacity-is-short)
  - [10.6 Definition of done (per feature)](#106-definition-of-done-per-feature)
- **[11. Testing and Security Checklist](#11-testing-and-security-checklist)**
  - [11.1 Test environment](#111-test-environment)
  - [11.2 UI tests](#112-ui-tests)
  - [11.3 Scanning tests](#113-scanning-tests)
  - [11.4 Deletion tests](#114-deletion-tests)
  - [11.5 Uninstallation tests](#115-uninstallation-tests)
  - [11.6 Reliability tests](#116-reliability-tests)
  - [11.7 Security checklist](#117-security-checklist)
  - [11.8 Performance regression protocol](#118-performance-regression-protocol)
- **[12. Implementation Results or Pending Work](#12-implementation-results-or-pending-work)**
  - [12.1 Delivered: icon system (Requirement 1)](#121-delivered-icon-system-requirement-1)
  - [12.2 Delivered: selection + deletion-gate reference core (Requirements 2 and 3, core logic)](#122-delivered-selection-deletion-gate-reference-core-requirements-2-and-3-core-logic)
  - [12.3 Not performed: changes to your application](#123-not-performed-changes-to-your-application)
  - [12.4 Recommended first ten working days (once access exists)](#124-recommended-first-ten-working-days-once-access-exists)
  - [12.5 Statement of accuracy for this report](#125-statement-of-accuracy-for-this-report)
- **[Appendix A — Source Index](#appendix-a-source-index)**
- **[Appendix B — Icon Technical Specification](#appendix-b-icon-technical-specification)**
- **[Appendix C — Registry Safety Rules](#appendix-c-registry-safety-rules)**
- **[Appendix D — Assumptions and Open Questions](#appendix-d-assumptions-and-open-questions)**

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

---

# 3. Real-World Windows Cleanup Problems

## 3.1 How to read these tables

Each row is a **problem family** (IDs `P-nn`) — a cluster of reports sharing symptoms, not a single post. The *Source* column lists the evidence IDs defined in Appendix A with an evidence tier (T1 first-hand report, T2 official documentation/acknowledgement, T3 technical press, T4 vendor/SEO corroboration) and the approximate period the evidence originates from. The *Root cause* column separates documented causes from community speculation; where the true cause is unknown or environmental, that is stated rather than resolved.

Status of the 36 families below: **33 are supported by at least one T1 or T2 source.** Three — P-20, P-21 and P-32 — rest only on T4 community/tutorial material and are labelled accordingly in their Source column; they are included for completeness but should be re-validated against first-hand reports before any engineering effort is spent on them.

## 3.2 Storage accounting and system-managed files

| ID | User problem | Source & date | Reported symptoms | Root cause / uncertainty | Existing solution & gap | Proposed feature | Risk | Pri |
|---|---|---|---|---|---|---|---|---|
| P-01 | Storage vanishes with no visible culprit | S01 T1 (≥2026); S03 T1 (≥2024); S04 T1 (≥2022) | 20–200 GB "missing"; folder totals far below drive usage; users reinstall Windows out of frustration | Multiple simultaneous causes: hiberfil/pagefile, VSS, hard-link double counting, system files hidden from enumeration, Delivery Optimization. **Not one cause.** | Install a third-party analyser; run Disk Cleanup; guess | R10 Disk Map + **F-05** reconciliation ledger | Low | P1 |
| P-02 | `hiberfil.sys` is huge, locked and invisible | S05 T1 (2025); S06 T1 (2025) | 30 GB+ file; "file is in use"; not visible even with hidden files shown; users try to delete it in Explorer | Kernel-locked system file with hidden+system attributes; sized from RAM. **Confirmed behaviour.** | `powercfg /h off`; `powercfg /h /type reduced`; users disable hibernation and lose Fast Startup without knowing | **F-07** explain, size, and offer reduced-hibernation instead of deletion | Med | P2 |
| P-03 | `pagefile.sys` misunderstanding and harmful advice | S07 T1 (2025); S08 T1 (≥2026); S09 T1 (≥2021) | 25 GB pagefile; contradictory advice: disable it, move it, cap it | System-managed by default; some applications require a pagefile for `VirtualAlloc` guarantees. Community advice ranges from sound to harmful | Forums; registry; no UI explains consequences | **F-07** (explain + safe cap guidance; deletion blocked) | Med | P2 |
| P-04 | `WinSxS` panic, and advice that can brick servicing | S10 T2 (MS Learn); S11 T1 (≥2023); S12 T1 (2023); S92 T2 (2026) | 30–60 GB folder; users told to delete it; cleanup "doesn't help" | Component store uses hard links so on-disk size is inflated by naive tools; superseded components retained deliberately. **Confirmed.** | `DISM /AnalyzeComponentStore`, `/StartComponentCleanup`, `/ResetBase`; cleanup recommendations | **F-09** analyzer that shows *reclaimable* vs *referenced*, and warns that `/ResetBase` is irreversible | High | P2 |
| P-05 | `Windows.old` cannot be removed; TrustedInstaller denies access | S13 T1 (2025); S14 T2/T1 (≥2026) | 20 GB+ folder; access denied; users take ownership and break servicing | Owned by TrustedInstaller; must be removed through servicing-aware tools to preserve rollback state. **Confirmed.** | Disk Cleanup "previous installations"; Storage; ownership hacks (unsupported) | **F-10** guided removal with explicit "you lose rollback" warning | High | P1 |
| P-06 | `C:\Windows\Installer` grows to tens of GB | S43 T1 (2020, still cited); S44 T1+T2 (2019); S45 T3 (2026) | 60 GB folder; users delete `.msi` files and then cannot repair or uninstall | MSI cache required for repair/modify/uninstall; orphaned patches accumulate when products are removed badly. **Confirmed by Microsoft technician [S44].** | PatchCleaner (with warnings); uninstall old Office/VS versions | **F-18** MSI-aware orphan patch detection (never raw deletion) | High | P2 |
| P-07 | `System Volume Information` consumes the drive | S49 T1 (2023); S50 T1 (≥2021); S51 T4 (2026) | 50–400 GB folder; access denied; `vssadmin` "access denied" even for admins | VSS shadow storage and restore points — **this is backup data, not junk** | `vssadmin delete shadows` (destroys recovery options), `resize shadowstorage` | **F-11** shadow-storage analyzer with backup-aware warnings | High | P2 |
| P-08 | "Temporary files" total cannot be cleaned | S23 T1 (2019, recurring); S24 T1 (2024); S25 T1+T2 (2025); S26 T1 (2025) | Settings reports 5–43 GB; cleanup removes a few hundred MB; number never changes | Category totals overlap with component store/update cache; reported size ≠ deletable size; restore points excluded from the category. **Partly confirmed, partly unexplained.** | `cleanmgr` system files; DISM; manual %TEMP% | **F-06** per-category drill-down splitting *reported*, *deletable*, and *why* | Low | **P0** |
| P-09 | 24H2 shows 8.63 GB of update cleanup that never shrinks | S17 T3 (2024-10); S18 T3 (2024-10); S19 T3 (2024); S20 T3+T2 (2024-10) | After 24H2, "Windows Update Cleanup" stays at 8.63 GB despite repeated cleanup | Checkpoint cumulative updates keep components referenced for future updates; Microsoft called the residual figure a **reporting error**. **Confirmed by Microsoft.** | None; wait for fix; clean install (absurd) | **F-05/F-08**: report *measured* free-space delta, never category totals | Low | **P0** |
| P-10 | `%TEMP%` refills within hours | S21 T1 (2020, recurring); S22 T1 (2025) | Temp folder grows to tens of GB; deletions do not stick; AV definitions rewritten continuously | Misbehaving application/service logging; corrupted CBS logs; AppX install loops. **Per-case, requires attribution.** | Delete temp; recurs | **F-06** churn rate + lock-holder attribution | Low | P2 |

## 3.3 Updates, junk and maintenance

| ID | User problem | Source & date | Reported symptoms | Root cause / uncertainty | Existing solution & gap | Proposed feature | Risk | Pri |
|---|---|---|---|---|---|---|---|---|
| P-11 | Windows Update cache bloats or updates fail at 0% | S15 T3 (2026); S16 T4 (2026) | Multi-GB `SoftwareDistribution`; updates stuck; "folder in use" | Corrupted or incomplete update cache. **Confirmed as a legitimate troubleshooting step**, but sources warn it is *not* routine maintenance | Stop `wuauserv`/BITS, delete `Download` contents, restart services | **F-08** guarded reset: stop → reset → restart → verify update engine healthy | Med | P2 |
| P-12 | Delivery Optimization cache eats 60–100 GB | S01 T1 (≥2026); S83 T1 (2025) | Huge hidden cache; folder reappears after deletion; users unsure whether it is junk | Peer-to-peer update cache; separate from `SoftwareDistribution`. **Confirmed by Microsoft FAQ-style documentation [S15].** | Disable DO; delete folder; no explanation of consequence | **F-08** DO analysis, size, scope, and correctly-worded disable option | Low | P2 |
| P-13 | Feature update refuses to install ("needs more space") | S67 T1 (2025-01); S68 T3 (2026); S17 T3 (2024) | 24H2 requires roughly 20–40 GB free; cleanup loop never satisfies it | Staging requires space for download + `Windows.old` + working room. **Confirmed.** | Delete caches, uninstall apps, move personal files | **F-08** "make room for this update" planner with measured requirements | Low | P2 |
| P-14 | Component store corruption; `sfc` cannot repair | S69 T1 (2026); S70 T1 (2025) | `SFC` reports unrepairable files; `DISM` errors; conflicting advice about order | Store corruption; wrong tool order. **Confirmed procedure: DISM `/RestoreHealth` before `sfc /scannow`.** | Follow Microsoft KBs; parse CBS.log manually | **F-24** maintenance console that runs documented tools and explains output | Med | P2 |
| P-15 | SSD maintenance anxiety (defrag vs TRIM) | S81 T3 (2026-11); S82 T3 (2026) | Users told "never defrag an SSD" and also "always defrag"; third-party optimizers marketed aggressively | Windows already schedules media-aware optimisation weekly; SSDs get retrim. **Confirmed by Microsoft documentation quoted in [S82].** | Built-in Optimize Drives | **F-24** health/maintenance page that delegates to documented tools and explains what happened | Low | P3 |
| P-16 | Disk Cleanup versus Storage Sense confusion | S65 T2 (MS Support); S91 T3 (2024/2026) | Users cannot tell which tool is current; some sources say Disk Cleanup is being phased out, others say it is not deprecated | Microsoft guidance has shifted toward Storage/cleanup recommendations; Disk Cleanup remains present and functional | Two overlapping tools with different category sets | **F-05/F-06** unify categories under one explainable model and label provenance | Low | P2 |

## 3.4 Applications, uninstall and leftovers

| ID | User problem | Source & date | Reported symptoms | Root cause / uncertainty | Existing solution & gap | Proposed feature | Risk | Pri |
|---|---|---|---|---|---|---|---|---|
| P-17 | Files and registry keys survive uninstall | S27 T1 (2021, recurring); S28 T3 (2023); S29 T3 (2026) | `AppData`, `ProgramData`, `Common Files`, registry remnants; new install "remembers" old settings | Uninstallers intentionally preserve user data; some remove nothing beyond the install dir. **Confirmed pattern.** | Manual deletion; Revo/BCUninstaller/Geek | **F-12** leftover engine + **R4** orchestration | High | P1 |
| P-18 | Leftover tools propose **other applications'** files | S30 T1 (2024); S31 T1 (2024) | Advanced scan offered Ableton Live files when uninstalling Affinity Designer; users told to "review thousands of entries" | Name-similarity and heuristic matching; no ownership proof. **Confirmed by user report.** | Manual review; "safe" scan modes trade recall for risk | **F-12** multi-signal ownership + explicit *Unverified* bucket | High | **P0** |
| P-19 | Application will not uninstall; entry broken | S32 T1 (2024); S33 T3 (2026); S34 T4 (2026) | "Cannot find uninstall.exe"; entry stays in Settings; user deleted program folder manually | Missing uninstaller binary; inconsistent registry metadata. **Confirmed.** | Delete the Uninstall key (incomplete and risky); reinstall then uninstall | **F-14** broken-entry repair with registry backup | Med | P1 |
| P-20 | Phantom startup entries after uninstall | S35 T4 (2026) | Task Manager lists "Program"; Properties fails; boot delay | Orphaned Run values / StartupApproved state / scheduled tasks | Autoruns; manual registry editing | **F-15** startup manager with orphan detection | Low | P1 |
| P-21 | Vendor services and tasks survive uninstall | S36 T4 (2026) | Security suite services still running; tasks relaunch components | Incomplete uninstall, self-protection | `sc delete`, Task Scheduler manually | **F-16** read-only orphan service/task report, then guided removal | Med | P2 |
| P-22 | Microsoft Store / `WindowsApps` app cannot be removed | S46 T1 (2021); S47 T1 (2021); S48 T1 (2022) | Access denied; broken game occupies space; users take ownership of `WindowsApps` and break the Store | ACL owned by TrustedInstaller by design. **Confirmed by community consensus.** | Uninstall via Settings; `Remove-AppxVolume`; ownership hacks (harmful) | **F-19** packaged-app manager using supported APIs only; `WindowsApps` is **Protected** | High | P1 |
| P-23 | Uninstalled game still occupies space | S71 T1 (2020, recurring); S72 T1 (2024) | Space never returned; folder remains under `steamapps\common` | Launcher-managed installs; Settings uninstall may not run the launcher's own removal | Delete folder manually; Revo | **F-25** launcher manifest-aware inventory (authoritative) | Low | P2 |
| P-24 | Registry cleaner recommendations of unknown value | S30 T1 (2024); S95 T1 (2025); S93 T1 (2022) | Users distrust cleaners after bad experiences; privacy concerns about tool telemetry | No credible evidence that broad registry cleaning improves performance. **Absence of evidence is itself the finding.** | Tools that do it anyway; advice to "just delete the folder" | **No registry optimiser.** Evidence-scoped registry analysis only (Appendix C) | High | **P0** |
| P-25 | Bulk uninstall/inventory work is impractical | S96 T1 (2025); S93 T1 (2022) | Users compare five uninstallers; each finds a different number of remnants | Different discovery sources and heuristics | Manual comparison | **F-13** inventory with provenance per entry | Med | P1 |

## 3.5 Files, permissions, developer and virtualisation

| ID | User problem | Source & date | Reported symptoms | Root cause / uncertainty | Existing solution & gap | Proposed feature | Risk | Pri |
|---|---|---|---|---|---|---|---|---|
| P-26 | File cannot be deleted although the owning app was uninstalled | S37 T1 (2026); S38 T1 (2025); S39 T1 (2024) | "Open in ReMouse/Filmora"; access denied; invisible holder (`dllhost`, COM surrogate) | Lock held by a service/COM surrogate; Explorer misleading | Reboot, safe mode, LockHunter, PowerToys File Locksmith | **F-17** Restart Manager lock inspector + delete-on-reboot | Low | P1 |
| P-27 | TrustedInstaller / ownership confusion | S40 T1 (2021); S41 T1 (2020); S42 T4 (2026) | "Access is denied" when changing owner; options greyed out; `takeown` advice | OS-owned ACLs; administrator ≠ owner. **Confirmed.** | `takeown`/`icacls` (often inappropriate), guides | **F-17** explain and block; ownership change never automatic | High | **P0** |
| P-28 | Disk analysers disagree with Explorer | S59 T1 (2017, still cited); S89 T3 (JAM KB); S98 T3 (OSS analyser, 2026) | Totals exceed drive capacity; TreeSize counted `WinSxS` hard links; folders look bigger than they are | Hard links counted multiple times; logical vs allocated; ADS; dedup; placeholders. **Confirmed by vendor documentation [S89].** | Tools differ with no explanation | **F-05** identity-based dedupe + allocated-size option + ledger | Low | **P0** |
| P-29 | Disk analysers are slow or freeze | S63 T1 (2025); S97 T1 (community, ongoing); S99 T1 (2013) | Scan stalls at 25%, UI freezes, crash on large export, Explorer memory explodes on mass delete [S74] | Traversal-based scanning, UI-thread work, unbounded memory | MFT-based tools are seconds instead of minutes [S84] | **R10** MFT backend + cancellable jobs + virtualised rendering | Low | **P0** |
| P-30 | OneDrive placeholders counted as local usage | S61 T1 (2026, GitHub issue); S62 T1 (2026, PR) | 44 GB counted as disk usage although files are cloud-only; tools disagree | Logical size vs allocated size; recall attributes. **Confirmed by a fix shipped in an OSS tool.** | Few tools handle it correctly | **F-22** cloud-aware size model and safety lens | Med | P1 |
| P-31 | Storage Sense removed files the user wanted | S64 T1 (2023); S65 T2 (MS); S66 T1 (2021); S02 T1 (2024) | Downloads folder emptied; only C: cleaned; setting re-enables itself | Automation defaults and scope. **Microsoft documents: system drive only; Downloads untouched unless configured.** | Turn Storage Sense off (losing the benefit) | **F-23** preview-first automation, per-drive scope, undo | High | P1 |
| P-32 | Startup bloat suspected of slowing boot | S35 T4 only; corroborated by widespread tutorial content | Dozens of startup entries; slow login | Real in principle; **evidence for magnitude is weak and vendor-sourced** | Task Manager startup tab; Autoruns | **F-15** (rank by measured launch cost, do not invent an "impact score") | Low | P2 |
| P-33 | Developer and VM storage grows without bound | S56 T1 (2025); S57 T1 (2024); S58 T1 (2024); S52 T1 (2025); S53 T1 (2025); S54 T1 (2024); S55 T1 (2024) | `node_modules` 70 GB; npm cache 60 GB; WSL `ext4.vhdx` 68 GB while guest uses 12 GB; Docker 30–400 GB after prune | Per-project dependency trees; global caches; dynamically expanding VHDX never shrinks. **Confirmed by multiple first-hand reports.** | `npkill`, pnpm; `diskpart compact vdisk`; `Optimize-VHD` | **F-20** (VHDX) and **F-21** (dev caches) with command-backed actions | Med | P2 |
| P-34 | Legacy admin tooling removed from Windows | S79 T3 (2026-08); S80 T4 (2026) | `wmic` scripts break; uninstall one-liners from old guides fail | WMIC not included in Windows 11 26H2 and not available as a Feature on Demand. **Confirmed by Microsoft flight notes as reported.** | Rewrite with PowerShell/CIM | Engineering constraint: **no WMIC dependency**; use MSI API, CIM, PowerShell bridges | Low | **P0** (eng.) |
| P-35 | Mass deletion in Explorer is dangerous or impossible | S73 T1 (2023); S74 T1 (2012, still cited); S75 T1 (2023) | Invert-selection workarounds; deleting 100M files crashes Explorer; no safe bulk path | Shell is not designed for bulk destructive operations; no dry-run | PowerShell one-liners, third-party | **R2 + R3** bulk selection with counted, previewed, reversible execution | Low | P1 |
| P-36 | Duplicate media consumes space with no practical way to review or remove it | S76 T1 (2021); S77 T1 (2025); S78 T1 (2020) | 140k-photo libraries with unknown duplication; free tools cap bulk deletion; users keep duplicates rather than risk deleting the wrong copy | Copies accumulate across folders, devices and re-downloads. **Confirmed as a common pattern; the true duplicate rate is environment-specific.** | Hash-based tools exist but differ in similarity handling and deletion workflow | **F-01** duplicate finder with survivor selection and group-level confirmation | Med | P2 |

## 3.6 Ten families examined in detail

**P-01 — The "where did my space go" family.** The reports are strikingly consistent: users see used space in Settings that no folder explains [S01][S03]. Respondents offer a rotating list of suspects — hibernation, pagefile, update cache, Delivery Optimization, "Chrome tabs" — which is itself the diagnostic failure: *nobody can see a ranked, reconciled ledger*. Two of the three threads eventually resolve with a third-party analyser, and one user reinstalls Windows instead. The engineering answer is not "scan faster" but "account completely": enumerate what can be enumerated, explicitly name what cannot (VSS, `$MFT`, registry hives, cluster slack), and show arithmetic that closes.

**P-09 — The reporting-error family.** In October 2024 Microsoft shipped 24H2 with checkpoint cumulative updates; users saw 8.63 GB of "Windows Update Cleanup" that would not delete [S17][S19]. Microsoft's response was that Disk Cleanup *may display an incorrect amount of disk space* [S20]. This is the single best argument for the design rule that DiskWise must **measure** the delta in free space with `GetDiskFreeSpaceExW` before and after an operation and report *that* number, with category estimates clearly labelled as estimates. A cleaner that cannot prove it freed space is indistinguishable from one that did nothing.

**P-04 — The component-store family.** Users are told two contradictory things: the folder is enormous, and it must not be touched [S10][S11]. Both are true. The correct product behaviour is to run `/AnalyzeComponentStore`, present *reclaimable* versus *referenced*, and treat `/ResetBase` as irreversible with a distinct confirmation class — never as a checkbox in a "clean everything" list.

**F-08/F-10 — The category-total family.** "I have 43.7 GB of temporary files but Windows offers to delete 101 MB" [S23] recurs across years and Windows versions [S24][S25]. The gap is explanatory, not technical: the category is a union of things with different recovery paths. DiskWise must split each category into *measured bytes*, *deletable bytes*, and *blocked bytes with reasons* — and never display a single number that implies the difference is recoverable.

**P-18 — The false-positive family.** The Affinity Designer → Ableton Live case [S30] is the sharpest available evidence that ownership is the hard problem, not deletion. A tool that deletes confidently and incorrectly is worse than a tool that deletes little. This drives the design decision in §9.5: **every leftover carries a confidence class and the evidence that produced it**, and only class-A items are eligible for bulk action.

**F-22/F-27 — The privilege family.** Users are routinely advised to take ownership of `WindowsApps` or `System32`, sometimes with catastrophic results ("I had to reinstall Windows") [S47][S41]. DiskWise must treat privilege escalation as a **last-resort, explicitly-labelled, non-default** action, and must never change ownership as a side effect of a cleanup operation. Where ownership change is genuinely required (e.g. a documented app-uninstall residue under a user profile), it must be scoped to the specific object and logged.

**P-23 — The launcher family.** "I uninstalled the game and got no space back" [S71][S72] is usually a launcher/manifest mismatch, not an orphan file hunt. Reading `appmanifest_*.acf` and Epic manifests gives an authoritative installed-set, which makes this family tractable without heuristics.

**F-28/F-29 — The analyser-accuracy family.** Hard-link inflation is not a corner case: `WinSxS` alone makes naive tools over-report by tens of gigabytes [S59][S89]. Any analyser DiskWise ships must deduplicate by file identity and offer allocated size, or it will lose the accuracy argument it is built on.

**P-31 — The automation family.** Storage Sense is documented to work on the system drive only and to leave Downloads and cloud content alone unless configured [S65], yet users report it deleting a Downloads folder [S66] and being useless on multi-drive systems [S64]. The product lesson: automation must be **scoped, previewable, and reversible**, with a visible record of every file it removed.

**P-33 — The developer/VM family.** The WSL thread is a perfect miniature of the whole product thesis: the guest reports 12 GB, the host file occupies 68 GB, and the fix is a documented compaction — not a deletion [S52][S53]. DiskWise should treat "reclaim without deleting" as a first-class operation alongside deletion.

## 3.7 Cross-cutting findings

1. **The trust gap is bigger than the feature gap.** Users do not primarily ask for more cleaning power; they ask for numbers that reconcile and actions that can be explained ([S01][S03][S23][S59]).
2. **Analysis, uninstallation and cleanup are one workflow in practice, three products in the market.** The research repeatedly shows users chaining analyser → uninstaller → Disk Cleanup → DISM → `diskpart` ([S17][S52][S59][S83]).
3. **"Leftovers" is an ownership problem, not a name-matching problem** ([S30][S31]).
4. **Automation without a preview damages trust disproportionately.** One accidental deletion outweighs hundreds of correct ones ([S64][S66]).
5. **Windows changes underneath utilities.** Storage Sense replaced guidance, WMIC was removed, 24H2 introduced checkpoint updates, Disk Cleanup's reporting was wrong for months ([S65][S79][S17][S20]). Architectural resilience — feature detection rather than version assumptions — is a product requirement.
6. **Deletion is often the wrong tool.** Compaction (WSL/Docker), deduplication (hard links), cap setting (VSS, pagefile) and supported servicing (`DISM`) reclaim space without destroying data ([S52][S53][S89][S10]).
7. **Registry cleaning remains a solution in search of a problem.** No source in this corpus demonstrates a performance benefit; several demonstrate risk ([S30][S95]).
8. **Bulk selection in Explorer is genuinely unsafe for large operations**, and users work around it with shell one-liners ([S73][S74][S75]).

## 3.8 Gap analysis: what the market does not do

| Capability | WizTree | WinDirStat | TreeSize Free | BCUninstaller | Revo | Storage Sense | **DiskWise target** |
|---|---|---|---|---|---|---|---|
| Fast NTFS scan | Yes (MFT) | Yes (2.x, improved) | Traversal | n/a | n/a | n/a | Yes, with documented-API fallback |
| Hard-link-aware totals | Yes | Recent versions | No (per vendor KB) | n/a | n/a | n/a | Yes, identity-based, always |
| OneDrive placeholder honesty | Partial | Partial | Allocated column | n/a | n/a | Yes (makes files online-only) | Yes, with recall-safety warnings |
| Leftover ownership evidence | No | No | No | Partial (heuristics) | Partial | No | Yes, scored + *Unverified* bucket |
| Safe delete pipeline with quarantine | Recycle-bin only | Recycle-bin only | Recycle-bin only | Partial | Yes (backup) | No undo | Yes, vault + manifest + verify |
| Preview of destructive automation | n/a | n/a | n/a | Partial | Partial | No | Yes, mandatory dry-run |
| Registry actions | No | No | No | Removes leftovers | Removes leftovers | No | Analysis + backup only |
| Reconciles with volume usage | Implied | No | No | n/a | n/a | No | Yes, explicit ledger |

The gap is not "one more analyser". It is the **joined-up, evidence-first pipeline** — and that is where the roadmap in §10 aims.

---


---

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


---

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


---

# 6. Competitor Comparison

## 6.1 Ground rules for this section

Three different things are frequently conflated, and are separated here:

1. **Learning from public functionality** — legitimate. Reading vendor documentation, help files, screenshots and community discussion to understand what a product does.
2. **Using documented interfaces** — legitimate when the licence or terms permit it (command-line flags, published SDKs, file formats).
3. **Reusing source code** — legitimate only where the licence permits combination with your product's distribution model.

No claim in this section is derived from inspecting a proprietary binary's internals. Marketing statements are tagged **[VENDOR CLAIM]** and are not treated as verified behaviour.

## 6.2 Comparison matrix

| Tool | Key strengths | Missing capabilities | Relevant user complaints | Integration feasibility | Licensing constraints |
|---|---|---|---|---|---|
| **WizTree** (Antibody Software) | Direct `$MFT` read: a whole NTFS volume in seconds [S84]; counts hard links once [S60]; treemap, largest files, duplicate finder, export; portable build | No application uninstall; no leftover ownership; no registry work; requires admin for the fast scan | "Free for personal use" is widely misunderstood — commercial use requires a supporter code [S85]; users report licence confusion and one-person-maintainer risk | **Process-level only.** Launch with a path, or accept an exported report. No SDK is published | **Proprietary.** EULA forbids modification, combination with other software, decompilation and reverse engineering [S85]. Source reuse: **not permitted** |
| **WinDirStat** (open source) | Long-standing, familiar UI; treemap; extension statistics; active again in 2.x; **GPL** | Traversal-based scan is much slower than MFT-based tools on large volumes [S84][S99]; hard links historically over-counted [S59]; no uninstall/leftover features | Freezes on some large scans; export crashes on large datasets [S99]; users moved to WizTree for speed [S84] | Source-level reuse *would* be technically feasible (C++/MFC) but see licence | **GPL-2.0** (README states the application is GPL v2 and that upgrading the licence version is not permitted) [S86][S87]. Linking into a closed-source product makes the whole work GPL. **Not compatible** with a proprietary distribution |
| **TreeSize Free / Professional** (JAM Software) | Enterprise-grade reporting; NTFS detail in paid editions; portable free edition | Free edition lacks duplicate search, scheduling, automation, network scanning and **hard-link/ADS/dedup evaluation** per vendor KB [S89] | Free version's totals differ from Explorer because NTFS features are not evaluated [S89]; users report the free edition is slower than MFT tools | Process-level; paid edition has CLI/scheduling | **Proprietary.** Free edition is free for personal and commercial individual use with documented limitations; Professional is paid |
| **Bulk Crap Uninstaller** | Excellent bulk uninstall; orphan detection; leftover cleanup; scripted uninstall support; **Apache-2.0** | .NET/WinForms desktop app; no disk map; no temp/system-junk cleanup depth; leftover cleaning is heuristic | Portable build is large (~364 MB unpacked, 1 337 files) [S93]; leftover lists can be long and require review | Process-level; command-line automation exists but should be verified per version [PENDING VERIFICATION] | **Apache-2.0** [S88] — redistribution of binaries is permitted with notices. **Source reuse is not practical for a C codebase** (C#/.NET); the licence is compatible, the technology is not |
| **Revo Uninstaller** | Pre-install tracing; registry + file leftovers; backup before removal; widely trusted | Paid tiers; heuristic matching can propose unrelated applications' files | **User report: uninstalling Affinity Designer proposed Ableton Live files** [S30]; colour-coded safety indicators exist but users still must review thousands of entries [S31] | None documented for automation | **Proprietary.** No reuse |
| **HiBit Uninstaller / Geek Uninstaller** | Simple, portable, fast; community favourites | Narrow scope; heuristic leftovers; no analysis depth | Compared in user tests with differing leftover counts per tool [S93][S96] | Process-level | **Proprietary.** No reuse |
| **Microsoft Storage Sense / Cleanup recommendations** | Built-in, supported, no install; documented behaviour; safe defaults | **System drive only** [S65]; no item-level preview; no undo; category totals can mislead [S23][S24]; 24H2 reporting defect [S20] | Deleted Downloads content users wanted [S66]; useless on multi-drive systems [S64]; "temporary files" size that cannot be reclaimed [S23] | OS-level. `cleanmgr` flags and Storage settings exist but are semi-documented and change between builds; treat as **fragile** | Microsoft OS component; no licensing issue, but behaviour is not a stable API contract |
| **Disk Cleanup (`cleanmgr.exe`)** | Ubiquitous; system-file categories; `/sageset`/`sagerun` automation | Dated UI; overlapping categories; known reporting defect [S20]; status ambiguous in community discussion [S91] | Users cannot reconcile category sizes with reality [S23][S25] | `cleanmgr /sagerun` is usable but its category IDs are undocumented and build-dependent | Microsoft OS component; same fragility caveat |
| **BleachBit** | Open source; broad application cleaners; Windows builds | Python runtime; Windows support is secondary to Linux; risky cleaner definitions if misconfigured | Not prominent in the Windows-focused threads reviewed | Process-level; `.ini` cleaner definitions are readable | **GPL-3.0** (verify exact version before any use). **Not compatible** with closed-source linking |
| **Everything (voidtools)** | MFT-based instant index [S90]; tiny footprint; excellent search | Search, not analysis; no treemap; no cleanup | n/a — frequently recommended *as* the tool to find leftovers [S02] | Process-level via documented IPC/export (verify current SDK terms) [PENDING VERIFICATION] | Freeware; source not public. Integration only through published interfaces |
| **PowerToys File Locksmith** | Finds which processes hold a file | Narrow; no deletion workflow | Recommended repeatedly for undeletable files [S37][S39] | Process-level | MIT (verify before bundling) [PENDING VERIFICATION] |

## 6.3 What the matrix actually says

1. **No single product spans the workflow.** The fastest analyser (WizTree) has no uninstaller. The best uninstaller (BCU) has no disk map. The safest built-in tool (Storage Sense) is confined to the system drive and has no preview. Integration is the open position.
2. **Accuracy is a differentiator that is easy to verify and hard to fake.** Hard-link inflation [S59][S89], placeholder accounting [S61] and category misreporting [S20] are the three most conspicuous accuracy failures in the market. A tool that reconciles its own totals wins arguments that competitors lose.
3. **The GPL boundary is real.** WinDirStat is the most commonly cited open-source analyser; its GPL-2.0 licence makes source-level reuse incompatible with a closed-source product [S86][S87]. Apache-2.0 (BCU) is licence-compatible but technologically distant [S88].
4. **The vendors' own knowledge bases concede gaps**: TreeSize Free documents that it does *not* evaluate hard links, ADS or dedup [S89]. That is not a criticism of the product — it is a precise statement of where an accurate newcomer can compete.
5. **Trust is the scarce resource.** Revo's false-positive report [S30] cost the tool credibility in a public thread. Any product making ownership claims must publish its evidence, per item.

## 6.4 Where DiskWise should compete — and where it should not

| Compete | Do not compete |
|---|---|
| Reconciliation and explanation (F-05, F-06) | Raw scan speed records — WizTree's MFT advantage is well established; match it, do not market around it |
| Ownership evidence for leftovers (F-12) | Breadth of application cleaner definitions — BleachBit/CCleaner cover thousands; risk without proof |
| Safety pipeline with quarantine + manifest (R3, F-27) | Registry optimisation — no evidence of benefit |
| Integrated workflow across analysis → uninstall → cleanup (R2–R4) | Enterprise reporting — TreeSize Professional's domain |
| Honest handling of cloud, VHDX and hard links (F-05, F-20, F-22) | Partitioning, secure erase, system cloning |

## 6.5 Integration patterns that are lawful and useful

| Pattern | Example | Cost | Verdict |
|---|---|---|---|
| **Hand-off** | "Open in WizTree / open containing folder / export CSV" via `ShellExecute` | Low | Recommended. Respects licences; no coupling |
| **Documented CLI bridge** | `winget list`/`uninstall`, `DISM /AnalyzeComponentStore`, `powercfg /hibernate`, `vssadmin`, `wsl --manage` | Low–Medium | Recommended, with strict argument construction (never string-concatenated user input) and output parsing that fails closed |
| **Published SDK/IPC** | Everything IPC, PowerToys interfaces | Medium | Optional, verify terms per version |
| **Binary bundling of third-party tools** | Shipping DISM alternatives or BCU inside the installer | Medium | Avoid unless necessary; adds licence notices, update burden and security surface |
| **Source linking** | Linking WinDirStat GPL code | — | **Prohibited** for a closed-source product |

---

# 7. WizTree and Open-Source Feasibility

## 7.1 WizTree: what may and may not be done

**Confirmed from the vendor's published EULA [S85]:**

- Free for **personal use only**; commercial use requires a supporter code per site; enterprise licensing for large organisations.
- The licensee is **not permitted** to edit, alter, modify, adapt, translate, combine or incorporate the software into other software, or to **decompile, disassemble or reverse engineer** it.

**Consequences for this project:**

| Question | Answer |
|---|---|
| May we read WizTree's documentation and learn from its feature set? | Yes. Features are public; §6.2 uses only public capability descriptions |
| May we benchmark it against our own product? | Only with a licence that covers your use, and only publishing your own measurements — never vendor marketing figures as if independently verified |
| May we inspect its binary, MFT parser, or memory? | **No.** Explicitly prohibited |
| May we bundle it? | Only under its redistribution terms; not advisable |
| May we use it as an external tool with a hand-off? | Only if the user has installed it and its licence permits their use; the product should not silently invoke it |

## 7.2 The open-source candidates

| Repository / product | Language | Licence (as published) | Maintenance | Build | Windows support | Scanning architecture | Compatibility with a C codebase |
|---|---|---|---|---|---|---|---|
| **windirstat/windirstat** [S86][S87] | C++/MFC | **GPL-2.0**, "parts under more lenient terms" but the application is GPL and the version may not be upgraded | Active (2.x line, recent releases) | Visual Studio solution | Windows 7+ (2.x) | Directory traversal; treemap UI | Source-level reuse **would taint** a closed-source product. Ideas and algorithms are public; code is not usable |
| **BCUninstaller/Bulk-Crap-Uninstaller** [S88] | C#/.NET | **Apache-2.0** | Active (16k+ stars, frequent commits) | .NET SDK | Windows 7+ | Registry + MSI enumeration, uninstaller execution | Licence compatible, **technology incompatible** (managed code, WinForms). Only process-level integration |
| **Drawer/TreeSizr** | (per repository description: Windows analyser with MFT fast scan) | **[PENDING VERIFICATION]** — licence must be read from the repository before any use | New project | Unknown | Windows | MFT + fallback; hard-link-aware; does not follow junctions | Potentially interesting as a *reference design* only; licence unverified, so no code may be copied |
| **czkawka** (duplicate finder) | Rust | **[PENDING VERIFICATION]** | Active | Cargo | Windows/Linux | Hash/perceptual pipelines | Process-level; useful as a *behavioural* reference for duplicate UX |
| **WSLCompact / `wsl --manage --set-sparse`** | Scripts / OS feature | Scripts: **[PENDING VERIFICATION]**; `wsl.exe` flags are documented by Microsoft | n/a | n/a | Windows 11 | VHDX compaction | Use the **documented `wsl.exe` flags** rather than third-party scripts |
| **Everything SDK** | C | Freeware SDK | Active | — | Windows | MFT index | IPC integration only; verify SDK terms |

> **Licence discipline:** no source file from any project above may be incorporated until its licence text has been read and its obligations are compatible with your distribution model. Where the licence is marked *[PENDING VERIFICATION]*, this report deliberately makes **no claim** about reuse rights.

## 7.3 Recommended approach: independent implementation on documented APIs

**Recommendation: build the scanning engine independently against documented Windows interfaces.** Rationale: (a) GPL proximity risk is eliminated; (b) the C codebase stays homogeneous; (c) the parts that matter for accuracy — hard-link identity, allocation size, placeholder handling — are straightforward once the file identity model is right, and that model is already implemented and tested in this engagement's reference core (§12.2).

### 7.3.1 Enumeration strategies, in preference order

1. **USN/`$MFT` enumeration via documented control codes** — `FSCTL_ENUM_USN_DATA` over an opened volume handle yields file reference numbers, parent references, names and attributes for every record: one sequential pass, no per-file path construction, no ACL checks per item. Requires **administrator** (volume handle with `GENERIC_READ`) and works on NTFS only. This is the correct basis for the "fast scan" and it is a documented interface, not reverse engineering.
2. **`GetFileInformationByHandleEx(…, FileIdBothDirectoryInfo)`** — directory-by-directory enumeration returning 64-bit file IDs alongside names. Works without volume-handle privileges, substantially faster than `FindFirstFile` on directories with many entries, and is the correct basis for the fallback scanner.
3. **`FindFirstFileExW` with `FindExInfoBasic`** — universal fallback for FAT32/exFAT/network paths where file IDs are unavailable or unstable.

### 7.3.2 Accuracy rules the engine must implement

| Rule | API / mechanism |
|---|---|
| Count each file identity once | `FILE_ID_INFO` (volume serial + 128-bit ID) in a hash set |
| Report allocated size, not logical length | `FILE_STANDARD_INFO.AllocationSize`; `GetCompressedFileSizeW` for compressed files |
| Detect hard links | `FILE_STANDARD_INFO.NumberOfLinks > 1` |
| Detect sparse/compressed | `FILE_ATTRIBUTE_SPARSE_FILE`, `FILE_ATTRIBUTE_COMPRESSED`; `FILE_STANDARD_INFO` vs logical size |
| Detect cloud placeholders | `FILE_ATTRIBUTE_RECALL_ON_OPEN` (0x00040000) and `FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS` (0x00400000): allocated = 0, logical = size |
| Never follow reparse points by default | `FILE_ATTRIBUTE_REPARSE_POINT` + `FSCTL_GET_REPARSE_POINT` to classify link vs junction/mount point |
| Name DOS 8.3 aliases correctly | `FILE_ID_BOTH_DIR_INFO.ShortName` — do not double count `PROGRA~1` |
| Account for what enumeration cannot see | `$MFT`, `$LogFile`, `System Volume Information`, cluster slack, NTFS metadata — reported as named buckets in the reconciliation ledger |

### 7.3.3 Treemap rendering

The **squarified treemap** algorithm (Bruls, Huizing, van Wijk, 2000) is published academic work; implementing it from the paper is lawful and requires no third-party code. Practical notes: compute layout on a worker thread for the visible nodes only; cache rectangles per zoom level; render with Direct2D or a memory DC; target ≤ 2 ms per relayout for 5 000 visible rectangles; use cushion shading only if it measures faster than flat fill on the target hardware.

### 7.3.4 Fallback and degradation

- If the volume is not NTFS, or the process is not elevated, or `FSCTL_ENUM_USN_DATA` fails with `ERROR_ACCESS_DENIED`, the engine **transparently falls back** to (2) then (3), and the UI states which backend produced the result and what it may under-report.
- If a directory is inaccessible, record it as an *unreadable* node with the error code rather than silently omitting it. This single behaviour converts a mysterious total into an explainable one.

### 7.3.5 Security-software coexistence

Raw `$MFT`/USN access plus a large number of file operations can resemble ransomware behaviour to EDR. Mitigations: code-sign the binary; request elevation explicitly and explain why; never enumerate beyond the selected scope; keep the scan read-only; declare no-network capability in the manifest; document the behaviour so enterprise administrators can allow-list deliberately. If a security product blocks the fast path, fall back silently and tell the user — never fight the AV.

---


---

# 8. UI/UX Redesign Blueprint

## 8.1 Identity: name, mark and brand caveat

**Provisional name: DiskWise.** It communicates the two pillars (disk intelligence, informed action). **Trademark verification is required before release:** the stem "Wise" is used by an established Windows cleaning vendor, and a similarly named product in the same category creates both confusion risk and potential opposition. Acceptable alternatives to evaluate with counsel: *DiskLens*, *Volumetric*, *Reclaim*, *Lumen Disk*. The mark and asset set in `assets/icons/` are name-neutral and survive a rename.

**Icon concept (delivered).** A 270° gradient arc (the platter, with a deliberate gap where space is being reclaimed), three ascending bars inside (analysis/data), and a warm amber shard floating out through the gap (what is being recovered). Colour system: deep navy tile `#1B2E4C → #080E18`, teal–cyan arc `#1FA8E0 → #2DE2C5`, amber accent `#FFC24D → #FF8A1F`. The amber is used **only** for "reclaim" moments in the UI, which keeps the mark semantically consistent with the product.

Variants shipped: `diskwise_master.svg` (app tile), `diskwise_dark.svg` (transparent, dark themes), `diskwise_light.svg` (light theme tile), `diskwise_small.svg` (optical variant for 16–24 px: thicker ring, no bars, larger shard), `diskwise_mono.svg` (single-colour, `currentColor`). The ICO contains 16/20/24/32/40/48/64/96/128/256 px; the 16–24 px frames come from the optical variant.

## 8.2 Design tokens

| Token | Dark | Light |
|---|---|---|
| `bg.base` | `#0D131F` | `#F3F6FA` |
| `bg.surface` | `#162032` | `#FFFFFF` |
| `bg.surface-2` | `#1D2A3E` | `#EAF0F6` |
| `border.subtle` | `rgba(255,255,255,0.08)` | `rgba(11,20,36,0.10)` |
| `text.primary` | `#E8F1FF` | `#0B1424` |
| `text.secondary` | `#9FB3C8` | `#52657A` |
| `accent` | `#2DE2C5` | `#0FA98F` |
| `accent.alt` | `#1FA8E0` | `#0E7FB8` |
| `warn` | `#FFB020` | `#B87300` |
| `danger` | `#FF5C5C` | `#C62828` |
| `ok` | `#3DDC97` | `#12805C` |
| `risk.safe` | `#3DDC97` | `#12805C` |
| `risk.review` | `#FFB020` | `#B87300` |
| `risk.high` | `#FF7A45` | `#C2410C` |
| `risk.protected` | `#9AA7B8` | `#5A6B7D` |

Protected is deliberately **grey, not red**: it is a lock, not a warning. Danger red is reserved for irreversible actions.

Spacing scale 4/8/12/16/24/32; radii 6 (controls), 10 (cards), 14 (modals). Type: Segoe UI Variable Text/Display — 12 (meta), 13 (grid body), 14 (body), 16 (section), 20 (page title), 28 (dashboard KPI). Row height 24/28/32 at 100/125/150% DPI. All text colours meet ≥ 4.5:1 against their surface in both themes.

## 8.3 Application shell

```
┌────────────────────────────────────────────────────────────────────────────────────┐
│  [icon] DiskWise      C: ▾  D: ▾        ⌕ Search everything…          —  □  ✕      │
├───────────────┬────────────────────────────────────────────────────────────────────┤
│ ⌂ Dashboard   │  Disk Map                                                    ⓘ ?   │
│ ▤ Cleanup     │  ┌──────────────┬────────────────────────────────────────────┐     │
│ ⧉ Old Files   │  │ Tree         │  Treemap                                   │     │
│ ⚑ Leftovers   │  │ ▾ Users  42G │  ┌────────────┬──────────┬───────────────┐ │     │
│ ▦ Apps        │  │  ▾ a     30G │  │            │          │               │ │     │
│ ⛁ Disk Map    │  │   Downloads  │  │   Windows  │  Users   │  ProgramData  │ │     │
│ ⚙ Maintenance │  │ ▸ Windows 31G│  │            ├──────────┴───────────────┤ │     │
│ ⚙ Settings    │  │ ▸ ProgData 8G│  │            │      Program Files       │ │     │
│               │  └──────────────┴──┴────────────┴──────────────────────────┘     │
│ ── this PC ── │  Largest files · 412 918 items · 512 GB                         │
│ C: 78% used   │  ☑ Name            Size    Type     Modified      Risk   Action  │
│ D: 41% used   │  ☑ pagefile.sys    25.2G   system   2026-10-30   High    Keep    │
├───────────────┴────────────────────────────────────────────────────────────────────┤
│ Scanning D:\Users · 412 918 items · 18 402/s · 00:22  [Pause] [Cancel]     62% ▓▓░ │
└────────────────────────────────────────────────────────────────────────────────────┘
```

**Rules.** Sidebar is fixed-width 232 px, collapses to a 56 px icon rail below 1100 px. The drive selector is global but each module **shows the scope it will actually use** and never widens it silently (§4.9). The status bar is the only place where long-running job state appears, so progress never blocks the content area.

## 8.4 Dashboard

```
┌ Used 398 GB / 512 GB ──────────────────────────────  [Rescan] [Make room for update]┐
│ ████████████████████████████████████░░░░░░░░░  Used  Used by system files ▓▓ 62 GB  │
├──────────────────┬──────────────────┬──────────────────┬────────────────────────────┤
│ Reclaimable      │ Safe to remove   │ Review required  │ Protected                  │
│ 91.4 GB          │ 38.2 GB          │ 46.6 GB          │ 6.6 GB                     │
│ ▓▓▓▓▓▓▓▓▓░░░     │ ▓▓▓░░░           │ ▓▓▓▓░░░          │ ▓░                         │
├──────────────────┴──────────────────┴──────────────────┴────────────────────────────┤
│ Top opportunities                                                    [Open Disk Map]│
│ ● Windows Update cleanup        12.4 GB  ● Safe        Windows Update cache       ▸  │
│ ● Windows.old                   21.0 GB  ● Review      Previous installation       ▸  │
│ ● WSL virtual disk              47.0 GB  ● Review      12 GB used, 47 GB allocated ▸  │
│ ● Delivery Optimization cache    9.8 GB  ● Safe        Update peer cache           ▸  │
│ ● Downloads older than 90 days  6.1 GB  ● Review      412 files                    ▸  │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ Storage changed since last scan:  +2.1 GB (WSL vdisk) · −3.4 GB (Temp)   [History]  │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

KPI cards are **measured**, not estimated: each shows the basis in a tooltip ("computed from 412 918 files, allocated size, hard links counted once; 6.6 GB in NTFS metadata and shadow storage is not attributable to a folder"). The "Top opportunities" list is the primary entry point into specialist modules, and every row carries a risk chip and a one-line reason.

## 8.5 Unified table (Old Files / Leftovers / Apps share one control)

```
┌ Search: [setup                                      ]   Within results: [old        ] │
│ Chips: [size > 100 MB ×] [age > 12 months ×] [C: ×]            [Clear filters]        │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ ☑ 34 selected · 12.8 GB   | 3 hidden by filter ⚠ Review  |  [Recycle] [Quarantine] [⋯] │
├──┬────────────────┬────────┬───────────────┬────────┬─────┬──────┬────────┬──────────┤
│☑ │ Name       ▲▾  │ Type   │ Path          │ Size ▾ │ Age │ Depth│ Risk   │ Action   │
├──┼────────────────┼────────┼───────────────┼────────┼─────┼──────┼────────┼──────────┤
│☑ │ old-setup.exe  │ file   │ C:\Users\a…   │ 1.2 GB │ 26  │ 5    │ Safe   │ Recycle  │
│☐ │ footage.mp4    │ file   │ D:\Media\…    │ 4.8 GB │ 19  │ 3    │ Review │ Review ▸ │
│☑ │ Steam/common…  │ folder │ E:\Steam\…    │ 6.1 GB │ 14  │ 4    │ High   │ Blocked  │
│  │                │        │               │        │     │      │        │          │
│  │ 3 items are selected but hidden by the current filter.                              │
│  │ [Show hidden selections]  [Exclude them from this action] (default)                 │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

Mandatory behaviours: the hidden-selection banner is **never dismissible while hidden selections exist**; the destructive buttons label the count that will actually be affected; the header checkbox tri-state reflects *visible* rows; `Action` column states what will happen rather than what *could*.

## 8.6 Deletion confirmation dialog

```
┌ Delete 34 items? ──────────────────────────────────────────────────────── [✕] ┐
│                                                                              │
│  34 items · 12.8 GB on disk                    Measured free space now: 61.4 GB │
│  31 visible · 3 hidden by filter → excluded by default                          │
│                                                                              │
│  Reversibility   Quarantine vault (restore for 30 days)          [Change ▸]     │
│  Privilege       Standard user — no elevation required                          │
│  Highest risk    High: E:\Steam\steamapps\common\… (active game library)        │
│                                                                              │
│  ⚠ Risk detail                                                                  │
│    • 2 items are inside an application's active install tree (Steam).            │
│      Deleting them may cause the game to fail to launch or verify files.         │
│    • 1 item is a cloud placeholder; deletion will propagate to OneDrive.         │
│    • 3 items have no usage evidence either way — absence of evidence is not      │
│      proof that a file is unused.                                                │
│                                                                              │
│  ☐ I have reviewed the High Risk items individually                            │
│  Will not touch: C:\Windows, Program Files\WindowsApps, any excluded path       │
│                                                                              │
│                    [Cancel]   [Review 3 hidden]   [Delete 31 items]             │
└──────────────────────────────────────────────────────────────────────────────┘
```

The primary button **names the exact count**. Cancelling is the default focus. "Review 3 hidden" switches context instead of silently including them.

## 8.7 Pre-execution review modal (Requirement 9)

```
┌ Review operation — Clean up Windows Update cache ──────────────────── [✕] ┐
│ What it does   Stops the update services, clears the download cache, then  │
│                restarts them. Windows re-downloads updates on demand.     │
│ Estimated      9.8 GB (category estimate) · will be measured after run    │
│ Duration       ~2–4 min                                                    │
│ Privilege      Administrator (services + protected folder)   [Elevate]     │
│ Reversible     No undo of cleared cache. Updates re-download automatically.│
│ Restart        Not required. A pending update may restart automatically.   │
│ Commands       sc stop wuauserv · sc stop bits · <contents of Download>    │
│                del · sc start bits · sc start wuauserv           [Copy]    │
│ Excluded       Update history, installed updates, Windows.old, DO cache    │
│ Risks          Update history may appear reset. No installed update is     │
│                removed. Do not run during an active download.              │
│ ☐ I understand the update engine will rebuild this cache                    │
│                                  [Cancel]   [Run and measure]               │
└───────────────────────────────────────────────────────────────────────────┘
```

Irreversible operations replace the checkbox with a typed confirmation whose token is derived from the operation (e.g. `RESETBASE`) and are visually distinct (danger border, no accent colour).

## 8.8 Disk Map (Disk Intelligence)

Layout: 30/70 split, resizable, persisted per user. Left: virtualised tree with inclusive/exclusive size toggle. Right top: treemap with breadcrumb. Right bottom: tabbed panel (Largest files · File types · Unreadable locations · Metadata buckets). Syntax-highlighted search filters the current result set only.

Interaction contract: click selects a node in both views; double-click zooms; `Alt+↑` zooms out; hover shows a 200 ms-delayed tooltip with full path, allocated size, logical size, link count and owner; right-click opens the R6 menu; `Enter` opens the node in Explorer. Scanning is read-only and the module never offers a delete verb that bypasses §8.6.

Status line always states the size model in force: `Allocated size · hard links counted once · 6.6 GB metadata and shadow storage not attributable`.

## 8.9 Settings

```
┌ Settings ────────────────────────────────────────────────────────────────┐
│ ⌕ Search settings…                                                        │
├────────────────────┬──────────────────────────────────────────────────────┤
│ General            │  Cleanup rules                                       │
│ Appearance         │  ────────────────────────────────────────────────    │
│ Scan preferences   │  Minimum file age before it can be recommended       │
│ Drive preferences  │   ( 12 ) months   ⓘ Below 7 days is blocked         │
│ Cleanup rules   ▸  │  Default removal method                              │
│ Uninstallation     │   (•) Quarantine vault   ( ) Recycle Bin   ( ) Permanent │
│ Registry safety    │  Quarantine retention      ( 30 ) days               │
│ Exclusions         │  ── Advanced risk settings ──────────────────────    │
│ Backups            │  ⚠ Allow cleanup inside application install trees    │
│ Performance        │     ( ) off   ( ) on — requires typed confirmation    │
│ Notifications      │  [Reset category]              [Reset all settings]  │
│ Privacy            │                                                      │
│ Advanced           │                                                      │
│ About              │                                                      │
└────────────────────┴──────────────────────────────────────────────────────┘
```

Dangerous settings live in a bordered "Advanced risk settings" block, are off by default, and require a typed confirmation the first time a given risk control is enabled. Every control has an adjacent explanation, not a hover-only tooltip.

## 8.10 Apps module (Requirement 4 surface)

```
┌ Apps ───────────────────────────────────────────────────────────────────────────────┐
│ ⌕ [vlc                    ]   Within results: [old        ]   Scope: [C:][D:] ▾     │
│ 247 applications · 41.2 GB · 6 with broken uninstallers · 3 installed on D:          │
├──┬───────────────────────┬───────────┬──────────────┬──────────┬─────────┬──────────┤
│☑ │ Name                  │ Publisher │ Installed    │ Size     │ Source  │ State    │
├──┼───────────────────────┼───────────┼──────────────┼──────────┼─────────┼──────────┤
│☑ │ VLC media player      │ VideoLAN  │ 2024-11-02   │ 318 MB   │ MSI     │ Healthy  │
│☐ │ Old Toolbox 3.1       │ Acme      │ 2019-03-14   │ 1.4 GB   │ missing │ Broken   │
│☐ │ Steam Game (E:)       │ Valve     │ 2025-06-01   │ 48.0 GB  │ Steam   │ Healthy  │
├──┴───────────────────────┴───────────┴──────────────┴──────────┴─────────┴──────────┤
│ Detail · Old Toolbox 3.1                                                            │
│  Uninstall command   C:\Program Files\Acme\OldToolbox\unins000.exe   ! missing       │
│  Install location    C:\Program Files\Acme\OldToolbox  (2.1 GB)                     │
│  Metadata on C:      4 registry keys, 1 startup value, 1 scheduled task             │
│  Evidence            MSI product code · publisher match · path match                │
│  Recommended         Repair the entry, then uninstall normally                       │
│                                                    [Repair entry]  [Uninstall]       │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

The Apps module distinguishes **install location** (any volume) from **metadata location** (system volume) in the detail pane, because "I installed it on D:" does not mean its registry state lives there (§4.9.2). The `State` column surfaces broken entries *before* the user attempts an uninstall, and `Uninstall` never deletes anything directly — it starts the R4 pipeline and ends in the leftover review below.

## 8.11 Leftover review and Clean Run preview

```
┌ Leftovers from "Old Toolbox 3.1" ───────────────────────────────────────────────────┐
│ 42 candidates · 2.3 GB · 4 preserved · 9 unverified                     [Scan again] │
│ Confidence:  A (27) · B (11) · C unverified (9 — never bulk-removable)              │
├────┬──────────────────────────────┬────────┬────────┬───────────────────────────────┤
│ ☑  │ Path                         │ Size   │ Conf.  │ Evidence                      │
├────┼──────────────────────────────┼────────┼────────┼───────────────────────────────┤
│ ☑  │ C:\ProgramData\Acme\Toolbox  │ 1.9 GB │ A      │ MSI component + install path  │
│ ☐  │ HKCU\...\Acme\Toolbox        │ 41 KB  │ A      │ product key, exported         │
│ ☐  │ C:\Users\a\AppData\...\acme* │ 220 MB │ B      │ name token only — review      │
│ ☐  │ C:\Program Files\Common...   │ 8 MB   │ C      │ shared folder — unverified    │
├────┴──────────────────────────────┴────────┴────────┴───────────────────────────────┤
│ Preserved: shared runtime VC++ 2015-2022 · 2 COM registrations used by other apps   │
│ [Export report]   [Quarantine 27 class-A items]   [Review individually]             │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

**Clean Run preview** reuses the same pattern with category rows instead of application rows: each category expands into items, each item carries a risk class and its evidence, and the footer states the measured baseline — *"Free space now 61.4 GB · estimated reclaim 12.8 GB · will be verified after the run."* Nothing is deleted until the review modal (§8.7) is confirmed, and unverified items are never included in a bulk selection.

## 8.12 Context menu structure

```
Open                        ── primary verbs ──
Open with…
Show in File Explorer
────────────────────────────
Analyse size in Disk Map
Check if in use…
────────────────────────────
Copy ▸            Path · Name · Details (TSV)
Advanced ▸        Find duplicates here · Find similar files · Inspect permissions ·
                  Inspect registry association · View associated application ·
                  Find other copies · Storage impact · Open elevated prompt here
────────────────────────────
Quarantine…      ── destructive, separated ──
Move to Recycle Bin…
Delete permanently…      (typed confirmation; hidden unless enabled in settings)
────────────────────────────
Add to exclusions…
Properties
```

Destructive entries never appear without a separator above them, never appear for Protected items, and always route through §8.6.

## 8.13 Keyboard model

| Key | Action |
|---|---|
| `Ctrl+F` / `Ctrl+Shift+F` | Focus primary search / search within results |
| `Ctrl+A` / `Ctrl+Shift+A` / `Ctrl+I` | Select visible / deselect all / invert visible |
| `Space` | Toggle focused row |
| `Enter` | Open / activate focused item |
| `Del` | Open deletion dialog (never delete directly) |
| `Shift+Del` | Open permanent-deletion dialog **only** if enabled in settings |
| `F2` | Rename? — *not offered*; this product does not rename user files |
| `F5` / `Esc` | Rescan / cancel current job |
| `Ctrl+E` | Export current view |
| `Shift+F10` | Context menu |
| `Alt+←/→` | Navigate history |
| `Ctrl+,` | Settings |

Full keyboard walkthrough is an acceptance test (no mouse-only path anywhere).

## 8.14 Component inventory to build or standardise

Risk chip · Evidence chip (confidence A/B/C) · Selection toolbar · Hidden-selection banner · Dual search field · Filter chip bar · Virtualised grid · Detail pane · Treemap canvas · Tree view · Progress job strip · KPI card · Review modal · Typed-confirmation field · Quarantine restore browser · Audit log viewer · Empty/loading/error state blocks.

Each component ships with: a keyboard contract, a screen-reader name, a high-contrast behaviour, and a DPI test at 150% and 200%.

---


---

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


---

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


---

# 12. Implementation Results or Pending Work

## 12.1 Delivered: icon system (Requirement 1)

**Built and verified in this environment.** Command: `python3 tools/build_icons.py` (cairo SVG rasterisation → Pillow compositing → ImageMagick ICO assembly). Output:

| Artefact | Detail |
|---|---|
| Vector masters | `assets/icons/svg/diskwise_master.svg`, `diskwise_dark.svg`, `diskwise_light.svg`, `diskwise_small.svg`, `diskwise_mono.svg` |
| PNG exports | 34 files: 7 sizes × 3 themes (256/128/96/64/48/40/32), 3 sizes × small-size variant (24/20/16), plus 512 and 1024 masters and two preview sheets |
| Windows ICO | `assets/icons/diskwise.ico`, 171 550 bytes, **10 embedded resolutions**: 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 — verified as RGBA and individually loadable |
| Size-ramp verification | Rendered at 256/64/48/32/24/20/16 px on both light and dark surfaces; 16–24 px use the optical variant (thicker ring, no inner bars, larger shard) and remain legible |

**What this does not prove.** The icon has **not** been embedded in an executable, has not been tested in the Windows taskbar, Start menu, Alt-Tab, or at 125/150/175/200% shell scaling, because no Windows host was available. Remaining integration work: add `IDI_APP_ICON` to the resource script, load with `LoadIconWithScaleDown`/`LoadIconMetric`, set an explicit `AppUserModelID` so the taskbar uses the product icon, and re-test on a real desktop.

## 12.2 Delivered: selection + deletion-gate reference core (Requirements 2 and 3, core logic)

**Built, compiled and tested in this environment.**

| File | Purpose |
|---|---|
| `prototype/selection_model.h` | Public API: `dw_key`, `dw_rec`, `dw_sel`, `dw_sel_stats`, `dw_policy`, `dw_verdict`, `dw_reason` |
| `prototype/selection_model.c` | Identity-keyed open-addressing selection store (tombstones, 70% load factor, mix64 hashing); view application with visible/hidden accounting; bulk verbs; rescan reconciliation; deletion gate |
| `prototype/tests.c` | 39 behavioural checks across 5 groups |
| `prototype/Makefile` | `make test` builds with `-std=c11 -O2 -Wall -Wextra -Werror -pedantic` **plus AddressSanitizer and UBSan** |

```
$ cd prototype && make clean && make test
cc -std=c11 -O2 -Wall -Wextra -Werror -pedantic -fsanitize=address,undefined ...
[selection] persistence across filters      10/10 PASS
[selection] bulk operations                  7/7  PASS
[selection] reconciliation after rescan      5/5  PASS
[gate] pre-deletion safety verdicts         12/12 PASS
[scale] 200k selections                      5/5  PASS

39 passed, 0 failed          (no ASan/UBSan diagnostics)
```

**What the tests actually demonstrate:**

1. Selections survive filter changes and are accounted as *visible* and *hidden* with byte totals — the case where a naive implementation silently widens destructive scope.
2. Invert and deselect operate only on visible rows; records outside the view are untouched.
3. "Select all matching" evaluates the full result set, not the rendered page.
4. After a rescan that removes one selected file and resizes another, the vanished item is **reported as stale, not dropped**, and the byte total is refreshed.
5. The gate returns `BLOCK` for identity mismatch, protected prefixes, reparse points, system attributes, missing files, null input and risk above policy; `WARN` for locked, hard-linked, cloud placeholder and recently-used files; `ALLOW` for ordinary disposable files.
6. 200 000 insertions/removals remain exact in count and aggregate size, with membership correct after mass deletion.

**What it does not demonstrate.** It is not Windows code: it does not call `GetFileInformationByHandleEx`, Restart Manager, or `IFileOperation`; it has no UI; and the policy weights are illustrative. It proves the *algorithm and accounting semantics*, which is precisely the part that is easy to get wrong and dangerous when wrong. Porting to Win32 is a mechanical step: populate `dw_rec` from `FILE_ID_INFO`/`FILE_STANDARD_INFO`/`GetFileAttributesW`, feed `dw_policy` from the protected list, and map `dw_verdict` to the confirmation class.

## 12.3 Not performed: changes to your application

**No changes were made to your existing application, because no source code was available.** Nothing was compiled, tested, benchmarked or profiled against your codebase, and this report does not claim otherwise.

To begin implementation, the following are required:

| # | Input | Why it is blocking |
|---|---|---|
| 1 | Repository URL or source archive (with build instructions) | Everything in §9.2 is unanswerable without it |
| 2 | Build system and compiler/CRT settings | Determines library and C11 feature availability |
| 3 | Target Windows versions and architectures | Determines API floors and test matrix |
| 4 | UI toolkit and framework version | Determines whether the virtualised grid is an extension or an addition |
| 5 | Screenshots of Dashboard, Old Files, Leftovers, Apps, settings, context menus | Required for the R5 polish audit and for UI/UX verification against reality |
| 6 | Current feature list or module descriptions | Prevents building duplicates of existing functionality |
| 7 | How deletion is currently performed, and whether the app runs elevated | The two highest-risk unknowns (R3, §9.7) |
| 8 | Settings storage location/format | Migration path for R14 |
| 9 | A disposable test VM (Windows 10 22H2 + Windows 11 24H2) | Destructive testing must never touch a working machine |
| 10 | Code-signing certificate availability | Required for the EDR-coexistence strategy in §7.3.5 |

## 12.4 Recommended first ten working days (once access exists)

| Day | Work | Output |
|---|---|---|
| 1 | Read the source: modules, toolkit, threading, deletion path, settings | Completed §9.2 questionnaire + feature-to-file map |
| 2 | Build the current application unmodified in a VM; record baseline behaviour | Reproducible build notes; known-issues list |
| 3 | Instrument: count direct delete call sites, registry writes, UI-thread I/O | Risk register entry per call site |
| 4–5 | Port `prototype/` to Win32 behind the existing UI; replace index-based selection only if present | Selection module + tests running on Windows |
| 6–7 | Insert the policy gate in front of every delete path; default to quarantine | All destructive paths pass the gate; no bypass remains |
| 8 | Add identity re-validation at execution time; add audit log | DE-04/DE-05-equivalent tests pass on Windows |
| 9 | Add the hidden-selection banner and counted destructive buttons to the existing views | R2/R3 acceptance subset |
| 10 | Measure before/after scan performance and selection latency; write the baseline document | Performance baseline to compare all later work against |

## 12.5 Statement of accuracy for this report

- **Research claims** trace to Appendix A; each source is real and was returned by the searches described in §2.4. Where a source is vendor-authored or low-authority, the tier column says so.
- **Design claims** are labelled `[PROPOSED]`. They have not been user-tested.
- **Build and test claims** refer only to the artefacts in this workspace and can be reproduced with the commands shown.
- **No benchmark or profiling number appears anywhere in this report**, because none could be measured here. §9.9 lists targets and the protocol to validate them.
- **X/Twitter evidence is absent** because the platform was unreachable; that gap is declared rather than filled.

---


---

# Appendix A — Source Index

Every claim in this report of the form `[Sxx]` resolves here. Tiers: **T1** first-hand user report · **T2** official documentation or vendor licence/KB · **T3** technical press with named reporting · **T4** vendor/SEO/tutorial content used only as corroboration.

**Notes on accuracy.** Reddit and forum content was retrieved through a search layer, so thread text is excerpt-level and exact timestamps are frequently unavailable; the "Date" column gives the best available indication (article date where published, otherwise the period inferred from content, marked with `≈`). Tier T1 sources establish that a symptom was experienced — not that the reporter's diagnosis was correct. No engagement metrics (upvotes, views) are quoted anywhere, because they could not be verified.

| ID | Source | URL | Tier | Date / notes |
|---|---|---|---|---|
| S01 | r/Windows11 — Windows 11 storage keeps disappearing | https://www.reddit.com/r/Windows11/comments/1quu3tx/windows_11_storage_keeps_disappearing_after/ | T1 | ≈2026; includes 60 GB Delivery Optimization claim and hibernation advice |
| S02 | r/pcmasterrace — Files disappearing on drives, Windows 11 | https://www.reddit.com/r/pcmasterrace/comments/1bgz0xp/files_disappearing_on_drives_windows_11/ | T1 | Mar 2024; Storage Sense blamed; files later found in a hidden folder |
| S03 | r/WindowsHelp — Gigabytes of free space mysteriously missing | https://www.reddit.com/r/WindowsHelp/comments/1hqeuea/gigabytes_of_free_space_mysteriously_missing_on/ | T1 | Dec 2024; Windows 7→10/11 volume discrepancy; third-party tool disagrees with OS |
| S04 | r/WindowsHelp — Moved to Windows 11, won't tell me what's taking space | https://www.reddit.com/r/WindowsHelp/comments/rwaazk/moved_to_windows_11_and_it_wont_tell_me_whats/ | T1 | Jan 2022; "200 GB of unknown stuff" |
| S05 | r/Windows11 — Can I delete hiberfil.sys? | https://www.reddit.com/r/Windows11/comments/1m0nc7e/can_i_delete_hiberfilsys_it_is_taking_a_lot_of/ | T1 | Jul 2025 |
| S06 | r/techsupport — hiberfil.sys 31.9 GB, cannot delete, not shown in Explorer | https://www.reddit.com/r/techsupport/comments/1hzjd33/hiberfilsys_uses_319_gb_of_storage_cant_delete_it/ | T1 | Jan 2025 |
| S07 | r/techsupport — pagefile.sys 25.2 GB and hiberfil.sys 6.1 GB | https://www.reddit.com/r/techsupport/comments/1lkziir/pagefilesys_is_252_gb_and_hiberfilsys_is_61_gb/ | T1 | Jun 2025 |
| S08 | r/techsupport — Why is pagefile.sys so large and how to reduce it | https://www.reddit.com/r/techsupport/comments/1umh1b0/why_is_pagefilesys_so_large_on_my_system_and_how/ | T1 | Jul 2026; conflicting advice |
| S09 | r/Windows10 — Can/should I disable page file? 64 GB RAM | https://www.reddit.com/r/Windows10/comments/mk6ws7/canshould_i_disable_page_file_i_have_64gb_ram/ | T1 | Apr 2021; conflicting advice |
| S10 | Microsoft Learn — Clean up the WinSxS folder | https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/clean-up-the-winsxs-folder?view=windows-11 | T2 | Current; "don't delete the folder" |
| S11 | Microsoft Q&A — Why is WinSxS 30 GB? | https://learn.microsoft.com/en-us/answers/questions/4102109/why-is-winsxs-folder-30gb-too-large-come-on | T1/T2 | ≈2023–2024 |
| S12 | Microsoft Q&A — WinSxS growing too large (Server 2016) | https://learn.microsoft.com/en-us/answers/questions/4162252/winsxs-folder-issue-growing-too-large | T1 | 2023 |
| S13 | Microsoft Q&A — Remove Windows.old after upgrade; TrustedInstaller lock | https://learn.microsoft.com/en-us/answers/questions/3888242/how-do-i-remove-the-windows-10-directory-after-upg | T1/T2 | Mar 2025 |
| S14 | Microsoft Q&A — Removing Windows.old | https://learn.microsoft.com/en-us/answers/questions/5863540/removing-windows-old | T1/T2 | ≈2026 |
| S15 | Tech-Hub — Safely clean the SoftwareDistribution folder | https://techhub.com.lk/kb/article/cleanup-softwaredistribution-folder | T4 | 2026; corroborates stop-services procedure |
| S16 | iTechGuides — SoftwareDistribution FAQs (incl. Delivery Optimization cache path) | https://www.itechguides.com/windows-softwaredistribution-folder-faqs/ | T3/T4 | 2026 |
| S17 | Windows Latest — 24H2 creates undeletable 8.63 GB update cache | https://www.windowslatest.com/2024/10/10/windows-11-24h2-issue-creates-undeletable-8-63-gb-windows-update-cache/ | T3 | Oct 2024; checkpoint-update explanation |
| S18 | TechRadar — 24H2 eats 8.6 GB | https://www.techradar.com/computing/windows/windows-11-24h2-update-yoinks-8-6gb-of-your-precious-drive-space-and-refuses-to-give-it-back-but-a-fix-is-coming | T3 | Oct 2024 |
| S19 | ExtremeTech — 24H2 8.63 GB cache | https://www.extremetech.com/computing/windows-11-24h2-update-creates-an-863gb-cache-file-that-cannot-be-deleted-easily | T3 | Oct 2024 |
| S20 | PCMag — 24H2 junk file; Microsoft: "this is a reporting error" | https://www.pcmag.com/news/windows-11-24h2-update-comes-with-8gb-junk-file-you-cant-delete | T3 | Oct 2024 |
| S21 | r/windows — Windows Temp folder filling up | https://www.reddit.com/r/windows/comments/ieb1lt/windows_temp_folder_filling_up/ | T1 | 2020; 3 TB of AV definitions in temp |
| S22 | r/computerhelp — C drive keeps filling up for no reason | https://www.reddit.com/r/computerhelp/comments/1kdhyg5/c_drive_keeps_filling_up_for_no_reason/ | T1 | May 2025 |
| S23 | r/windows — "Crap load of temporary files" (43.7 GB, only 101 MB deletable) | https://www.reddit.com/r/windows/comments/dtxf6w/crap_load_of_temporary_files/ | T1 | 2019; recurring complaint |
| S24 | r/Windows11 — 5 GB of temporary files, nothing to delete | https://www.reddit.com/r/Windows11/comments/1h9vpap/how_do_i_delete_this_temporary_files_it_says_i/ | T1 | Dec 2024 |
| S25 | Microsoft Q&A — Temporary file size in Storage settings not accurate | https://learn.microsoft.com/en-us/answers/questions/3917031/temporary-file-size-to-remove-in-settings-system-s | T1/T2 | Jan 2025 |
| S26 | Microsoft Q&A — High storage temporary files in Windows 11 | https://learn.microsoft.com/en-us/answers/questions/3870567/high-storage-temporary-files-in-windows-11 | T1 | May 2025 |
| S27 | r/windows — Proven tool to remove installation leftovers? | https://www.reddit.com/r/windows/comments/n8od7u/is_there_a_proven_tool_to_remove_installations/ | T1 | 2021 |
| S28 | TheWindowsClub — Remove leftover files after uninstall | https://www.thewindowsclub.com/how-to-remove-leftover-files-after-uninstall-in-windows | T4 | 2023 |
| S29 | Bretech — Safely remove leftover files after uninstalling | https://www.bretech.net/blog/how-to-remove-leftover-files-after-uninstalling-software/ | T4 | 2026 |
| S30 | r/software — Revo Uninstaller safe? (Affinity Designer → Ableton Live false positive) | https://www.reddit.com/r/software/comments/1bofvtr/revo_uninstaller_safe/ | T1 | Mar 2024 |
| S31 | r/antivirus — Revo Uninstaller scan modes; risk of unrelated matches | https://www.reddit.com/r/antivirus/comments/1bpwfvi/question_about_revo_uninstaller/ | T1 | Mar 2024 |
| S32 | r/computers — Program won't let me uninstall (missing uninstaller) | https://www.reddit.com/r/computers/comments/1h23jmv/need_help_program_wont_let_me_uninstall_it/ | T1 | Nov 2024 |
| S33 | TechBloat — Uninstall via Registry Editor; broken uninstall strings | https://www.techbloat.com/how-to-uninstall-programs-using-registry-editor-in-windows-11.html | T4 | 2026 |
| S34 | Recoverit — "Windows cannot find uninstall.exe" | https://recoverit.wondershare.com/computer-problems/windows-cannot-find-uninstall-exe.html | T4 | 2026 |
| S35 | ITTrip — Remove uninstalled "ghost" startup entries | https://en.ittrip.xyz/windows11/win11-startup-ghost-app | T4 | 2026 |
| S36 | RealityPathing — McAfee services still running after uninstall | https://realitypathing.com/mcafee-services-still-run-after-uninstall-how-to-clean-up/ | T4 | 2026 |
| S37 | r/techsupport — I can't delete this file ("open in ReMouse", uninstalled) | https://www.reddit.com/r/techsupport/comments/1qtlfpl/i_cant_delete_this_file/ | T1 | ≈2026 |
| S38 | r/techsupport — File won't delete in Windows 11 (LockHunter; dllhost) | https://www.reddit.com/r/techsupport/comments/1j6jec4/file_wont_delete_in_windows_11/ | T1 | Mar 2025 |
| S39 | r/WindowsHelp — File cannot be deleted because it is in use (Filmora) | https://www.reddit.com/r/WindowsHelp/comments/1g2n1hd/file_cannot_be_deleted_because_it_is_being_used/ | T1 | Oct 2024 |
| S40 | r/techsupport — Taking ownership: access denied (TrustedInstaller owner) | https://www.reddit.com/r/techsupport/comments/mpvj7o/taking_ownership_of_program_file_folder_access/ | T1 | 2021 |
| S41 | r/windows — TrustedInstaller rights for System32 | https://www.reddit.com/r/windows/comments/hukf76/how_to_get_trusted_installer_rights_for_system32/ | T1 | 2020 |
| S42 | Systweak — "You require permission from TrustedInstaller" | https://www.systweak.com/blogs/fix-trustedinstaller-permission-error-windows/ | T4 | 2026 |
| S43 | Ten Forums — C:\Windows\Installer 60 GB, safe to delete? | https://www.tenforums.com/performance-maintenance/152725-c-windows-installer-60-gbs-safe-delete.html | T1 | 2020; still widely cited |
| S44 | Super User — Is it safe to delete C:\Windows\Installer (Microsoft technician quoted) | https://superuser.com/questions/1392087/is-it-safe-to-delete-c-windows-installer-on-windows-10 | T1/T2 | 2019 |
| S45 | DiskMop — C:\Windows\Installer is 30 GB: what can be deleted | https://diskmop.com/blog/windows-installer-folder-cleanup | T4 | 2026 |
| S46 | r/Windows10 — Why does Windows deny access to WindowsApps | https://www.reddit.com/r/Windows10/comments/ob40fy/why_does_windows_deny_my_access_to_windowsapps/ | T1 | 2021 |
| S47 | r/windowsapps — WindowsApps folder ("had to reinstall Windows") | https://www.reddit.com/r/windowsapps/comments/qihmpw/windowsapp_folder/ | T1 | 2021 |
| S48 | r/techsupport — WindowsApps/ModifiableWindowsApps on a games drive | https://www.reddit.com/r/techsupport/comments/x8cbl4/windowsapps_and_modifiablewindowsapps_appeared_on/ | T1 | 2022 |
| S49 | r/sysadmin — System Volume Information consuming the volume | https://www.reddit.com/r/sysadmin/comments/13anol5/csystem_volume_information_taking_up_massive/ | T1 | May 2023 |
| S50 | r/techsupport — What is System Volume Information, can I delete it | https://www.reddit.com/r/techsupport/comments/rd1y9x/what_is_system_volume_information_and_can_i/ | T1 | 2021 |
| S51 | ACSApp — Reduce a large System Volume Information folder | https://acsapp.com/blog/how-to-reduce-a-large-system-volume-information-folder-in-windows/ | T4 | 2026 |
| S52 | r/wsl2 — ext4.vhdx taking too much storage with no usage | https://www.reddit.com/r/wsl2/comments/1mi4sj5/ext4vhdx_taking_too_much_storage_with_no_usage/ | T1 | Aug 2025 |
| S53 | r/docker — Docker + WSL2 VHDX keep growing even when empty | https://www.reddit.com/r/docker/comments/1msw42x/docker_wsl2_vhdx_files_keep_growing_even_when/ | T1 | Aug 2025 |
| S54 | r/docker — Docker taking 30 GB after prune | https://www.reddit.com/r/docker/comments/1bzkp0u/docker_taking_30gb_of_disk_space_even_after_a/ | T1 | Apr 2024 |
| S55 | r/docker — Docker drive suddenly large; sparse VHD advice | https://www.reddit.com/r/docker/comments/1fqv2ub/docker_drive_on_windows_recently_started_taking/ | T1 | Sep 2024 |
| S56 | r/webdev — node_modules eating 70 GB | https://www.reddit.com/r/webdev/comments/1kizdsz/node_modules_is_eating_70gb_of_my_projects_folder/ | T1 | May 2025 |
| S57 | r/node — How much space does Node.js take | https://www.reddit.com/r/node/comments/1dej3hy/how_much_space_does_nodejs_take/ | T1 | Jun 2024 |
| S58 | r/nextjs — npm run dev takes 10 GB on C: | https://www.reddit.com/r/nextjs/comments/1d4qu7y/npm_run_dev_and_after_1_hour_it_take_10gb_in_c/ | T1 | May 2024 |
| S59 | r/sysadmin — WizTree 3 release thread (TreeSize over-counts WinSxS hard links) | https://old.reddit.com/r/sysadmin/comments/6x43dr/wiztree_3_has_just_been_released_its_an_amazingly/ | T1 | 2017; historically important |
| S60 | FileCroco — WizTree review (hard links counted once; totals reconcile) | https://www.filecroco.com/download-wiztree/ | T3 | 2026 |
| S61 | GitHub dundee/gdu #688 — OneDrive online-only files counted as disk usage | https://github.com/dundee/gdu/issues/688 | T1 | Oct 2026 |
| S62 | GitHub dundee/gdu #692 — Fix: zero disk usage for online-only files | https://github.com/dundee/gdu/pull/692 | T1 | 2026 |
| S63 | r/WindowsHelp — No matter how much storage I cut off it's never enough | https://www.reddit.com/r/WindowsHelp/comments/1ii2csg/no_matter_how_much_storage_i_cut_off_its_never/ | T1 | Feb 2025 |
| S64 | r/Windows11 — Storage Sense only cleans C: | https://www.reddit.com/r/Windows11/comments/11m8bjm/storage_sense_is_so_frustratingly_close_to_being/ | T1 | Mar 2023 |
| S65 | Microsoft Support — Manage drive space with Storage Sense | https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/manage-drive-space-with-storage-sense | T2 | Current |
| S66 | r/WindowsHelp — Whole Downloads folder cleared | https://www.reddit.com/r/WindowsHelp/comments/ogre44/my_whole_downloads_folder_has_been_cleared_except/ | T1 | 2021 |
| S67 | Microsoft Q&A — Windows 11 24H2 update space issue | https://learn.microsoft.com/en-us/answers/questions/3905630/windows-11-24h2-update-space-issue | T1 | Jan 2025 |
| S68 | CloudHouse — Fix 24H2 update failing (20 GB requirement) | https://cloudhousetechnologies.com/blog/how-to-fix-windows-11-24h2-update-failing-not-installing-2026 | T4 | 2026 |
| S69 | Microsoft Q&A — How to fix corrupted files (SFC) | https://learn.microsoft.com/en-us/answers/questions/5683492/how-to-fix-corrupted-files-in-windows | T1/T2 | 2025 |
| S70 | Microsoft Q&A — SFC cannot repair corrupted system files | https://learn.microsoft.com/en-us/answers/questions/6059921/windows-sfc-cannot-repair-corrupted-system-files | T1 | 2026 |
| S71 | r/pcgamingtechsupport — Uninstalled game still taking up space | https://www.reddit.com/r/pcgamingtechsupport/comments/gmf7v3/uninstalled_game_still_taking_up_space/ | T1 | 2020 |
| S72 | r/Steam — Uninstalling Steam games eats some GB still | https://www.reddit.com/r/Steam/comments/1bp9rb1/uninstalling_steam_games_eats_some_gb_still/ | T1 | Mar 2024 |
| S73 | r/windows — Delete mass amounts of subfolders (invert-selection workaround) | https://www.reddit.com/r/windows/comments/18i1984/need_to_delete_mass_amounts_of_subfolders_but/ | T1 | Dec 2023 |
| S74 | Super User — How to delete huge number of files (Explorer memory blow-up) | https://superuser.com/questions/416071/how-to-delete-huge-number-of-files-on-windows | T1 | 2012; still referenced |
| S75 | r/Windows11 — Delete multiple files matching a pattern | https://www.reddit.com/r/Windows11/comments/18gemcx/how_do_you_delete_multiple_files_with_the_same/ | T1 | Dec 2023 |
| S76 | r/DataHoarder — Free duplicate photo finder for 140k photos | https://www.reddit.com/r/DataHoarder/comments/m7m6i1/free_duplicated_photo_finder_for_windows_i_have/ | T1 | 2021 |
| S77 | r/DataHoarder — Duplicate photo finder for Windows | https://www.reddit.com/r/DataHoarder/comments/1jz22mo/duplicate_photo_finder_for_windows/ | T1 | Apr 2025 |
| S78 | r/Windows10 — Best duplicate photo free app (free-tier deletion caps) | https://www.reddit.com/r/Windows10/comments/grkypo/best_duplicate_photo_free_app/ | T1 | 2020 |
| S79 | EndpointWeekly — WMIC removed from Windows 11 | https://endpointweekly.com/blog/wmic-removed-windows-11-replace-scripts-powershell-cim.html | T3 | Aug 2026 |
| S80 | Neowin — Microsoft removing a 25-year-old legacy feature (WMIC) | https://www.neowin.net/news/microsoft-is-removing-a-legacy-windows-feature-that-has-been-around-for-25-years/ | T3 | Aug 2026 |
| S81 | iTechGuides — Optimize/defragment drives in Windows 11 (retrim) | https://www.itechguides.com/how-to-optimize-and-defragment-drives-in-windows-11/ | T3 | 2026 |
| S82 | Windows Latest — Optimize Drives redesign; weekly schedule; SSDs are trimmed | https://www.windowslatest.com/2026/11/03/windows-11s-big-redesign-now-covers-even-its-ancient-defrag-tool-as-microsoft-finally-bets-on-winui-after-years-of-neglect-and-pushing-webview/ | T3 | Nov 2026 |
| S83 | r/WindowsHelp — "What is this file, it's taking 100 GB" (Delivery Optimization) | https://www.reddit.com/r/WindowsHelp/comments/1kkhpe4/what_is_this_file_is_it_safe_to_delete_its_taking/ | T1 | May 2025 |
| S84 | r/software — WizTree vs TreeSize (MFT read; seconds vs minutes) | https://www.reddit.com/r/software/comments/ugawm0/wiztree_vs_treesize_which_would_you_recommend_and/ | T1 | May 2022 |
| S85 | WizTree — End User License Agreement | https://diskanalyzer.com/eula | T2 | Current; personal-use grant; no reverse engineering |
| S86 | GitHub windirstat/windirstat — repository and GPL notice | https://github.com/windirstat/windirstat | T2 | Current |
| S87 | GitHub harryytm/windirstat — README stating GPL-2.0 | https://github.com/harryytm/windirstat | T2 | Mirror/derivative; licence text |
| S88 | GitHub Klocman/Bulk-Crap-Uninstaller — README, Apache-2.0 | https://github.com/Klocman/Bulk-Crap-Uninstaller | T2 | Current |
| S89 | JAM Software KB — TreeSize Free limitations (no hard links/ADS/dedup) | https://knowledgebase.jam-software.com/treesize_free/ | T2 | Current; vendor KB |
| S90 | Evinco — Everything by voidtools (MFT indexing) | https://www.evinco-software.com/eng/blog/everything-by-voidtools-the-ultimate-fast-windows-file-search-utility/ | T3 | 2026 |
| S91 | NinjaOne — How to use Disk Cleanup (status/practice) | https://www.ninjaone.com/blog/how-to-use-disk-cleanup-in-windows/ | T3 | 2025 |
| S92 | Microsoft Q&A — Windows component cleanup (StartComponentCleanup /ResetBase) | https://learn.microsoft.com/en-au/answers/questions/5927876/windows-component-cleanup | T1/T2 | Jun 2026 |
| S93 | r/software — Program to remove leftovers (tool-by-tool leftover counts; BCU 364 MB portable) | https://www.reddit.com/r/software/comments/zk1m80/program_to_remove_leftovers_from_various_programs/ | T1 | Dec 2022 |
| S94 | r/Windows11 — Which is the best uninstaller? | https://www.reddit.com/r/Windows11/comments/ybivqe/which_is_the_best_uninstaller/ | T1 | Oct 2022 |
| S95 | r/privacy — Alternative to CCleaner's uninstall feature | https://www.reddit.com/r/privacy/comments/1k3bf82/looking_for_an_alternative_to_ccleaners_uninstall/ | T1 | Apr 2025 |
| S96 | r/software — Best uninstaller for Windows?!? | https://www.reddit.com/r/software/comments/1mvg2vj/best_uninstaller_for_windows/ | T1 | Aug 2025 |
| S97 | r/WinDirStat — community reports (CSV export crash, slow scanning) | https://www.reddit.com/r/WinDirStat/ | T1 | ≈2026 crawl |
| S98 | GitHub Drawer/TreeSizr — MFT analyser with hard-link/placeholder handling | https://github.com/Drawer/TreeSizr | T3 | 2026; **licence not verified** — no reuse permitted until checked |
| S99 | r/techsupport — Analyser stops at 25% on a full drive | https://www.reddit.com/r/techsupport/comments/1oum8r/finding_ghost_data_on_almostfull_hard_drive_both/ | T1 | 2013; historical pattern |

## A.1 Sources deliberately not used

- SEO listicles and "best cleaner 2026" pages (no first-hand evidence, frequently rewritten copy).
- AI-generated answer summaries on Q&A sites, except where they merely restate a documented Microsoft procedure already supported by T1/T2 sources.
- Any X/Twitter content: not retrievable in this environment (§2.2).

---

# Appendix B — Icon Technical Specification

| Property | Value |
|---|---|
| Master canvas | 512 × 512, 96 px corner radius (~22.5%) |
| Safe margin | Geometry confined to a 360 px circle; the amber shard reaches x≈375, within the 512 canvas |
| Palette | Navy `#1B2E4C → #080E18`; arc `#1FA8E0 → #2DE2C5`; shard `#FFC24D → #FF8A1F` |
| ICO frames | 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 (RGBA) |
| 16–24 px handling | Optical variant: ring stroke 66 (vs 50), no inner bars, larger shard, higher arc contrast |
| Light theme | Tile `#FFFFFF → #EAF1F8`, arc `#0E7FB8 → #16C9AB`, shard `#F5A623 → #E07A00`, inner marks `#0B1424` |
| Dark theme | Transparent background, brighter arc `#7CF7DE`, used on dark surfaces where a navy tile would disappear |
| Monochrome | Single-colour paths using `currentColor` for docs, print, and themed contexts |

**Shell integration checklist (to be completed in your build):**

1. Add the ICO to the resource script: `IDI_APP_ICON ICON "diskwise.ico"`.
2. Load with `LoadIconWithScaleDown(hInst, MAKEINTRESOURCE(IDI_APP_ICON), cx, cy, &hicon)` so Windows selects the nearest frame instead of stretching a single bitmap.
3. Set an explicit `AppUserModelID` (`SetCurrentProcessExplicitAppUserModelID(L"YourCompany.DiskWise")`) and register the same ID in the shortcut so taskbar pinning and jump lists use the product icon.
4. Window class icons: `wcex.hIcon` = 32 px, `wcex.hIconSm` = 16 px; on DPI change, reload at the new scale rather than letting the shell scale.
5. Verify at 100/125/150/175/200% and in Start menu "All apps" (44×44 target), taskbar (32 px at 100%, 40 px at 125%), title bar (16/20/24 px), and the installer/Apps list (48 px).

---

# Appendix C — Registry Safety Rules

The product's registry capability is deliberately narrow. These rules are normative for implementation.

## C.1 Allowed scope (read-only by default)

| Scope | Allowed operation |
|---|---|
| `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\<product>` and WOW6432Node equivalent | Read; export; remove **only** as part of a verified uninstall of that exact product |
| `HKLM\SOFTWARE\<Publisher>\<Product>` | Read; export; remove only for class-A ownership |
| `HKCU\SOFTWARE\<Publisher>\<Product>` | Same, per-user scope only |
| `HKLM\SYSTEM\CurrentControlSet\Services\<service>` | Read-only report; removal only through the service control manager after review |
| `...\CurrentVersion\Run` / `RunOnce` values referencing the removed product's executable | Remove only with class-A evidence and export |
| File-association entries owned by the product (`HKCR\Applications\<exe>`, product-specific ProgIDs) | Remove only if no other installed product references them |
| `...\Installer\Products` / `Features` | **Never** modified directly; MSI APIs only |

## C.2 Absolute prohibitions

- No bulk "registry scan and clean" mode exists in the product.
- No modification of `HKEY_CLASSES_ROOT\CLSID`, `Interface`, `TypeLib` entries, Windows Defender, servicing, policy, or security-provider keys.
- No change to ownership or ACLs on any registry key.
- No removal of a key that fails ownership verification — it is reported, not removed.
- No claim that registry cleaning improves performance.

## C.3 Mandatory procedure for any approved removal

1. Export the key (or subtree) to `.reg` into the job's restore bundle; record SHA-256 of the export.
2. Verify the export is readable by re-parsing it before proceeding.
3. Remove the key with `RegDeleteTreeW` scoped to the verified path (never a recursive delete of a parent).
4. Record the operation in the audit log with the evidence class and the export path.
5. On restore: import with `RegRestoreKey`/`reg import`, then verify key presence and value data.
6. If a restore fails, surface the exact export path to the user with instructions — never silently swallow the failure.

## C.4 Restore-bundle contents per job

`manifest.jsonl` (every item + reason code) · `files/` (quarantined files with sidecar metadata) · `registry/*.reg` (exports) · `verify.txt` (hashes and post-conditions) · `README.txt` (plain-language restore instructions).

---

# Appendix D — Assumptions and Open Questions

## D.1 Assumptions made in this report

1. The existing application is a native Windows desktop program written predominantly in C with a Win32 or MFC-style UI; this is inferred from the brief, not verified.
2. Distribution is proprietary/closed-source. If the product is or will be GPL-compatible, §7's licence conclusions change materially — and WinDirStat's codebase becomes an option.
3. The target audience is technically curious home users and small IT; enterprise deployment is a secondary concern reflected only in the allow-listing and CLI notes.
4. The developer is a small team; effort estimates assume one competent Windows C developer.
5. The product may request elevation per operation, and the current build is not `requireAdministrator` — **[PENDING VERIFICATION]**.

## D.2 Questions that must be answered before Phase 0 ends

| # | Question | Why blocking |
|---|---|---|
| Q1 | Does the current build run elevated at startup? | Determines whether the broker in §9.7 is mandatory or optional |
| Q2 | Is selection index-based, path-based, or handle-based? | Determines whether R2 is an extension or a replacement |
| Q3 | Are deletions direct, `SHFileOperation`, or `IFileOperation`? | Determines the reversibility strategy |
| Q4 | Which toolkit renders the tables, and are they virtual? | Determines R8's performance ceiling |
| Q5 | Is there an existing quarantine/backup concept? | Determines Phase 0 scope |
| Q6 | What is the current settings format and location? | Determines migration work in R14 |
| Q7 | Which Windows versions are supported today? | Determines API floors (e.g. `GetFileInformationByHandleEx` is Vista+, USN is universal) |
| Q8 | Is the binary code-signed today? | Determines the EDR strategy and SmartScreen behaviour |
| Q9 | Is there an installer/updater to preserve? | Determines packaging work |
| Q10 | Are there existing localisation resources? | Determines string-handling rules in the new UI |

## D.3 Closing note

This document separates three things carefully: what the evidence shows (§3), what the design should be (§4–§9), and what was actually built (§12). Only the last is a claim of accomplishment, and it is limited to the icon assets and the tested C reference core. The rest is a plan that becomes real the moment the source code is available — and the first ten days of that plan are written out in §12.4 so that no further analysis is needed to begin.

---
