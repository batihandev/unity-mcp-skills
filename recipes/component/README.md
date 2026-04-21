# Component Recipes

Recipes for the `unity-component` skill. Each file covers one command.

| Command | File | Description |
|---------|------|-------------|
| `component_add` | [component_add.md](component_add.md) | Add a component to a GameObject |
| `component_add_batch` | [component_add_batch.md](component_add_batch.md) | Add components to multiple GameObjects |
| `component_remove` | [component_remove.md](component_remove.md) | Remove a component from a GameObject |
| `component_remove_batch` | [component_remove_batch.md](component_remove_batch.md) | Remove components from multiple GameObjects |
| `component_list` | [component_list.md](component_list.md) | List all components on a GameObject |
| `component_set_property` | [component_set_property.md](component_set_property.md) | Set a component property or field |
| `component_set_property_batch` | [component_set_property_batch.md](component_set_property_batch.md) | Set properties on components across multiple objects |
| `component_get_properties` | [component_get_properties.md](component_get_properties.md) | Get all properties and fields of a component |
| `component_copy` | [component_copy.md](component_copy.md) | Copy a component from one object to another |
| `component_set_enabled` | [component_set_enabled.md](component_set_enabled.md) | Enable or disable a component |

## Quick Reference

**Batch-first rule**: use `*_batch` commands when operating on 2+ objects to reduce API calls from N to 1.

**Object targeting**: all single-object commands accept `name`, `instanceId` (preferred), or `path`.

**Common mistakes**:
- `component_create` and `component_find` do not exist — use `component_add` and `component_list`.
- `componentType` is case-sensitive: `Rigidbody` not `rigidbody`.
- To enable/disable a component use `component_set_enabled`, not `component_set_property`.
