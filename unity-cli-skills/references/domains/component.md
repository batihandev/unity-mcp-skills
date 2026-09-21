# Component operations

Read [foundation routing and safety](foundation.md) and [GameObject operations](gameobject.md) first. Resolve the exact GameObject, then enumerate ordered component handles. A type name is discovery only: ambiguous short/full type, repeated type without selected handle, or stale target refuses unchanged.

Validate inputs before changing the component and read back the requested result. Use Undo for Unity-serialized state; nonserialized public fields require retaining and explicitly restoring their old value. Native `batch(transactional=false,on_error=continue)` returns ordered item results. Check both native success and a typed command's nested `Ok` field.

## Add, list, inspect, and copy

`add_component` takes the exact GameObject `target` and uniquely resolved full `type`. Refuse ambiguous types and attempts to add another Transform. A type that permits multiple components can produce several distinct handles; enumerate them before a later edit or removal. Use native batch for supported additions and inspect each result. To create a new component class, use the [script workflow](script.md) and wait for compilation before adding its exact type.

### Read-only component list

Use this when the native component list does not return ordered decimal handles, missing-script slots, or the enabled-state profile. Set `gameObjectHandle` to the exact unsigned-decimal GameObject handle returned by discovery. The body resolves only that handle; an unknown, stale, or non-GameObject handle throws. It preserves `GetComponents<Component>()` order, including `null` missing-script slots and duplicate component types. `enabled` is emitted only for `Behaviour`, `Renderer`, and `Collider`.

Discover `eval` first, then pass the body as `code`. The captured beta.10 form is:

```bash
unity --json command --project-path "$PROJECT_PATH" --query eval --detail full
unity --json command --project-path "$PROJECT_PATH" eval -- --code "$LIST_EVAL"
```

```csharp
string gameObjectHandle = "18446744073709542610";
if (!ulong.TryParse(gameObjectHandle, System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture, out var parsedGameObjectHandle))
    throw new System.ArgumentException("gameObjectHandle must be an unsigned decimal EntityId string");
var gameObject = UnityEditor.EditorUtility.EntityIdToObject(
    UnityEngine.EntityId.FromULong(parsedGameObjectHandle)) as UnityEngine.GameObject;
if (gameObject == null)
    throw new System.ArgumentException("GameObject handle is stale or does not resolve to a GameObject");
string HandleOf(UnityEngine.Object value) => value == null ? null :
    UnityEngine.EntityId.ToULong(value.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
var components = gameObject.GetComponents<UnityEngine.Component>();
var entries = new System.Collections.Generic.List<object>();
for (var index = 0; index < components.Length; index++) {
    var component = components[index];
    if (component == null) {
        entries.Add(new { index, missing = true, handle = (string)null,
            type = (string)null, fullType = (string)null });
        continue;
    }
    var type = component.GetType();
    if (component is UnityEngine.Behaviour behaviour)
        entries.Add(new { index, missing = false, handle = HandleOf(component),
            type = type.Name, fullType = type.FullName, enabled = behaviour.enabled });
    else if (component is UnityEngine.Renderer renderer)
        entries.Add(new { index, missing = false, handle = HandleOf(component),
            type = type.Name, fullType = type.FullName, enabled = renderer.enabled });
    else if (component is UnityEngine.Collider collider)
        entries.Add(new { index, missing = false, handle = HandleOf(component),
            type = type.Name, fullType = type.FullName, enabled = collider.enabled });
    else
        entries.Add(new { index, missing = false, handle = HandleOf(component),
            type = type.Name, fullType = type.FullName });
}
return new {
    gameObject = new { handle = HandleOf(gameObject), name = gameObject.name },
    componentCount = entries.Count,
    components = entries
};
```

### Read-only component inspection

Native `get_component_properties` reads serialized data. For public reflection properties and fields, use this body with the selected GameObject handle and one selected component handle from the list result. The component must still resolve and be attached to that GameObject. `includePrivate` is false by default. With true, the body walks the component type through its base types, taking each type's declared instance members once; `declaringType` identifies a hidden or inherited member.

