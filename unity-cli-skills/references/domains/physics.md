# Physics workflows

Read [foundation routing and safety](foundation.md), [asset operations](asset.md), and [component operations](component.md) first. Discover each selected project's live command schema before invocation. Physics queries return world-space points and normals. Adding a Rigidbody or Collider belongs to the component workflow; simulation runs in Play Mode.

## Casts and overlaps

Use discovered `eval` with `--code` for the six bounded read-only queries below. Replace only the input declarations. `maxDistance` defaults to `1000f`, `layerMask` to `-1`, and box half extents to `(0.5f, 0.5f, 0.5f)`. Validate every numeric input as finite. Reject a zero direction before normalizing. Reject negative distance, radius, or half extents. Call `Physics.SyncTransforms()` before a query when the fixture's transforms were just changed. Choose the layer mask explicitly when other scene colliders could affect a result. Unity's default trigger policy applies unless the caller supplies a `QueryTriggerInteraction` overload; report the policy used with the captured result.

This `eval` body covers `physics_raycast`, `physics_raycast_all`, `physics_spherecast`, and `physics_boxcast`. Set `kind` to the requested cast and supply `radius` or `halfExtents` for its shape. A single cast returns `{ hit: true, result: { collider, colliderInstanceId, objectName, objectInstanceId, instanceId, path, point, normal, distance } }`; read its hit fields from `result`. A miss returns `{ hit: false }`. `raycast_all` returns `{ count, hits }`, with those identity and geometry fields on each hit. Identities are unsigned decimal EntityId strings. All hits are sorted by distance, with collider ID as a stable tie breaker.

```csharp
string kind = "raycast"; // raycast, raycast_all, spherecast, boxcast
var origin = new UnityEngine.Vector3(0f, 5f, 0f);
var direction = new UnityEngine.Vector3(0f, -1f, 0f);
float maxDistance = 1000f;
int layerMask = -1;
float radius = 0.5f;
var halfExtents = new UnityEngine.Vector3(0.5f, 0.5f, 0.5f);
bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
bool VectorFinite(UnityEngine.Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
if (!VectorFinite(origin) || !VectorFinite(direction) || !Finite(maxDistance) ||
    !Finite(radius) || !VectorFinite(halfExtents) || maxDistance < 0f ||
    radius < 0f || halfExtents.x < 0f || halfExtents.y < 0f || halfExtents.z < 0f)
    throw new System.ArgumentException("Cast inputs must be finite and dimensions nonnegative");
if (direction.sqrMagnitude < 1e-6f)
    throw new System.ArgumentException("Direction vector cannot be zero");
direction.Normalize();
string PathOf(UnityEngine.GameObject go) {
    var names = new System.Collections.Generic.List<string>();
    for (var t = go.transform; t != null; t = t.parent) names.Add(t.name);
    names.Reverse();
    return string.Join("/", names);
}
object Hit(UnityEngine.RaycastHit h) => new {
    collider = h.collider.name,
    colliderInstanceId = UnityEngine.EntityId.ToULong(h.collider.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
    objectName = h.collider.gameObject.name,
    objectInstanceId = UnityEngine.EntityId.ToULong(h.collider.gameObject.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
    instanceId = UnityEngine.EntityId.ToULong(h.collider.gameObject.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
    path = PathOf(h.collider.gameObject),
    point = new { x = h.point.x, y = h.point.y, z = h.point.z },
    normal = new { x = h.normal.x, y = h.normal.y, z = h.normal.z },
    distance = h.distance
};
UnityEngine.Physics.SyncTransforms();
if (kind == "raycast_all") {
    var hits = UnityEngine.Physics.RaycastAll(origin, direction, maxDistance, layerMask)
        .OrderBy(h => h.distance).ThenBy(h => UnityEngine.EntityId.ToULong(h.collider.GetEntityId())).Select(Hit).ToArray();
    return new { count = hits.Length, hits };
}
UnityEngine.RaycastHit hit;
bool found;
if (kind == "raycast") found = UnityEngine.Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
else if (kind == "spherecast") found = UnityEngine.Physics.SphereCast(origin, radius, direction, out hit, maxDistance, layerMask);
else if (kind == "boxcast") found = UnityEngine.Physics.BoxCast(origin, halfExtents, direction, out hit, UnityEngine.Quaternion.identity, maxDistance, layerMask);
else throw new System.ArgumentException("Unknown cast kind");
return found ? new { hit = true, result = Hit(hit) } : (object)new { hit = false };
```

For `physics_check_overlap`, select `sphere`; for `physics_overlap_box`, select `box`. Sphere `radius` and box half extents must be finite and nonnegative. The result includes collider name for the sphere contract and object path and trigger state for both. Exact collider IDs disambiguate repeated names.

