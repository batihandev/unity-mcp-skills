# Smart scene operations

Read [foundation](foundation.md) and [editor Undo](editor.md). These operations require the public command package and the live Pipeline connection. Discover each command's full schema before invocation. Use exact unsigned decimal EntityId references, never narrowed signed instance IDs.

| Operation | Owner and defaults | Effect |
| --- | --- | --- |
| Align to ground | `smart.align-ground`: maxDistance100, alignRotationfalse | Selected-set transform Undo |
| Distribute | `smart.distribute`: axisX, minimum three objects | Selected-set transform Undo |
| Randomize transform | `smart.random-transform`: posRange0, rotRange0, scaleMin1, scaleMax1 | Selected-set transform Undo |
| Bind references | `smart.reference-plan` + native `set_component_properties`; public properties use `smart.reference-property` | Native serialized collection Undo or bounded property scope |
| Replace objects | `smart.replace`: required prefabPath | Scene structure Undo; writes Selection |
| Scene layout | `smart.layout`: Linear, axisX, spacing2, columns3, arcAngle180, lookAtCenterfalse | Selected-set transform Undo |
| Query component values | `smart.query`: required componentName/propertyName/value, op==, limit50 | Report; reflected properties can execute getters |
| Query a sphere | `smart.spatial`: required x/y/z, radius10, componentFilternull, limit50 | Physics report |
| Select by component | Native `find_gameobjects` + bounded public evaluation below | Ephemeral Editor Selection |
| Snap to grid | `smart.snap-grid`: gridSize1 | Selected-set transform Undo |

## Preview and transform planning

All Smart scene mutators default `dryRun=true,confirm=false`. Preview wins over confirmation. Review the proposed exact targets and changed counts, then pass `dryRun=false,confirm=true` with the same input. Omitted `targetsJson` uses current `Selection.gameObjects`; an explicit JSON array of exact references makes the chosen set reproducible. Duplicates are removed preserving first occurrence. Empty sets, stale/persistent/hidden/preview/prefab-stage objects and selected ancestor/descendant pairs refuse as a whole before any mutation. Authoring also refuses Play, transitions and compilation. Preview changes no Selection, scene dirtiness, Undo group or global random state.

```bash
unity --json command --project-path "$PROJECT_PATH" --query smart.layout --detail full
unity --json command --project-path "$PROJECT_PATH" smart.layout -- \
  --layoutType Grid --spacing 2 --columns 3 --targetsJson "$TARGETS_JSON" --dryRun true
unity --json command --project-path "$PROJECT_PATH" smart.layout -- \
  --layoutType Grid --spacing 2 --columns 3 --targetsJson "$TARGETS_JSON" --dryRun false --confirm true
unity --json command --project-path "$PROJECT_PATH" smart.align-ground -- \
  --maxDistance 100 --alignRotation true --targetsJson "$TARGETS_JSON" --dryRun true
unity --json command --project-path "$PROJECT_PATH" smart.snap-grid -- \
  --gridSize 1 --targetsJson "$TARGETS_JSON" --dryRun true
```

Ground rays start at world position plus Y0.1 and point down, using `RaycastAll` and the nearest external hit. Colliders on each selected object's own hierarchy are excluded; the offset alone cannot prevent self-hits on large colliders. Misses keep their transforms. `alignRotation=true` aligns up to the surface normal. Global Physics trigger policy applies unchanged. `HitCount`, `ProcessedCount` and `ChangedCount` describe different facts.

Distribution sorts stably by sibling index, retaining supplied order for ties. Endpoints remain fixed. Interior positions interpolate the endpoint projections along the selected axis and preserve all other coordinates. X/Y/Z/-X/-Y/-Z are case-insensitive. Sign reversal produces the same distribution coordinates.

Layout uses that same stable sibling order and the original first position as start/center. Linear follows the signed axis. Grid uses positive X columns and negative Z rows. Circle starts forward and rotates offsets about Y through a full360 degrees. Arc includes both ends from -arcAngle/2 to +arcAngle/2; a singleton uses the start angle. Spacing is nonnegative and finite; columns are positive for Grid; Arc angle is finite0..360. Only applicable parameters are validated. Center facing applies to Circle/Arc; a zero direction preserves rotation. Rotation/local scale remain unchanged when no orientation is requested.

Randomization follows supplied Selection order. Each object draws world position x/y/z offsets, world Euler x/y/z offsets, then one uniform local-scale value. Zero position/rotation ranges skip those channels; scaleMin=scaleMax=1 skips scaling and its draw. Other equal positive bounds set uniform scale. Ranges must be finite and nonnegative; scale bounds finite, positive and ordered. It uses Unity's global unseeded random stream. Preview restores the stream after calculating its proposal. A committed no-op reports processed objects and zero changed objects.

