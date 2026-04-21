---
name: unity-asset
description: "Use when users want to import, move, delete, duplicate, or organize project assets."
---

# Unity Asset Skills

## When to Use

Use this module to import external files into the project, move or rename assets, delete assets, search for assets, create folders, or refresh the AssetDatabase. For import **settings** (texture compression, audio load type, model scale) use the `importer` module instead.

> **BATCH-FIRST**: Use `*_batch` skills when operating on 2+ assets.

## Quick Reference

| Single | Batch | Use Batch When |
|--------|-------|----------------|
| `asset_import` | `asset_import_batch` | Importing 2+ files |
| `asset_delete` | `asset_delete_batch` | Deleting 2+ assets |
| `asset_move` | `asset_move_batch` | Moving 2+ assets |

**No batch needed**: `asset_duplicate`, `asset_find`, `asset_create_folder`, `asset_refresh`, `asset_get_info`, `asset_reimport`, `asset_reimport_batch`, `asset_set_labels`, `asset_get_labels`, `batch_query_assets`

## Common Mistakes


**DO NOT** (common hallucinations):
- `asset_create` does not exist → use `asset_create_folder` (folders), `material_create` (materials), `script_create` (scripts)
- `asset_rename` does not exist → use `asset_move` with new path
- `asset_search` does not exist → use `asset_find` with searchFilter syntax (e.g. `t:Texture2D player`)
- `asset_copy` does not exist → use `asset_duplicate`

**Routing**:
- For texture/model/audio import settings → use `importer` module
- For material creation → use `material` module
- For script creation → use `script` module

## Skills

### asset_import
Import an external file into the project.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sourcePath` | string | Yes | External file path |
| `destinationPath` | string | Yes | Project destination |

### asset_import_batch
Import multiple external files.

`items` currently expects a JSON string, not a native array.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, sourcePath, destinationPath}]}`

### asset_delete
Delete an asset from the project.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | Yes | Asset path to delete |

### asset_delete_batch
Delete multiple assets.

`items` currently expects a JSON string, not a native array.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, path}]}`

### asset_move
Move or rename an asset.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sourcePath` | string | Yes | Current asset path |
| `destinationPath` | string | Yes | New path/name |

### asset_move_batch
Move multiple assets.

`items` currently expects a JSON string, not a native array.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, sourcePath, destinationPath}]}`

### asset_duplicate
Duplicate an asset.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | Yes | Asset to duplicate |

### asset_find
Find assets by search filter.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchFilter` | string | Yes | - | Search query |
| `limit` | int | No | 50 | Max results to return |

**Search Filter Syntax**:
| Filter | Example | Description |
|--------|---------|-------------|
| `t:Type` | `t:Texture2D` | By type |
| `l:Label` | `l:Architecture` | By label |
| `name` | `player` | By name |
| Combined | `t:Material player` | Multiple filters |

**Returns**: `{success, count, assets: [path]}`

### asset_create_folder
Create a folder in the project.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `folderPath` | string | Yes | Full folder path |

### asset_refresh
Refresh the AssetDatabase after external changes.

No parameters.

### asset_get_info
Get information about an asset.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | Yes | Asset path |

### asset_reimport
Force reimport of an asset.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | Yes | Asset path to reimport |

### asset_reimport_batch
Reimport multiple assets matching a pattern.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `searchFilter` | string | No | AssetDatabase search filter (default `*`) |
| `folder` | string | No | Folder root to search (default `Assets`) |
| `limit` | int | No | Max assets to reimport (default `100`) |

### asset_set_labels
Set labels on an asset (overwrites existing labels).

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | Yes | Asset path |
| `labels` | string | Yes | Comma-separated labels (e.g. `"ui,icon,hud"`). Empty entries are dropped |

**Returns**: `{success, assetPath, labels: [...]}`

### asset_get_labels
Get the labels currently attached to an asset.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | Yes | Asset path |

**Returns**: `{success, assetPath, labels: [...]}`

### batch_query_assets
Query multiple asset groups by type, label, or name filter in a single call.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `queries` | string | Yes | - | JSON array of `{filter}` objects using AssetDatabase search syntax |
| `limit` | int | No | 50 | Max results per query group |

**Returns**: `{success, totalGroups, groups: [{filter, count, assets: [path]}]}`

---

Recipe path rule: `../../recipes/asset/<command>.md`

## Best Practices

1. Organize assets in logical folders
2. Use consistent naming conventions
3. Refresh after external file changes
4. Use search filters for efficiency
5. Backup before bulk delete operations

---

## Redirects

- `batch_query_assets` → use the native MCP tool `Unity_FindProjectAssets` (semantic + type + name search). The recipe file is a redirect pointer.
