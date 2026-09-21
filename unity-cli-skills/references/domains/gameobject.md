# GameObject operations

Read [foundation routing and safety](foundation.md) first. Discover each command on the selected project before invocation. A name or path search is discovery only: duplicate, stale, or changed candidates require caller selection of a returned exact identity.

Read back the state requested by the operation. Native `batch(transactional=false,on_error=continue)` returns ordered results and continues after individual failures; default transactional batch aborts and rolls back supported scene operations. Check each result, including a workflow's nested `success` field and a typed command's `Ok`. For typed operations, use the [batch result and restoration workflow](foundation.md#typed-results-inside-a-batch). Undo is available for the scene changes described here; use it when restoration is requested.

## Creation, deletion, duplication, and lookup

`create_gameobject` accepts optional name, primitive, and parent. Omitted primitive creates Empty; allowed primitives are `cube`, `sphere`, `capsule`, `cylinder`, `plane`, and `quad`. Native parenting preserves world position, so parent-relative creation uses a transactional batch: give creation an `id`, then call `set_transform(target="$<id>.instanceId", position=[x,y,z], rotation=[0,0,0], scale=[1,1,1])`. Default the requested local position to `[0,0,0]`; supply explicit rotation/scale when requested. Read back identity, parent, local transform, and primitive components. Validate the exact parent and primitive before creation.

`create_gameobjects` has `count=1` by default and accepts a shared name/primitive/parent plus exactly-`count` local position, rotation, and scale triples. Heterogeneous creation uses ordered batch operations. Reject mismatched array lengths before creation. Use explicit local transforms for parent-relative placement.

`delete_gameobject(target)` deletes the exact selected object; identical display names do not identify the same object. Read back absence. A stale handle fails. For several targets, choose native continuation or transactional batch according to whether earlier deletions should survive a later failure.

Duplication uses an Undo-recorded public-API evaluation with exact source selection. Preserve hierarchy, components, active state, local transform, and parent; return the fresh copy identity. Copies use the source name plus `_Copy`, made unique among siblings. Batch requests run serially with per-success Undo. Transactional duplication is unsupported and must refuse before creating a copy.

The combined search below traverses loaded scenes in scene order and roots in deterministic preorder. It combines case-insensitive name substring, exact tag, named layer, component type, and nonnegative `limit=50`. Tagged search excludes inactive objects; untagged traversal includes them. Invalid filters refuse, and `count` is the returned count after limiting.

For an exact name, tag, type, or hierarchy path, discover native `find_gameobjects` and use its returned candidates. For the combined substring/layer/limit search, use the read-only evaluation below. Neither search selects a mutation target automatically. Keep decimal identities as strings; names and hierarchy paths can collide.

Detailed object inspection uses the selected identity and returns path/tag/layer, self/effective active state, local/world transform, parent, direct children, and ordered component handles. Duplicate names require an exact selection; stale identities refuse.

## Rename, state, hierarchy, and transforms

`rename_gameobject(target,name)` changes name/path while preserving identity. Empty or missing names refuse; matching another object's name is allowed. Use exact identities for subsequent commands and native batch for several renames.

`set_active(target,active)` sets the object's own active flag. Read both `activeSelf` and `activeInHierarchy`: an active child under an inactive parent remains effectively inactive. Repeating the same value is harmless. Several targets use native batch.

`set_layer(target,layer)` accepts a numeric string from 0 through 31 or an installed layer name. It changes one object. For recursive changes, freeze the selected object's descendant identities and submit one operation per object; without recursion, do not include descendants. `set_tag(target,tag)` requires an installed tag. Both operations support native batch; read back the exact targets.

Validated parenting uses a narrow public-API evaluation: resolve exact child/parent identities, refuse self/descendant cycles before mutation, call `Undo.SetTransformParent`, and verify the resulting parent. Null parent detaches. Default `worldPositionStays=true` preserves world placement; false preserves local placement. For several requests, continuation must validate each against the graph produced by preceding successes. Transactional mode must restore its entire Undo group on failure. Native `set_parent` alone does not report Unity's cycle rejection reliably.

`set_transform` takes optional local `position`, `rotation` (Euler), and `scale` triples. Omitted channels remain unchanged; to change one axis, read the current triple and retain its other axes. RectTransform anchored position, anchors, pivot, and size delta use their discovered serialized fields. `transform.world` owns independently optional world XYZ/Euler axes, and `rect-transform.size` owns physical width/height. For a mixed request, apply channels in this order: world position, local position, world Euler rotation, local scale, anchored position, anchor minimum, anchor maximum, pivot, size delta, physical width/height. Thus local position takes precedence over world position when both are supplied. Preserve omitted axes at each step. Refuse UI-only inputs on a non-RectTransform before changing any channel. Verify the resulting local/world or rectangular state.

## Combined search evaluation

Set the first two lines, then pass this body to discovered `eval` or `eval_file`. It reads loaded scenes in scene order and each hierarchy in sibling preorder. `count` is the number returned after the limit. A supplied tag excludes inactive objects; without a tag, inactive objects remain searchable. Unknown tags, layers, and nonunique component types fail without changing the scene.

```csharp
string name = null, tag = null, layer = null, component = null;
int limit = 50;
if (limit < 0) throw new System.ArgumentOutOfRangeException(nameof(limit));
// Validate the tag before filtering the loaded-scene traversal.
if (!string.IsNullOrEmpty(tag)) UnityEngine.GameObject.FindGameObjectsWithTag(tag);
int layerId = -1;
if (!string.IsNullOrEmpty(layer)) {
    layerId = UnityEngine.LayerMask.NameToLayer(layer);
    if (layerId < 0) throw new System.ArgumentException("Unknown layer: " + layer);
}
System.Type componentType = null;
if (!string.IsNullOrEmpty(component)) {
    var candidates = UnityEditor.TypeCache.GetTypesDerivedFrom<UnityEngine.Component>()
        .Concat(new[] { typeof(UnityEngine.Component) })
        .Where(t => !t.ContainsGenericParameters && (t.FullName == component || t.Name == component))
        .Distinct().ToArray();
    if (candidates.Length != 1)
        throw new System.ArgumentException("Component type must resolve uniquely: " + component);
    componentType = candidates[0];
}
string PathOf(UnityEngine.Transform t) {
    var parts = new System.Collections.Generic.List<string>();
    for (; t != null; t = t.parent) parts.Add(t.name);
    parts.Reverse(); return "/" + string.Join("/", parts);
}
var candidatesInOrder = new System.Collections.Generic.List<UnityEngine.GameObject>();
void Visit(UnityEngine.Transform t) {
    candidatesInOrder.Add(t.gameObject);
    for (int i = 0; i < t.childCount; i++) Visit(t.GetChild(i));
}
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    if (scene.isLoaded) foreach (var root in scene.GetRootGameObjects()) Visit(root.transform);
}
var objects = candidatesInOrder.Where(g =>
    (string.IsNullOrEmpty(name) || g.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0) &&
    (string.IsNullOrEmpty(tag) || (g.activeInHierarchy && g.CompareTag(tag))) &&
    (layerId < 0 || g.layer == layerId) &&
    (componentType == null || g.GetComponent(componentType) != null))
    .Take(limit).Select(g => new {
        name = g.name, instanceId = UnityEngine.EntityId.ToULong(g.GetEntityId()).ToString(),
        path = PathOf(g.transform), tag = g.tag, layer = UnityEngine.LayerMask.LayerToName(g.layer),
        position = new { x = g.transform.position.x, y = g.transform.position.y, z = g.transform.position.z }
    }).ToArray();
return new { count = objects.Length, objects };
```

## Detailed inspection evaluation

Set `entityId` to the selected decimal GameObject identity, then pass this body to discovered `eval` or `eval_file`. It returns the selected object's hierarchy, active state, transforms, direct children, and component handles. A missing, stale, unloaded, or non-GameObject identity fails without changing the scene.

```csharp
string entityId = "REPLACE_WITH_SELECTED_DECIMAL_ID";
if (!ulong.TryParse(entityId, System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture, out var rawId) || rawId == 0)
    throw new System.ArgumentException("Select a nonzero decimal GameObject identity");
var go = UnityEditor.EditorUtility.EntityIdToObject(UnityEngine.EntityId.FromULong(rawId)) as UnityEngine.GameObject;
if (go == null || !go.scene.IsValid() || !go.scene.isLoaded)
    throw new System.ArgumentException("Selected scene GameObject is missing or stale");
string PathOf(UnityEngine.Transform t) {
    var parts = new System.Collections.Generic.List<string>();
    for (; t != null; t = t.parent) parts.Add(t.name);
    parts.Reverse(); return "/" + string.Join("/", parts);
}
object Vec(UnityEngine.Vector3 v) => new {x=v.x,y=v.y,z=v.z};
var children = new System.Collections.Generic.List<object>();
for (int i = 0; i < go.transform.childCount; i++) {
    var child = go.transform.GetChild(i);
    children.Add(new { name=child.name, instanceId=UnityEngine.EntityId.ToULong(child.gameObject.GetEntityId()).ToString(), path=PathOf(child) });
}
var handles = go.GetComponents<UnityEngine.Component>();
var rect = go.transform as UnityEngine.RectTransform;
return new {
    name=go.name, instanceId=UnityEngine.EntityId.ToULong(go.GetEntityId()).ToString(), path=PathOf(go.transform),
    tag=go.tag, layer=UnityEngine.LayerMask.LayerToName(go.layer),
    isActive=go.activeSelf, activeInHierarchy=go.activeInHierarchy,
    position=Vec(go.transform.position), rotation=Vec(go.transform.eulerAngles), scale=Vec(go.transform.localScale),
    localPosition=Vec(go.transform.localPosition), localRotation=Vec(go.transform.localEulerAngles),
    parent=go.transform.parent == null ? null : go.transform.parent.name,
    parentPath=go.transform.parent == null ? null : PathOf(go.transform.parent),
    childCount=children.Count, children,
    components=handles.Where(c=>c!=null).Select(c=>c.GetType().Name).ToArray(),
    componentHandles=handles.Select(c=>c==null ? null : new {instanceId=UnityEngine.EntityId.ToULong(c.GetEntityId()).ToString(),type=c.GetType().FullName}).ToArray(),
    rectTransform=rect==null ? null : new {
        anchoredPosition=new {x=rect.anchoredPosition.x,y=rect.anchoredPosition.y},
        sizeDelta=new {x=rect.sizeDelta.x,y=rect.sizeDelta.y},
        size=new {width=rect.rect.width,height=rect.rect.height}
    }
};
```

## Duplication evaluation

Set `sourceHandles` to one or more exact unsigned-decimal GameObject identities, then pass this body to discovered `eval` or `eval_file`. It processes requests in order and returns one result per source. Each successful copy has its own Undo group. `transactional=true` is unsupported and returns a refusal before resolving or creating a copy; use `transactional=false`.

```csharp
// Supply one or more exact unsigned-decimal GameObject EntityId values.
string[] sourceHandles = new[] { "REPLACE_WITH_SOURCE_DECIMAL_ID" };
bool transactional = false;

ulong ParseHandle(string value) {
    if (!ulong.TryParse(value, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        throw new System.ArgumentException("Each source handle must be an unsigned decimal EntityId string");
    return parsed;
}
UnityEngine.GameObject Resolve(string value) {
    return UnityEditor.EditorUtility.EntityIdToObject(
        UnityEngine.EntityId.FromULong(ParseHandle(value))) as UnityEngine.GameObject;
}
string HandleOf(UnityEngine.Object value) => value == null ? null :
    UnityEngine.EntityId.ToULong(value.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
string PathOf(UnityEngine.GameObject value) {
    var parts = new System.Collections.Generic.List<string>();
    for (var t = value.transform; t != null; t = t.parent) parts.Add(t.name);
    parts.Reverse();
    return string.Join("/", parts);
}
object Failure(string sourceHandle, string error) => new { success = false, sourceHandle, error };

if (transactional)
{
    var refusedHandles = sourceHandles ?? System.Array.Empty<string>();
    return new {
        success = false, transactional, totalItems = refusedHandles.Length, successCount = 0,
        failCount = refusedHandles.Length, refusedBeforeMutation = true,
        error = "Transactional duplication is unsupported; set transactional=false.",
        results = refusedHandles.Select(handle =>
            Failure(handle, "Not run because transactional duplication is unsupported")).ToArray()
    };
}

if (sourceHandles == null || sourceHandles.Length == 0)
    throw new System.ArgumentException("sourceHandles must contain at least one exact GameObject handle");

var resolved = new UnityEngine.GameObject[sourceHandles.Length];
var validationErrors = new string[sourceHandles.Length];
for (var index = 0; index < sourceHandles.Length; index++) {
    try {
        resolved[index] = Resolve(sourceHandles[index]);
        if (resolved[index] == null)
            validationErrors[index] = "Source handle is stale or does not resolve to a GameObject";
    } catch (System.Exception exception) {
        validationErrors[index] = exception.Message;
    }
}

var results = new System.Collections.Generic.List<object>();
var successCount = 0;
for (var index = 0; index < sourceHandles.Length; index++) {
    var sourceHandle = sourceHandles[index];
    var source = resolved[index];
    if (validationErrors[index] != null) {
        results.Add(Failure(sourceHandle, validationErrors[index]));
        continue;
    }

    // Every successful copy has its own Undo group, so one Undo removes only that copy.
    UnityEngine.GameObject copy = null;
    var previousActiveScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    var changedActiveScene = false;
    try {
        if (source.transform.parent == null && previousActiveScene != source.scene) {
            if (!UnityEngine.SceneManagement.SceneManager.SetActiveScene(source.scene))
                throw new System.InvalidOperationException("Could not make the source scene active for root-copy naming");
            changedActiveScene = true;
        }
        var uniqueName = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(
            source.transform.parent, source.name + "_Copy");
        UnityEditor.Undo.IncrementCurrentGroup();
        var undoGroup = UnityEditor.Undo.GetCurrentGroup();
        UnityEditor.Undo.SetCurrentGroupName("Duplicate " + source.name);
        copy = UnityEngine.Object.Instantiate(source, source.transform.parent);
        copy.name = uniqueName;
        copy.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
        if (copy.scene != source.scene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, source.scene);
        UnityEditor.Undo.RegisterCreatedObjectUndo(copy, "Duplicate " + source.name);
        results.Add(new {
            success = true, sourceHandle, sourceName = source.name, copyHandle = HandleOf(copy),
            copyName = copy.name, copyPath = PathOf(copy), copyScene = copy.scene.path, parentHandle = HandleOf(copy.transform.parent == null ? null : copy.transform.parent.gameObject),
            siblingIndex = copy.transform.GetSiblingIndex(), undoGroup
        });
        successCount++;
    } catch (System.Exception exception) {
        if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
        results.Add(Failure(sourceHandle, exception.Message));
    } finally {
        if (changedActiveScene)
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousActiveScene);
    }
}
return new {
    success = successCount == sourceHandles.Length, transactional, totalItems = sourceHandles.Length,
    successCount, failCount = sourceHandles.Length - successCount, refusedBeforeMutation = false,
    results = results.ToArray()
};
```

## Validated parenting evaluation

Supply matching arrays of exact child and parent identities; a null parent detaches. One item handles a single request. With `transactional=false`, failures leave that item unchanged and later items continue; each successful change has its own Undo group. With `transactional=true`, the first failure rolls back preceding changes and skips remaining items. Refusals and no-op requests create no Undo entry.

```csharp
string[] targets = { "REPLACE_WITH_CHILD_ID" };
string[] parents = { null };
bool worldPositionStays = true;
bool transactional = false;
if (targets == null || parents == null || targets.Length != parents.Length)
    throw new System.ArgumentException("Supply one parent entry per child; null detaches");
UnityEngine.GameObject Resolve(string text) {
    if (!ulong.TryParse(text, System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture, out var id) || id == 0)
        throw new System.ArgumentException("Select an exact unsigned decimal GameObject identity");
    var go = UnityEditor.EditorUtility.EntityIdToObject(UnityEngine.EntityId.FromULong(id)) as UnityEngine.GameObject;
    if (go == null || !go.scene.IsValid() || !go.scene.isLoaded)
        throw new System.ArgumentException("Selected scene GameObject is missing or stale");
    return go;
}
string Id(UnityEngine.GameObject go) => go == null ? null :
    UnityEngine.EntityId.ToULong(go.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
object Vector(UnityEngine.Vector3 v) => new { x = v.x, y = v.y, z = v.z };
var results = new System.Collections.Generic.List<object>();
int transactionGroup = -1, successCount = 0, failCount = 0;
bool reverted = false;
for (int i = 0; i < targets.Length; i++) {
    int itemGroup = -1;
    try {
        var child = Resolve(targets[i]);
        var parent = parents[i] == null ? null : Resolve(parents[i]);
        if (parent != null && parent.transform.IsChildOf(child.transform))
            throw new System.ArgumentException("A parent cannot be the child or its descendant");
        bool changed = child.transform.parent != (parent == null ? null : parent.transform);
        if (changed) {
            if (!transactional || transactionGroup < 0) {
                UnityEditor.Undo.IncrementCurrentGroup();
                itemGroup = UnityEditor.Undo.GetCurrentGroup();
                if (transactional) transactionGroup = itemGroup;
            } else itemGroup = transactionGroup;
            UnityEditor.Undo.SetTransformParent(child.transform,
                parent == null ? null : parent.transform, worldPositionStays, "Set Parent");
            if (child.transform.parent != (parent == null ? null : parent.transform))
                throw new System.InvalidOperationException("Unity did not apply the requested parent");
        }
        results.Add(new { index = i, success = true, target = Id(child), parent = Id(parent), changed,
            localPosition = Vector(child.transform.localPosition), worldPosition = Vector(child.transform.position) });
        successCount++;
    } catch (System.Exception error) {
        if (transactional && transactionGroup >= 0) {
            UnityEditor.Undo.RevertAllDownToGroup(transactionGroup);
            reverted = true;
        } else if (itemGroup >= 0) UnityEditor.Undo.RevertAllDownToGroup(itemGroup);
        results.Add(new { index = i, success = false, target = targets[i], error = error.Message });
        failCount++;
        if (transactional) break;
    }
}
if (transactional && transactionGroup >= 0 && !reverted)
    UnityEditor.Undo.CollapseUndoOperations(transactionGroup);
return new { success = failCount == 0, totalItems = targets.Length, successCount, failCount,
    skippedCount = targets.Length - successCount - failCount, transactional, reverted, results };
```
