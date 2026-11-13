# SpacePilot — safety and use notice

**Version 1.1 — 2026-11-12**

SpacePilot is open source under the MIT License. See [LICENSE](../LICENSE). This notice explains the safety model and the expectations for using a tool that can change files. It does not add restrictions to the MIT License.

1. **Destructive actions.** SpacePilot can delete, move, archive, or uninstall items after a review step. Always read the review dialog and keep backups of anything important.

2. **Preview first.** Clean, recycle, archive, uninstall, and maintenance actions are shown for review before they run. Scheduled runs execute only rules marked `safe = true`; other ticked rules are queued for manual review.

3. **Protected paths.** Windows folders, user profile folders, and user-added protected paths are blocked before deletion. Do not remove protections unless you understand the consequence.

4. **Admin tasks.** Some System tools require administrator rights and trigger a Windows UAC prompt. You choose whether to approve each prompt. The non-admin verification pass does not cover elevated flows.

5. **No warranty.** The software is provided "as is", without warranty of any kind, express or implied. Use it at your own risk.

6. **Privacy.** Processing is local. Telemetry is off by default. Crash reporting is opt-in and shown before sending, with paths redacted. See [PRIVACY.md](PRIVACY.md).

7. **Third-party components.** See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

8. **No claim of complete removal.** An uninstall or cleanup does not guarantee that every trace of an application or file is gone.