```csharp
string kind = "sphere"; // sphere or box
var center = new UnityEngine.Vector3(0f, 0f, 0f);
float radius = 1f;
var halfExtents = new UnityEngine.Vector3(0.5f, 0.5f, 0.5f);
int layerMask = -1;
bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
if (!Finite(center.x) || !Finite(center.y) || !Finite(center.z) || !Finite(radius) || radius < 0f ||
    !Finite(halfExtents.x) || !Finite(halfExtents.y) || !Finite(halfExtents.z) ||
    halfExtents.x < 0f || halfExtents.y < 0f || halfExtents.z < 0f)
    throw new System.ArgumentException("Overlap inputs must be finite and dimensions nonnegative");
UnityEngine.Physics.SyncTransforms();
UnityEngine.Collider[] colliders;
if (kind == "sphere") colliders = UnityEngine.Physics.OverlapSphere(center, radius, layerMask);
else if (kind == "box") colliders = UnityEngine.Physics.OverlapBox(center, halfExtents, UnityEngine.Quaternion.identity, layerMask);
else throw new System.ArgumentException("Unknown overlap kind");
var rows = colliders.OrderBy(c => UnityEngine.EntityId.ToULong(c.GetEntityId())).Select(c => {
    var names = new System.Collections.Generic.List<string>();
    for (var t = c.transform; t != null; t = t.parent) names.Add(t.name);
    names.Reverse();
    return new { collider = c.name,
        colliderInstanceId = UnityEngine.EntityId.ToULong(c.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
        objectName = c.gameObject.name, path = string.Join("/", names), isTrigger = c.isTrigger };
}).ToArray();
return new { count = rows.Length, colliders = rows };
```

## Gravity

Use native `get_physics_settings` for `physics_get_gravity`; read `gravityX`, `gravityY`, and `gravityZ` from the returned settings. For `physics_set_gravity`, capture all three values, then call native `set_physics_settings` with `settings={"gravityX":0,"gravityY":-9.81,"gravityZ":0}` and `dry_run=true`. Check the preview, call again with `confirm=true`, and read all three components back with `get_physics_settings`. If the workflow is temporary, restore the captured vector through the same confirmed native command and verify the restore. This is a project setting write without an Undo guarantee. Validate requested components as finite before sending them.

```bash
unity --json command --project-path "$PROJECT_PATH" get_physics_settings
unity --json command --project-path "$PROJECT_PATH" set_physics_settings -- \
  --settings '{"gravityX":0,"gravityY":-9.81,"gravityZ":0}' --dry_run true
unity --json command --project-path "$PROJECT_PATH" set_physics_settings -- \
  --settings '{"gravityX":0,"gravityY":-9.81,"gravityZ":0}' --confirm true
```

## PhysicsMaterial assets

For `physics_create_material`, use the native `create_asset` with `type="UnityEngine.PhysicsMaterial"` and a confined `Assets/.../*.physicMaterial` destination. The default name is `New PhysicMaterial`, the default folder is `Assets`, and the default `dynamicFriction`, `staticFriction`, and `bounciness` are `0.6`, `0.6`, and `0`. Reject an empty name or a name containing `/`, `\\`, or `..`. A read-only `AssetDatabase.GenerateUniqueAssetPath(preferredPath)` evaluation can allocate a distinct destination. Check the destination is vacant again before creation and leave `confirm=false` so a collision refuses. Never use native overwrite confirmation to implement the original unique-path contract. Native creation creates missing parent folders under the authoring root. The PhysicsMaterial factory supplies the three defaults. To apply caller values, preflight the created asset's exact type and discover its serialized float field names with native `get_serialized_fields`; call native `set_serialized_field(target,field,value)` once for each supplied field. Save, reload, and compare all three public values. The serialized field setter owns its Undo scope; asset creation remains non-Undo. Cleanup removes only an asset created by the workflow.

```bash
unity --json command --project-path "$PROJECT_PATH" create_asset -- \
  --path Assets/Materials/Grip.physicMaterial --type UnityEngine.PhysicsMaterial
unity --json command --project-path "$PROJECT_PATH" get_serialized_fields -- \
  --target Assets/Materials/Grip.physicMaterial
```

Use the returned exact serialized field paths for the three optional setter calls; they may differ from public property names. The caller owns the save boundary and must reload before reporting the authored values.

For `physics_set_material`, resolve the target GameObject by one exact scene handle and enumerate its Colliders. Require exactly one selected Collider; if more than one exists, the caller must select a component handle. Preflight the material path as a loadable `UnityEngine.PhysicsMaterial`, then call `component.member-set` with that Collider's exact handle, `member="sharedMaterial"`, and `reference` set to the asset path. Check the nested typed `Ok` and read `Collider.sharedMaterial` back on that exact component. A missing asset, wrong type, stale handle, or ambiguous Collider selection refuses before mutation. The component setter records Undo; retain the prior shared material for explicit restoration when needed.

## Layer collision matrix

For `physics_get_layer_collision`, validate both integer layer indices from `0` through `31`, then use a bounded read-only `eval`:

```csharp
int layer1 = 0, layer2 = 8;
if (layer1 < 0 || layer1 > 31 || layer2 < 0 || layer2 > 31)
    throw new System.ArgumentOutOfRangeException("layer indices must be 0 through 31");
return new { layer1, layer2,
    collisionEnabled = !UnityEngine.Physics.GetIgnoreLayerCollision(layer1, layer2) };
```

For `physics_set_layer_collision`, call the package's `physics.layer-collision-set` with `layer1`, `layer2`, optional `enableCollision=true`, and `dryRun=true` to capture `Before` and `Requested`. A real write requires `confirm=true`; the command reads `After` from Unity and returns `Applied`, `DryRun`, and `Undoable=false`. Restore `Before` through another confirmed call and verify its readback after a temporary change. Invalid indices and missing confirmation refuse without changing the matrix.

```bash
unity --json command --project-path "$PROJECT_PATH" physics.layer-collision-set -- \
  --layer1 0 --layer2 8 --enableCollision true --dryRun true
unity --json command --project-path "$PROJECT_PATH" physics.layer-collision-set -- \
  --layer1 0 --layer2 8 --enableCollision true --confirm true
```
