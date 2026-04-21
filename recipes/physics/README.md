# Physics Recipes

Per-command recipes for the `unity-physics` skill. Each file corresponds to one command ID.

## Commands

| File | Command |
|------|---------|
| [physics_raycast.md](physics_raycast.md) | `physics_raycast` |
| [physics_check_overlap.md](physics_check_overlap.md) | `physics_check_overlap` |
| [physics_get_gravity.md](physics_get_gravity.md) | `physics_get_gravity` |
| [physics_set_gravity.md](physics_set_gravity.md) | `physics_set_gravity` |
| [physics_raycast_all.md](physics_raycast_all.md) | `physics_raycast_all` |
| [physics_spherecast.md](physics_spherecast.md) | `physics_spherecast` |
| [physics_boxcast.md](physics_boxcast.md) | `physics_boxcast` |
| [physics_overlap_box.md](physics_overlap_box.md) | `physics_overlap_box` |
| [physics_create_material.md](physics_create_material.md) | `physics_create_material` |
| [physics_set_material.md](physics_set_material.md) | `physics_set_material` |
| [physics_get_layer_collision.md](physics_get_layer_collision.md) | `physics_get_layer_collision` |
| [physics_set_layer_collision.md](physics_set_layer_collision.md) | `physics_set_layer_collision` |

## Routing Notes

- Adding physics components (Rigidbody, BoxCollider, etc.) → use the `component` module, not this one.
- Physics simulation only runs during Play mode; there is no `physics_simulate` command.
- Raycast results use world-space coordinates.

## Usage

Use these templates in `Unity_RunCommand`. Recipe path rule: `../../recipes/physics/<command>.md`
