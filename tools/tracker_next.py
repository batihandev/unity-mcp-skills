#!/usr/bin/env python3
"""Print recipes that are missing a given gate, in domain order.

Usage:
    python3 tools/tracker_next.py --gate comp --limit 10
    python3 tools/tracker_next.py --gate ext --domain gameobject
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

TRACKER = Path(__file__).resolve().parents[1] / "docs" / "superpowers" / "notes" / "recipe-validation-tracker.md"
DOMAIN_HEADER = re.compile(r'^## (?P<name>[a-z_]+) \(\d+ recipes\)\s*$')
ROW = re.compile(
    r'^\|\s*(?P<recipe>[^|]+?)\s*\|\s*(?P<ext>[-xB])\s*\|\s*(?P<pre>[-xB])\s*\|\s*(?P<comp>[-xB])\s*\|\s*(?P<run>[-xB])\s*\|(?P<notes>[^|]*)\|'
)


def parse_tracker(path: Path) -> list[dict]:
    rows: list[dict] = []
    current_domain: str | None = None
    for line in path.read_text(encoding="utf-8").splitlines():
        m = DOMAIN_HEADER.match(line)
        if m:
            current_domain = m.group("name")
            continue
        m = ROW.match(line)
        if m and current_domain:
            rows.append({
                "domain": current_domain,
                "recipe": m.group("recipe").strip(),
                "ext": m.group("ext"),
                "pre": m.group("pre"),
                "comp": m.group("comp"),
                "run": m.group("run"),
                "notes": m.group("notes").strip(),
            })
    return rows


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--gate", choices=["ext", "pre", "comp", "run"], required=True)
    ap.add_argument("--limit", type=int, default=10)
    ap.add_argument("--domain", default=None)
    args = ap.parse_args()

    rows = parse_tracker(TRACKER)
    pending = [r for r in rows if r[args.gate] == "-"]
    if args.domain:
        pending = [r for r in pending if r["domain"] == args.domain]

    if not pending:
        print(f"no pending recipes for gate={args.gate}" + (f" in domain={args.domain}" if args.domain else ""))
        return 0

    for r in pending[:args.limit]:
        flags = f"ext:{r['ext']} pre:{r['pre']} comp:{r['comp']} run:{r['run']}"
        note = f" — {r['notes']}" if r['notes'] else ""
        print(f"recipes/{r['domain']}/{r['recipe']}.md  [{flags}]{note}")

    remaining = len(pending) - args.limit
    if remaining > 0:
        print(f"... {remaining} more pending")
    return 0


if __name__ == "__main__":
    sys.exit(main())
