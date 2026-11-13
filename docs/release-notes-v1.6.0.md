# SpacePilot 1.6.0

This release is Batch 3.1: a real Settings page and safer settings persistence. The cleanup, scan, Disk Map, Apps, Backups, Maintenance, Schedule, and Rules flows from 1.5.3 are unchanged.

## Settings

- **14 categories** in a dedicated sidebar: General, Appearance, Scan, Drives, Cleanup rules, Uninstall, Registry safety, Exclusions & protected, Backups & recovery, Performance, Notifications, Privacy & diagnostics, Advanced, and About & updates.
- **Search** across setting names, descriptions, categories, and keywords. Multi-word searches such as `low space` work. Selecting a result opens its category.
- **Per-field reset** buttons, **Reset category**, and **Reset all**. Reset all requires two confirmations. Dangerous resets also require the `ADVANCED` unlock.
- Empty categories are explicit placeholders. This batch does not invent settings without a backing field.
- The About card shows the installed version and opens the logs folder, data folder, third-party notices, privacy policy, safety notice, and GitHub releases.
- Saving or resetting the audit-redaction setting applies it immediately; no restart is needed.

## Safer settings storage

- Settings are versioned with schema version `1`. Existing 1.5.x files load without migration.
- Saves are atomic: SpacePilot writes a temporary file, swaps it in with `File.Replace`, and keeps the previous file as `settings.json.bak`.
- A corrupt file or a file written by a newer schema falls back to defaults with a notice on the Settings page. The existing file is not changed until you save again.

## Dangerous settings

These three changes are behind a typed `ADVANCED` unlock:

- A custom `RulesPath`.
- Clearing all extra `ProtectedPaths`.
- Setting `VaultCapGb` to `0` (no vault cap) when a cap was set.

The unlock lasts for the current app session. Restarting SpacePilot requires typing `ADVANCED` again. The controls live in a bordered **Advanced risk settings** box. Adding or removing one protected path remains a normal confirmed action; clearing the whole list is the dangerous one.

## Verification

- Release build: 0 warnings, 0 errors.
- Core tests: **112/112 passed**.
- Smoke test: all 15 pages open.
- Full UI suite: all checks pass, one foreground-only keyboard check skipped.
- Disk Map UI suite: all 19 checks pass.
- Fixture safety verification: **26/26 checks passed**.

## Install

1. Download `SpacePilot-1.6.0-x64.zip`.
2. Extract it to a folder you control.
3. Run `CDClean.exe`. The executable name is unchanged for compatibility; the product is SpacePilot.
4. Keep `rules.toml` next to the executable, or set a custom path in Settings → Advanced.

The release is not code-signed yet. Windows SmartScreen may warn on first launch. Code signing is planned for Tier 4.

## Known limitations

- Admin/UAC-only flows, Fast Scan/MFT, app uninstall execution, and real-data destructive flows remain outside the non-admin verification pass.
- The Clean preview for a very large drive (about 1.3 million files) still takes roughly 75 seconds.
- Developer-cache matches for `bin`, `obj`, and `dist` are intentionally broad and should be reviewed.
- The foreground-only Shift+F10 row-menu check is skipped in background UI automation.
