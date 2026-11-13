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
