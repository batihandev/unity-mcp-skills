# Prefab operations

Read [foundation routing and safety](foundation.md) first. Discover every native command against the selected project before invoking it. A search result, name, hierarchy path, or prefab path is discovery evidence, never authority to mutate a later object. Retain the returned exact unsigned-decimal EntityId strings; do not transport them as JSON numbers.

Prefab work has two state surfaces. Scene instances are connected GameObjects whose reversible changes may be recorded in scene Undo. Prefab assets are persistent files; creating, overwriting, or changing their serialized data must be treated as asset work and verified after a reload. Read the selected instance and source asset before and after every mutation.

## Create and instantiate

Use `create_prefab(source,path)`. `source` is one selected scene GameObject reference; `path` is a path relative to the authoring root, with an optional `Assets/` prefix and an optional `.prefab` extension. It creates the asset and connects the source to it. Read the selected source and resulting asset back before continuing. The command takes no confirmation, dry-run, or overwrite parameter.

Use `create_prefab_variant(base,path)`. `base` is the selected prefab asset reference and `path` is a variant asset path relative to the authoring root, with `.prefab` added when omitted. Read the resulting asset and its selected base relationship back. The command takes no confirmation, dry-run, or overwrite parameter.

Both creation commands can overwrite an existing destination while retaining its GUID. Before calling either command, validate the confined destination and its current state. An existing destination requires replacement to be covered by the requested change; retain its bytes and metadata for restoration. A create-only request refuses an existing destination before invoking Unity.

