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
    ProjectSkills.*                     -> _shared/project_skills.md
    PerceptionHelpers.* | _SceneMetricsSnapshot | _SceneHotspot
                                        -> _shared/perception_helpers.md (+ gameobject_finder)

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
COMPACT_PREREQ = re.compile(r'^\*\*Prerequisites:\*\*')

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
    (re.compile(r'\bProjectSkills\.\w+|\bRenderPipelineType\b'), 'project_skills.md',
     'for `ProjectSkills.*` / `RenderPipelineType`'),
    (re.compile(r'\bPerceptionHelpers\.\w+|\b_SceneMetricsSnapshot\b|\b_SceneHotspot\b'),
     'perception_helpers.md',
     'for `PerceptionHelpers.*` / scene metric + hotspot types'),
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
    needs_gameobject_finder = False
    for pat, fname, purpose in DETECTORS:
        if pat.search(csharp_text):
            if fname in seen:
                continue
            found.append((fname, purpose))
            seen.add(fname)
            if fname == 'component_type_finder.md':
                needs_skills_common = True
            if fname == 'perception_helpers.md':
                needs_gameobject_finder = True
    if needs_skills_common and 'skills_common.md' not in seen:
        found.append(('skills_common.md',
                      'required by `component_type_finder.md` for `SkillsCommon.GetAllLoadedTypes`'))
        seen.add('skills_common.md')
    if needs_gameobject_finder and 'gameobject_finder.md' not in seen:
        found.append(('gameobject_finder.md',
                      'required by `perception_helpers.md` for `GameObjectFinder.GetSceneObjects` / `GetDepth` / `GetCachedPath`'))
        seen.add('gameobject_finder.md')
    return found


def render_section(prereqs: list[tuple[str, str]]) -> list[str]:
    """Compact single-line form:
    **Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)
    """
    links = [f'[`{fname[:-3]}`](../_shared/{fname})' for fname, _ in prereqs]
    return [f'**Prerequisites:** {", ".join(links)}', '']


def find_existing_prereq_span(lines: list[str]) -> tuple[int, int] | None:
    """Return (start_idx, end_idx_exclusive) of an existing Prerequisites section.
    Handles both the compact single-line form (`**Prerequisites:** [...]`) and the
    legacy `## Prerequisites` + description + bullets form. Legacy span ends at the
    last bullet so footers like `**Requires:** ...` between bullets and next heading
    are preserved."""
    # Compact form
    for i, line in enumerate(lines):
        if COMPACT_PREREQ.match(line.strip()):
            return i, i + 1
    # Legacy section form
    for i, line in enumerate(lines):
        if PREREQ_HEADING.match(line):
            saw_bullet = False
            last_content = i  # heading itself
            j = i + 1
            while j < len(lines):
                stripped = lines[j].strip()
                if HEADING.match(lines[j]) or CSHARP_FENCE.match(lines[j]):
                    break
                if stripped.startswith('- '):
                    saw_bullet = True
                    last_content = j
                elif stripped == '':
                    pass
                elif not saw_bullet and stripped.startswith('Concatenate '):
                    last_content = j
                elif not saw_bullet:
                    last_content = j
                else:
                    break
                j += 1
            return i, last_content + 1
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
    # section already ends with a blank; drop any leading blanks from tail to avoid doubling
    while tail and tail[0].strip() == '':
        tail.pop(0)
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
