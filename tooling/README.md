# Tooling

Install-once Editor script templates for Unity projects.

## How this differs from `recipes/`

| | `recipes/` | `tooling/` |
|--|-----------|------------|
| Execution | `Unity_RunCommand` (one-shot `IRunCommand`) | `Unity_CreateScript` (install into project) |
| Persistence | None — runs and returns | Permanent file in project's Editor folder |
| Validation gate | compile + run required | None |

## Usage

1. Find the tool in the domain subfolder below.
2. Create the C# file at the specified install path via `Unity_CreateScript`.
3. Wait for Unity to recompile.
4. Call the resulting menu items via `EditorApplication.ExecuteMenuItem`.

## Domain Map

| Domain | Folder | Contents |
|--------|--------|----------|
| ui | [`tooling/ui/`](ui/README.md) | UGUI Editor tools |