Use `instantiate_prefab(prefab,scene_path?,name?)`. `prefab` is the selected prefab asset reference; `scene_path` selects a loaded destination scene and defaults to the active scene; `name` is optional and defaults to the prefab name. The command has no parent or transform fields, so it does not provide a parent-relative or world-transform input. For parent-relative placement, instantiate first, retain its exact identity, use the [validated parenting evaluation](gameobject.md#validated-parenting-evaluation) with `worldPositionStays=false`, then use `set_transform` for the requested local position, rotation, and scale. For world placement, use `set_transform` without parenting. Read local and world transforms back after those steps.

For two or more instances, use the native ordered batch form of `instantiate_prefab`. Each item uses that command's selected `prefab` and optional `scene_path`/`name` fields. Preserve order and inspect each result, including any nested typed `Ok` result. Use continuation when earlier successful instances should remain after a later failure; an S-F-S request must create the first and third instances and report the middle failure. Default transactional batch has different semantics: it must be used only when every item is eligible for its supported scene transaction and its returned rollback/Undo outcome is verified. A typed `Ok:false` does not itself fail Pipeline's transaction; follow [the typed batch restoration workflow](foundation.md#typed-results-inside-a-batch).

## Find instances and inspect overrides

`find_gameobjects` is suitable for exact scene-object discovery. To find every instance of a selected prefab, use the following narrow read-only `eval` or `eval_file` body. `scenePaths` is optional: omit it to search all loaded scenes, or supply one or more exact loaded scene paths to limit the search. It compares the nearest instance root's asset path by ordinal equality and projects only scalar data.

```csharp
string prefabPath = "Assets/Prefabs/Example.prefab";
string[] scenePaths = null;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
if (prefab == null || !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(prefab))
    throw new System.ArgumentException("prefabPath must identify a prefab asset");
var selectedScenes = scenePaths == null ? null : new System.Collections.Generic.HashSet<string>(
    scenePaths, System.StringComparer.Ordinal);
string Id(UnityEngine.Object value) => value == null ? null :
    UnityEngine.EntityId.ToULong(value.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
string PathOf(UnityEngine.Transform value) {
    var parts = new System.Collections.Generic.List<string>();
    for (var current = value; current != null; current = current.parent) parts.Add(current.name);
    parts.Reverse(); return "/" + string.Join("/", parts);
}
var instances = new System.Collections.Generic.List<object>();
for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    if (!scene.isLoaded) continue;
    if (selectedScenes != null && !selectedScenes.Contains(scene.path)) continue;
    foreach (var root in scene.GetRootGameObjects()) {
        foreach (var transform in root.GetComponentsInChildren<UnityEngine.Transform>(true)) {
            var instanceRoot = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(transform.gameObject);
            if (instanceRoot != transform.gameObject ||
                !string.Equals(UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot), prefabPath,
                    System.StringComparison.Ordinal)) continue;
            instances.Add(new { instanceId = Id(instanceRoot), name = instanceRoot.name,
                path = PathOf(instanceRoot.transform), scene = scene.path });
        }
    }
}
return new { prefabPath, count = instances.Count, instances = instances.ToArray() };
```

For selected-instance override inspection, resolve the exact scene GameObject handle and use its outermost prefab root. The following read-only body reports property, added-component, removed-component, added-GameObject, and removed-GameObject counts, plus exact property paths. It never returns Unity object graphs.

```csharp
string instanceId = "18446744073709542610";
if (!ulong.TryParse(instanceId, System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture, out var rawId) || rawId == 0)
    throw new System.ArgumentException("instanceId must be an unsigned decimal EntityId string");
var selected = UnityEditor.EditorUtility.EntityIdToObject(
    UnityEngine.EntityId.FromULong(rawId)) as UnityEngine.GameObject;
if (selected == null || !selected.scene.IsValid() || !selected.scene.isLoaded)
    throw new System.ArgumentException("instanceId is stale or does not resolve to a loaded scene GameObject");
var root = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(selected);
if (root == null) throw new System.ArgumentException("Selected GameObject is not a prefab instance");
string Id(UnityEngine.Object value) => value == null ? null :
    UnityEngine.EntityId.ToULong(value.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
var modifications = UnityEditor.PrefabUtility.GetPropertyModifications(root) ??
    System.Array.Empty<UnityEditor.PropertyModification>();
var propertyOverrides = modifications.Where(modification => modification.target != null).Select(modification => new {
    targetId = Id(modification.target), propertyPath = modification.propertyPath, value = modification.value
}).ToArray();
var addedComponents = UnityEditor.PrefabUtility.GetAddedComponents(root).Select(item => new {
    componentId = Id(item.instanceComponent)
}).ToArray();
var removedComponents = UnityEditor.PrefabUtility.GetRemovedComponents(root).Select(item => new {
    containingInstanceId = Id(item.containingInstanceGameObject), assetComponentId = Id(item.assetComponent)
}).ToArray();
var addedGameObjects = UnityEditor.PrefabUtility.GetAddedGameObjects(root).Select(item => new {
    instanceId = Id(item.instanceGameObject), siblingIndex = item.siblingIndex
}).ToArray();
var removedGameObjects = UnityEditor.PrefabUtility.GetRemovedGameObjects(root).Select(item => new {
    parentInstanceId = Id(item.parentOfRemovedGameObjectInInstance), assetGameObjectId = Id(item.assetGameObject)
}).ToArray();
return new {
    rootId = Id(root), prefabPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root),
    propertyOverrides, propertyOverrideCount = propertyOverrides.Length,
    addedComponents, addedComponentCount = addedComponents.Length,
    removedComponents, removedComponentCount = removedComponents.Length,
    addedGameObjects, addedGameObjectCount = addedGameObjects.Length,
    removedGameObjects, removedGameObjectCount = removedGameObjects.Length,
    hasOverrides = propertyOverrides.Length != 0 || addedComponents.Length != 0 ||
        removedComponents.Length != 0 || addedGameObjects.Length != 0 || removedGameObjects.Length != 0
};
```

These raw property counts can include Unity's default root Transform/name modifications on a fresh instance. A nonzero count alone does not establish an authored override, and apply/revert need not reduce it to zero. Compare the exact authored properties and structural changes before and after the operation.

Use this inspection immediately before any full-override operation and again afterwards. A strict requested subset has no complete owner here: refuse it rather than widening it to a full apply or revert.

## Apply, revert, and unpack

`apply_prefab_overrides(instance)` applies the complete override set of the exact selected instance to its source asset. First inspect the outermost root and ensure the complete set is covered by the caller's requested change. Existing task authorization is sufficient. Reload the source asset and inspect the instance again. The command takes only `instance`; it has no confirmation parameter.

`revert_prefab_overrides(instance)` discards the complete override set. Its preview is the inspection above; the caller's request must cover discarding that complete set. Inspect the exact resulting state afterwards. The command takes only `instance`; it has no confirmation parameter.

`unpack_prefab(instance,completely=false)` disconnects a selected instance. Preview the selected outermost root and source path and apply the disconnect covered by the caller's request. For this workflow, default an omitted caller choice to `completely=true`, even though the native command's default is `false`: `false` unpacks the outermost level; `true` unpacks all nested prefab levels. Inspect the selected connection and source asset afterwards. The command has no confirmation parameter.

## Edit one prefab-asset serialized property

This operation targets a prefab asset, never a scene instance. First validate the confined prefab path according to [foundation](foundation.md#path-contract). Before selecting a value or calling a native writer, run this narrow read-only `eval` or `eval_file` body. `assetPath` is already confined; `gameObjectName`, `childPath`, and `componentLocalFileId` are optional; `componentType` is the exact fully qualified type name; and `propertyName` is required. With no child selector, it selects the prefab root. A nonempty `childPath` selects that exact prefab-relative path. Otherwise it tries exact relative `Transform.Find(gameObjectName)`, then deep exact-name candidates. It refuses ambiguity with exact candidate strings; it never selects the first duplicate.

```csharp
string assetPath = "Assets/Prefabs/Example.prefab";
string gameObjectName = null;
string childPath = null;
string componentType = "Example.Namespace.ExampleComponent";
string componentLocalFileId = null;
string propertyName = "speed";

if (string.IsNullOrEmpty(componentType))
    throw new System.ArgumentException("componentType is required");
if (string.IsNullOrEmpty(propertyName))
    throw new System.ArgumentException("propertyName is required");
var root = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(assetPath);
if (root == null || !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(root))
    throw new System.ArgumentException("assetPath must identify a prefab asset");
if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(root, out var guid, out long rootLocalFileId))
    throw new System.InvalidOperationException("Could not resolve the prefab asset GUID");
string RelativePath(UnityEngine.Transform value) {
    if (value == root.transform) return "";
    var parts = new System.Collections.Generic.List<string>();
    for (var current = value; current != root.transform; current = current.parent) {
        if (current == null) throw new System.InvalidOperationException("Transform is outside the prefab root");
        parts.Add(current.name);
    }
    parts.Reverse();
    return string.Join("/", parts);
}
string Id(UnityEngine.Object value) => value == null ? null :
    UnityEngine.EntityId.ToULong(value.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
var selected = root.transform;
if (!string.IsNullOrEmpty(childPath)) {
    selected = root.transform.Find(childPath);
    if (selected == null)
        throw new System.ArgumentException("childPath does not identify a child of the prefab root");
} else if (!string.IsNullOrEmpty(gameObjectName)) {
    selected = root.transform.Find(gameObjectName);
    if (selected == null) {
        var matches = new System.Collections.Generic.List<UnityEngine.Transform>();
        foreach (var candidate in root.GetComponentsInChildren<UnityEngine.Transform>(true))
            if (candidate != root.transform && string.Equals(candidate.name, gameObjectName, System.StringComparison.Ordinal))
                matches.Add(candidate);
        if (matches.Count == 0)
            throw new System.ArgumentException("gameObjectName did not identify a child of the prefab root");
        if (matches.Count != 1) {
            var candidates = new System.Collections.Generic.List<string>();
            foreach (var match in matches) candidates.Add(RelativePath(match));
            return new { resolved = false, reason = "ambiguous child name", candidateChildPaths = candidates.ToArray() };
        }
        selected = matches[0];
    }
}
var typedComponents = new System.Collections.Generic.List<UnityEngine.Component>();
foreach (var candidate in selected.GetComponents<UnityEngine.Component>())
    if (candidate != null && string.Equals(candidate.GetType().FullName, componentType, System.StringComparison.Ordinal))
        typedComponents.Add(candidate);
if (typedComponents.Count == 0)
    throw new System.ArgumentException("componentType is not present on the selected child");
UnityEngine.Component component = null;
if (!string.IsNullOrEmpty(componentLocalFileId)) {
    foreach (var candidate in typedComponents) {
        if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out var candidateGuid, out long candidateLocalFileId))
            throw new System.InvalidOperationException("Could not resolve a component local file ID");
        if (string.Equals(candidateLocalFileId.ToString(System.Globalization.CultureInfo.InvariantCulture), componentLocalFileId,
                System.StringComparison.Ordinal)) {
            component = candidate;
            break;
        }
    }
    if (component == null)
        throw new System.ArgumentException("componentLocalFileId does not identify the requested component on the selected child");
} else if (typedComponents.Count == 1) {
    component = typedComponents[0];
} else {
    var candidateLocalFileIds = new System.Collections.Generic.List<string>();
    foreach (var candidate in typedComponents) {
        if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out var candidateGuid, out long candidateLocalFileId))
            throw new System.InvalidOperationException("Could not resolve a component local file ID");
        candidateLocalFileIds.Add(candidateLocalFileId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    return new { resolved = false, reason = "ambiguous component type", childPath = RelativePath(selected),
        componentType, candidateComponentLocalFileIds = candidateLocalFileIds.ToArray() };
}
if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var componentGuid, out long componentLocalFileIdValue))
    throw new System.InvalidOperationException("Could not resolve the selected component local file ID");
using (var serialized = new UnityEditor.SerializedObject(component)) {
    var property = serialized.FindProperty(propertyName);
    if (property == null) {
        var capitalized = char.ToUpperInvariant(propertyName[0]) + propertyName.Substring(1);
        property = serialized.FindProperty("m_" + capitalized) ??
            serialized.FindProperty("_" + propertyName) ??
            serialized.FindProperty("m_" + propertyName);
    }
    if (property == null) {
        var availablePropertyNames = new System.Collections.Generic.List<string>();
        var iterator = serialized.GetIterator();
        for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false) {
            if (iterator.name != "m_Script" && availablePropertyNames.Count < 30)
                availablePropertyNames.Add(iterator.name);
        }
        return new { resolved = false, reason = "property not found", availablePropertyNames = availablePropertyNames.ToArray() };
    }
    return new {
        resolved = true, resolvedRootPath = UnityEditor.AssetDatabase.GetAssetPath(root), guid,
        childPath = RelativePath(selected), componentTypeName = component.GetType().FullName,
        componentLocalFileId = componentLocalFileIdValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
        propertyPath = property.propertyPath, propertyType = property.propertyType.ToString(), componentEntityId = Id(component)
    };
}
```

For an asset reference, run this second read-only preflight after the component and exact serialized path resolve. `componentEntityId` and `propertyPath` are the exact scalar values returned above, and `assetReferencePath` has already passed the foundation path contract. The preflight resolves the declared field through public reflection over instance fields, including base fields and array/list element segments. When a native serialized property has no matching managed field, it resolves the public `SerializedProperty.type` token to an exact loaded `UnityEngine.<Type>` when present; otherwise it requires exactly one matching loaded `UnityEngine.Object` type. It refuses missing, ambiguous, or incompatible types before mutation.

```csharp
string componentEntityId = "1099511627776";
string propertyPath = "materialReference";
string assetReferencePath = "Assets/Materials/Example.mat";
if (!ulong.TryParse(componentEntityId, System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture, out var rawId) || rawId == 0)
    throw new System.ArgumentException("componentEntityId must be an unsigned decimal EntityId string");
var component = UnityEditor.EditorUtility.EntityIdToObject(
    UnityEngine.EntityId.FromULong(rawId)) as UnityEngine.Component;
if (component == null)
    throw new System.ArgumentException("componentEntityId is stale or does not resolve to a component");
System.Reflection.FieldInfo FindInstanceField(System.Type type, string name) {
    for (var current = type; current != null; current = current.BaseType) {
        var found = current.GetField(name, System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.DeclaredOnly);
        if (found != null && !found.IsStatic) return found;
    }
    return null;
}
System.Type ElementType(System.Type type) {
    if (type.IsArray) return type.GetElementType();
    if (type.IsGenericType && typeof(System.Collections.IList).IsAssignableFrom(type))
        return type.GetGenericArguments()[0];
    return null;
}
using (var serialized = new UnityEditor.SerializedObject(component)) {
    var property = serialized.FindProperty(propertyPath);
    if (property == null || !string.Equals(property.propertyPath, propertyPath, System.StringComparison.Ordinal))
        throw new System.ArgumentException("propertyPath does not identify the exact serialized property");
    if (property.propertyType != UnityEditor.SerializedPropertyType.ObjectReference)
        throw new System.ArgumentException("propertyPath must identify an ObjectReference property");
    System.Type declaredType = null;
    var resolutionSource = "managedField";
    var managedType = component.GetType();
    var managedFieldResolved = true;
    foreach (var segment in property.propertyPath.Replace(".Array.data[", "[").Split('.')) {
        var bracket = segment.IndexOf('[');
        var fieldName = bracket < 0 ? segment : segment.Substring(0, bracket);
        var field = FindInstanceField(managedType, fieldName);
        if (field == null) { managedFieldResolved = false; break; }
        managedType = field.FieldType;
        if (bracket >= 0) {
            managedType = ElementType(managedType);
            if (managedType == null) { managedFieldResolved = false; break; }
        }
    }
    if (managedFieldResolved && managedType != null &&
            typeof(UnityEngine.Object).IsAssignableFrom(managedType)) {
        declaredType = managedType;
    } else {
        resolutionSource = "serializedType";
        var serializedType = property.type;
        const string prefix = "PPtr<$";
        if (!serializedType.StartsWith(prefix, System.StringComparison.Ordinal) ||
                !serializedType.EndsWith(">", System.StringComparison.Ordinal))
            throw new System.InvalidOperationException("ObjectReference serialized type is not resolvable");
        var typeName = serializedType.Substring(prefix.Length, serializedType.Length - prefix.Length - 1);
        var candidates = new System.Collections.Generic.List<System.Type>();
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
            System.Type[] types;
            try { types = assembly.GetTypes(); }
            catch (System.Reflection.ReflectionTypeLoadException error) {
                types = error.Types.Where(candidate => candidate != null).ToArray();
            }
            foreach (var candidate in types) {
                if (candidate != null && typeof(UnityEngine.Object).IsAssignableFrom(candidate) &&
                        (string.Equals(candidate.Name, typeName, System.StringComparison.Ordinal) ||
                         string.Equals(candidate.FullName, typeName, System.StringComparison.Ordinal)) &&
                        !candidates.Any(existing => existing == candidate))
                    candidates.Add(candidate);
            }
        }
        var exactUnityType = candidates.Where(candidate => string.Equals(candidate.FullName,
            "UnityEngine." + typeName, System.StringComparison.Ordinal)).ToArray();
        if (exactUnityType.Length == 1) declaredType = exactUnityType[0];
        else if (candidates.Count == 1) declaredType = candidates[0];
        else throw new System.InvalidOperationException(candidates.Count == 0 ?
            "ObjectReference declared type is unresolvable" : "ObjectReference declared type is ambiguous");
    }
    var actual = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetReferencePath);
    if (actual == null)
        throw new System.ArgumentException("assetReferencePath does not identify a main asset");
    if (!declaredType.IsInstanceOfType(actual))
        throw new System.ArgumentException("Referenced asset type " + actual.GetType().FullName +
            " is not assignable to " + declaredType.FullName);
    if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual,
            out var referenceGuid, out long referenceLocalFileId))
        throw new System.InvalidOperationException("Could not resolve the referenced asset identity");
    return new { resolved = true, componentEntityId = UnityEngine.EntityId.ToULong(
        component.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
        propertyPath = property.propertyPath, declaredType = declaredType.FullName,
        actualType = actual.GetType().FullName, assetPath = UnityEditor.AssetDatabase.GetAssetPath(actual),
        guid = referenceGuid, localFileId = referenceLocalFileId.ToString(
            System.Globalization.CultureInfo.InvariantCulture), resolutionSource };
}
```

Invoke native `set_serialized_field` only after this preflight succeeds for the same component entity ID, property path, and asset path. The preflight returns only scalar type and asset identity data; it does not write or convert a value. If the discovered native catalog cannot express the subsequent request, report the concrete missing native capability instead of wrapping a public mutation evaluation around it.

Resolve the serialized property in this order: exact name, `m_` plus capitalized name, `_` plus original name, then `m_` plus the original name. If none resolves, return at most 30 available visible property names, excluding `m_Script`. Exactly one of a scalar value or an asset reference is required. `set_serialized_field(target,field,value,component?)` owns the mutation: `target` is the selected component/asset reference, `field` is the resolved serialized path, `value` is its JSON value or object reference, and `component` is used only when `target` is a GameObject. Do not introduce a generic converter.

After explicit child/component/property selection, use `set_serialized_field` for the component and exact serialized path. Then mark/save the prefab asset, reload it, and read the same child, component local file ID, property path, and value back. Report the prefab path and selected targets as scalars. Asset persistence is separate from scene Undo.

Keep native transport errors separate from typed results, and for batch items inspect both outer success and nested `Ok`.
