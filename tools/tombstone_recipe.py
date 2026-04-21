#!/usr/bin/env python3
"""Replace a recipe file's contents with a retirement tombstone.

A tombstone keeps the recipe name + short description + a pointer to the
replacing MCP tool or recipe. The file stays on disk so external links /
agents that land here are redirected, not 404'd.

Usage:
    python3 tools/tombstone_recipe.py \\
        --recipe recipes/package/package_check.md \\
        --mcp-tool Unity_PackageManager_GetData \\
        --summary "Check whether a specific package is installed." \\
        --hint '{ "packageID": "com.unity.cinemachine", "installedOnly": true }'

    python3 tools/tombstone_recipe.py \\
        --recipe recipes/sample/create_cube.md \\
        --redirect-recipe recipes/gameobject/gameobject_create.md \\
        --summary "Create a cube primitive." \\
        --hint 'Pass `primitiveType = "Cube"` to gameobject_create.'

Exactly one of `--mcp-tool` or `--redirect-recipe` is required.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

TEMPLATE_MCP = """# {stem}

{summary}

> **This recipe is a redirect. Use the native Unity MCP tool instead.**

## Use this instead

**MCP tool:** `{mcp_tool}`

{hint_block}See `mcp-tools.md` in the repo root for the full MCP tool surface.
"""

TEMPLATE_REDIRECT = """# {stem}

{summary}

> **This recipe is a redirect. Use the recipe below instead.**

## Use this instead

**Recipe:** [`{redirect}`]({redirect})

{hint_block}"""


def format_hint_block(hint: str | None) -> str:
    if not hint:
        return ""
    if hint.strip().startswith("{") or hint.strip().startswith("["):
        return f"Example payload:\n\n```json\n{hint.strip()}\n```\n\n"
    return f"{hint.strip()}\n\n"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--recipe", required=True, type=Path)
    ap.add_argument("--summary", required=True,
                    help="One-line description of what the recipe did.")
    ap.add_argument("--hint", default=None,
                    help="Example payload (JSON → fenced block) or short note.")
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--mcp-tool", default=None,
                   help="Native MCP tool name (e.g. Unity_PackageManager_GetData).")
    g.add_argument("--redirect-recipe", default=None,
                   help="Repo-rooted recipe path to redirect to (e.g. recipes/gameobject/gameobject_create.md).")
    args = ap.parse_args()

    if not args.recipe.is_file():
        print(f"error: recipe file not found: {args.recipe}", file=sys.stderr)
        return 1

    stem = args.recipe.stem
    hint_block = format_hint_block(args.hint)

    if args.mcp_tool:
        body = TEMPLATE_MCP.format(
            stem=stem, summary=args.summary.strip(),
            mcp_tool=args.mcp_tool, hint_block=hint_block,
        )
    else:
        body = TEMPLATE_REDIRECT.format(
            stem=stem, summary=args.summary.strip(),
            redirect=args.redirect_recipe, hint_block=hint_block,
        )

    args.recipe.write_text(body, encoding="utf-8")
    print(f"tombstoned {args.recipe} → {args.mcp_tool or args.redirect_recipe}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
