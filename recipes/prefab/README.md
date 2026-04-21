# Prefab Recipes

Recipes for the `unity-prefab` skill. Each file covers one command.

| Command | File | Description |
|---------|------|-------------|
| `prefab_create` | [prefab_create.md](prefab_create.md) | Create a prefab asset from a scene GameObject |
| `prefab_instantiate` | [prefab_instantiate.md](prefab_instantiate.md) | Instantiate a single prefab into the scene |
| `prefab_instantiate_batch` | [prefab_instantiate_batch.md](prefab_instantiate_batch.md) | Instantiate multiple prefabs in one call |
| `prefab_apply` | [prefab_apply.md](prefab_apply.md) | Apply instance changes back to the prefab asset |
| `prefab_unpack` | [prefab_unpack.md](prefab_unpack.md) | Break a prefab instance connection |
| `prefab_get_overrides` | [prefab_get_overrides.md](prefab_get_overrides.md) | Inspect overrides on a prefab instance |
| `prefab_revert_overrides` | [prefab_revert_overrides.md](prefab_revert_overrides.md) | Revert all overrides back to prefab values |
| `prefab_apply_overrides` | [prefab_apply_overrides.md](prefab_apply_overrides.md) | Apply all overrides to the source prefab asset |
| `prefab_create_variant` | [prefab_create_variant.md](prefab_create_variant.md) | Create a prefab variant from an existing prefab |
| `prefab_find_instances` | [prefab_find_instances.md](prefab_find_instances.md) | Find all scene instances of a prefab |
| `prefab_set_property` | [prefab_set_property.md](prefab_set_property.md) | Set a property on a component inside a prefab asset |

## Quick Reference

**Batch-first rule**: use `prefab_instantiate_batch` when spawning 2+ instances — one call is more efficient than N calls to `prefab_instantiate`.

**Common mistakes**:
- `prefab_create_from_object` does not exist — use `prefab_create`.
- `prefab_spawn` does not exist — use `prefab_instantiate`.
- `prefab_edit` / `prefab_modify` do not exist — use `prefab_set_property` (direct asset edit) or instantiate, modify, then `prefab_apply`.
- `prefab_save` does not exist — use `prefab_apply`.

**Apply vs Revert**:
- `prefab_apply` / `prefab_apply_overrides` — push instance changes to the asset (both are equivalent).
- `prefab_revert_overrides` — discard instance changes, restore asset values.

**Direct asset editing vs scene instance**:
- To set a property on the prefab asset without touching a scene instance → `prefab_set_property`.
- To modify a live scene instance and then save those changes → use `component` module skills, then `prefab_apply`.
