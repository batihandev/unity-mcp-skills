---
name: unity-console
description: "Use when users want to capture, filter, or clear console logs."
---

# Unity Console Skills

> **IMPORTANT:** Do NOT use `Unity_RunCommand` for this module natively. Use the dedicated MCP tool for console. See `mcp-tools.md`.

Work with the Unity console - capture logs, write messages, and debug your project.

Recipe path rule: `../../recipes/console/<command>.md`

## Common Mistakes


**DO NOT** (common hallucinations):
- `console_write` does not exist → use `console_log`
- For reading, filtering, or clearing console logs, use the native `Unity_GetConsoleLogs` or `Unity_ReadConsole` — not a custom recipe.

**Routing**:
- Read console entries / stack traces / errors → `Unity_GetConsoleLogs` or `Unity_ReadConsole` (native MCP)
- Clear the console → `Unity_ReadConsole` (clear action)
- Compilation-status check → `editor_get_state` (`isCompiling` field)
- Console settings (collapse, clear-on-play) → `console_set_collapse` / `console_set_clear_on_play` (this module)
- Scripting defines or forced recompile → `debug_get_defines`, `debug_set_defines`, `debug_force_recompile` (this module)

## Retained Debug Commands

The standalone `debug` module is gone. Three debug commands relevant to compilation and build configuration are retained here:

| Command | Description |
|---------|-------------|
| `debug_force_recompile` | Force script recompilation via `AssetDatabase.Refresh` + `CompilationPipeline.RequestScriptCompilation` |
| `debug_get_defines` | Get scripting define symbols for the current build target group |
| `debug_set_defines` | Set scripting define symbols for the current build target group |

Recipes: see `../../recipes/console/debug_force_recompile.md`, `debug_get_defines.md`, `debug_set_defines.md`.

## Skills Overview

| Skill | Description |
|-------|-------------|
| `console_start_capture` | Start capturing logs |
| `console_stop_capture` | Stop capturing logs |
| `console_log` | Write log message |
| `console_set_pause_on_error` | Enable or disable Error Pause in Play mode |
| `console_export` | Export console logs to a file |
| `console_get_stats` | Get log statistics (count by type) |
| `console_set_collapse` | Set console log collapse mode |
| `console_set_clear_on_play` | Set clear on play mode |
| `debug_force_recompile` | Force script recompilation |
| `debug_get_defines` | Get scripting define symbols for current platform |
| `debug_set_defines` | Set scripting define symbols for current platform |

---

## Skills

### console_start_capture
Start capturing Unity console logs.

No parameters.

### console_stop_capture
Stop capturing logs.

No parameters.

### console_log
Write a custom log message.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `message` | string | Yes | - | Log message |
| `type` | string | No | "Log" | Log/Warning/Error |

### `console_set_pause_on_error`
Enable or disable Error Pause in Play mode.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `enabled` | bool | No | true | Enable or disable error pause |

**Returns:** `{ success, enabled }`

### `console_export`
Export console logs to a file. Uses captured buffer when console_start_capture is active; otherwise reads directly from Unity Console history (no setup needed).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `savePath` | string | No | "Assets/console_log.txt" | File path to save logs |

**Returns:** `{ success, path, count, source }`

### `console_get_stats`
Get log statistics (count by type). Uses captured buffer when console_start_capture is active; otherwise reads directly from Unity Console history.

No parameters.

**Returns:** `{ success, total, source, logs, warnings, errors, exceptions, asserts }`

### `console_set_collapse`
Set console log collapse mode.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `enabled` | bool | Yes | - | Enable or disable collapse mode |

**Returns:** `{ success, setting, enabled }`

### `console_set_clear_on_play`
Set clear on play mode.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `enabled` | bool | Yes | - | Enable or disable clear on play |

**Returns:** `{ success, setting, enabled }`

---

## Example Usage

*See `../../recipes/console/<command>.md` for per-command C# templates.*

## Best Practices

1. Start capture before play mode for runtime logs
2. Filter by Error to quickly find problems
3. Use custom logs to mark AI agent actions
4. Clear console before starting new capture session
5. Stop capture when done to free resources
