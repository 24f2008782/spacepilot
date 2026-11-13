# Tier 4 plan: Publish on Windows (public 1.0)

| Field | Value |
|---|---|
| Date | 2026-11-05 |
| Starts after | Tier 3 (Batches 3.1–3.4), verified 2026-11-05 |
| Source | `playbook-v2.md` §7 (Tier 4), §9.11, §11.7, §7.3.5 (MFT reader + antivirus), `tier3-plan.md` |
| Platform | Windows only, WinUI 3 (x64 + ARM64) |
| Rule for this tier | Nothing ships unsigned or untested in a VM. Destructive tests never run on a working PC. |

## 0. Administrator permission from inside the app (UAC)

**Yes — the app already requests admin from inside itself.** `AdminTasks.Start(taskId)` launches `CDClean.Elevated.exe` with `UseShellExecute = true, Verb = "runas"`, which triggers the Windows UAC prompt. No external tool or manual elevation is needed.

- Per-operation elevation only: the app runs at medium integrity by default; admin is requested per task, never at startup.
- The MFT fast scan (Tier 2 15× path) will use the same mechanism: a new `mft-scan` task in `CDClean.Elevated.exe` that streams compact records back, with the in-process scanner as the fallback when the user declines UAC.
- Risk: an unsigned elevated helper is a red flag for antivirus/EDR. Code signing (§2) must happen before any public build that includes the MFT reader.

## 1. What Tier 4 must deliver

- A public, signed, installable Windows app (MSIX + portable ZIP + winget manifest).
- ARM64 build for Snapdragon/Copilot+ laptops.
- Self-contained runtime option (no .NET 9 install required).
- Localised UI (first languages TBD).
- Opt-in crash reporting (local-first, paths redacted).
- Quality bar: all tests pass in disposable VMs (Win 10 22H2, Win 11 24H2/25H2, x64 + ARM64); performance regression > 15% blocks release.

## 2. Batches

### Batch 4.1: Identity, legal, and naming
- [DECIDE D3] Name: "DiskWise" clashes with WiseCleaner's "Wise Disk Cleaner". Search trademarks. Alternatives from the report: DiskLens, Volumetric, Reclaim, Lumen Disk. ("SpacePilot" is the current working name — decide whether to keep or change before publishing.)
- [DECIDE D4] Licence: proprietary vs open source (MIT/GPL). Either way, BCU (Apache-2.0) integrates at process level only. WinDirStat (GPL) code can never be used in a proprietary build.
- EULA / licence text, no-warranty clause.
- Privacy policy: local-only processing, opt-in diagnostics.
- Third-party notices: Tomlyn, WinAppSDK, CommunityToolkit, Win2D.

Acceptance: name trademark-searched; licence chosen; EULA + privacy policy + notices committed.

### Batch 4.2: Code signing
- Authenticode signing for `CDClean.exe`, `CDClean.Elevated.exe`, and the installer.
- Options: Azure Trusted Signing (cheapest for individuals) or OV/EV certificate.
- Submit builds to Microsoft Defender as "false positive / allow" if flagged (especially the MFT reader).
- Sign the elevated helper before any public build.

Acceptance: SmartScreen no longer blocks; antivirus does not flag the MFT reader.

### Batch 4.3: Build matrix and CI
- Architectures: x64 (Intel + AMD, no extra work) and ARM64 (separate build; test all P/Invoke structs on ARM64).
- Self-contained vs framework-dependent: measure self-contained + trimming against the current ~92 MB.
- GitHub Actions `windows-latest`: build, tests, sign, package, checksums.
- Reproducible builds with build hash recorded in the audit log.

Acceptance: CI produces signed x64 + ARM64 artifacts on every push to main.

### Batch 4.4: Packaging and distribution
- MSIX installer (clean uninstall + auto-update) + portable ZIP.
- winget manifest (`winget install <id>`).
- Optional Microsoft Store listing (MSIX, free certification).
- `AppUserModelID`, Start menu + taskbar icons, 44/150 px MSIX logo assets.
- Updater: signed update manifest verified before applying; never auto-update while the elevated helper is running; stable + beta channels.

Acceptance: install from MSIX, update from beta channel, uninstall cleanly.

### Batch 4.5: Quality bar and localisation
- All tests in §10 pass in disposable VMs: Windows 10 22H2, Windows 11 24H2/25H2, x64 + ARM64. Destructive tests never run on a working PC.
- Performance numbers measured on reference hardware against §9.4 targets; regression > 15% blocks release.
- Accessibility: Narrator names on every control, contrast ≥ 4.5:1 in both themes, full keyboard parity, high-contrast mode.
- Localisation: all strings to `.resw`; no string concatenation; dates/sizes formatted per locale; RTL layout check. First languages TBD.
- Crash reporting: opt-in, local-first; minidump shown to the user before sending; paths redacted.

Acceptance: accessibility checklist green; localised build runs in at least one non-English locale.

### Batch 4.6: Launch kit
- Store listing, screenshots, demo video.
- Website / docs: "what this cannot do" page per feature, restore how-to.
- Support channel: GitHub Issues with a template that asks for the audit log.
- Beta with 10–20 users first; watch for false positives in leftovers before public release.

Acceptance: beta cohort of 10–20 users, no blocking false positives.

## 3. Risks for Tier 4

1. **Antivirus false positives** on the MFT reader and the elevated helper — sign first, submit to Defender, ship a "false positive / allow" note.
2. **SmartScreen** blocks unsigned downloads — signing is not optional for a public release.
3. **ARM64 P/Invoke** structs may differ in size/alignment — test every `DllImport` on ARM64.
4. **Self-contained runtime** inflates the 92 MB package — measure trimming before committing.
5. **Localisation** is large if done late — move strings to `.resw` early in Tier 4, not at the end.
6. **Beta false positives** in leftovers can damage trust — keep the "never claims completely removed" rule and the preserved list.
7. **Name/trademark** search must happen before publishing under any name.

## 4. Test plan

- All Core + UI tests in VMs (Win 10 22H2, Win 11 24H2/25H2, x64 + ARM64).
- Performance regression gate: > 15% blocks release.
- Accessibility audit (Narrator, contrast, keyboard, high-contrast).
- Localisation smoke in at least one non-English locale.
- Signing verification: SmartScreen + Defender scan of the published artifact.

## 5. Effort estimate

Tier 4 adds roughly 8–14 weeks for one full-time developer (playbook §10.4 note). Tiers 0–3 (27–39 weeks) are complete in code; Tier 4 is the publishing and hardening pass.