Snap uses `Mathf.Round(worldCoordinate/gridSize)*gridSize` on each axis, including Unity midpoint rounding. Grid size must be finite and positive. Rotation and local scale are preserved.

A successful changed transform set occupies one Undo group, records prefab instance property overrides and dirties only changed scenes. Undo/Redo restores authored transforms. Before success, actual world pose/local scale must match the proposal within .0002 units for position/scale and .02 degrees for rotation. A mismatch reports observed/proposed values and the owned rollback outcome. Save/reopen only the affected scene when persistence is requested.

Transform result position, scale and normal fields are numeric `X/Y/Z` objects; rotation fields are numeric `X/Y/Z/W` objects. Spatial query centers use the same `X/Y/Z` shape.

## Serialized reference planning and native assignment

`smart.reference-plan` is read-only. Supply an exact `target` Component reference, or an exact GameObject with `componentName`, or unique `targetName` plus unique qualified `componentName`. Multiple matching GameObjects/components refuse. `fieldName` is required; public fields resolve exact name, then m_UpperFirst, then _name. The actual serialized path is reported. Supported collections are one-dimensional arrays or List of GameObject/Component with native ObjectReference elements. Nonserialized fields produce an unsupported plan. The plan does not invoke property getters.

Provide `sourceTag`, `sourceName`, or both. Tags are validated even when name matches exist. Sources belong to active ordinary loaded scenes, including native-eligible hidden objects. Native `GameObject.FindGameObjectsWithTag` candidates come first in native tag order. Name candidates use `AnalysisSceneObjects.NativeGameObjects` with unsorted native discovery and ordinal case-sensitive name containment, followed by first-occurrence deduplication. Component element conversion uses the attached component regardless of its hide flags and skips sources missing that component. Replace proposes only converted sources; append preserves every existing element, including null/duplicates, and deduplicates additions. `OriginalReferences`, `ProposedReferences`, `Component`, `PropertyPath` and `PropertiesJson` carry the exact native assignment proposal.

```bash
unity --json command --project-path "$PROJECT_PATH" --query smart.reference-plan --detail full
unity --json command --project-path "$PROJECT_PATH" smart.reference-plan -- \
  --target "$COMPONENT_REFERENCE" --fieldName Objects --sourceName Source --appendMode true
unity --json command --project-path "$PROJECT_PATH" --query set_component_properties --detail full
```

Before applying, re-resolve the exact component and every proposed nonnull reference, revalidate ordinary Edit-mode eligibility, and compare its current serialized collection with `OriginalReferences`. Re-run the identical plan and require the same component, path, original/proposed arrays and properties JSON. Refuse intervening differences and regenerate for review. Submit the complete `PropertiesJson` unchanged to native `set_component_properties`, targeting the exact component; omit its `type` argument. Native assignment owns collection serialization and Undo.

```bash
unity --json command --project-path "$PROJECT_PATH" set_component_properties -- \
  --target "$COMPONENT_REFERENCE" --properties "$PROPERTIES_JSON"
```

Check native actual readback against every proposed reference, including nulls. Observe one Undo/Redo group, connected prefab instance overrides, and saved-scene reopen before claiming persistence. Keep a readback failure visible; do not call a proposal or an unchecked native response a completed bind.

## Public property binding

`smart.reference-property` covers one public writable nonindexer instance property of supported array/List type. It defaults `allowPropertyAccess=false` and refuses before executing any getter/setter unless explicitly enabled. Append requires a public readable getter. Replace does not run a getter merely to obtain old items. Static/readonly/indexer properties and ambiguous component instances refuse.

```bash
unity --json command --project-path "$PROJECT_PATH" --query smart.reference-property --detail full
unity --json command --project-path "$PROJECT_PATH" smart.reference-property -- \
  --target "$COMPONENT_REFERENCE" --fieldName Backed --sourceName Source \
  --allowPropertyAccess true --dryRun true
```

Opt-in executes user code. Undo/persistence and rollback cover the target component's serialized state. A property's nonserialized backing is volatile; arbitrary effects of its setter on external objects cannot be guaranteed rollback. The result states these scopes and whether serialized target state changed. Getter/setter exceptions are failures with a target rollback outcome. A successful setter alone does not prove scene persistence. Confirm a serialized-backed property's actual Undo/Redo and saved-scene roundtrip before reporting persistent binding.

## Component predicate and Physics sphere reports

```bash
unity --json command --project-path "$PROJECT_PATH" --query smart.query --detail full
unity --json command --project-path "$PROJECT_PATH" smart.query -- \
  --componentName UnityEngine.Light --propertyName intensity --op '>' --value 2 --limit 50
unity --json command --project-path "$PROJECT_PATH" smart.spatial -- \
  --x 0 --y 0 --z 0 --radius 10 --componentFilter UnityEngine.Light --limit 50
```

