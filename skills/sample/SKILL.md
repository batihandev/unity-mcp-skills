---
name: unity-sample
description: "Redirect skill — sample-* recipes map to existing gameobject / scene recipes. Use the target recipes directly."
---

# Sample Operations

The `sample/*` recipe names are redirect pointers. For any `sample/*` name, use the target recipe directly instead.

## Routing

| `sample/*` name | Target recipe |
|---|---|
| `create_cube`, `create_sphere` | [`recipes/gameobject/gameobject_create.md`](../../recipes/gameobject/gameobject_create.md) with `primitiveType = "Cube"` / `"Sphere"` |
| `delete_object` | [`recipes/gameobject/gameobject_delete.md`](../../recipes/gameobject/gameobject_delete.md) |
| `find_objects_by_name` | [`recipes/scene/scene_find_objects.md`](../../recipes/scene/scene_find_objects.md) |
| `get_scene_info` | [`recipes/scene/scene_get_info.md`](../../recipes/scene/scene_get_info.md) |
| `set_object_position`, `set_object_rotation`, `set_object_scale` | [`recipes/gameobject/gameobject_set_transform.md`](../../recipes/gameobject/gameobject_set_transform.md) with the appropriate argument set |

Every file under `recipes/sample/` is a redirect to one of the above.
