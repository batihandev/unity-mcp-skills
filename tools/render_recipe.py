#!/usr/bin/env python3
"""Render a recipe's full Unity_RunCommand payload: concatenate `_shared/*` helper
paste-ins with the recipe's `CommandScript` csharp block. Output goes to stdout.

Helpers are discovered by parsing the recipe's `## Prerequisites` bullets and
reading each referenced `_shared/*.md` file. Each helper's C# contribution is
the first fenced csharp block AFTER the `## Paste-in` heading.

Usage:
    python3 tools/render_recipe.py <recipe_path>            # run-mode payload
    python3 tools/render_recipe.py <recipe_path> --comp     # compile-only (body in if(false))
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
PREREQ_BULLET = re.compile(r'^-\s+`(recipes/_shared/[^`]+\.md)`')
CSHARP_FENCE = re.compile(r'^```csharp\s*$')
PASTE_IN_HEADING = re.compile(r'^##\s+Paste-in\s*$', re.IGNORECASE)


def extract_csharp_blocks(lines: list[str]) -> list[tuple[int, int, str]]:
    """All (fence_start_line, fence_end_line, text_without_fences) triples."""
    out: list[tuple[int, int, str]] = []
    i = 0
    while i < len(lines):
        if CSHARP_FENCE.match(lines[i]):
            for j in range(i + 1, len(lines)):
                if lines[j].strip() == '```':
                    out.append((i, j, '\n'.join(lines[i + 1:j])))
                    i = j + 1
                    break
            else:
                i += 1
        else:
            i += 1
    return out


def helper_block(helper_path: Path) -> str:
    """Return the first csharp block after `## Paste-in` in the helper file."""
    lines = helper_path.read_text(encoding='utf-8').split('\n')
    for idx, line in enumerate(lines):
        if PASTE_IN_HEADING.match(line):
            rest = lines[idx + 1:]
            for fs, fe, text in extract_csharp_blocks(rest):
                return text
            break
    blocks = extract_csharp_blocks(lines)
    if blocks:
        return blocks[0][2]
    raise ValueError(f'{helper_path} has no csharp block')


def parse_prereqs(recipe_lines: list[str]) -> list[Path]:
    in_section = False
    paths: list[Path] = []
    for line in recipe_lines:
        if line.strip().startswith('## '):
            if in_section:
                break
            in_section = line.strip().lower() == '## prerequisites'
            continue
        if in_section:
            m = PREREQ_BULLET.match(line.strip())
            if m:
                paths.append(REPO / m.group(1))
    return paths


EXECUTE_SIG = re.compile(r'public\s+void\s+Execute\s*\(\s*ExecutionResult\s+result\s*\)')


def wrap_execute_in_if_false(csharp: str) -> str:
    """Wrap the Execute method body in `if (false) { ... }` so it compiles but
    does not run any side effects. Preserves everything outside Execute.
    Uses a brace-balanced walk so nested braces inside the body do not confuse
    the match."""
    m = EXECUTE_SIG.search(csharp)
    if not m:
        return csharp
    # Find first '{' after the signature that opens Execute's body.
    i = csharp.find('{', m.end())
    if i < 0:
        return csharp
    depth = 1
    j = i + 1
    n = len(csharp)
    while j < n and depth > 0:
        c = csharp[j]
        if c == '/' and j + 1 < n and csharp[j + 1] == '/':
            nl = csharp.find('\n', j)
            j = n if nl < 0 else nl + 1
            continue
        if c == '/' and j + 1 < n and csharp[j + 1] == '*':
            end = csharp.find('*/', j + 2)
            j = n if end < 0 else end + 2
            continue
        if c == '"':
            # skip string
            k = j + 1
            is_verbatim = j > 0 and csharp[j - 1] == '@'
            while k < n:
                if csharp[k] == '\\' and not is_verbatim:
                    k += 2
                    continue
                if csharp[k] == '"':
                    if is_verbatim and k + 1 < n and csharp[k + 1] == '"':
                        k += 2
                        continue
                    break
                k += 1
            j = k + 1
            continue
        if c == "'":
            k = j + 1
            while k < n:
                if csharp[k] == '\\':
                    k += 2
                    continue
                if csharp[k] == "'":
                    break
                k += 1
            j = k + 1
            continue
        if c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                body = csharp[i + 1:j]
                head = csharp[:i + 1]
                tail = csharp[j:]
                return head + '\n        if (false)\n        {' + body + '        }\n    ' + tail
        j += 1
    return csharp


def render(recipe_path: Path, comp: bool) -> str:
    lines = recipe_path.read_text(encoding='utf-8').split('\n')
    prereqs = parse_prereqs(lines)
    helper_texts = [helper_block(p) for p in prereqs]

    recipe_blocks = extract_csharp_blocks(lines)
    if not recipe_blocks:
        raise ValueError(f'{recipe_path} has no csharp block')
    recipe_text = recipe_blocks[0][2]

    # Merge: keep the recipe's `using` lines at the top, then helper classes, then CommandScript.
    using_lines: list[str] = []
    body_lines: list[str] = []
    for line in recipe_text.split('\n'):
        stripped = line.strip()
        if stripped.startswith('using ') and stripped.endswith(';'):
            if line not in using_lines:
                using_lines.append(line)
        else:
            body_lines.append(line)
    body = '\n'.join(body_lines).strip('\n')
    if comp:
        body = wrap_execute_in_if_false(body)

    parts = []
    if using_lines:
        parts.append('\n'.join(using_lines))
        parts.append('')
    for h in helper_texts:
        parts.append(h.rstrip())
        parts.append('')
    parts.append(body)
    return '\n'.join(parts) + '\n'


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('recipe', type=Path)
    ap.add_argument('--comp', action='store_true', help='Wrap Execute body in if(false) for compile-only smoke')
    args = ap.parse_args()

    recipe = args.recipe
    if not recipe.is_absolute():
        recipe = (Path.cwd() / recipe).resolve()
    print(render(recipe, comp=args.comp), end='')
    return 0


if __name__ == '__main__':
    sys.exit(main())
