# DiskWise — workspace deliverables

This workspace contains the research, specification and the two artefacts that were actually
built for the DiskWise engagement (a Windows storage-intelligence / application-management
suite for an existing C desktop application).

## Main deliverable

| Path | What it is |
|---|---|
| **`report/DiskWise_Report.md`** | The full report: research (36 problem families, 99 sourced references), audit of all 15 requirements, 28 additional feature proposals, competitor and licensing analysis, UI/UX blueprint, technical architecture, roadmap, test plan, and an honest account of what was built |

## Built and verified here

| Path | What it is | Verification |
|---|---|---|
| `assets/icons/svg/` | 5 vector masters (app tile, dark, light, small-size optical, monochrome) | Rendered with cairosvg |
| `assets/icons/png/` | 34 PNG exports incl. 512/1024 masters and preview sheets | `preview-sizes.png`, `preview-lockup.png` |
| `assets/icons/diskwise.ico` | Windows icon, 10 resolutions (16–256 px), RGBA | Frame-by-frame load check |
| `prototype/` | Portable C11 reference core: identity-keyed selection + deletion safety gate | `make test` → **39/39 pass** under ASan + UBSan |
| `tools/build_icons.py` | Rebuilds the icon set | `python3 tools/build_icons.py` |
| `tools/build_report.py` | Rebuilds the single-file report from `report/src/` | `python3 tools/build_report.py` |
| `report/src/` | Report source parts (edit these, then rebuild) | — |

## Not done — and why

No source code for the existing application was provided, so **nothing in your codebase was
inspected, modified, compiled or tested**. Report §12.3 lists exactly what is needed to start;
§12.4 gives a first-ten-days plan.

## Quick start

```bash
cd prototype && make test          # selection + safety-gate tests (ASan/UBSan)
python3 tools/build_icons.py       # regenerate icons
python3 tools/build_report.py      # regenerate report/DiskWise_Report.md
```