The body reports readable nonindexer properties, including `canWrite`, and fields including visibility/serialization metadata. Getter failures are `(error reading)`; a successful read-only member keeps its value. For a Renderer, `material` and `materials` read through shared variants to avoid instantiating materials. Values use scalars, Unity value types, exact object references, arrays limited to 64 items and four nesting levels, or display strings limited to 4096 characters. Use an exact member read when the bounded summary is insufficient. Resolve native serialized ownership with `SerializedObject.FindProperty`; the displayed field metadata alone does not establish it.

```bash
unity --json command --project-path "$PROJECT_PATH" eval -- --code "$INSPECT_EVAL"
```

```csharp
string gameObjectHandle = "18446744073709542610";
string componentHandle = "18446744073709542611";
bool includePrivate = false;
ulong ParseHandle(string text, string parameter) {
    if (!ulong.TryParse(text, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        throw new System.ArgumentException(parameter + " must be an unsigned decimal EntityId string");
    return parsed;
}
UnityEngine.Object Resolve(string text, string parameter) =>
    UnityEditor.EditorUtility.EntityIdToObject(UnityEngine.EntityId.FromULong(ParseHandle(text, parameter)));
string HandleOf(UnityEngine.Object value) => value == null ? null :
    UnityEngine.EntityId.ToULong(value.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
var gameObject = Resolve(gameObjectHandle, nameof(gameObjectHandle)) as UnityEngine.GameObject;
var component = Resolve(componentHandle, nameof(componentHandle)) as UnityEngine.Component;
if (gameObject == null || component == null || component.gameObject != gameObject)
    throw new System.ArgumentException("GameObject or component handle is stale, invalid, or no longer attached");
object Project(object value, int depth = 0) {
    const int maxItems = 64, maxText = 4096;
    if (value == null) return null;
    if (depth >= 4) return new { type = value.GetType().FullName, truncated = true };
    if (value is UnityEngine.Object unityObject) return new {
        handle = HandleOf(unityObject), name = unityObject == null ? null : unityObject.name,
        type = unityObject == null ? null : unityObject.GetType().FullName
    };
    if (value is string text) return text.Length <= maxText ? text : text.Substring(0, maxText);
    if (value is bool || value is char || value is byte || value is sbyte || value is short ||
        value is ushort || value is int || value is uint || value is long || value is ulong ||
        value is float || value is double || value is decimal || value.GetType().IsEnum) return value;
    if (value is UnityEngine.Vector2 v2) return new { x = v2.x, y = v2.y };
    if (value is UnityEngine.Vector3 v3) return new { x = v3.x, y = v3.y, z = v3.z };
    if (value is UnityEngine.Vector4 v4) return new { x = v4.x, y = v4.y, z = v4.z, w = v4.w };
    if (value is UnityEngine.Quaternion q) return new { x = q.x, y = q.y, z = q.z, w = q.w };
    if (value is UnityEngine.Color c) return new { r = c.r, g = c.g, b = c.b, a = c.a };
    if (value is UnityEngine.Rect r) return new { x = r.x, y = r.y, width = r.width, height = r.height };
    if (value is System.Collections.IEnumerable sequence) {
        var items = new System.Collections.Generic.List<object>();
        var enumerator = sequence.GetEnumerator();
        bool truncated;
        try {
            while (items.Count < maxItems && enumerator.MoveNext()) items.Add(Project(enumerator.Current, depth + 1));
            truncated = enumerator.MoveNext();
        }
        finally { (enumerator as System.IDisposable)?.Dispose(); }
        return new { items, truncated };
    }
    var display = System.Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
    return display.Length <= maxText ? display : display.Substring(0, maxText);
}
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
if (includePrivate) flags |= System.Reflection.BindingFlags.NonPublic;
var properties = new System.Collections.Generic.List<System.Reflection.PropertyInfo>();
var fields = new System.Collections.Generic.List<System.Reflection.FieldInfo>();
if (includePrivate) {
    for (var current = component.GetType(); current != null && current != typeof(object); current = current.BaseType) {
        properties.AddRange(current.GetProperties(flags | System.Reflection.BindingFlags.DeclaredOnly));
        fields.AddRange(current.GetFields(flags | System.Reflection.BindingFlags.DeclaredOnly));
    }
} else {
    properties.AddRange(component.GetType().GetProperties(flags));
    fields.AddRange(component.GetType().GetFields(flags));
}
var propertyEntries = properties
    .Where(property => property.GetGetMethod(includePrivate) != null && property.GetIndexParameters().Length == 0)
    .OrderBy(property => property.DeclaringType.FullName, System.StringComparer.Ordinal)
    .ThenBy(property => property.Name, System.StringComparer.Ordinal)
    .Select(property => {
        try {
            var reader = property;
            if (component is UnityEngine.Renderer && property.Name == "material")
                reader = typeof(UnityEngine.Renderer).GetProperty("sharedMaterial");
            else if (component is UnityEngine.Renderer && property.Name == "materials")
                reader = typeof(UnityEngine.Renderer).GetProperty("sharedMaterials");
            return new { name = property.Name, declaringType = property.DeclaringType.FullName,
                type = property.PropertyType.Name, fullType = property.PropertyType.FullName,
                value = Project(reader.GetValue(component)), canWrite = property.CanWrite };
        } catch {
            return new { name = property.Name, declaringType = property.DeclaringType.FullName,
                type = property.PropertyType.Name, fullType = property.PropertyType.FullName,
                value = (object)"(error reading)", canWrite = property.CanWrite };
        }
    }).ToArray();
var fieldEntries = fields
    .Where(field => !field.IsStatic && !field.Name.StartsWith("<", System.StringComparison.Ordinal))
    .OrderBy(field => field.DeclaringType.FullName, System.StringComparer.Ordinal)
    .ThenBy(field => field.Name, System.StringComparer.Ordinal)
    .Select(field => {
        var isSerializable = field.IsPublic || field.IsDefined(typeof(UnityEngine.SerializeField), false);
        try {
            return new { name = field.Name, declaringType = field.DeclaringType.FullName,
                type = field.FieldType.Name, fullType = field.FieldType.FullName,
                value = Project(field.GetValue(component)), isSerializable };
        } catch {
            return new { name = field.Name, declaringType = field.DeclaringType.FullName,
                type = field.FieldType.Name, fullType = field.FieldType.FullName,
                value = (object)"(error reading)", isSerializable };
        }
    }).ToArray();
return new {
    gameObject = new { handle = HandleOf(gameObject), name = gameObject.name },
    component = new { handle = HandleOf(component), type = component.GetType().Name,
        fullType = component.GetType().FullName },
    includePrivate, properties = propertyEntries, fields = fieldEntries
};
```

