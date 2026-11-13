# Security policy

## Supported versions

| Version | Supported |
|---|---|
| 1.5.x | Yes |
| Earlier | No |

## Reporting a vulnerability

Please do not open a public issue for a security problem.

Use **GitHub private vulnerability reporting** on the Security tab of this repository:

https://github.com/24f2008782/spacepilot/security/advisories/new

Include:

- A short description of the issue.
- Steps to reproduce.
- The affected version and Windows build.
- Whether the issue requires administrator rights.
- Any suggested fix or mitigation.

Please remove personal paths, file names, and other sensitive data from screenshots and logs before attaching them.

## Scope

SpacePilot can delete, move, or modify files after a review step. Security reports are especially relevant for:

- A path that bypasses the protected-path policy.
- A review dialog that does not match the action being performed.
- A scheduled run that deletes a non-Safe rule.
- A backup-vault verification failure.
- A privilege boundary in the elevated helper.
- A crash-report or telemetry path that leaks file names or contents.

The project does not currently have a bug bounty.
