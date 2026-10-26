# NavMesh

Use these owners for the ten NavMesh recipe intents. `com.unity.ai.navigation` 2.x is needed only for `NavMeshSurface` build and data removal. Agent, obstacle, area-cost and path queries use Unity's built-in navigation APIs. Run commands against a stopped, compiled Editor; inspect the nested typed `Ok` and `Error` fields as well as outer CLI success.

| Recipe intent | Owner |
| --- | --- |
| `navmesh_bake`, `navmesh_clear` | `navmesh.surface-build` and `navmesh.surface-remove-data` on each selected surface |
| `navmesh_add_agent`, `navmesh_add_obstacle` | Native `add_component`; `set_component_properties` for requested fields |
| `navmesh_set_agent`, `navmesh_set_obstacle` | Native `get_component_properties`, then `set_component_properties` on the exact component handle |
| `navmesh_set_area_cost` | `navmesh.area-cost-set` |
| `navmesh_get_settings`, `navmesh_calculate_path`, `navmesh_sample_position` | Bounded read-only `eval` bodies below |

## Surface build and data removal

Enumerate active and enabled `NavMeshSurface` components in the active, regular scene with this read-only body. Its `target` values are exact component EntityIds. Exclude preview and prefab-stage objects. A build with no selected surfaces refuses. A clear with no selected surfaces completes with zero work. Confirm each target separately and check every result; one failed surface does not prove another surface was changed.

```csharp
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (!scene.IsValid() || !scene.isLoaded || UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(scene))
    throw new System.InvalidOperationException("A regular loaded active scene is required");
var surfaces = scene.GetRootGameObjects()
    .SelectMany(root => root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true))
    .Where(surface => surface.isActiveAndEnabled &&
        !UnityEditor.EditorUtility.IsPersistent(surface) &&
        !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(surface) &&
        UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(surface.gameObject) == null)
    .OrderBy(surface => UnityEngine.EntityId.ToULong(surface.GetEntityId()))
    .Select(surface => new {
        target = UnityEngine.EntityId.ToULong(surface.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
        objectName = surface.gameObject.name,
        hasData = surface.navMeshData != null,
        scenePath = surface.gameObject.scene.path
    }).ToArray();
return new { scene = scene.path, count = surfaces.Length, surfaces };
```

`navmesh.surface-build` requires the surface to remain active and enabled in a regular loaded scene. Preview first with `dryRun=true`; `confirm=true` calls public `NavMeshSurface.BuildNavMesh()` synchronously. The command succeeds only when a new non-null `navMeshData` reference replaces the previous reference. It reports `DataBefore`, `DataAfter`, `DataReferenceChanged`, `Dispatched`, `Undoable=false`, and `SavedAsset=false`. The data is session data and is not automatically saved as a NavMesh asset. A new reference does not prove the mesh contains walkable polygons; verify with the path and sample queries below.

`navmesh.surface-remove-data` previews and confirms similarly. Public `RemoveData()` unregisters that surface's runtime instance and retains its `navMeshData` reference and any backing asset. The command reports `DataRetained`, `Dispatched`, and `Undoable=false`. Re-enabling the surface or calling `AddData()` may register the retained data again. Inspect path/sample results after removal when absence of traversal matters.

```bash
unity --json command --project-path "$PROJECT_PATH" navmesh.surface-build -- --target "$SURFACE_ID" --dryRun true
unity --json command --project-path "$PROJECT_PATH" navmesh.surface-build -- --target "$SURFACE_ID" --confirm true
unity --json command --project-path "$PROJECT_PATH" navmesh.surface-remove-data -- --target "$SURFACE_ID" --dryRun true
unity --json command --project-path "$PROJECT_PATH" navmesh.surface-remove-data -- --target "$SURFACE_ID" --confirm true
```

## Agents and obstacles

Resolve one exact scene GameObject handle before adding a component. Reject a stale or ambiguous target, and inspect whether the requested component already exists. Native `add_component` records Undo and returns the new component handle; use that returned handle for every later read or write. `NavMeshAgent` and `NavMeshObstacle` are built-in component types. For an obstacle, the original add recipe requires `carve=true`; set it explicitly after addition if its observed default differs. Native `set_component_properties` owns one Undo step and refuses unknown serialized fields before applying its batch.

```bash
unity --json command --project-path "$PROJECT_PATH" add_component -- --target "$GAMEOBJECT_REF" --type UnityEngine.AI.NavMeshAgent
unity --json command --project-path "$PROJECT_PATH" add_component -- --target "$GAMEOBJECT_REF" --type UnityEngine.AI.NavMeshObstacle
unity --json command --project-path "$PROJECT_PATH" get_component_properties -- --target "$COMPONENT_REF"
```

For `navmesh_set_agent`, require the exact selected component to be a `NavMeshAgent` and accept only supplied finite, nonnegative `speed`, `acceleration`, `angularSpeed`, `radius`, `height`, and `stoppingDistance`. For `navmesh_set_obstacle`, require a `NavMeshObstacle` and accept only `Box` or `Capsule`, finite nonnegative size components, and optional `carving`. Check the component type returned by `get_component_properties`; preflight every requested value and serialized field on that same handle. Supply only requested fields to `set_component_properties`; omitted values remain unchanged. Read the component properties back and compare all requested and omitted values. A failed preflight leaves the component unchanged. A created component can be undone through native Undo; snapshot claims from the old workflow do not apply.

