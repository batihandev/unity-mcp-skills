# Scene Recipes

Per-command recipes for the `unity-scene` skill. Each file corresponds to one command ID.

## Commands

| Command | File | Description |
|---------|------|-------------|
| `scene_create` | [scene_create.md](scene_create.md) | Create a new scene asset |
| `scene_load` | [scene_load.md](scene_load.md) | Load a scene by path or name |
| `scene_save` | [scene_save.md](scene_save.md) | Save the active scene |
| `scene_get_info` | [scene_get_info.md](scene_get_info.md) | Get metadata about the active scene |
| `scene_get_hierarchy` | [scene_get_hierarchy.md](scene_get_hierarchy.md) | Get the full GameObject hierarchy of the active scene |
| `scene_screenshot` | [scene_screenshot.md](scene_screenshot.md) | Capture a screenshot of the scene view |
| `scene_get_loaded` | [scene_get_loaded.md](scene_get_loaded.md) | List all currently loaded scenes |
| `scene_unload` | [scene_unload.md](scene_unload.md) | Unload an additively loaded scene |
| `scene_set_active` | [scene_set_active.md](scene_set_active.md) | Set a loaded scene as the active scene |
| `scene_find_objects` | [scene_find_objects.md](scene_find_objects.md) | Find GameObjects in a scene by name, tag, or component |

## Usage

Use these templates in `Unity_RunCommand`. Recipe path rule: `../../recipes/scene/<command>.md`
