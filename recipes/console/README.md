# Console Recipes

Per-command recipe files for the `unity-console` skill. Use the native MCP tools (`Unity_ReadConsole`, `Unity_GetConsoleLogs`) where available; fall back to `Unity_RunCommand` with these templates when native tools lack the needed capability.

Recipe path rule: `../../recipes/console/<command>.md`

## Console Commands

| Command | File | Description |
|---------|------|-------------|
| `console_start_capture` | [console_start_capture.md](console_start_capture.md) | Start capturing logs into an in-memory buffer |
| `console_stop_capture` | [console_stop_capture.md](console_stop_capture.md) | Stop capturing and detach log listener |
| `console_get_logs` | [console_get_logs.md](console_get_logs.md) | Get logs (direct or capture mode, with optional filter) |
| `console_clear` | [console_clear.md](console_clear.md) | Clear Unity console and capture buffer |
| `console_log` | [console_log.md](console_log.md) | Write a custom log/warning/error message |
| `console_set_pause_on_error` | [console_set_pause_on_error.md](console_set_pause_on_error.md) | Enable/disable Error Pause in Play mode |
| `console_export` | [console_export.md](console_export.md) | Export console logs to a file |
| `console_get_stats` | [console_get_stats.md](console_get_stats.md) | Get log count by type |
| `console_set_collapse` | [console_set_collapse.md](console_set_collapse.md) | Enable/disable console Collapse mode |
| `console_set_clear_on_play` | [console_set_clear_on_play.md](console_set_clear_on_play.md) | Enable/disable Clear on Play |

## Retained Debug Commands

These three commands from the former `debug` module are now hosted in this skill.

| Command | File | Description |
|---------|------|-------------|
| `debug_force_recompile` | [debug_force_recompile.md](debug_force_recompile.md) | Force script recompilation |
| `debug_get_defines` | [debug_get_defines.md](debug_get_defines.md) | Get scripting define symbols for current platform |
| `debug_set_defines` | [debug_set_defines.md](debug_set_defines.md) | Set scripting define symbols for current platform |
