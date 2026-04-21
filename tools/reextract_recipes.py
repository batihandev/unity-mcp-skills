#!/usr/bin/env python3
"""
Re-extract stubbed recipe bodies from the upstream SkillsForUnity C# source.

A recipe is "stubbed" if its C# block contains a `/* Original Logic: */` comment
with the real logic trapped inside. This script locates the matching upstream
method (via the `[UnitySkill("<skill_id>", ...)]` attribute), pulls its body,
rewrites `return X;` as `result.SetResult(X); return;` so it fits IRunCommand.Execute,
and writes the de-stubbed recipe back.

Usage:
    python3 tools/reextract_recipes.py --upstream /tmp/upstream-unity-skills \\
                                       --recipes recipes/ \\
                                       [--dry-run] [--only <skill_id>[,<skill_id>...]]

Expected upstream SHA: 55b03ef32de920f4f2d884c9eed1491a535c2ae5
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

SKILL_ATTR = re.compile(r'\[UnitySkill\("(?P<id>[^"]+)"')
METHOD_SIG = re.compile(
    r'public\s+static\s+(?:object|void|[A-Za-z_][A-Za-z0-9_<>.,\s]*?)\s+'
    r'(?P<name>[A-Za-z_][A-Za-z0-9_]*)\s*\('
)
CSHARP_FENCE_START = re.compile(r'^```csharp\s*$')
CSHARP_FENCE_END = re.compile(r'^```\s*$')
ORIGINAL_LOGIC_START = re.compile(r'/\*\s*Original Logic:\s*$')
ORIGINAL_LOGIC_END = re.compile(r'\*/\s*$')


def find_matching_brace(source: str, open_idx: int) -> int:
    """Given the index of '{', return the index of the matching '}'.
    Handles nested braces, strings, verbatim strings, chars, // comments, /* */ comments."""
    assert source[open_idx] == '{'
    depth = 0
    i = open_idx
    n = len(source)
    while i < n:
        c = source[i]
        if c == '/':
            if i + 1 < n and source[i+1] == '/':
                nl = source.find('\n', i)
                i = n if nl == -1 else nl + 1
                continue
            if i + 1 < n and source[i+1] == '*':
                end = source.find('*/', i + 2)
                i = n if end == -1 else end + 2
                continue
        elif c == '"':
            # Verbatim string @"..."?
            if i > 0 and source[i-1] == '@':
                j = i + 1
                while j < n:
                    if source[j] == '"':
                        if j + 1 < n and source[j+1] == '"':
                            j += 2
                            continue
                        break
                    j += 1
                i = j + 1
                continue
            # Interpolated verbatim $@"..." / @$"..."
            if i >= 2 and (source[i-2:i] in ('$@', '@$')):
                j = i + 1
                while j < n:
                    if source[j] == '"':
                        if j + 1 < n and source[j+1] == '"':
                            j += 2
                            continue
                        break
                    j += 1
                i = j + 1
                continue
            # Regular string "..."
            j = i + 1
            while j < n:
                if source[j] == '\\':
                    j += 2
                    continue
                if source[j] == '"':
                    break
                j += 1
            i = j + 1
            continue
        elif c == "'":
            # Char literal '.' or '\x'
            j = i + 1
            while j < n:
                if source[j] == '\\':
                    j += 2
                    continue
                if source[j] == "'":
                    break
                j += 1
            i = j + 1
            continue
        elif c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                return i
        i += 1
    raise ValueError(f"Unmatched brace starting at index {open_idx}")


