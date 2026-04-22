---
name: unity-cleaner
description: "Use when users want to find unused assets, duplicate files, or clean up the project."
---

Recipe path rule: `../../recipes/cleaner/<command>.md`

# Unity Cleaner Skills

## Overview

Cleaner skills are read-only analysis except for `cleaner_delete_empty_folders` and `cleaner_fix_missing_scripts`. To delete assets, route to the `asset` module (`asset_delete` / `asset_delete_batch`); `cleaner_delete_assets` returns a preview only.

## Common Mistakes

**DO NOT** (common hallucinations):
- `cleaner_delete` / `cleaner_remove` do not exist → use `asset_delete` / `asset_delete_batch` to actually remove.
- `cleaner_fix` does not exist → use `cleaner_fix_missing_scripts` specifically for missing script references.
- `cleaner_scan` / `cleaner_find_unused` do not exist → use specific skills: `cleaner_find_unused_assets`, `cleaner_find_duplicates`, `cleaner_find_missing_references`, `cleaner_find_empty_folders`, `cleaner_find_large_assets`.

**Routing**:
- To delete found assets → `asset` module's `asset_delete` / `asset_delete_batch`.
- For project validation → `validation` module.

## Skills Overview

| Skill | Description |
|-------|-------------|
| `cleaner_find_unused_assets` | Find assets not referenced by others |
| `cleaner_find_duplicates` | Find duplicate files by content hash |
| `cleaner_find_missing_references` | Find missing scripts/asset references |
| `cleaner_delete_assets` | Delete assets (two-step confirmation required) |
| `cleaner_get_asset_usage` | Find what references a specific asset |
| `cleaner_find_empty_folders` | Find empty folders in the project |
| `cleaner_find_large_assets` | Find largest assets by file size |
| `cleaner_delete_empty_folders` | Delete all empty folders |
| `cleaner_fix_missing_scripts` | Remove missing script components from GameObjects |
| `cleaner_get_dependency_tree` | Get dependency tree for an asset |

---

## Skills

### cleaner_find_unused_assets
Find potentially unused assets of a specific type.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `assetType` | string | No | "Material" | Asset type filter |
| `searchPath` | string | No | "Assets" | Search path |
| `limit` | int | No | 100 | Max results |

**Returns**: `{success, assetType, searchPath, potentiallyUnusedCount, note, assets: [{path, name, type, sizeBytes}]}`

*Recipe: [../../recipes/cleaner/cleaner_find_unused_assets.md](../../recipes/cleaner/cleaner_find_unused_assets.md)*

### cleaner_find_duplicates
Find duplicate files by MD5 hash.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `assetType` | string | No | "Texture2D" | Asset type |
| `searchPath` | string | No | "Assets" | Search path |
| `limit` | int | No | 50 | Max groups |

**Returns**: `{success, assetType, count, wastedBytes, groups: [{count, sizeBytes, wastedBytes, files}]}`

*Recipe: [../../recipes/cleaner/cleaner_find_duplicates.md](../../recipes/cleaner/cleaner_find_duplicates.md)*

### cleaner_find_missing_references
Find components with missing scripts or null references.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `includeInactive` | bool | No | true | Include inactive objects |

**Returns**: `{success, issueCount, missingScripts, missingReferences, issues: [{type, gameObject, path, ...}]}`

*Recipe: [../../recipes/cleaner/cleaner_find_missing_references.md](../../recipes/cleaner/cleaner_find_missing_references.md)*

### cleaner_delete_assets
Preview a deletion against a list of asset paths. Returns size totals without mutating anything; route to `asset_delete` / `asset_delete_batch` to actually remove.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `paths` | string[] | Yes | Asset paths to preview |

**Returns**: `{success, mode: "preview", previewCount, totalBytes, totalMB, assets: [{path, exists, sizeBytes, sizeMB}]}`

*Recipe: [../../recipes/cleaner/cleaner_delete_assets.md](../../recipes/cleaner/cleaner_delete_assets.md)*

### cleaner_get_asset_usage
Find what objects reference a specific asset.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | Yes | Asset path |
| `limit` | int | No | Max results (default 50) |

**Returns**: `{success, asset: {path, name, type}, usedByCount, usedBy: [{path, name, type}]}`

*Recipe: [../../recipes/cleaner/cleaner_get_asset_usage.md](../../recipes/cleaner/cleaner_get_asset_usage.md)*

### `cleaner_find_empty_folders`
Find empty folders in the project.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchPath` | string | No | "Assets" | Search path |

**Returns:** `{ success, count, folders }`

*Recipe: [../../recipes/cleaner/cleaner_find_empty_folders.md](../../recipes/cleaner/cleaner_find_empty_folders.md)*

### `cleaner_find_large_assets`
Find largest assets by file size.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchPath` | string | No | "Assets" | Search path |
| `limit` | int | No | 20 | Max results |
| `minSizeBytes` | long | No | 0 | Minimum file size in bytes |

**Returns:** `{ success, count, assets: [{ path, sizeBytes, sizeMB }] }`

*Recipe: [../../recipes/cleaner/cleaner_find_large_assets.md](../../recipes/cleaner/cleaner_find_large_assets.md)*

### `cleaner_delete_empty_folders`
Delete all empty folders.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchPath` | string | No | "Assets" | Search path |

**Returns:** `{ success, deleted, total }`

*Recipe: [../../recipes/cleaner/cleaner_delete_empty_folders.md](../../recipes/cleaner/cleaner_delete_empty_folders.md)*

### `cleaner_fix_missing_scripts`
Remove missing script components from GameObjects.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `includeInactive` | bool | No | true | Include inactive objects |

**Returns:** `{ success, removedComponents }`

*Recipe: [../../recipes/cleaner/cleaner_fix_missing_scripts.md](../../recipes/cleaner/cleaner_fix_missing_scripts.md)*

### `cleaner_get_dependency_tree`
Get dependency tree for an asset.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `assetPath` | string | Yes | - | Asset path |
| `recursive` | bool | No | true | Recursively resolve dependencies |

**Returns:** `{ success, assetPath, dependencyCount, dependencies: [{ path, type }] }` or `{ error }` if asset not found.

*Recipe: [../../recipes/cleaner/cleaner_get_dependency_tree.md](../../recipes/cleaner/cleaner_get_dependency_tree.md)*

---

## Example Workflow: Clean Project

1. `cleaner_find_unused_assets` — identify candidates.
2. `cleaner_get_asset_usage` — verify each candidate has no active references.
3. `cleaner_delete_assets` — preview total size + per-asset report.
4. `asset_delete_batch` — actually delete the vetted set.
5. `cleaner_find_empty_folders` → `cleaner_delete_empty_folders` — clean up leftover folders.

---
