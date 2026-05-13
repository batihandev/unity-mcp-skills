# Tooling Folder Design

Introduce `tooling/` as a first-class folder in the skill pack, parallel to `recipes/`. Separates install-once Editor script templates from run-once Unity_RunCommand recipes.

## Problem

`recipes/` has a strict contract: every file is a Unity_RunCommand IRunCommand template subject to compile and run validation gates. `UIScreenshotTools` is a `[MenuItem]` static class — an Editor script installed once into a project, not a one-shot RunCommand execution. It cannot satisfy the recipes contract and does not belong in `recipes/`.

## Solution

Add `tooling/<domain>/` folders. Each file documents an Editor script template to be installed via `Unity_CreateScript`. The folder ships to `main` alongside `recipes/`, `skills/`, and `references/`.

## Folder Structure

```
tooling/
  README.md
  ui/
    README.md
    ui_screenshot_tool.md      ← first entry, moved from recipes/ui/
```

Domain subfolders mirror `recipes/` naming conventions. New domains get their own subfolder.

## File Contract (Approach A)

Every `tooling/<domain>/<name>.md` follows this structure:

```markdown
# <tool_name>

<one-line description of what the tool does once installed>

## Install

**Path:** `Assets/<YourProject>/Scripts/Editor/<FileName>.cs`

Install via Unity_CreateScript with the C# content below.

## Post-install

- Menus available after compile: list the menu paths
- Wait for Unity to recompile before calling via ExecuteMenuItem. Poll EditorApplication.isCompiling first.

## Adapt

| What to change | Where |
|----------------|-------|
| Project-specific value | Location in code |

[C# code block — paste-ready content]

## Pitfalls

- Known failure modes
```

The C# block sits between `## Adapt` and `## Pitfalls`. Validation gate (compile + run) does NOT apply to `tooling/` files.

## Cross-cutting Changes

| File | Change |
|------|--------|
| `tooling/README.md` | New — explains folder, distinguishes from `recipes/`, states no validation gate |
| `tooling/ui/README.md` | New — domain table |
| `tooling/ui/ui_screenshot_tool.md` | New — `ui_screenshot_tool` reformatted to Approach A contract |
| `recipes/ui/ui_screenshot_tool.md` | Deleted |
| `recipes/ui/README.md` | Remove `ui_screenshot_tool` entry |
| `recipes/README.md` | Add pointer to `tooling/` |
| `skills/ui/SKILL.md` | Phase 3 prerequisite path updated: `recipes/ui/` → `tooling/ui/` |
| Root `SKILL.md` | Add `tooling` to domain map |

## Validation Tracker

`tools/tracker_next.py` and `tools/tracker_update.py` scan `recipes/`. Confirm during implementation that neither enumerates `tooling/`. If they do, add an explicit exclusion.

## Out of Scope

- Changes to `tools/` (dev-only, does not publish)
- Changes to `references/`
- Any other tooling scripts beyond `ui_screenshot_tool`
