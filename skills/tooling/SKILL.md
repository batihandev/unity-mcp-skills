---
name: unity-tooling
description: "Use when about to write or install a custom Editor script, add a [MenuItem], build a custom Editor window, or create a one-off Editor helper — to check whether a ready-to-paste template already exists in tooling/<domain>/ before reaching for Unity_CreateScript."
---

# Tooling Index

Install-once Editor script templates live under `../../tooling/<domain>/`. Check here before writing a new Editor script from scratch.

## When to Use

Reach for `tooling/` when the task needs a persistent Editor-side helper rather than a one-shot `Unity_RunCommand` body. Signals:

- About to paste a `[MenuItem("Tools/...")]` script via `Unity_CreateScript`.
- Need a tool the user re-runs from the Editor menu later (screenshots, exporters, custom build steps, batch operations).
- A recipe in `recipes/<domain>/` carries a "Prerequisite: install Editor script" note pointing at `tooling/<domain>/`.

Do NOT use for one-shot operations — those belong in `recipes/<domain>/` and run through `Unity_RunCommand`.

## Routing

1. Open `../../tooling/<domain>/README.md` for the domain that matches the task (e.g. `tooling/ui/`).
2. If a matching tool exists, install via `Unity_CreateScript` at the path the tool file specifies.
3. Wait for Unity to recompile — poll `EditorApplication.isCompiling` (see UI Authoring Loop Phase 1 in `../ui/SKILL.md`).
4. Call the installed menu items via `EditorApplication.ExecuteMenuItem`.

If no tool covers the case, write one — but check first.

## Available domains

| Domain | Folder | Tools |
|--------|--------|-------|
| ui | [`../../tooling/ui/`](../../tooling/ui/README.md) | `ui_screenshot_tool` |

## Common Mistakes

- Pasting a `tooling/` C# block into `Unity_RunCommand`. These are install-once Editor scripts, not `IRunCommand` bodies — they will not compile in the RunCommand context. Always install via `Unity_CreateScript` at the path the tool file specifies.
- Calling `EditorApplication.ExecuteMenuItem` immediately after install. Unity needs to recompile the new script before the menu items register. Poll `isCompiling` first.
- Treating `tooling/` files as compile/run-gated like `recipes/`. They have no validation gate — install into a scratch project once before trusting at scale.
