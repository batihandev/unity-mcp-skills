---
name: unity-prefab
description: "Use when users want to create, instantiate, apply, or unpack prefabs."
---

# Unity Prefab Skills

## Overview

> **BATCH-FIRST**: Use `prefab_instantiate_batch` when spawning 2+ prefab instances.

## Common Mistakes


**DO NOT** (common hallucinations):
- `prefab_create_from_object` does not exist → use `prefab_create` (takes scene object name/instanceId and savePath)
- `prefab_spawn` does not exist → use `prefab_instantiate`
- `prefab_edit` / `prefab_modify` do not exist → use `prefab_set_property` (edit prefab asset directly) or instantiate, modify, then `prefab_apply`
- `prefab_save` does not exist → use `prefab_apply` (applies instance changes to source prefab)

**Routing**:
- To modify components on a prefab instance in scene → use `component` module skills, then `prefab_apply`
- To set a property directly on the prefab asset → `prefab_set_property` (this module)
- To find all instances of a prefab → `prefab_find_instances` (this module)

## Skills Overview

| Single Object | Batch Version | Use Batch When |
|---------------|---------------|----------------|
| `prefab_instantiate` | `prefab_instantiate_batch` | Spawning 2+ instances |

**No batch needed**:
- `prefab_create` - Create prefab from scene object
- `prefab_apply` - Apply instance changes to prefab
- `prefab_unpack` - Unpack prefab instance
- `prefab_get_overrides` - Get instance overrides
- `prefab_revert_overrides` - Revert to prefab values
- `prefab_apply_overrides` - Apply overrides to prefab
- `prefab_create_variant` - Create a prefab variant
- `prefab_find_instances` - Find all instances of a prefab in scene
- `prefab_set_property` - Set a property on a component inside a Prefab asset (supports basic types, vectors, colors, and asset references)

---

## Skills

### prefab_create
Create a prefab from a scene GameObject.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Source object name |
| `instanceId` | int | No* | Instance ID |
| `path` | string | No* | Object path |
| `savePath` | string | Yes | Prefab save path |

**Returns**: `{success, prefabPath, sourceObject}`

### prefab_instantiate
Instantiate a prefab into the scene.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `prefabPath` | string | Yes | - | Prefab asset path |
| `name` | string | No | prefab name | Instance name |
| `x`, `y`, `z` | float | No | 0 | Local position (relative to parent if set) |
| `parentName` | string | No | null | Parent object name |
| `parentInstanceId` | int | No | 0 | Parent instance ID |
| `parentPath` | string | No | null | Parent hierarchy path |

**Returns**: `{success, name, instanceId, path, prefabPath, position}`

### prefab_instantiate_batch
Instantiate multiple prefabs in one call.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `items` | array | Yes | Array of instantiation configs |

**Item properties**: `prefabPath`, `name`, `x`, `y`, `z`, `rotX`, `rotY`, `rotZ`, `scaleX`, `scaleY`, `scaleZ`, `parentName`, `parentInstanceId`, `parentPath`

**Returns**: `{success, totalItems, successCount, failCount, results: [{success, name, instanceId, prefabPath, position}]}`

Recipe path rule: `../../recipes/prefab/<command>.md`

### prefab_apply
Apply instance changes back to the prefab asset.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Prefab instance name |
| `instanceId` | int | No* | Instance ID |
| `path` | string | No* | Object path |

**Returns**: `{success, gameObject, prefabPath}`

### prefab_unpack
Unpack a prefab instance (break prefab connection).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | No* | - | Prefab instance name |
| `instanceId` | int | No* | - | Instance ID |
| `path` | string | No* | - | Object path |
| `completely` | bool | No | false | Unpack all nested prefabs |

**Returns**: `{success, gameObject, mode}`

### prefab_get_overrides
Get list of property overrides on a prefab instance.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Prefab instance name |
| `instanceId` | int | No* | Instance ID |

**Returns**: `{success, overrides: [{type, path, property}]}`

### prefab_revert_overrides
Revert all overrides on a prefab instance back to prefab values.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Prefab instance name |
| `instanceId` | int | No* | Instance ID |

### prefab_apply_overrides
Apply all overrides from instance to source prefab asset.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Prefab instance name |
| `instanceId` | int | No* | Instance ID |

### prefab_create_variant
Create a prefab variant from an existing prefab.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `sourcePrefabPath` | string | Yes | - | Path to the source prefab asset |
| `variantPath` | string | Yes | - | Save path for the new variant |

**Returns:** `{ success, sourcePath, variantPath, name }`

### prefab_find_instances
Find all instances of a prefab in the current scene.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `prefabPath` | string | Yes | - | Prefab asset path to search for |
| `limit` | int | No | 50 | Maximum number of instances to return |

**Returns:** `{ success, prefabPath, count, instances: [{ name, path, instanceId }] }`

### prefab_set_property
Set a property on a component inside a Prefab asset file (without instantiating it). Supports basic types, vectors, colors, enums, and asset references.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `prefabPath` | string | Yes | - | Path to the prefab asset |
| `componentType` | string | Yes | - | Component type name |
| `propertyName` | string | Yes | - | Serialized property name |
| `value` | string | Cond. | null | Value for basic types (int/float/bool/string/enum/vector/color) |
| `assetReferencePath` | string | Cond. | null | Asset path for Object reference fields (Material, Texture, AudioClip, ScriptableObject, etc.) |
| `gameObjectName` | string | No | null | Child object name inside prefab (defaults to root) |

> Provide either `value` (basic types) or `assetReferencePath` (asset references).

**Returns:** `{ success, prefabPath, gameObject, component, property, valueSet }`

---

## Best Practices

1. Organize prefabs in dedicated folders
2. Use prefabs for repeated objects
3. Apply changes to update all instances
4. Unpack only when unique modifications needed
5. Use batch instantiation for level generation

---
