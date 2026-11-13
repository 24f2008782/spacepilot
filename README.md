# SpacePilot

[![Build and test](https://github.com/24f2008782/spacepilot/actions/workflows/build.yml/badge.svg)](https://github.com/24f2008782/spacepilot/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**Safe, read-only-first Windows cleanup and disk analysis.**

SpacePilot is a Windows desktop app built with WinUI 3 and .NET 9. It is designed around one rule: **look first, explain what will happen, and ask before anything is changed.**

The executable is still named `CDClean.exe` for historical reasons. The product name is SpacePilot.

## Status

Version **1.5.3** is verified for the non-admin release pass on 2026-11-12:

- Release build: 0 warnings, 0 errors.
- Core tests: **107/107 passed**.
- Smoke test: all 15 pages open.
- Full UI suite: all checks pass, with one foreground-only keyboard check skipped.
- Disk Map UI suite: all 19 checks pass.
- Fixture safety verification: **26/26 checks passed**.
- Clean preview: 1,297,124 files scanned; 1,200,573 blocked by safety checks; 96,551 actionable; about 75 seconds.

See [docs/verification.md](docs/verification.md) for the detailed verification record and known limitations.

## What it does

| Area | What you get |
|---|---|
| **Disk Map** | Treemap of a drive or folder, folder tree, largest files, search, unreadable-item list, and a storage ledger. |
| **Clean** | Rule-based cache cleaning with a preview table, safety blocking, review dialog, and a backup vault for archived files. |
| **Scan** | Scoped scans with file counts, sizes, and a readable summary. |
| **Apps** | Installed-program inventory, MSIX details, and a review-first uninstall handoff. |
| **Leftovers / Old files / Large files / Duplicates** | Read-only finders that surface candidates before anything is changed. |
| **Organize folder** | Suggests a folder layout without silently moving files. |
| **Backups** | Backup vault browser, retention, and restore/delete review flows. |
| **Maintenance** | Read-only WinSxS, shadow-storage, Windows.old, and Windows Update cache analysis. |
| **System tools** | Admin-task cards, orphan-service reporting, startup inventory, MSI cache, and VHDX listing. Read-only analysis comes first. |
| **Schedule** | Weekly or daily safe-rule runs. Manual rules are queued for review instead of being deleted silently. |
| **Rules** | Editable `rules.toml` with validation and a save check. |
| **Settings** | Backup location, retention, vault cap, rules path, low-space warning, audit redaction, and advanced safety controls. |

## Safety model

SpacePilot is not a "one click and hope" cleaner.

- **Preview first.** Clean, uninstall, recycle, archive, and maintenance actions go through a review step.
- **Safe rules only for scheduled runs.** A scheduled run executes rules marked `safe = true`. Other ticked rules are queued for manual review on the next launch.
- **Protected paths.** Windows folders, user profile folders, and user-added protected paths are blocked before deletion.
- **Review dialog.** The exact count and the items being changed are shown before a destructive action.
- **Backup vault.** Archive rules copy a file to the vault and verify the copy before removing the original.
- **Local only.** No file names or file contents are uploaded. Telemetry is off by default; crash reporting is opt-in and shown before sending.
- **Audit log.** Actions are recorded locally under `%LOCALAPPDATA%\CDClean\audit\`.

Read [docs/EULA.md](docs/EULA.md) for the safety and use notice, and [docs/PRIVACY.md](docs/PRIVACY.md) for the privacy policy.

## Requirements

- Windows 10 version 1809 or newer, or Windows 11.
- x64 processor.
- [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) for the framework-dependent release.
- No administrator rights are needed for the normal read-only and review-first flows.
- Some System tools require a UAC prompt. Those paths are not part of the non-admin verification pass.

## Install

1. Download `SpacePilot-1.5.3-x64.zip` from the [latest release](https://github.com/24f2008782/spacepilot/releases).
2. Extract it to a folder you control.
3. Run `CDClean.exe`.
4. Keep `rules.toml` next to the executable, or point Settings to another rules file.

The release is not code-signed yet. Windows SmartScreen may warn on first launch. Code signing is planned for Tier 4.

## Build

Prerequisites:

- .NET 9 SDK
- Windows 10/11
- Visual Studio 2022 Build Tools with the Windows App SDK workload, or an equivalent Windows SDK setup

```powershell
dotnet build src/CDClean.App/CDClean.App.csproj -c Release
dotnet test tests/CDClean.Core.Tests/CDClean.Core.Tests.csproj -c Release
```

Publish the app and the elevated helper into one folder:

```powershell
dotnet publish src/CDClean.App/CDClean.App.csproj -c Release -r win-x64 --self-contained false -o publish/spacepilot
dotnet publish src/CDClean.Elevated/CDClean.Elevated.csproj -c Release -r win-x64 --self-contained false -o publish/spacepilot
```

The app is framework-dependent. Add `--self-contained true` if you need a runtime-bundled build.

## Test

```powershell
dotnet test tests/CDClean.Core.Tests/CDClean.Core.Tests.csproj -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File tools/smoke.ps1 -Exe .\publish\spacepilot\CDClean.exe
powershell -NoProfile -ExecutionPolicy Bypass -File tools/uitest.ps1 -Exe .\publish\spacepilot\CDClean.exe
powershell -NoProfile -ExecutionPolicy Bypass -File tools/uitest-diskmap.ps1 -Exe .\publish\spacepilot\CDClean.exe
```

The UI tests run in the background and park the window off-screen. The foreground-only `Shift+F10` row-menu check is skipped unless the test is run with `-Foreground`.

## Architecture

```text
src/
  CDClean.Core/       Scanner, rules, safety policy, executor, vault, analyzers
  CDClean.App/        WinUI 3 shell, pages, dialogs, view models
  CDClean.Elevated/   Helper used by admin-only tasks
tests/
  CDClean.Core.Tests/ xUnit tests on synthetic fixtures
tools/                Smoke and UI automation scripts
docs/                 Plans, policies, verification records
installer/            MSIX manifest draft
winget/               Winget manifest draft
```

Design choices:

- Streaming scans with a bounded worker count.
- A safety policy that classifies every target before execution.
- Review-first destructive actions; no silent scheduled deletion of manual rules.
- Planned for Batch 3.1: atomic, versioned settings persistence with a `.bak` copy.

## Roadmap

Tier 2 is complete and verified. Tier 3 work is in progress:

- **3.1 Settings** — 14-category sidebar, search, atomic settings store, reset controls, typed confirmation for dangerous settings.
- **3.2 Automation and history** — preview-first automation, review queue, storage history, startup manager.
- **3.3 Specialists** — VHDX compactor, developer cache manager, game library analysis, maintenance console.
- **3.4 Finders** — duplicate/similar finder, downloads triage, browser cache analysis, MSI cache audit.

## Known limitations

- The full Clean preview is still large: 1.3 million rows can be slow for external UI Automation or screen-reader traversal. The in-app preview completes in about 75 seconds on the verification machine.
- The developer-cache scan currently matches broad `bin`, `obj`, and `dist` folders. Result-quality tuning is planned for Tier 3.
- Admin/UAC tasks, Fast Scan/MFT, real-data destructive flows, and code signing are not part of the non-admin verification pass.
- The foreground-only `Shift+F10` row menu is not covered by the background UI test.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Please keep destructive changes review-first and add a test or fixture check for new behavior.

## Security

Please do not open a public issue for a vulnerability. Use GitHub's private vulnerability reporting on the Security tab. See [SECURITY.md](SECURITY.md).

## License

MIT. See [LICENSE](LICENSE).