`smart.query` resolves a unique Component type and exact public instance field/readable nonindexer property. It scans public native component discovery in InstanceID order across active ordinary loaded scenes, retaining several components on the same GameObject. Native GameObject and component flag eligibility is preserved, including eligible hidden targets; persistent, preview and prefab-stage objects are excluded. Reading a property opts into user getter execution; inspect that component's code when side effects matter. Getter failures appear in diagnostics even when the result cap is zero or filled. Supported operators are ==, !=, >, <, >=, <=, contains. SQL shorthand refuses. Missing/null operand refuses; empty string is valid. Null member values skip. Numeric equality uses strict absolute difference <.0001; inequality uses >=.0001. Numeric comparisons require finite invariant numbers; ordinary strings named NaN remain strings. String equality is ordinal and contains is case-insensitive. Formatting is invariant. Full totals precede the nonnegative cap; limit0 shows no rows.

`smart.spatial` uses `Physics.OverlapSphere`, preserving collider discovery order and multiple colliders on one GameObject. Exact collider and GameObject identities distinguish occurrences. A collider surface can overlap while its transform center is outside the radius; reported distance is to that transform. Ordinary loaded-scene membership filters occurrences, including hidden hierarchy objects, while global trigger policy remains unchanged. Optional component presence uses the collider GameObject without component hide-flag filtering. Center/radius must be finite, radius nonnegative, cap nonnegative and an optional filter a unique Component type. Full occurrence totals precede the cap. It is a sphere query.

## Component selection composition

Normalize `componentName`/`componentType` to one supplied type name; absent names and different supplied aliases refuse before changing Selection. Discover registered `eval`, then use this bounded public body. The shared `AnalysisSceneObjects.NativeComponents` owner handles concrete and abstract Component types with native eligibility and InstanceID order. It excludes persistent, preview and prefab-stage objects and scopes results to active ordinary loaded scenes. Eligible hidden targets participate. First-occurrence GameObject deduplication preserves native component order; an empty result clears Selection.

```bash
unity --json command --project-path "$PROJECT_PATH" --query eval --detail full
```

```csharp
string componentName = "UnityEngine.Light";
string componentType = null;
if (string.IsNullOrWhiteSpace(componentName) && string.IsNullOrWhiteSpace(componentType))
    throw new System.ArgumentException("Supply a Component type name");
if (!string.IsNullOrWhiteSpace(componentName) && !string.IsNullOrWhiteSpace(componentType) &&
    !string.Equals(componentName,componentType,System.StringComparison.OrdinalIgnoreCase))
    throw new System.ArgumentException("Supply matching Component type aliases");
var type = BatihanDev.UnityCliCommands.Analysis.AnalysisSceneObjects.ResolveType(string.IsNullOrWhiteSpace(componentName) ? componentType : componentName, typeof(UnityEngine.Component));
var components = BatihanDev.UnityCliCommands.Analysis.AnalysisSceneObjects.NativeComponents(type, sortMode: UnityEngine.FindObjectsSortMode.InstanceID);
var candidates = components.Select(component => component.gameObject).Distinct().ToArray();
UnityEditor.Selection.objects = candidates;
return new { component = type.FullName, selected = UnityEditor.Selection.gameObjects.Length,
    ids = UnityEditor.Selection.gameObjects.Select(go => UnityEngine.EntityId.ToULong(go.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray(),
    ephemeral = true, undoable = false };
```

Read back actual selected identities/count and compare their order with the candidate array. Missing, ambiguous and nonComponent types refuse. Selection is ephemeral and should be saved/restored explicitly by test fixtures. Smart authoring operations separately prevalidate their selected set before editing.

## Prefab replacement

`smart.replace` requires a confined existing Assets prefab path and an independently eligible selected set. Preview makes no objects. Commit instantiates each prefab into its original object's scene, preserves parent/world position/world rotation/local scale/sibling index, destroys the originals under one grouped Undo and selects the replacements. Unselected siblings and unrelated scenes remain intact. Nested selected ancestor/descendant sets refuse before creating or destroying anything. Connected template children and nested prefab instance objects refuse as a whole; select their outermost instance root. Actual added GameObject overrides and ordinary scene children are supported structural Undo targets.

```bash
unity --json command --project-path "$PROJECT_PATH" --query smart.replace --detail full
unity --json command --project-path "$PROJECT_PATH" smart.replace -- \
  --prefabPath Assets/Prefabs/Replacement.prefab --targetsJson "$TARGETS_JSON" --dryRun true
```

Structural Undo/Redo restores the scene shape and object lifecycle. Selection is a separate ephemeral Editor state; it is not automatically undoable. Read actual post-operation selection, and explicitly restore selection when a fixture or workflow needs it. Save/reopen the affected scene to verify prefab connection and authored placement.
