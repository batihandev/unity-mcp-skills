---
name: unity-gameobject
description: "Use when users want to create, delete, move, rotate, scale, or parent GameObjects."
---

# Unity GameObject Skills

## Overview

> **BATCH-FIRST**: Use `*_batch` skills when operating on 2+ objects to reduce API calls from N to 1.

## Common Mistakes


**DO NOT** (common hallucinations):
- `gameobject_move` / `gameobject_rotate` / `gameobject_set_scale` do not exist → use `gameobject_set_transform` (handles position, rotation, and scale together)
- `gameobject_set_position` does not exist → use `gameobject_set_transform` with `posX/posY/posZ`
- `gameobject_add_component` does not exist → use `component_add` (component module)
- `gameobject_get_transform` does not exist → use `gameobject_get_info` (returns position/rotation/scale)

**Routing**:
- To add/remove components → use `component` module
- To set material/color → use `material` module
- To search objects by name/tag/component → `gameobject_find` (this module) or `scene_find_objects` (scene module, Semi-Auto)

> **Object Targeting**: All single-object skills accept three identifiers: `name` (string), `instanceId` (int, preferred for precision), `path` (string, hierarchy path like "Parent/Child"). Provide at least one. When only `name` is shown in a parameter table, `instanceId` and `path` are also accepted.

## Skills Overview

| Single Object | Batch Version | Use Batch When |
|---------------|---------------|----------------|
| `gameobject_create` | `gameobject_create_batch` | Creating 2+ objects |
| `gameobject_delete` | `gameobject_delete_batch` | Deleting 2+ objects |
| `gameobject_duplicate` | `gameobject_duplicate_batch` | Duplicating 2+ objects |
| `gameobject_rename` | `gameobject_rename_batch` | Renaming 2+ objects |
| `gameobject_set_transform` | `gameobject_set_transform_batch` | Moving 2+ objects |
| `gameobject_set_active` | `gameobject_set_active_batch` | Toggling 2+ objects |
| `gameobject_set_parent` | `gameobject_set_parent_batch` | Parenting 2+ objects |
| - | `gameobject_set_layer_batch` | Setting layer on 2+ objects |
| - | `gameobject_set_tag_batch` | Setting tag on 2+ objects |

**Query Skills** (no batch needed):
- `gameobject_find` - Find objects by name/tag/layer/component
- `gameobject_get_info` - Get detailed object information

---

## Single-Object Skills

### gameobject_create
Create a new GameObject (primitive or empty).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | Yes | - | Object name |
| `primitiveType` | string | No | null | Cube/Sphere/Capsule/Cylinder/Plane/Quad (null=Empty) |
| `x`, `y`, `z` | float | No | 0 | Local position (relative to parent if set) |
| `parentName` | string | No | null | Parent object name |
| `parentInstanceId` | int | No | 0 | Parent instance ID |
| `parentPath` | string | No | null | Parent hierarchy path |

**Returns**: `{success, name, instanceId, path, parent, position}`

### gameobject_delete
Delete a GameObject.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Object name |
| `instanceId` | int | No* | Instance ID (preferred) |
| `path` | string | No* | Hierarchy path |

*At least one identifier required

### gameobject_duplicate
Duplicate a GameObject.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Object name |
| `instanceId` | int | No* | Instance ID |
| `path` | string | No* | Hierarchy path |

**Returns**: `{originalName, copyName, copyInstanceId, copyPath}`

### gameobject_rename
Rename a GameObject.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Current object name |
| `instanceId` | int | No* | Instance ID (preferred) |
| `newName` | string | Yes | New name |

**Returns**: `{success, oldName, newName, instanceId}`

### gameobject_find
Find GameObjects matching criteria.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | No | null | Name filter |
| `tag` | string | No | null | Tag filter |
| `layer` | string | No | null | Layer filter |
| `component` | string | No | null | Component type filter |
| `useRegex` | bool | No | false | Use regex for name |
| `limit` | int | No | 50 | Max results |

**Returns**: `{count, objects: [{name, instanceId, path, tag, layer}]}`

