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
