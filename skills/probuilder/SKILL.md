---
name: unity-probuilder
description: "Use when users want to create ProBuilder shapes, extrude faces, bevel edges, subdivide meshes, or perform procedural mesh operations. Requires com.unity.probuilder package."
---

# Unity ProBuilder Skills

## When to Use

Use this module for editable ProBuilder meshes, not regular primitive GameObjects. It is best for blockout, level geometry, and procedural mesh refinement.

> **Requires**: `com.unity.probuilder` package.
> **Batch-first**: For scene blockout or level generation, prefer `probuilder_create_batch` when creating `2+` shapes.

## Common Mistakes

**DO NOT** (common hallucinations):
- Package not installed → recipes silently fail; install first: `Unity_PackageManager_ExecuteAction` with `packageId=com.unity.probuilder`, `action=Add`
- `probuilder_create_mesh` does not exist -> use `probuilder_create_shape`
- `probuilder_edit_face` does not exist -> use the specific face skills such as `probuilder_extrude_faces`, `probuilder_delete_faces`, `probuilder_merge_faces`
- `probuilder_set_material` and `probuilder_set_face_material` are different -> whole object vs selected faces
- Regular meshes do not become ProBuilder meshes automatically
- Mesh rebuild calls (`ToMesh()` + `Refresh()`) are already handled by the skills. Do not invent a manual rebuild step

**Routing**:
- For ordinary primitive objects without editable topology -> use `gameobject_create`
- For material asset creation or shader work -> use `material`
- For large blockout generation -> combine this module with `material` and `light`, but keep geometry creation here

## Object Targeting

Most edit/query skills accept one of:
- `name`
- `instanceId`
- `path`

Prefer `instanceId` when multiple scene objects share the same name.

## Quick Reference

### Create and Batch

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `probuilder_create_shape` | Create a parametric ProBuilder shape | `shape`, `name?`, `x/y/z?`, `sizeX/Y/Z?`, `rotX/Y/Z?` |
| `probuilder_create_batch` | Create multiple shapes in one call | `items`, `defaultParent?` |

Supported `shape` values: `Cube`, `Sphere`, `Cylinder`, `Cone`, `Torus`, `Prism`, `Arch`, `Pipe`, `Stairs`, `Door`, `Plane`.

### Face and Edge Editing

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `probuilder_extrude_faces` | Extrude selected faces | target, `faceIndexes?`, `distance?`, `method?` |
| `probuilder_delete_faces` | Delete faces by index | target, `faceIndexes` |
| `probuilder_merge_faces` | Merge faces into one | target, `faceIndexes?` |
| `probuilder_flip_normals` | Reverse face direction | target, `faceIndexes?` |
| `probuilder_detach_faces` | Split faces from shared vertices | target, `faceIndexes?`, `deleteSourceFaces?` |
| `probuilder_bevel_edges` | Chamfer edges | target, `edgeIndexes?`, `amount?` |
| `probuilder_extrude_edges` | Extrude edges outward | target, `edgeIndexes`, `distance?`, `extrudeAsGroup?` |
| `probuilder_bridge_edges` | Bridge two edges with new face | target, `edgeA`, `edgeB`, `allowNonManifold?` |

### Mesh, Vertex, UV, Material

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `probuilder_subdivide` | Add detail by subdivision | target, `faceIndexes?` |
| `probuilder_conform_normals` | Make normals point consistently outward | target, `faceIndexes?` |
| `probuilder_move_vertices` | Offset vertices by delta | target, `vertexIndexes`, `deltaX/Y/Z?` |
| `probuilder_set_vertices` | Set absolute vertex positions | target, `vertices` |
| `probuilder_get_vertices` | Query vertex positions | target, `vertexIndexes?`, `verbose?` |
| `probuilder_weld_vertices` | Merge close vertices | target, `vertexIndexes`, `radius?` |
| `probuilder_project_uv` | Box-project UVs | target, `faceIndexes?`, `channel?` |
| `probuilder_set_face_material` | Assign material to faces | target, `faceIndexes?`, `materialPath?`, `submeshIndex?` |
| `probuilder_set_material` | Assign whole-object material or quick color | target, `materialPath?`, `r/g/b/a?` |
| `probuilder_combine_meshes` | Merge multiple meshes | `names` |

### Query and Pivot

| Skill | Use | Key parameters |
|-------|-----|----------------|
| `probuilder_get_info` | Get face/vertex/material stats | target |
| `probuilder_center_pivot` | Center or reposition pivot | target, `worldX/Y/Z?` |

## Spatial Reference

### Positioning Rules

- `y` is the **center** of the shape, not its bottom.
- Floor top surface at `0` with `sizeY=0.3` means `y = -0.15`.
- To stack B on A: `B.y = A.y + A.sizeY/2 + B.sizeY/2`.
- Pillars should usually use `y = height/2`.

### Human Scale Reference

| Reference | Typical size | Use for |
|----------|--------------|---------|
| Standing person | `1.8m` tall | Doors `>=2.2m`, ceilings `>=2.5m` |
| Shoulder width | `0.5m` | Corridors `>=1.5m`, doors `>=0.9m` |
| Single step | `0.18m` rise / `0.28m` run | Stairs |
| Single room | `4x4m` | Interior planning |
| Story height | `3m` | Multi-story spaces |

### Common Modeling Patterns

| Pattern | Typical setup |
|--------|----------------|
| Floor | `sizeY = 0.2-0.5`, wide X/Z |
| Wall | thin depth (`0.2-0.3`), tall Y |
| Pillar | `Cylinder`, small X/Z, `y = height/2` |
| Ramp | `Cube` + vertex edits |
| Staircase | `Stairs`, `sizeY = total rise`, `sizeZ = total run` |
| Bridge | thin Cube, aligned to connected platforms |
| Room | floor + ceiling + 4 walls |

### Furniture Decomposition

Real props should rarely be a single box. Split them into structural parts.

| Furniture | Main parts |
|----------|------------|
| Desk | tabletop + 4 legs |
| Chair | seat + 4 legs + backrest |
| Shelf | 2 sides + shelves + optional back |
| Bed | mattress + frame + headboard |

Typical furniture sizes: desk `1.2x0.6x0.75m`, chair seat `0.45x0.45x0.45m` (total `0.85m` height), bookshelf `0.8x0.3x1.8m`.

## Blockout Workflow

1. Create major volumes with `probuilder_create_batch`.
2. Verify traversal and gameplay scale before adding detail.
3. Refine surfaces with face/edge operations.
4. Use vertex edits for ramps, slopes, and irregular silhouettes.
5. Apply quick colors or real materials after layout stabilizes.

## C# Templates

Recipe path rule: `../../recipes/probuilder/<command>.md`

See [../../recipes/probuilder/README.md](../../recipes/probuilder/README.md) for the full command index and per-command `Unity_RunCommand` C# templates.

## Important Notes

1. ProBuilder objects keep editable topology through `ProBuilderMesh`.
2. Use `probuilder_get_info` before face edits and `probuilder_get_vertices` before vertex edits.
3. MeshCollider on physics-driven props must be convex.
4. Quick color assignment is fine for prototype passes; use material assets for production.
5. Package missing errors are expected if ProBuilder is not installed.