### gameobject_get_info
Get detailed GameObject information.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Object name |
| `instanceId` | int | No* | Instance ID |
| `path` | string | No* | Hierarchy path |

**Returns**: `{name, instanceId, path, tag, layer, isActive, position, rotation, scale, parent, parentPath, childCount, components, children}`

### gameobject_set_transform
Set position, rotation, scale, and/or RectTransform properties. All transform params are optional — only supplied fields are applied.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Object name |
| `instanceId` | int | No* | Instance ID (preferred) |
| `path` | string | No* | Hierarchy path |
| `posX/posY/posZ` | float? | No | World position |
| `rotX/rotY/rotZ` | float? | No | Rotation (euler) |
| `scaleX/scaleY/scaleZ` | float? | No | Local scale |
| `localPosX/localPosY/localPosZ` | float? | No | Local position (3D and UI) |
| `anchoredPosX/anchoredPosY` | float? | No | Anchored position (UI only) |
| `anchorMinX/anchorMinY` | float? | No | Anchor min (UI only) |
| `anchorMaxX/anchorMaxY` | float? | No | Anchor max (UI only) |
| `pivotX/pivotY` | float? | No | Pivot (UI only) |
| `sizeDeltaX/sizeDeltaY` | float? | No | Size delta (UI only) |
| `width/height` | float? | No | Size shortcut via SetSizeWithCurrentAnchors (UI only) |

*At least one identifier required

**Returns (3D)**: `{success, name, instanceId, isUI: false, position, localPosition, rotation, scale}`
**Returns (UI)**: `{success, name, instanceId, isUI: true, anchoredPosition, anchorMin, anchorMax, pivot, sizeDelta, rect, localPosition}`

### gameobject_set_parent
Set parent-child relationship.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `childName` | string | No* | Child object name |
| `childInstanceId` | int | No* | Child Instance ID (preferred) |
| `childPath` | string | No* | Child hierarchy path |
| `parentName` | string | No* | Parent object name (empty string = unparent) |
| `parentInstanceId` | int | No* | Parent Instance ID |
| `parentPath` | string | No* | Parent hierarchy path |

*At least one child identifier and one parent identifier required

### gameobject_set_active
Enable or disable a GameObject.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Object name |
| `instanceId` | int | No* | Instance ID (preferred) |
| `path` | string | No* | Hierarchy path |
| `active` | bool | Yes | Enable state |

*At least one identifier required

---

Recipe path rule: `../../recipes/gameobject/<command>.md`

## Batch Skills

### gameobject_create_batch
Create multiple GameObjects in one call.

**Item properties**: `name`, `primitiveType`, `x`, `y`, `z`, `rotX`, `rotY`, `rotZ`, `scaleX`, `scaleY`, `scaleZ`, `parentName`, `parentInstanceId`, `parentPath`

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, name, instanceId, path, position}]}`

### gameobject_delete_batch
Delete multiple GameObjects. `items` accepts an array of strings (names) or objects `{name, instanceId, path}`.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, target}]}`

### gameobject_duplicate_batch
Duplicate multiple GameObjects.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, originalName, copyName, copyInstanceId, copyPath}]}`

### gameobject_rename_batch
Rename multiple GameObjects.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, oldName, newName, instanceId}]}`

### gameobject_set_transform_batch
Set transforms for multiple objects. Each item accepts the same fields as `gameobject_set_transform` plus `name`/`instanceId`/`path`.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, name, pos: {x,y,z}}]}`

### gameobject_set_active_batch
Toggle multiple objects. Each item: `{name, instanceId, path, active}`.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, target, active}]}`

### gameobject_set_parent_batch
Parent multiple objects. Each item supports `childName`/`childInstanceId`/`childPath` and `parentName`/`parentInstanceId`/`parentPath`.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, target, parent}]}`

### gameobject_set_layer_batch
Set layer for multiple objects. Each item: `{name, instanceId, path, layer, recursive}`. Set `recursive: true` to apply to all children.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, target, layer}]}`

### gameobject_set_tag_batch
Set tag for multiple objects. Each item: `{name, instanceId, path, tag}`.

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, target, tag}]}`

---
