# Contributing

Thanks for taking a look at SpacePilot.

## Before you change destructive behavior

SpacePilot is built around a review-first safety model. A change that deletes, moves, archives, or uninstalls something must:

- keep a visible review step;
- preserve the protected-path policy;
- explain what will happen in plain language;
- include a fixture-based test or a focused Core test.

Do not test destructive flows against real user data. Use a synthetic folder under `%TEMP%` or another disposable location.

## Build and test

```powershell
dotnet build src/CDClean.App/CDClean.App.csproj -c Release
dotnet test tests/CDClean.Core.Tests/CDClean.Core.Tests.csproj -c Release
```

UI checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/smoke.ps1 -Exe .\publish\spacepilot\CDClean.exe
powershell -NoProfile -ExecutionPolicy Bypass -File tools/uitest.ps1 -Exe .\publish\spacepilot\CDClean.exe
powershell -NoProfile -ExecutionPolicy Bypass -File tools/uitest-diskmap.ps1 -Exe .\publish\spacepilot\CDClean.exe
```

## Pull requests

- Keep the change focused.
- Update `CHANGELOG.md` for user-visible changes.
- Add or update tests.
- State which checks you ran and which you did not.
- Do not commit `bin`, `obj`, `publish`, local logs, or personal paths.

## Code style

Match the surrounding code. Prefer the smallest complete change over a new abstraction. Comments should explain why, not restate the code.
