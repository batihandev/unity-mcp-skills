#!/usr/bin/env python3
"""Update one gate of one recipe row in the tracker.

Usage:
    python3 tools/tracker_update.py gameobject_create comp x
    python3 tools/tracker_update.py component_add comp B --note "missing Newtonsoft"
    python3 tools/tracker_update.py material_set_color run x --note "green"

After updating a row, refreshes the Summary counters at the top.
"""
from __future__ import annotations

import argparse
import re
import sys
from datetime import date
from pathlib import Path

TRACKER = Path(__file__).resolve().parents[1] / "docs" / "superpowers" / "notes" / "recipe-validation-tracker.md"
GATE_IDX = {"ext": 1, "pre": 2, "comp": 3, "run": 4}
ROW = re.compile(r'^\|\s*([^|]+?)\s*\|\s*([-xB])\s*\|\s*([-xB])\s*\|\s*([-xB])\s*\|\s*([-xB])\s*\|([^|]*)\|$')


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("recipe", help="Recipe stem (no .md extension)")
    ap.add_argument("gate", choices=list(GATE_IDX))
    ap.add_argument("value", choices=["x", "-", "B"])
    ap.add_argument("--note", default=None)
    args = ap.parse_args()

    lines = TRACKER.read_text(encoding="utf-8").splitlines()
    target = args.recipe.strip()
    updated = False

    for i, line in enumerate(lines):
        m = ROW.match(line)
        if not m or m.group(1).strip() != target:
            continue
        cells = [m.group(1).strip(), m.group(2), m.group(3), m.group(4), m.group(5), m.group(6).strip()]
        cells[GATE_IDX[args.gate]] = args.value
        if args.note:
            stamp = date.today().isoformat()
            snippet = f"{stamp}: {args.note}"
            cells[5] = f"{cells[5]}; {snippet}" if cells[5] else snippet
        lines[i] = f"| {cells[0]} | {cells[1]} | {cells[2]} | {cells[3]} | {cells[4]} | {cells[5]} |"
        updated = True
        break

    if not updated:
        print(f"error: recipe '{target}' not found in tracker", file=sys.stderr)
        return 1

    totals = {"ext": 0, "pre": 0, "comp": 0, "run": 0}
    total_recipes = 0
    for line in lines:
        m = ROW.match(line)
        if not m:
            continue
        total_recipes += 1
        for g, col in (("ext", 2), ("pre", 3), ("comp", 4), ("run", 5)):
            if m.group(col) == "x":
                totals[g] += 1

    in_summary = False
    new_lines: list[str] = []
    for line in lines:
        if line.strip() == "## Summary":
            in_summary = True
            new_lines.append(line)
            continue
        if in_summary and line.startswith("## "):
            in_summary = False
        if in_summary:
            if line.startswith("- Total recipes:"):
                new_lines.append(f"- Total recipes: **{total_recipes}**")
                continue
            matched = False
            for g in ("ext", "pre", "comp", "run"):
                if line.startswith(f"- {g}:"):
                    new_lines.append(f"- {g}: **{totals[g]}** / {total_recipes}")
                    matched = True
                    break
            if not matched:
                new_lines.append(line)
            continue
        new_lines.append(line)

    TRACKER.write_text("\n".join(new_lines) + "\n", encoding="utf-8")
    print(f"updated {target}: {args.gate}={args.value}" + (f" ({args.note})" if args.note else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
