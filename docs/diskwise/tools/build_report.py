#!/usr/bin/env python3
"""Assemble the DiskWise report from ordered section files.

Run:  python3 tools/build_report.py
Output: report/DiskWise_Report.md  (single self-contained document)
"""
import re
from pathlib import Path

SRC = Path("report/src")
OUT = Path("report/DiskWise_Report.md")

ORDER = [
    "01_front.md",
    "03_problems.md",
    "04_requirements.md",
    "05_features.md",
    "06_competitors.md",
    "08_ui.md",
    "09_architecture.md",
    "10_roadmap_tests.md",
    "12_results.md",
    "13_appendices.md",
]


def slug(text: str) -> str:
    text = text.strip().lower()
    text = re.sub(r"[^\w\s-]", "", text)
    return re.sub(r"[\s]+", "-", text)


def build_toc(parts: list[str]) -> str:
    lines = ["## Contents", ""]
    in_fence = False
    seen_title = False
    for part in parts:
        for raw in part.split("\n"):
            if raw.startswith("```"):
                in_fence = not in_fence
                continue
            if in_fence:
                continue
            m = re.match(r"^(#{1,2})\s+(.*)$", raw)
            if not m:
                continue
            level, title = len(m.group(1)), m.group(2).strip()
            if level == 1:
                if not seen_title:
                    seen_title = True
                    continue
                lines.append(f"- **[{title}](#{slug(title)})**")
            elif re.match(r"^\d+(\.\d+)?\s", title) or title[0].isdigit():
                lines.append(f"  - [{title}](#{slug(title)})")
    lines.append("")
    return "\n".join(lines)


def main() -> None:
    parts = [(SRC / name).read_text(encoding="utf-8") for name in ORDER]
    body = "\n\n---\n\n".join(p.rstrip() + "\n" for p in parts)

    front_end = parts[0].find("## How to read this document")
    # Insert the TOC just before the first numbered section of the front matter.
    anchor = parts[0].find("\n# 1. Executive Summary")
    toc = build_toc(parts)
    combined = parts[0][:anchor] + "\n" + toc + parts[0][anchor:]
    combined = combined.rstrip() + "\n\n---\n\n" + "\n\n---\n\n".join(p.rstrip() + "\n" for p in parts[1:])

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(combined, encoding="utf-8")

    words = len(re.findall(r"\S+", combined))
    print(f"wrote {OUT}  ({words:,} words, {len(combined):,} chars)")
    print(f"sections: {combined.count(chr(10) + '# ')} top-level headings")


if __name__ == "__main__":
    main()
