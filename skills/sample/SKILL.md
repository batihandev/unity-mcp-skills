---
name: unity-sample
description: "Retired skill — sample recipes duplicated gameobject/scene operations. Kept as a routing stub."
---

# Sample Operations (Retired 2026-04-21)

The `sample/*` recipes were low-level wrappers around primitive creation, deletion, transform editing, and scene queries. Every one of them duplicates a first-class recipe elsewhere in this repo.

## Routing

| Old recipe | Replacement |
|---|---|
| `create_cube`, `create_sphere` | [`recipes/gameobject/gameobject_create.md`](../../recipes/gameobject/gameobject_create.md) with `primitiveType = "Cube"` / `"Sphere"` |
| `delete_object` | [`recipes/gameobject/gameobject_delete.md`](../../recipes/gameobject/gameobject_delete.md) |
| `find_objects_by_name` | [`recipes/scene/scene_find_objects.md`](../../recipes/scene/scene_find_objects.md) |
| `get_scene_info` | [`recipes/scene/scene_get_info.md`](../../recipes/scene/scene_get_info.md) |
| `set_object_position`, `set_object_rotation`, `set_object_scale` | [`recipes/gameobject/gameobject_set_transform.md`](../../recipes/gameobject/gameobject_set_transform.md) with the appropriate argument set |

All eight recipe files under `recipes/sample/` are preserved as tombstones that point to the same replacements.

## Why retired

Duplicating `gameobject_create` with a fixed `primitiveType` argument adds surface area without adding capability. Agents were landing on the sample recipes and missing the richer parent recipe. Retirement collapses the surface.
