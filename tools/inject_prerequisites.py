#!/usr/bin/env python3
"""Inject a `## Prerequisites` section into every recipe that uses a `_shared/*.md`
helper symbol. Idempotent: re-running replaces the existing section instead of
duplicating.

Detection targets (must match inside the recipe's `csharp` fenced block):
    result.SetResult                    -> _shared/execution_result.md
    Validate.*                          -> _shared/validate.md
    GameObjectFinder.* or FindHelper.*  -> _shared/gameobject_finder.md
    WorkflowManager.*                   -> _shared/workflow_manager.md
    SkillsCommon.*                      -> _shared/skills_common.md
    ComponentSkills.FindComponentType   -> _shared/component_type_finder.md (+ skills_common)
    ComponentSkills.ConvertValue        -> _shared/value_converter.md

Usage:
    python3 tools/inject_prerequisites.py --recipes recipes/ [--dry-run]
                                          [--only recipe_stem[,stem...]]

The section is inserted immediately before the heading that precedes the first
`csharp` fence (typically `## Recipe` or `## C# Template`). Recipes with zero
helper usage get no section and are reported as `no-prereqs`.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

CSHARP_FENCE = re.compile(r'^```csharp\s*$')
HEADING = re.compile(r'^##\s+\S')
PREREQ_HEADING = re.compile(r'^##\s+Prerequisites\s*$')

# (detection regex, shared filename, one-line purpose for the bullet)
DETECTORS: list[tuple[re.Pattern[str], str, str]] = [
    (re.compile(r'\bresult\.SetResult\s*\('), 'execution_result.md',
     'for `result.SetResult(...)`'),
    (re.compile(r'\bValidate\.\w+'), 'validate.md',
     'for `Validate.Required` / `Validate.SafePath`'),
    (re.compile(r'\b(?:GameObjectFinder|FindHelper)\.\w+'), 'gameobject_finder.md',
     'for `GameObjectFinder` / `FindHelper`'),
    (re.compile(r'\bWorkflowManager\.\w+'), 'workflow_manager.md',
     'for `WorkflowManager.*`'),
    (re.compile(r'\bSkillsCommon\.\w+'), 'skills_common.md',
     'for `SkillsCommon.*`'),
    (re.compile(r'\bComponentSkills\.FindComponentType\b'), 'component_type_finder.md',
     'for `ComponentSkills.FindComponentType` (transitively needs `skills_common.md`)'),
    (re.compile(r'\bComponentSkills\.ConvertValue\b'), 'value_converter.md',
     'for `ComponentSkills.ConvertValue`'),
]


def extract_csharp_block(lines: list[str]) -> tuple[int, int, str] | None:
    """Return (fence_start_idx, fence_end_idx, block_text) for the first csharp fence."""
    for i, line in enumerate(lines):
        if CSHARP_FENCE.match(line):
            for j in range(i + 1, len(lines)):
                if lines[j].strip() == '```':
                    return i, j, '\n'.join(lines[i + 1:j])
            return None
    return None


def detect_prereqs(csharp_text: str) -> list[tuple[str, str]]:
    """Return ordered list of (shared_filename, purpose) tuples for helpers in use.
    `component_type_finder.md` pulls `skills_common.md` in transitively."""
    found: list[tuple[str, str]] = []
    seen: set[str] = set()
    needs_skills_common = False
    for pat, fname, purpose in DETECTORS:
        if pat.search(csharp_text):
            if fname in seen:
                continue
            found.append((fname, purpose))
            seen.add(fname)
            if fname == 'component_type_finder.md':
                needs_skills_common = True
    if needs_skills_common and 'skills_common.md' not in seen:
        found.append(('skills_common.md',
                      'required by `component_type_finder.md` for `SkillsCommon.GetAllLoadedTypes`'))
    return found


def render_section(prereqs: list[tuple[str, str]]) -> list[str]:
    lines = [
        '## Prerequisites',
        '',
        'Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:',
    ]
    for fname, purpose in prereqs:
        lines.append(f'- `recipes/_shared/{fname}` — {purpose}')
    lines.append('')
    return lines


def find_existing_prereq_span(lines: list[str]) -> tuple[int, int] | None:
    """Return (start_idx, end_idx_exclusive) of an existing `## Prerequisites` section, or None.
    The section ends at the next `##` heading OR the next ```csharp fence (whichever comes
    first). Without the fence sentinel, recipes whose csharp fence is not preceded by a
    `##` heading would have their code block absorbed into the Prerequisites span."""
    for i, line in enumerate(lines):
        if PREREQ_HEADING.match(line):
            for j in range(i + 1, len(lines)):
                if HEADING.match(lines[j]) or CSHARP_FENCE.match(lines[j]):
                    return i, j
            return i, len(lines)
    return None


def find_insertion_anchor(lines: list[str], fence_start: int) -> int:
    """Find the heading that immediately precedes the csharp fence — insert above it.
    Fall back to the fence index if no heading is found."""
    for i in range(fence_start - 1, -1, -1):
        if HEADING.match(lines[i]):
            return i
    return fence_start


def inject(recipe_path: Path, dry_run: bool) -> str:
    text = recipe_path.read_text(encoding='utf-8')
    lines = text.split('\n')
    fence = extract_csharp_block(lines)
    if fence is None:
        return 'no-csharp-block'
    fence_start, _, block = fence
    prereqs = detect_prereqs(block)

    existing = find_existing_prereq_span(lines)

    if not prereqs:
        if existing is not None:
            # No helpers in use but a stale Prerequisites section exists — remove it.
            start, end = existing
            # Also drop a trailing blank line to avoid double blanks.
            if end < len(lines) and lines[end - 1].strip() == '' and (end == len(lines) or lines[end].strip() == ''):
                end_drop = end
            else:
                end_drop = end
            new_lines = lines[:start] + lines[end_drop:]
            if not dry_run:
                recipe_path.write_text('\n'.join(new_lines), encoding='utf-8')
            return 'removed-stale'
        return 'no-prereqs'

    section = render_section(prereqs)

    # Target layout: <prefix, ending with one blank> <section, ending with one blank> <next heading onward>
    if existing is not None:
        start, end = existing
        prefix = lines[:start]
        tail = lines[end:]
        action = 'updated'
    else:
        anchor = find_insertion_anchor(lines, fence_start)
        prefix = lines[:anchor]
        tail = lines[anchor:]
        action = 'inserted'

    while prefix and prefix[-1].strip() == '':
        prefix.pop()
    new_lines = prefix + [''] + section + tail

    if not dry_run:
        recipe_path.write_text('\n'.join(new_lines), encoding='utf-8')
    return action


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument('--recipes', required=True, type=Path)
    ap.add_argument('--only', default=None,
                    help='Comma-separated list of recipe stems to process')
    ap.add_argument('--dry-run', action='store_true')
    args = ap.parse_args()

    only: set[str] | None = None
    if args.only:
        only = {s.strip() for s in args.only.split(',') if s.strip()}

    tallies: dict[str, int] = {}
    for md in sorted(args.recipes.rglob('*.md')):
        # Skip _shared templates and READMEs — these aren't recipes.
        if '_shared' in md.parts or md.name.lower() == 'readme.md':
            continue
        if only is not None and md.stem not in only:
            continue
        result = inject(md, args.dry_run)
        tallies[result] = tallies.get(result, 0) + 1
        if result not in ('no-prereqs',):
            print(f'  {result:<14} {md.relative_to(args.recipes.parent)}')

    print()
    print('summary:', ', '.join(f'{k}={v}' for k, v in sorted(tallies.items())))
    return 0


if __name__ == '__main__':
    sys.exit(main())