def parse_upstream_methods(upstream_skills_dir: Path) -> dict[str, str]:
    """Walk *Skills.cs files and return {skill_id: method_body_without_outer_braces}."""
    methods: dict[str, str] = {}
    for cs_file in sorted(upstream_skills_dir.glob("*Skills.cs")):
        text = cs_file.read_text(encoding="utf-8")
        for attr_match in SKILL_ATTR.finditer(text):
            skill_id = attr_match.group("id")
            # attr_match.end() points just after [UnitySkill("<id>"
            # We need to skip to past the closing ')]' of this attribute, which may
            # contain nested new[] { ... } or array syntax. Track paren + bracket depth.
            i = attr_match.end()
            paren_depth = 1  # already inside UnitySkill(
            bracket_depth = 1  # already inside [ of [UnitySkill
            while i < len(text) and (paren_depth > 0 or bracket_depth > 0):
                ch = text[i]
                if ch == '"':
                    # Skip string literal
                    j = i + 1
                    while j < len(text):
                        if text[j] == '\\':
                            j += 2
                            continue
                        if text[j] == '"':
                            break
                        j += 1
                    i = j + 1
                    continue
                if ch == '(':
                    paren_depth += 1
                elif ch == ')':
                    paren_depth -= 1
                elif ch == '[':
                    bracket_depth += 1
                elif ch == ']':
                    bracket_depth -= 1
                i += 1
            # Skip subsequent attribute lines / whitespace until we land on a method signature.
            while i < len(text):
                while i < len(text) and text[i] in ' \t\r\n':
                    i += 1
                if i < len(text) and text[i] == '[':
                    # Another attribute — skip it with paren+bracket tracking.
                    i += 1
                    pd = 0
                    bd = 1
                    while i < len(text) and (pd > 0 or bd > 0):
                        ch = text[i]
                        if ch == '"':
                            j = i + 1
                            while j < len(text):
                                if text[j] == '\\':
                                    j += 2
                                    continue
                                if text[j] == '"':
                                    break
                                j += 1
                            i = j + 1
                            continue
                        if ch == '(':
                            pd += 1
                        elif ch == ')':
                            pd -= 1
                        elif ch == '[':
                            bd += 1
                        elif ch == ']':
                            bd -= 1
                        i += 1
                    continue
                break
            # Now at method signature. Match it.
            sig_match = METHOD_SIG.match(text, i)
            if not sig_match:
                # Probably a property or something we don't care about; skip
                continue
            # Find opening '{' of method body
            open_brace = text.find('{', sig_match.end())
            if open_brace == -1:
                continue
            close_brace = find_matching_brace(text, open_brace)
            body = text[open_brace + 1:close_brace]
            methods[skill_id] = body
    return methods


def dedent_body(body: str) -> str:
    """Normalize indentation: strip minimum leading whitespace common to non-blank lines."""
    lines = body.split('\n')
    # Trim leading/trailing blank lines but keep internal structure
    while lines and lines[0].strip() == '':
        lines.pop(0)
    while lines and lines[-1].strip() == '':
        lines.pop()
    if not lines:
        return ''
    min_indent = min(
        (len(line) - len(line.lstrip())) for line in lines if line.strip()
    )
    return '\n'.join(line[min_indent:] if len(line) >= min_indent else line for line in lines)


RETURN_STMT = re.compile(r'\breturn\b')


def _skip_strings_and_comments(source: str, i: int) -> int | None:
    """If source[i] starts a string/char/comment, return the index just past it.
    Otherwise return None. Handles //, /* */, "...", @"...", $"...", '...'."""
    n = len(source)
    c = source[i]
    if c == '/' and i + 1 < n:
        if source[i+1] == '/':
            nl = source.find('\n', i)
            return n if nl == -1 else nl
        if source[i+1] == '*':
            end = source.find('*/', i + 2)
            return n if end == -1 else end + 2
    if c == '"':
        # Handle preceding @ or $ or @$ / $@
        is_verbatim = i > 0 and source[i-1] == '@'
        is_interp_verbatim = i >= 2 and source[i-2:i] in ('@$', '$@')
        j = i + 1
        if is_verbatim or is_interp_verbatim:
            while j < n:
                if source[j] == '"':
                    if j + 1 < n and source[j+1] == '"':
                        j += 2
                        continue
                    return j + 1
                j += 1
            return n
        # Regular or interpolated string — track braces in interpolations
        is_interp = i > 0 and source[i-1] == '$'
        while j < n:
            if source[j] == '\\':
                j += 2
                continue
            if is_interp and source[j] == '{' and j + 1 < n and source[j+1] != '{':
                # Skip balanced {...} inside interpolation
                depth = 1
                j += 1
                while j < n and depth > 0:
                    inner = _skip_strings_and_comments(source, j)
                    if inner is not None:
                        j = inner
                        continue
                    if source[j] == '{': depth += 1
                    elif source[j] == '}': depth -= 1
                    j += 1
                continue
            if source[j] == '"':
                return j + 1
            j += 1
        return n
    if c == "'":
        j = i + 1
        while j < n:
            if source[j] == '\\':
                j += 2
                continue
            if source[j] == "'":
                return j + 1
            j += 1
        return n
    return None


def _find_statement_end(source: str, start: int) -> int:
    """Starting at a position right after the `return` keyword, find the index of
    the terminating `;` at brace/paren depth 0 (relative to start). Returns the
    index of the `;`, or -1 if not found."""
    n = len(source)
    i = start
    paren = 0
    brace = 0
    bracket = 0
    while i < n:
        skipped = _skip_strings_and_comments(source, i)
        if skipped is not None:
            i = skipped
            continue
        c = source[i]
        if c == '(': paren += 1
        elif c == ')': paren -= 1
        elif c == '{': brace += 1
        elif c == '}': brace -= 1
        elif c == '[': bracket += 1
        elif c == ']': bracket -= 1
        elif c == ';' and paren == 0 and brace == 0 and bracket == 0:
            return i
        i += 1
    return -1


