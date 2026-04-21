---
name: unity-validation
description: "Use when users want to find missing scripts, validate references, or check project health."
---

# Unity Validation Skills

## Overview

Maintain project health - find problems, clean up, and validate your Unity project.

## Common Mistakes


**DO NOT** (common hallucinations):
- Validation skill routes use the `validate_*` prefix, not `validation_*`
- `validation_run` / `validation_check` do not exist → use specific skills such as `validate_scene`, `validate_project_structure`, `validate_missing_references`
- `validation_fix` does not exist → validation skills report issues; use other modules to fix them
- `validation_clean` does not exist → use `cleaner` module for cleanup operations

**Routing**:
- For unused/duplicate asset cleanup → use `cleaner` module
- For missing script fix → `cleaner_fix_missing_scripts` (cleaner module)
- For compile errors → `script_get_compile_feedback` (script module)

## Skills Overview

| Skill | Description |
|-------|-------------|
| `validate_scene` | Comprehensive scene validation |
| `validate_find_missing_scripts` | Find objects with missing scripts |
| `validate_fix_missing_scripts` | Remove missing script components |
| `validate_cleanup_empty_folders` | Remove empty folders |
| `validate_find_unused_assets` | Find potentially unused assets |
| `validate_texture_sizes` | Check texture sizes |
| `validate_project_structure` | Get project overview |
| `validate_missing_references` | Find null/missing object references on components |
| `validate_mesh_collider_convex` | Find non-convex MeshColliders |
| `validate_shader_errors` | Find shaders with compilation errors |

---

## Skills

### validate_scene
Comprehensive scene validation.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `checkMissingScripts` | bool | No | true | Check for missing scripts |
| `checkMissingPrefabs` | bool | No | true | Check for missing prefabs |
| `checkDuplicateNames` | bool | No | false | Check duplicate names |

**Returns**: `{success, sceneName, totalIssues, missingScripts, missingPrefabs, duplicateNames}`

### validate_find_missing_scripts
Find objects with missing script references.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchInPrefabs` | bool | No | false | Also check prefab assets |

**Returns**: `{success, count, objectsWithMissingScripts: [{name, path, missingCount}]}`

### validate_fix_missing_scripts
Remove missing script components.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `dryRun` | bool | No | true | Preview only, don't remove |

**Returns**: `{success, totalFixed, fixedObjects}`

### validate_cleanup_empty_folders
Remove empty folders from project.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `rootPath` | string | No | "Assets" | Starting folder |
| `dryRun` | bool | No | true | Preview only, don't delete |

**Returns**: `{success, count, foldersToDelete: [path]}`

### validate_find_unused_assets
Find potentially unused assets.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `assetType` | string | No | null | Filter: Texture/Material/Prefab/etc |
| `limit` | int | No | 100 | Max results |

**Returns**: `{success, count, unusedAssets: [path]}`

### validate_texture_sizes
Check for oversized textures.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `maxRecommendedSize` | int | No | 2048 | Warn if larger |
| `limit` | int | No | 50 | Max results |

**Returns**: `{success, totalChecked, oversizedCount, oversizedTextures: [{path, width, height, recommendation}]}`

### validate_project_structure
Get project folder structure overview.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `rootPath` | string | No | "Assets" | Starting folder |
| `maxDepth` | int | No | 3 | Max folder depth |

**Returns**: `{success, structure, summary: {totalFolders, totalAssets}}`

### `validate_missing_references`
Find null/missing object references on components in the scene.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `limit` | int | No | 50 | Max results |

**Returns**: `{ success, count, issues: [{ gameObject, path, component, property }] }`

### `validate_mesh_collider_convex`
Find non-convex MeshColliders (potential performance issue).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `limit` | int | No | 50 | Max results |

**Returns**: `{ success, count, nonConvexColliders: [{ gameObject, path, vertexCount }] }`

### `validate_shader_errors`
Find shaders with compilation errors.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `limit` | int | No | 50 | Max results |

**Returns**: `{ success, count, shaders: [{ name, path, errorCount }] }`

---

## Common Workflows

Recipe path rule: `../../recipes/validation/<command>.md`

### Pre-Build Check
*See `../../recipes/validation/<command>.md` for C# templates.*

### Project Cleanup
*See `../../recipes/validation/<command>.md` for C# templates.*

## Best Practices

1. **Always use `dryRun=True` first** to preview changes
2. Run validation before major builds
3. Review unused assets manually before deletion
4. Keep texture sizes appropriate for target platform
5. Fix missing scripts before they cause runtime errors
6. Regular cleanup prevents project bloat

---
