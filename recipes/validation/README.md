# validation recipes

Recipe path rule: `../../recipes/validation/<command>.md`

| Command | File | Description |
|---------|------|-------------|
| `validate_scene` | [validate_scene.md](validate_scene.md) | Validate the active scene for missing scripts, missing prefabs, duplicate names, and empty GameObjects |
| `validate_find_missing_scripts` | [validate_find_missing_scripts.md](validate_find_missing_scripts.md) | Find all GameObjects with missing script references, optionally including prefab assets |
| `validate_fix_missing_scripts` | [validate_fix_missing_scripts.md](validate_fix_missing_scripts.md) | Remove missing script components from scene GameObjects, with dry-run support |
| `validate_cleanup_empty_folders` | [validate_cleanup_empty_folders.md](validate_cleanup_empty_folders.md) | Find and optionally delete empty folders under a root path |
| `validate_find_unused_assets` | [validate_find_unused_assets.md](validate_find_unused_assets.md) | Find potentially unused assets of a given type via dependency analysis |
| `validate_texture_sizes` | [validate_texture_sizes.md](validate_texture_sizes.md) | Find Texture2D assets that exceed a recommended size threshold |
| `validate_project_structure` | [validate_project_structure.md](validate_project_structure.md) | Get an overview of project folder structure and asset counts by type |
| `validate_missing_references` | [validate_missing_references.md](validate_missing_references.md) | Find null/missing object references on components in the active scene |
| `validate_mesh_collider_convex` | [validate_mesh_collider_convex.md](validate_mesh_collider_convex.md) | Find non-convex MeshColliders that may cause physics performance issues |
| `validate_shader_errors` | [validate_shader_errors.md](validate_shader_errors.md) | Find shader assets with compilation errors |