def transform_returns(body: str) -> tuple[str, int]:
    """Rewrite every `return <expr>;` at statement level as
    `result.SetResult(<expr>); return;`. Leaves bare `return;` alone.
    Handles multi-line anonymous types and nested braces via a tokenizer
    that respects strings, char literals, and comments."""
    out = []
    i = 0
    n = len(body)
    count = 0
    while i < n:
        skipped = _skip_strings_and_comments(body, i)
        if skipped is not None:
            out.append(body[i:skipped])
            i = skipped
            continue
        # Match the keyword `return` at a word boundary, followed by whitespace/newline.
        if body[i:i+6] == 'return' and (i == 0 or not (body[i-1].isalnum() or body[i-1] == '_')):
            tail = i + 6
            if tail < n and (body[tail].isalnum() or body[tail] == '_'):
                out.append(body[i])
                i += 1
                continue
            # Find the terminating `;`
            # Skip whitespace after 'return'
            j = tail
            while j < n and body[j] in ' \t\r\n':
                j += 1
            if j < n and body[j] == ';':
                # Bare `return;` — keep
                out.append(body[i:j+1])
                i = j + 1
                continue
            end = _find_statement_end(body, tail)
            if end < 0:
                out.append(body[i])
                i += 1
                continue
            expr = body[tail:end].strip()
            # Wrap in braces so single-statement `if`/`else`/`case` bodies that
            # held a `return <expr>;` still contain both emitted statements.
            # Without braces, the trailing `return;` escapes the conditional.
            replacement = f'{{ result.SetResult({expr}); return; }}'
            out.append(replacement)
            i = end + 1
            count += 1
            continue
        out.append(body[i])
        i += 1
    return ''.join(out), count


def reindent(body: str, indent: str) -> str:
    return '\n'.join((indent + line if line.strip() else line) for line in body.split('\n'))