`component.copy(source,destination)` takes one exact source component handle and one destination GameObject handle. It adds a component of that type and copies its serialized state and object references. It does not overwrite an arbitrary existing component. Transform copying refuses. Check nested `Ok`, retain the returned created handle, and read back that component.

## Remove, enabled state, and property setting

`component.remove(component)` removes one selected handle after dependency validation. Transform, missing/stale handles, and required components refuse. Enumerate repeated types before selecting one. For an all-instances request, freeze those exact handles and process them in request/component order, reporting each nested `Ok`/error and checking absence after success.

To enable or disable a component, first verify it is a Behaviour, Renderer, or Collider, then set its exact `m_Enabled` field through `set_component_properties`. Default to true when the request is simply “enable.” Unsupported kinds refuse before mutation. Read back the selected handle's enabled state.

For a public member write, resolve exactly one public writable nonindexer property or mutable public instance field. Case-insensitive spelling works only when unique; collisions refuse. Readonly fields, private setters, static members, indexers, unknown names, and invalid conversions refuse before mutation.

A field uses native serialized ownership only when `SerializedObject.FindProperty(field.Name)` establishes that field. Map a property to native serialization only through verified metadata/public API for its backing property; never guess `m_`/underscore names. Other writable members use `component.member-set(target,member,value,reference)`. It returns the canonical member name, declared value type, and converted value under the typed result envelope.

Supported values include float/int, bool and `1`/`0`, Vector2/3/4 CSV, Color float CSV/hex/name, Quaternion Euler or XYZW, enum names optionally qualified by their own short/full type, LayerMask name/integer mask, and AnimationCurve `linear`, `easein`, `easeout`, `easeinout`, or `constant`. Object references use an exact selected scene/asset reference; `reference="null"` explicitly clears one. Read back the typed value. Keep a before value for explicit restoration of nonserialized state.

Property batches use ordered exact handles and selected cross-scene GlobalObjectId references, never name-only cross-scene selection. Native serialized operations can use their supported transaction boundary. For typed writes, check each nested `Ok` and retain per-item outcomes; use the [typed batch result and restoration workflow](foundation.md#typed-results-inside-a-batch).