The serialized field names returned by `get_component_properties` are the write keys. On Unity 6000.6, agent speed, acceleration, angular speed, radius, height, and stopping distance map to `m_Speed`, `m_Acceleration`, `m_AngularSpeed`, `m_Radius`, `m_Height`, and `m_StoppingDistance`. Obstacle carving maps to `m_Carve`; shape maps to `m_Shape` (`Capsule=0`, `Box=1`). Obstacle public `size` maps to twice the serialized `m_Extents` vector. Read `m_Extents` first, replace only supplied public size axes with half their requested values, and send the complete three-element extents array. This preserves omitted axes. Check the public component values after the write. The installed Editor's readback remains authoritative if its serialized layout differs.

```bash
unity --json command --project-path "$PROJECT_PATH" set_component_properties -- \
  --target "$AGENT_COMPONENT_REF" --properties '{"m_Speed":3.5,"m_StoppingDistance":1}'
unity --json command --project-path "$PROJECT_PATH" set_component_properties -- \
  --target "$OBSTACLE_COMPONENT_REF" --properties '{"m_Carve":true}'
```

## Default build settings

`navmesh_get_settings` reads the first agent build settings. It returns only these four fields; it does not read global area costs.

```csharp
var settings = UnityEngine.AI.NavMesh.GetSettingsByIndex(0);
return new { success = true, agentRadius = settings.agentRadius,
    agentHeight = settings.agentHeight, agentSlope = settings.agentSlope,
    agentClimb = settings.agentClimb };
```

## Path and nearest point

For `navmesh_calculate_path`, use finite world coordinates and an area mask (default `UnityEngine.AI.NavMesh.AllAreas`). The path API's boolean false branch reports `NoPath` and `valid=false`. Otherwise preserve its `PathComplete`, `PathPartial`, or `PathInvalid` status and all corners. `valid` is true only for `PathComplete`; distance sums consecutive returned corners only for complete or partial paths. A partial route is useful evidence but does not reach the requested end.

```csharp
var start = new UnityEngine.Vector3(0f, 0f, 0f);
var end = new UnityEngine.Vector3(10f, 0f, 10f);
int areaMask = UnityEngine.AI.NavMesh.AllAreas;
bool finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
if (!finite(start.x) || !finite(start.y) || !finite(start.z) ||
    !finite(end.x) || !finite(end.y) || !finite(end.z))
    throw new System.ArgumentException("Path coordinates must be finite");
var path = new UnityEngine.AI.NavMeshPath();
var hasPath = UnityEngine.AI.NavMesh.CalculatePath(start, end, areaMask, path);
if (!hasPath) return new { status = "NoPath", valid = false, distance = 0f,
    cornerCount = 0, corners = System.Array.Empty<object>() };
var corners = path.corners;
float distance = 0f;
if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete ||
    path.status == UnityEngine.AI.NavMeshPathStatus.PathPartial)
    for (int i = 0; i + 1 < corners.Length; i++)
        distance += UnityEngine.Vector3.Distance(corners[i], corners[i + 1]);
return new { status = path.status.ToString(), valid = path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete,
    distance, cornerCount = corners.Length,
    corners = corners.Select(c => new { x = c.x, y = c.y, z = c.z }).ToArray() };
```

For `navmesh_sample_position`, the default maximum search distance is 10 world units. A miss reports `success=true, found=false`, without a point or distance.

```csharp
var source = new UnityEngine.Vector3(5f, 0f, 5f);
float maxDistance = 10f;
bool finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
if (!finite(source.x) || !finite(source.y) || !finite(source.z) ||
    !finite(maxDistance) || maxDistance < 0f)
    throw new System.ArgumentException("Sample position and maxDistance must be finite; maxDistance must be nonnegative");
if (UnityEngine.AI.NavMesh.SamplePosition(source, out var hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas))
    return new { success = true, found = true,
        point = new { x = hit.position.x, y = hit.position.y, z = hit.position.z }, distance = hit.distance };
return new { success = true, found = false };
```

## Area traversal cost

`navmesh.area-cost-set` validates index `0..31` and finite, nonnegative cost before any write. Preview with `dryRun=true`; `confirm=true` calls Unity's global `NavMesh.SetAreaCost`, reads the actual value back, and reports `Before`, `Requested`, `After`, `Applied`, and `Undoable=false`. The engine may reject a value such as zero; check `After` or the typed error, rather than assuming the requested cost took effect. Save the original `Before` and explicitly restore it through another confirmed command after temporary use. This global setting is not an Undo operation.

```bash
unity --json command --project-path "$PROJECT_PATH" navmesh.area-cost-set -- --areaIndex 3 --cost 2 --dryRun true
unity --json command --project-path "$PROJECT_PATH" navmesh.area-cost-set -- --areaIndex 3 --cost 2 --confirm true
```
