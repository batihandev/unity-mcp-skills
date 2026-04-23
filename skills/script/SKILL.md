---
name: unity-script
description: "Use when creating, reading, editing, searching, validating, or deleting C# scripts in a Unity project."
---

# Script Management

**Do not use `Unity_RunCommand` for script operations.** The Unity MCP server exposes dedicated tools for every common script operation. Use them directly — they handle Domain Reload coordination and diagnostics natively.

## Routing

| Operation | Tool | Typical use |
|---|---|---|
| Create a new C# script | `Unity_CreateScript` | Writes a new file under `Assets/`; sets up the MonoBehaviour/ScriptableObject/Editor template. |
| Delete a C# script | `Unity_DeleteScript` | By URI (`unity://path/...`) or by `Assets/...` path. |
| List script files | `Unity_ListResources` | Commonly filtered to `*.cs` under `Assets/`. |
| Search inside a script | `Unity_FindInFile` | Regex-level matches with line numbers. |
| Structured edits | `Unity_ScriptApplyEdits` | Method-level replace / insert / remove. Prefer this over raw text edits. |
| Syntax + diagnostics check | `Unity_ValidateScript` | Call before and after edits; surfaces compile errors before they trigger a Domain Reload. |
| Hash for concurrency | `Unity_GetSha` | Compare against your expected hash to detect conflicting edits before writing. |

## Filename must match class name

The `.cs` filename (without extension) must exactly match the top-level class name. Unity's asset database relies on the 1:1 mapping — mismatches silently break MonoBehaviour attachment and asset references.

- When creating a script, pass the `scriptName` **without** `.cs`.
- When renaming a class, rename the file in the same operation.

## Domain Reload coordination

After any script create / edit / delete, Unity triggers a Domain Reload (compilation + assembly swap). Tools return a `compilation` block where applicable — check it before issuing the next operation.

1. If the response reports `isCompiling: true`, wait for it to settle before the next script edit.
2. Once compilation completes, call `Unity_ValidateScript` on the affected file to surface any errors.
3. Group related edits in a single task where possible — fewer edits = fewer Domain Reloads.

## Style guardrails

- Meaningful, domain-specific class names.
- Group scripts by feature under `Assets/Scripts/<Feature>/`.
- Decide the class role first: `MonoBehaviour`, `ScriptableObject`, or plain C# helper.
- Prefer explicit dependencies + small responsibilities + event-driven notifications over hidden globals.
- Avoid `Update`-loop polling, repeated `GameObject.Find` calls, reflection in hot paths, avoidable allocations.
- Start from the minimum structure that solves the need — don't dump boilerplate.
- After any edit, run `Unity_ValidateScript` and fix reported errors before marking a task done.
