#!/usr/bin/env python3
"""Task 22: recipe prose cleanup pass.

Removes verbose prose sections from every non-retired recipe:
  1. ## Recipe / ## C# Template / ## Unity_RunCommand Template headers (h2 line only)
  2. ## Parameters section (entire) if **Signature:** inline exists in preamble
  3. ## Returns section  (entire) if **Returns:**  inline exists in preamble
  4. ## Notes section (per-bullet filtering — drop trivially obvious bullets;
     remove section entirely if no bullets survive)

Does NOT modify csharp code block content.

Usage:
    python3 tools/prose_cleanup.py --recipes recipes/ [--dry-run] [--stats]
"""
from __future__ import annotations

import argparse
import hashlib
import os
import re
import sys
from pathlib import Path

# Patterns for trivially-obvious Notes bullets to drop
_DROP_PATTERNS = [
    re.compile(r"^`[^`]+`\s+is\s+required[\.\s]"),           # `x` is required.
    re.compile(r"^`[^`]+`\s+is\s+optional[\.\s]"),           # `x` is optional.
    re.compile(r"^Returns\s+an\s+error\s+if"),               # Returns an error if ...
    re.compile(r"^Fails\s+if"),                              # Fails if ...
    re.compile(r"^Read-only"),                               # Read-only: ...
    re.compile(r"^`[^`]+`\s+defaults\s+to\s+`[^`]+`\s*[\.\(—]"),  # `x` defaults to `y`.
    re.compile(r"^`[^`]+`\s+is\s+the\s+(?:asset\s+)?path"),  # `x` is the path ...
    re.compile(r"^`[^`]+`\s+is\s+the\s+name"),              # `x` is the name ...
    re.compile(r"^`[^`]+`\s+is\s+a\s+"),                    # `x` is a string/int/...
    re.compile(r"^`[^`]+`\s+must\s+be\s+a\s+valid\s+`Assets/`"),  # path must be Assets/
]

# Metadata lines to strip (old importer/perception format)
_META_LINES = re.compile(
    r"^\*\*(Skill ID|Skill|Source|C# method):\*\*[^\n]*\n",
    re.MULTILINE,
)

# Section headers to remove (h2 line only — not content)
_RECIPE_HEADERS = re.compile(
    r"^## (Recipe|C# Template|Unity_RunCommand Template|Unity RunCommand Template|RunCommand Recipe)\n",
    re.MULTILINE,
)

# Boundary lookahead shared across all section removals
_SECTION_BOUNDARY = r"(?=^##|^\*\*Prerequisites:|^\*\*Signature:|^\*\*Returns:|^\*\*Note[s]?:|\x00CODEBLOCK|\Z)"

# Full ## Parameters section (h2 + content until boundary)
_PARAMS_SECTION = re.compile(
    r"^## Parameters\n.*?" + _SECTION_BOUNDARY,
    re.MULTILINE | re.DOTALL,
)

# Full ## Returns / Return Shape section (h2 + content until boundary)
_RETURNS_SECTION = re.compile(
    r"^## Return(?:s| Shape)\n.*?" + _SECTION_BOUNDARY,
    re.MULTILINE | re.DOTALL,
)

# Full ## Notes section (h2 + content until boundary)
_NOTES_SECTION = re.compile(
    r"^## Notes\n(.*?)" + _SECTION_BOUNDARY,
    re.MULTILINE | re.DOTALL,
)

# ## Signature section exists (old format — importer/perception)
_SIGNATURE_SECTION = re.compile(r"^## Signature\n", re.MULTILINE)

# Inline markers
_INLINE_RETURNS   = re.compile(r"^\*\*Returns:\*\*",    re.MULTILINE)
_INLINE_SIGNATURE = re.compile(r"^\*\*Signature:\*\*",  re.MULTILINE)

# All fenced code blocks (to mask before processing)
_CODE_BLOCK = re.compile(r"(```[^\n]*\n.*?```)", re.DOTALL)


def _mask_code_blocks(content: str) -> tuple[str, list[str]]:
    """Replace fenced code blocks with placeholders; return (masked, originals)."""
    blocks: list[str] = []
    def replacer(m: re.Match) -> str:
        idx = len(blocks)
        blocks.append(m.group(1))
        return f"\x00CODEBLOCK{idx}\x00"
    masked = _CODE_BLOCK.sub(replacer, content)
    return masked, blocks


def _restore_code_blocks(masked: str, blocks: list[str]) -> str:
    for idx, block in enumerate(blocks):
        masked = masked.replace(f"\x00CODEBLOCK{idx}\x00", block)
    return masked


