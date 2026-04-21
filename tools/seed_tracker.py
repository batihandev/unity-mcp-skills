#!/usr/bin/env python3
"""Seed the recipe validation tracker.

One-shot: walks recipes/<domain>/*.md, detects current ext/pre state,
writes docs/superpowers/notes/recipe-validation-tracker.md.

Run this exactly once to bootstrap. After that, tracker is updated in-place
by tools/tracker_update.py.
"""
from __future__ import annotations

import sys
from datetime import date
from pathlib import Path


def classify_recipe(path: Path) -> tuple[str, str]:
    """Return (ext_flag, pre_flag) for this recipe as of right now."""
    text = path.read_text(encoding="utf-8", errors="ignore")
    ext = "-" if "Original Logic:" in text else "x"
    pre = "x" if "## Prerequisites" in text else "-"
    return ext, pre


def main() -> int:
    repo = Path(__file__).resolve().parents[1]
    recipes_dir = repo / "recipes"
    out_path = repo / "docs" / "superpowers" / "notes" / "recipe-validation-tracker.md"
    out_path.parent.mkdir(parents=True, exist_ok=True)

    domains: dict[str, list[tuple[str, str, str]]] = {}
    for domain_dir in sorted(p for p in recipes_dir.iterdir() if p.is_dir()):
        if domain_dir.name.startswith("_"):
            continue
        rows: list[tuple[str, str, str]] = []
        for recipe in sorted(domain_dir.glob("*.md")):
            if recipe.name.lower() == "readme.md":
                continue
            ext, pre = classify_recipe(recipe)
            rows.append((recipe.stem, ext, pre))
        if rows:
            domains[domain_dir.name] = rows

    total = sum(len(v) for v in domains.values())
    ext_ok = sum(1 for v in domains.values() for _, e, _ in v if e == "x")
    pre_ok = sum(1 for v in domains.values() for _, _, p in v if p == "x")

    lines: list[str] = []
    lines.append("# Recipe Validation Tracker")
    lines.append("")
    lines.append(
        f"State as of {date.today().isoformat()}. "
        "Updated in-place by `tools/tracker_update.py`; queried by "
        "`tools/tracker_next.py`."
    )
    lines.append("")
    lines.append("## Gates")
    lines.append("")
    lines.append("- **ext** — body re-extracted from upstream (or never stubbed).")
    lines.append("- **pre** — `## Prerequisites` section declares every `_shared/*.md` it uses.")
    lines.append("- **comp** — passes compile-only Unity_RunCommand (body in `if (false)`).")
    lines.append("- **run** — passes end-to-end Unity_RunCommand.")
    lines.append("")
    lines.append("Cell values: `x` = done, `-` = pending, `B` = blocker (see notes).")
    lines.append("")
    lines.append("## Summary")
    lines.append("")
    lines.append(f"- Total recipes: **{total}**")
    lines.append(f"- ext: **{ext_ok}** / {total}")
    lines.append(f"- pre: **{pre_ok}** / {total}")
    lines.append("- comp: **0** / " + str(total))
    lines.append("- run: **0** / " + str(total))
    lines.append("")
    lines.append("## Domains")
    lines.append("")
    toc = " · ".join(f"[{d}](#{d}-{len(rows)}-recipes)" for d, rows in domains.items())
    lines.append(toc)
    lines.append("")

    for domain, rows in domains.items():
        lines.append(f"## {domain} ({len(rows)} recipes)")
        lines.append("")
        lines.append("| recipe | ext | pre | comp | run | notes |")
        lines.append("|---|---|---|---|---|---|")
        for name, ext, pre in rows:
            lines.append(f"| {name} | {ext} | {pre} | - | - |  |")
        lines.append("")

    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote tracker: {out_path} ({total} recipes across {len(domains)} domains)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
