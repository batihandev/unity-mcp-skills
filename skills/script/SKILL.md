---
name: unity-script
description: "Use when creating, reading, replacing, or analyzing C# scripts in a Unity project."
---

# Unity Script Skills

## Native Tool First

Use the dedicated MCP script tools (`Unity_CreateScript`, `Unity_ScriptApplyEdits`, `script_create`, `script_replace`, `script_append`, `script_get_compile_feedback`, etc.) directly. Do NOT route script operations through `Unity_RunCommand` unless no dedicated script tool covers the need.

Use `script_create_batch` when creating 2 or more scripts in one task to minimize Domain Reloads.

## Filename Must Match Class Name

The script filename (without `.cs`) must exactly match the C# class name. Unity's asset database relies on this 1:1 mapping — mismatches silently break MonoBehaviour attachment and asset references.

- `scriptName` parameter must NOT include the `.cs` extension.
- When renaming a class, always rename the file with `script_rename` at the same time.

## Common Tool Mistakes to Avoid

- `script_edit` and `script_update` do not exist — use `script_replace` for find-and-replace edits.
- `script_write` does not exist — use `script_create` (new file) or `script_replace` (modify existing).
- Templates only accept: `MonoBehaviour`, `ScriptableObject`, `Editor`, `EditorWindow`.

## Domain Reload and Compile Warning

After creating or editing any script, Unity triggers a Domain Reload (recompilation).

1. Check the `compilation` field returned by `script_create` / `script_replace` / `script_append`.
2. If `compilation.isCompiling` is `true`, wait for Unity to finish before proceeding.
3. After compilation completes, call `script_get_compile_feedback` for the affected script and fix any reported errors before continuing.
4. Use batch creation (`script_create_batch`) to group related scripts and minimize the number of Domain Reloads in a single task.

## Best-Practice Guardrails

1. Use meaningful script names that match the class name exactly.
2. Organize scripts in logical folders (e.g. `Assets/Scripts/<Feature>/`).
3. Before creating gameplay code, decide the class role first: `MonoBehaviour`, `ScriptableObject`, or a plain C# helper/service class.
4. Reduce coupling: prefer explicit dependencies, small responsibilities, and event-driven notifications over hidden globals.
5. Consider performance: avoid unnecessary `Update` loops, repeated `Find` calls, reflection in hot paths, and avoidable allocations.
6. Consider maintainability: clear naming, explicit ownership, Inspector-friendly fields, and simple module boundaries.
7. Start from the smallest structure that solves the current need — avoid giant boilerplate dumps.
8. Do not default to UniTask or a global event bus unless the project context justifies them.
9. Avoid cryptic abbreviations in class, field, and method names unless they are already a project convention.
10. After any script edit, call `script_get_compile_feedback` and fix all reported errors before marking the task done.
