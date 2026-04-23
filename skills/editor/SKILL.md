---
name: unity-editor
description: "Use when users want to enter play mode, select objects, undo/redo, or execute menu commands."
---

# Unity Editor Skills

## Overview

Control the Unity Editor itself - enter play mode, manage selection, undo/redo, and execute menu items.

## Common Mistakes


**DO NOT** (common hallucinations):
- `editor_run` does not exist → use `editor_play` to enter play mode
- `editor_compile` / `editor_recompile` do not exist → use `debug_force_recompile`
- `editor_save` does not exist → use `editor_execute_menu` with menuPath `"File/Save"`
- `editor_execute_menu` requires exact menu path — typos cause silent failure

**Routing**:
- For compilation check → use `editor_get_state` (`isCompiling` field)
- For console errors → use native `Unity_ReadConsole` or `Unity_GetConsoleLogs`
- For scene save → `scene_save` (scene module) or `editor_execute_menu` menuPath="File/Save"

## Skills Overview

| Skill | Description |
|-------|-------------|
| `editor_play` | Enter play mode |
| `editor_stop` | Exit play mode |
| `editor_pause` | Toggle pause |
| `editor_select` | Select GameObject |
| `editor_get_selection` | Get selected objects |
| `editor_get_context` | Get full editor context (selection, assets, scene) |
| `editor_undo` | Undo last action |
| `editor_redo` | Redo last action |
| `editor_get_state` | Get editor state |
| `editor_execute_menu` | Execute menu item |
| `editor_get_tags` | Get all tags |
| `editor_get_layers` | Get all layers |
| `console_set_pause_on_error` | Pause play mode on error (console module) |

---

## Skills

### editor_play
Enter play mode. Fire-and-forget: sets `EditorApplication.isPlaying = true`
and returns `{ success, mode, started }` immediately. Observe the
transition by calling `editor_get_state` (check `isPlaying`) in a later
call — recipes cannot span domain reloads.

### editor_stop
Exit play mode.

### editor_pause
Toggle pause state.

### editor_select
Select a GameObject.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Object name |
| `instanceId` | int | No* | Instance ID |
| `path` | string | No* | Object path |

*One identifier required

### editor_get_selection
Get currently selected objects.

**Returns**: `{success, count, objects: [{name, instanceId}]}`

### editor_get_context
Get full editor context including selection, assets, and scene info.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `includeComponents` | bool | No | false | Include component list |
| `includeChildren` | bool | No | false | Include children info |

**Returns**:
- `selectedGameObjects`: Objects in Hierarchy (instanceId, path, tag, layer)
- `selectedAssets`: Assets in Project window (GUID, path, type, isFolder)
- `activeScene`: Current scene info (name, path, isDirty)
- `focusedWindow`: Name of focused editor window
- `isPlaying`, `isCompiling`: Editor state

### editor_undo
Undo the last action.

### editor_redo
Redo the last undone action.

### editor_get_state
Get current editor state.

**Returns**: `{isPlaying, isPaused, isCompiling, timeSinceStartup, unityVersion, platform}`

### editor_execute_menu
Execute a menu command.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `menuPath` | string | Yes | Menu item path |

**Common Menu Paths**:
| Menu Path | Action |
|-----------|--------|
| `File/Save` | Save current scene |
| `File/Build Settings...` | Open build settings |
| `Edit/Play` | Toggle play mode |
| `GameObject/Create Empty` | Create empty object |
| `Window/General/Console` | Open console |
| `Assets/Refresh` | Refresh assets |

### editor_get_tags
Get all available tags.

**Returns**: `{success, tags: [string]}`

### editor_get_layers
Get all available layers.

**Returns**: `{success, layers: [{index, name}]}`

### Pause On Error
Pause-on-error is provided by the console module, not the editor module.

Use `console_set_pause_on_error` from the `console` module.

---

## Example Usage

*See `../../recipes/editor/<command>.md` for C# templates.*

Recipe path rule: `../../recipes/editor/<command>.md`

## Best Practices

1. Check editor state before play mode operations
2. Don't modify scene during play mode (changes lost)
3. Use undo for safe experimentation
4. Use `editor_get_context` to get instanceId for batch operations
5. Menu commands must match exact paths