def _filter_notes(notes_body: str) -> str | None:
    """Return filtered notes body, or None if nothing survives."""
    kept = []
    for line in notes_body.strip().splitlines():
        stripped = line.lstrip("- ").lstrip("* ").strip()
        if not stripped:
            continue
        if any(p.match(stripped) for p in _DROP_PATTERNS):
            continue
        kept.append(line)
    if not kept:
        return None
    return "\n".join(kept) + "\n"


def process(content: str) -> tuple[str, list[str]]:
    """Return (cleaned_content, list_of_changes_applied)."""
    changes: list[str] = []

    # Mask code blocks so regexes can't touch them
    masked, blocks = _mask_code_blocks(content)

    # 0. Strip old-format metadata lines (**Skill ID:**, **Source:**, etc.)
    new_masked, n = _META_LINES.subn("", masked)
    if n:
        changes.append(f"removed {n} metadata line(s) (Skill ID / Source / C# method)")
    masked = new_masked

    # 1. Remove recipe section headers (h2 line only)
    new_masked, n = _RECIPE_HEADERS.subn("", masked)
    if n:
        changes.append(f"removed {n} recipe-section h2 header(s)")
    masked = new_masked

    # 2. Remove ## Parameters if **Signature:** inline OR ## Signature section exists
    has_sig = _INLINE_SIGNATURE.search(masked) or _SIGNATURE_SECTION.search(masked)
    if has_sig and _PARAMS_SECTION.search(masked):
        masked = _PARAMS_SECTION.sub("", masked)
        changes.append("removed ## Parameters section (redundant with Signature)")

    # 3. Remove ## Returns / Return Shape if **Returns:** inline exists
    if _INLINE_RETURNS.search(masked) and _RETURNS_SECTION.search(masked):
        masked = _RETURNS_SECTION.sub("", masked)
        changes.append("removed ## Returns section (redundant with inline Returns)")

    # 4. Filter ## Notes bullets
    m = _NOTES_SECTION.search(masked)
    if m:
        filtered = _filter_notes(m.group(1))
        if filtered is None:
            masked = _NOTES_SECTION.sub("", masked)
            changes.append("removed ## Notes section (all bullets trivial)")
        elif filtered.strip() != m.group(1).strip():
            new_section = "## Notes\n" + filtered + "\n"
            masked = masked[:m.start()] + new_section + masked[m.end():]
            changes.append("trimmed ## Notes section (dropped trivial bullets)")

    # Collapse multiple blank lines → two max
    masked = re.sub(r"\n{3,}", "\n\n", masked)

    # Restore code blocks
    result = _restore_code_blocks(masked, blocks)

    # Safety check: blocks must be bit-identical
    _, orig_blocks = _mask_code_blocks(content)
    _, new_blocks  = _mask_code_blocks(result)
    if orig_blocks != new_blocks:
        raise ValueError("csharp block content changed — aborting this file")

    return result, changes


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--recipes", default="recipes", help="recipes root dir")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--stats", action="store_true", help="print per-file line delta")
    ap.add_argument("--only", help="comma-separated recipe stems to process")
    args = ap.parse_args()

    only = set(args.only.split(",")) if args.only else None
    root = Path(args.recipes)

    total_files = 0
    total_changed = 0
    total_lines_before = 0
    total_lines_after = 0
    errors: list[str] = []

    for md in sorted(root.rglob("*.md")):
        if md.name == "README.md":
            continue
        if md.parent.name == "_shared":
            continue
        stem = md.stem
        if only and stem not in only:
            continue

        original = md.read_text(encoding="utf-8")
        lines_before = original.count("\n")
        total_lines_before += lines_before

        try:
            cleaned, changes = process(original)
        except ValueError as e:
            errors.append(f"{md}: {e}")
            total_lines_after += lines_before
            continue

        lines_after = cleaned.count("\n")
        total_lines_after += lines_after
        total_files += 1

        if changes:
            total_changed += 1
            delta = lines_after - lines_before
            if args.stats or args.dry_run:
                print(f"{md} ({delta:+d} lines): {'; '.join(changes)}")
            if not args.dry_run:
                md.write_text(cleaned, encoding="utf-8")

    print(f"\nProcessed {total_files} files, changed {total_changed}")
    print(f"Total lines: {total_lines_before} → {total_lines_after} "
          f"(delta {total_lines_after - total_lines_before:+d}, "
          f"{100*(total_lines_before - total_lines_after)/max(total_lines_before,1):.1f}% reduction)")
    if errors:
        print(f"\nERRORS ({len(errors)}):")
        for e in errors:
            print(f"  {e}")
    if errors:
        sys.exit(1)


if __name__ == "__main__":
    main()
