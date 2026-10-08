# Project and scene validation

Use this guide to report project health and preview the two cleanup workflows. Read [foundation](foundation.md), discover live schemas, and require the nested typed `Ok=true` before consuming `Result`. Reports inspect ordinary loaded scenes or project assets and preserve authored state. Scene defaults use native active GameObject discovery across ordinary loaded scenes, preserving native hide-flag eligibility and native discovery order. Attached component inspection uses each discovered GameObject without adding a component hide-flag filter. Persistent assets, preview scenes and prefab-stage objects are excluded; inactive scope requires an explicit flag.

| Requested operation | Canonical owner and signature defaults |
| --- | --- |
| Validate scene | `validation.scene`: missingScriptstrue, missingPrefabstrue, duplicateNamestrue, emptyGameObjectsfalse. |
| Find missing scripts | `analysis.scene-missing`: includeInactivefalse, includeScriptstrue, includeReferencesfalse, searchInPrefabstrue. |
| Fix missing scripts | `validation.missing-scripts-fix`: includeInactivefalse, dryRuntrue; confirm required for a real write. |
| Cleanup empty folders | `analysis.asset-folders`, searchPathAssets; select Leaftrue/Rootfalse; default preview then exact `asset.trash`. |
| Find unused candidates | `analysis.assets-unused`: assetTypeMaterial, searchPathAssets, limit100, globalCandidatestrue, excludeResourcesCandidatesfalse, excludeResourcesSourcesfalse. |
| Texture size validation | `validation.texture-sizes`: maxRecommendedSize2048, limit50. |
| Project structure | `analysis.project-structure`: rootPathAssets, maxDepth2. |
| Missing references | `analysis.scene-missing`: includeInactivefalse, includeScriptsfalse, includeReferencestrue, firstReferencePerComponenttrue, visibleReferencesOnlytrue, limit50. |
| Convex collider validation | `validation.mesh-colliders`: limit50. |
| Shader compilation messages | `validation.shader-messages`: limit50. |

```bash
unity --json command --project-path "$PROJECT_PATH" --query validation.scene --detail full
unity --json command --project-path "$PROJECT_PATH" validation.scene
unity --json command --project-path "$PROJECT_PATH" analysis.scene-missing -- \
  --includeInactive false --includeScripts true --includeReferences false --searchInPrefabs true
unity --json command --project-path "$PROJECT_PATH" analysis.scene-missing -- \
  --includeInactive false --includeScripts false --includeReferences true \
  --firstReferencePerComponent true --visibleReferencesOnly true --limit 50
unity --json command --project-path "$PROJECT_PATH" analysis.assets-unused -- \
  --assetType Material --searchPath Assets --limit 100 --globalCandidates true \
  --excludeResourcesCandidates false --excludeResourcesSources false
unity --json command --project-path "$PROJECT_PATH" validation.missing-scripts-fix -- \
  --includeInactive false --dryRun true
```

Missing-script reports include scene and prefab paths, exact objects, unresolved component indices and complete counts. The prefab flag scans actual global prefab assets, including inactive descendants, and unloads any owned prefab contents in finally. Report affected objects by grouping exact scene identity/object reference or prefab path/object path, retain missing component count separately, and never edit prefab assets. Null reference values with zero missing ID are legitimate; reference reports inspect visible serialized properties and show the first unresolved field per component. The strict cap applies after complete collection, including when one GameObject has more than50 affected components; limit0 shows0 and preserves full `Total`/`Truncated`.

Scene validation emits Error per unresolved script slot, Warning per missing prefab object, Info per duplicate exact-case loaded name cluster, and optional Info per transform-only empty leaf. Its three primary checks default true. Toggles control findings and complete severity totals; disabled components on active objects remain included.

Unused candidates use global AssetDatabase type discovery, with recursive dependency sources confined to Assets. Resources source dependencies and Resources candidates both participate. Preserve discovery order and full totals before cap; runtime, script, Resources.Load and Addressables use remain uncertain. Review candidate use before cleanup.

Texture reports inspect imported Texture2D objects with actual TextureImporter state. Width or height must be strictly greater than the threshold; importer maxTextureSize is a different value and is reported separately, with format/recommendation. No importer is changed. Shader reports scan all project shader assets with messages, count actual messages and Error/Warning severities separately, and retain warning-only shaders. Message count is distinct from shader count; a warning is never relabeled an error. Collider reports include active loaded nonconvex MeshColliders, including disabled components; absent sharedMesh yields vertexCount0. Every capped report has full totals; negative caps/thresholds refuse before collection.

Project structure validates an existing confined Assets directory and each visited descendant through the shared read-only directory policy, refusing traversal, outside roots, reparse/linked entries, and inaccessible directories. Depth2 shows two descendant levels and their direct nonmeta file counts; depth0 shows none. Seven asset counts cover Material/Prefab/Script/Texture2D/AudioClip/Scene/Shader under the selected root. No directories or files are created by reports. Optional ProjectAuditor has a separate package/rule contract; absent Auditor is unavailable and does not establish a clean project.

`validation.mesh-colliders` uses public native MeshCollider discovery with unsorted native order across active ordinary loaded scenes. Native GameObject and component flag eligibility is preserved, including eligible hidden targets; persistent, preview and prefab-stage objects are excluded. Disabled components participate, nonconvex colliders are reported, and absent shared meshes have vertex count0. Full totals precede the result cap.

## Missing-script removal

Preview uses includeInactivefalse and dryRuntrue. Review exact objects and unresolved counts, then invoke the same discovered owner with `confirm=true,dryRun=false`. The operation is scene-only with one grouped full-hierarchy Undo; Undo/Redo restores/removes the actual unresolved slots. `SelectedCount` counts affected objects; `RemovedComponents` counts removed slots and is0 in preview. Inactive objects are untouched with the validation flag, and prefab files retain exact bytes. Save the affected scenes only when persistence is requested; verify save/reopen separately. Missing asset references require an explicit reassignment and are not repaired by deleting scripts.

## Current empty leaves and recoverable Trash

Collect `analysis.asset-folders(searchPath=rootPath)`, default rootAssets, and select only `Leaf=true && Root=false`. A recursively empty parent containing child directories is not a current leaf. Keep this initial leaf plan fixed: removing its leaves must not silently extend the same pass to parents that become empty. Preview defaults true and performs no moves.

Use the complete [Cleaner ordered recoverable Trash workflow](cleaner.md#ordered-recoverable-trash), selecting these exact leaf paths as `MODE=assets` and its `PATHS` JSON array. It prevalidates every exact path, excludes root, captures bytes/meta/GUID externally, previews `asset.trash`, then requires explicit reviewed apply. Leaf paths cannot overlap. Immediately before apply rescan and require the same Leaftrue/Rootfalse set and captured GUID/meta identity. Apply exact `asset.trash(confirm=true)` serially, retain one per-path outcome, and count only actual successful moves; a failure is attributed to its path and later independent leaves may proceed. This is persistent/non-Undo.

For recovery use that canonical workflow's restore body: restore only successful owned paths, create owned ancestors before descendants, preserve differing destination bytes, import/reimport exact paths, and verify every captured file/meta hash plus each original folder GUID. Never overwrite neighboring folders or claim recovery from cached AssetDatabase GUID alone. Retain external snapshots and report recovery-required when identity/hash guards or restoration fail.