def rewrite_recipe(recipe_path: Path, method_body: str) -> tuple[bool, str]:
    """Rewrite recipe file, replacing the stubbed Execute body with re-extracted logic.
    Returns (changed, reason)."""
    text = recipe_path.read_text(encoding="utf-8")
    lines = text.split('\n')

    # Find the first csharp fenced block that looks stubbed — either the classic
    # `/* Original Logic: */` comment form, or the newer `UnitySkillsBridge.Call(...)`
    # placeholder emitted by a prior extractor pass.
    fence_start = fence_end = None
    stub_kind: str | None = None
    for i, line in enumerate(lines):
        if CSHARP_FENCE_START.match(line):
            for j in range(i + 1, len(lines)):
                if CSHARP_FENCE_END.match(lines[j]):
                    block = '\n'.join(lines[i+1:j])
                    if 'Original Logic:' in block:
                        fence_start, fence_end = i, j
                        stub_kind = 'original_logic'
                    elif 'UnitySkillsBridge.Call' in block:
                        fence_start, fence_end = i, j
                        stub_kind = 'bridge_call'
                    break
            if fence_start is not None:
                break
    if fence_start is None:
        return False, 'no stubbed csharp block'

    block = lines[fence_start + 1:fence_end]

    # Find 'public void Execute(ExecutionResult result)' line
    exec_line = None
    for i, l in enumerate(block):
        if 'public void Execute(ExecutionResult result)' in l:
            exec_line = i
            break
    if exec_line is None:
        return False, 'no Execute method in csharp block'

    # Find '{' of Execute (usually next non-blank line)
    exec_open = None
    for i in range(exec_line + 1, len(block)):
        if block[i].strip() == '{':
            exec_open = i
            break
    if exec_open is None:
        return False, 'no opening brace for Execute'

    # Find matching close of Execute
    exec_close = None
    depth = 1
    for i in range(exec_open + 1, len(block)):
        s = block[i]
        # naive brace counter on lines (good enough for these method bodies)
        for ch in s:
            if ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
                if depth == 0:
                    exec_close = i
                    break
        if exec_close is not None:
            break
    if exec_close is None:
        return False, 'no closing brace for Execute'

    # Within the Execute body, find the 'Original Logic:' comment span and the
    # parameter-defaults preamble (lines before the comment that are legit local
    # variable declarations — we keep those).
    inner = block[exec_open + 1:exec_close]

    if stub_kind == 'original_logic':
        orig_start = orig_end = None
        for i, l in enumerate(inner):
            if ORIGINAL_LOGIC_START.search(l):
                orig_start = i
            elif orig_start is not None and ORIGINAL_LOGIC_END.search(l):
                orig_end = i
                break
        if orig_start is None or orig_end is None:
            return False, 'no /* Original Logic: */ block'

        preamble = inner[:orig_start]
    else:
        # UnitySkillsBridge.Call stub — discard the entire body; it's a placeholder
        # that doesn't reflect any parameter-example preamble worth keeping.
        preamble = []

    # Trim trailing blank lines and any '// TODO: Replace parameters with your actual logic' banner
    while preamble and (preamble[-1].strip() == '' or '// TODO:' in preamble[-1]):
        preamble.pop()

    # Drop illegal literal-named placeholders: lines of the form
    # `  <type> "Literal" = default;` or `  <type> 123 = default;` or `  <type> true = default;`
    cleaned_preamble: list[str] = []
    placeholder_pat = re.compile(r'^\s*\w+\s+("[^"]*"|\d+|\bfalse\b|\btrue\b|\bnull\b)\s*=\s*default\s*;')
    for line in preamble:
        if placeholder_pat.match(line):
            continue
        cleaned_preamble.append(line)
    preamble = cleaned_preamble

    # Transform method body
    dedented = dedent_body(method_body)
    transformed, return_count = transform_returns(dedented)

    # Determine the Execute body indent (match first non-empty original line or default 8-space)
    exec_indent = '        '  # 8 spaces — matches existing pattern
    if inner:
        for l in inner:
            if l.strip():
                leading = l[:len(l) - len(l.lstrip())]
                if leading:
                    exec_indent = leading
                break

    transformed_indented = reindent(transformed, exec_indent)

    # Rebuild Execute body
    new_inner: list[str] = []
    new_inner.extend(preamble)
    if preamble and preamble[-1].strip() != '':
        new_inner.append('')
    new_inner.extend(transformed_indented.split('\n'))

    new_block = block[:exec_open + 1] + new_inner + block[exec_close:]
    new_lines = lines[:fence_start + 1] + new_block + lines[fence_end:]
    new_text = '\n'.join(new_lines)
    recipe_path.write_text(new_text, encoding="utf-8")
    return True, f'rewrote with {return_count} return→SetResult transforms'


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--upstream", required=True, type=Path,
                    help="Path to cloned upstream Unity-Skills repo")
    ap.add_argument("--recipes", required=True, type=Path,
                    help="Path to recipes/ directory")
    ap.add_argument("--only", default=None,
                    help="Comma-separated list of skill IDs to process (default: all stubbed)")
    ap.add_argument("--dry-run", action="store_true",
                    help="Report what would change; don't write")
    args = ap.parse_args()

    upstream_skills = args.upstream / "SkillsForUnity" / "Editor" / "Skills"
    if not upstream_skills.is_dir():
        print(f"error: upstream skills dir not found: {upstream_skills}", file=sys.stderr)
        return 2

    methods = parse_upstream_methods(upstream_skills)
    print(f"parsed {len(methods)} skill methods from upstream")

    only_ids: set[str] | None = None
    if args.only:
        only_ids = {s.strip() for s in args.only.split(',') if s.strip()}

    stubbed = []
    for md in args.recipes.rglob("*.md"):
        text = md.read_text(encoding="utf-8", errors="ignore")
        if "Original Logic:" in text or "UnitySkillsBridge.Call" in text:
            stubbed.append(md)

    print(f"found {len(stubbed)} stubbed recipes")

    wrote = skipped = missing = failed = 0
    for md in stubbed:
        skill_id = md.stem
        if only_ids is not None and skill_id not in only_ids:
            continue
        body = methods.get(skill_id)
        if body is None:
            print(f"  MISS  {skill_id}: no upstream method")
            missing += 1
            continue
        if args.dry_run:
            print(f"  would rewrite: {skill_id}")
            wrote += 1
            continue
        try:
            ok, reason = rewrite_recipe(md, body)
            if ok:
                print(f"  ok    {skill_id}: {reason}")
                wrote += 1
            else:
                print(f"  skip  {skill_id}: {reason}")
                skipped += 1
        except Exception as e:
            print(f"  FAIL  {skill_id}: {e}")
            failed += 1

    print()
    print(f"summary: wrote={wrote} skipped={skipped} missing={missing} failed={failed}")
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
