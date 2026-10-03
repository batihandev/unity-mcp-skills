# ProBuilder

Use this guide with `com.unity.probuilder` 6.1.2 through 6.x and the optional Unity CLI commands package. Discover each `probuilder.*` registration from the selected Editor's live command catalog before invoking it. The commands accept exact numeric or GlobalObjectId handles for scene objects; names and first-match searches do not identify edit targets. Read [foundation safety](foundation.md), inspect returned `Ok` and actual Unity state, and save the scene explicitly after authoring.

Every edit command requires Edit mode and a loaded regular scene. Use `dryRun=true` to validate and preview without writing; use `confirm=true` to apply. Preview takes precedence if both are supplied. An edit gets its own Undo group. Compiled Mesh assets and meshes shared by another scene MeshFilter are refused because those writes would affect another owner. Face indexes and vertex indexes are zero-based, comma-separated, unique and fully in range. Edge selections use actual face-edge pairs such as `0-1,2-3`; malformed, repeated, or nonexistent edges fail before the write. Query the current mesh again after topology changes because indexes can change.

## Create and inspect

`probuilder.create-shape` creates one of `Cube`, `Sphere`, `Cylinder`, `Cone`, `Torus`, `Prism`, `Arch`, `Pipe`, `Stairs`, `Door`, or `Plane`. `shape=Cube`, position and Euler rotation default to zero, and size defaults to `(1,1,1)`. The size is baked into editable geometry. Supply an exact same-scene `parent` and an existing `materialPath` when needed. Invalid parent or material references fail before creation. The result reports the exact new component handle and actual vertex and face counts.

For batch creation, parse an explicit JSON `items` array in the caller and invoke `probuilder.create-shape` for each item. Each item may set shape, name, position, size, rotation, parent, and material path; an omitted item shape defaults to Cube, and an omitted parent may use a caller `defaultParent`. Keep each nested command result, including failures. A later failure does not undo earlier successful creations. This composition retains the individual Undo and validation boundary.

`probuilder.inspect` reports the editable mesh's face and vertex counts, edge incidence count, current shape name where the parametric component exposes it, position, compiled local bounds, renderer material handles, and face counts by material slot. A custom or nonparametric shape reports `unknown`. `probuilder.vertices` returns selected local positions when `vertexIndexes` is supplied. With no filter, `verbose=false` and more than 100 vertices, it returns bounds and count without the full position array.

## Edit topology and geometry

| Task | Command | Selection and defaults |
|---|---|---|
| Delete faces | `probuilder.face-delete` | Required `faceIndexes` |
| Merge faces | `probuilder.face-merge` | Optional `faceIndexes`; at least two selected |
| Flip winding | `probuilder.face-flip` | Optional `faceIndexes`; all by default |
| Detach faces | `probuilder.face-detach` | Optional `faceIndexes`; `deleteSourceFaces=false` |
| Extrude faces | `probuilder.face-extrude` | Optional `faceIndexes`; `distance=0.5`, `method=FaceNormal` (`IndividualFaces`, `FaceNormal`, `VertexNormal`) |
| Subdivide faces | `probuilder.face-subdivide` | Optional `faceIndexes`; all by default |
| Conform normals | `probuilder.face-conform` | Optional `faceIndexes`; all by default |
| Extrude edges | `probuilder.edge-extrude` | Required `edgeIndexes`; `distance=0.5`, `extrudeAsGroup=true`, `enableManifoldExtrude=false` |
| Bevel edges | `probuilder.edge-bevel` | Optional `edgeIndexes`; `amount=0.2`, allowed range `(0,1]` |
| Bridge edges | `probuilder.edge-bridge` | Exactly one `edgeA` and one distinct `edgeB`; `allowNonManifold=false` |
| Move vertices | `probuilder.vertex-move` | Required `vertexIndexes`; local `deltaX/Y/Z=0` |
| Set vertices | `probuilder.vertex-set` | Required JSON `vertices` array of unique `{index,x,y,z}` objects in local space |
| Weld vertices | `probuilder.vertex-weld` | Required `vertexIndexes`; `radius=0.01`, must be positive |
| Set pivot | `probuilder.pivot` | No axes centers; supplied `worldX/Y/Z` retain current transform values for omitted axes |

All numeric coordinates and distances must be finite. Negative extrusion distance is valid. The command result reports actual counts after rebuilding the compiled mesh; for `face-conform`, `SelectedFaces` is the selection size and `OutputFaces` is the number whose winding changed. A ProBuilder operation returning no result or a non-success status fails and rolls back its Undo group. Read both `ProBuilderMesh` data and the compiled Mesh when checking the result.

## Materials and UVs

`probuilder.face-material` assigns an existing exact `materialPath` to selected faces, reusing its renderer slot or appending one. Alternatively, supply an existing `submeshIndex`. Supplying both owners or an invalid slot fails. Omit `faceIndexes` to affect all faces. For the whole renderer, use the native material and Renderer slot commands in [Materials](material.md). To make a color material, create an asset at an explicit durable `Assets/...` path with native material authoring, default omitted RGB channels to `0.5` and alpha to `1`, then assign that asset through the native Renderer owner.

`probuilder.uv-box` projects selected faces (all by default) into `channel=0`, `1`, `2`, or `3`. It chooses the nearest signed principal axis from each face normal, projects local positions, marks the faces as manual UV, clears their element groups, and splits selected UV associations. Unselected coordinates stay intact. The UV-only command restores other channels after its own mesh compilation. Channel 1 lives on the compiled Mesh and survives a saved-scene reopen, but any later command that calls `ToMesh()` clears it, including material and pivot edits as well as geometry edits. Project channel 1 after all such edits, then inspect projected coordinates and save the scene.

## Combine

`probuilder.combine` takes `targets` as two or more ordered, comma-separated exact ProBuilderMesh handles. Resolve a desired current Selection to explicit handles before calling it. All inputs must be unique, in the same active loaded scene, and independent in the hierarchy. The first input is the target. The result includes every actual output handle, face/vertex counts, and consumed input handles. A capacity remainder can be an unchanged input or a newly created mesh, so retain every returned output and inspect its world geometry and material slots. Undo restores consumed inputs and the target mesh.
