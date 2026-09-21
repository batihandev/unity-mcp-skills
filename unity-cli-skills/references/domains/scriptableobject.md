# ScriptableObject assets

Use an exact project-relative asset path for a persistent ScriptableObject. Discover the exact command schema before calling a command; the command surface captured for Unity 6.6 is a baseline, not proof for another installation. Foundation owns project targeting, path confinement, exact object references, typed values, confirmation, and the difference between Undo and persisted asset state. See [foundation](foundation.md).

## Create, copy, find, and recoverably trash

`create_asset` is the native creation route. Pass `path`, a fully qualified concrete ScriptableObject `type`, and its discovered `confirm` and `dry_run` fields. The captured command also accepts `shader`, which is Material-only and has no ScriptableObject meaning. Do not select a short type name: enumerate candidates with the type evaluation below and require one exact qualified type. Confirm an overwrite only after identifying the existing target; read the created asset and its GUID back afterwards.

`copy_asset` is the native duplication route. After validating a preferred destination and confirming that the selected source is a ScriptableObject, preview `AssetDatabase.GenerateUniqueAssetPath(preferredDestination)` with the evaluation below. Pass that previewed unused path as `destination`, with `asset` set to the selected source and `dry_run=false`; no overwrite confirmation is needed. The catalog says a successful copy receives a fresh GUID. Read both paths and GUIDs back.

`find_assets` is the native search route. Use the exact qualified `type`, `search_in: "Assets"` unless the caller selects another confined root, and `limit: 50` to retain the documented ScriptableObject default. Treat its returned path/GUID/type entries as discovery only; an ambiguous result never selects a later mutation target.

For deletion, first make the read-only exact-kind check below, then use `asset.trash` with `asset`, `dryRun`, `confirm`, and (only when deliberately allowed) `allowEmbeddedPackages`. A successful non-dry-run result is recoverable OS-trash removal of the asset and `.meta`; it is not Undoable. Verify absence before considering cleanup complete.

### Read-only ScriptableObject-kind preflight

Set `assetPath` to the exact target and use this body with discovered `eval` or `eval_file`. A failure means the target must not be passed to `asset.trash`.

```csharp
string assetPath = "Assets/Data/Config.asset";
var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath) as UnityEngine.ScriptableObject;
if (asset == null) throw new System.ArgumentException("assetPath must identify a ScriptableObject");
return new {
    path = UnityEditor.AssetDatabase.GetAssetPath(asset),
    type = asset.GetType().FullName,
    entityId = UnityEngine.EntityId.ToULong(asset.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture)
};
```

### Unique copy-path preview

After foundation path validation, set the selected ScriptableObject `sourceAsset` and the preferred project-relative `destination`, then run this read-only body through discovered `eval` or `eval_file`. Use the returned `destination` in `copy_asset`; do not replace it with a display name or an existing path.

```csharp
string sourceAsset = "Assets/Data/Config.asset";
string preferredDestination = "Assets/Data/Config Copy.asset";
var source = UnityEditor.AssetDatabase.LoadMainAssetAtPath(sourceAsset) as UnityEngine.ScriptableObject;
if (source == null) throw new System.ArgumentException("sourceAsset must identify a ScriptableObject");
var destination = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(preferredDestination);
return new { source = UnityEditor.AssetDatabase.GetAssetPath(source), destination };
```

## Inspect public members

`scriptableobject.member-set` owns one writable public instance field or non-indexer property with a public setter. Its discovered input is `asset`, `member`, optional scalar `value`, optional exact Unity-object `reference`, `dryRun=false`, and `confirm=false`. Use the command's typed conversion rules; do not invent a universal text format. `dryRun=true` previews conversion without changing state. A non-dry-run request requires `confirm=true`, records Undo, marks the object dirty, and calls `AssetDatabase.SaveAssets()`.

Read it back with the evaluation below. The command's returned `ValueSet` is a display string, so it is not authoritative for Unity object or vector-like values. Undo covers serialized backing state. Nonserialized fields and properties without serialized backing require an explicit inverse; saving does not make them persistent. Verify disk state through reload when persistence matters.

### Ordered field updates

`scriptableobject.fields-set(asset,updates,dryRun=false,confirm=false)` accepts a nonempty ordered array such as `[{"Name":"Number","Value":"4"},{"Name":"Text","Value":"after"}]`. A null or empty array is refused before mutation. Each item has `Name`, scalar `Value`, and optional exact Unity-object `Reference`. Only mutable public instance fields are eligible; properties, static fields and readonly fields are refused. Names are case-sensitive. Duplicate field names apply in input order, and unknown names, invalid values or wrong reference types produce individual failures while later items continue.

Inspect `Result.Failed` and every `Result.Results` item even when outer `Ok` is true. The aggregate also reports `Total`, `Succeeded`, `Applied`, and `DryRun`; successful no-op values need not count as applied changes. Dry run writes nothing, and an actual write requires `confirm=true`. The command records one loaded-object Undo operation and saves only the selected asset once after serialized changes. It does not promise transactional rollback; nonserialized fields require explicit inverse restoration.

### Read-only member evaluation

Set `assetPath` to an exact `Assets/...` path, then pass the body to discovered `eval` or `eval_file`. It returns public instance fields and readable public instance non-indexer properties. Static members and indexers are excluded; one throwing getter returns `"(error)"` without failing the rest. Values are JSON-friendly scalars, numeric arrays for Unity value types, or exact unsigned-decimal EntityId strings for Unity objects. Other values retain their display text in a `display` object; that text is for inspection, not typed mutation or persistence verification.

```csharp
string assetPath = "Assets/Data/Config.asset";
var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath) as UnityEngine.ScriptableObject;
if (asset == null) throw new System.ArgumentException("assetPath must identify a ScriptableObject");
object ValueOf(object value) {
    if (value == null || value is string || value is bool || value is byte || value is sbyte ||
        value is short || value is ushort || value is int || value is uint || value is long ||
        value is ulong || value is float || value is double || value is decimal || value is char) return value;
    if (value is System.Enum enumValue) {
        var underlying = System.Enum.GetUnderlyingType(enumValue.GetType());
        return underlying == typeof(byte) || underlying == typeof(ushort) || underlying == typeof(uint) || underlying == typeof(ulong)
            ? (object)System.Convert.ToUInt64(enumValue, System.Globalization.CultureInfo.InvariantCulture)
            : System.Convert.ToInt64(enumValue, System.Globalization.CultureInfo.InvariantCulture);
    }
    if (value is UnityEngine.LayerMask mask) return mask.value;
    if (value is UnityEngine.Object unity)
        return UnityEngine.EntityId.ToULong(unity.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
    if (value is UnityEngine.Vector2 v2) return new[] { v2.x, v2.y };
    if (value is UnityEngine.Vector3 v3) return new[] { v3.x, v3.y, v3.z };
    if (value is UnityEngine.Vector4 v4) return new[] { v4.x, v4.y, v4.z, v4.w };
    if (value is UnityEngine.Vector2Int v2i) return new[] { v2i.x, v2i.y };
    if (value is UnityEngine.Vector3Int v3i) return new[] { v3i.x, v3i.y, v3i.z };
    if (value is UnityEngine.Quaternion q) return new[] { q.x, q.y, q.z, q.w };
    if (value is UnityEngine.Color c) return new[] { c.r, c.g, c.b, c.a };
    if (value is UnityEngine.Color32 c32) return new[] { c32.r, c32.g, c32.b, c32.a };
    if (value is UnityEngine.Rect rect) return new[] { rect.x, rect.y, rect.width, rect.height };
    if (value is UnityEngine.Bounds bounds) return new[] { bounds.center.x, bounds.center.y, bounds.center.z, bounds.size.x, bounds.size.y, bounds.size.z };
    if (value is System.Array array) {
        var values = new object[array.Length];
        for (var index = 0; index < array.Length; index++) values[index] = ValueOf(array.GetValue(index));
        return values;
    }
    try { return new { display = value.ToString() }; }
    catch { return new { display = "(error)" }; }
}
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var type = asset.GetType();
var fields = type.GetFields(flags).Select(field => new {
    name = field.Name, type = field.FieldType.FullName, value = ValueOf(field.GetValue(asset))
}).ToArray();
var properties = type.GetProperties(flags)
    .Where(property => property.GetGetMethod(false) != null && property.GetIndexParameters().Length == 0)
    .Select(property => {
        try { return new { name = property.Name, type = property.PropertyType.FullName, value = ValueOf(property.GetValue(asset)) }; }
        catch { return new { name = property.Name, type = property.PropertyType.FullName, value = (object)"(error)" }; }
    }).ToArray();
return new { path = assetPath, type = type.FullName, fields, properties };
```

## Discover types and export JSON

The following evaluation is read-only. Set `filter` to null or a desired substring; `limit` defaults to 50 when omitted. It enumerates concrete, closed ScriptableObject subclasses, filters their simple name with ordinal case-sensitive matching (the recipe's `Contains` behavior), orders deterministically by qualified name, and returns qualified candidates so the caller can choose one for `create_asset`. If more than one loaded type has the same qualified name, it refuses instead of choosing an assembly arbitrarily.

```csharp
string filter = null;
int? limit = 50;
if (limit.HasValue && limit.Value < 0) throw new System.ArgumentOutOfRangeException("limit");
var types = System.AppDomain.CurrentDomain.GetAssemblies()
    .Where(assembly => !assembly.IsDynamic)
    .SelectMany(assembly => { try { return assembly.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException error) { return error.Types.Where(type => type != null); } })
    .Where(type => type != null && type != typeof(UnityEngine.ScriptableObject) && type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type))
    .Where(type => string.IsNullOrEmpty(filter) || type.Name.IndexOf(filter, System.StringComparison.Ordinal) >= 0)
    .OrderBy(type => type.FullName, System.StringComparer.Ordinal).ToArray();
var ambiguity = types.GroupBy(type => type.FullName, System.StringComparer.Ordinal).FirstOrDefault(group => group.Count() != 1);
if (ambiguity != null) throw new System.ArgumentException("Loaded ScriptableObject types share the qualified name: " + ambiguity.Key);
var result = limit.HasValue ? types.Take(limit.Value).Select(type => type.FullName).ToArray() : types.Select(type => type.FullName).ToArray();
return new { count = result.Length, types = result };
```

For inline export, set `assetPath` and use this read-only body. It produces the `EditorJsonUtility` JSON string directly.

```csharp
string assetPath = "Assets/Data/Config.asset";
var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath) as UnityEngine.ScriptableObject;
if (asset == null) throw new System.ArgumentException("assetPath must identify a ScriptableObject");
return new { path = assetPath, json = UnityEditor.EditorJsonUtility.ToJson(asset, true) };
```

For a file export, use the canonical [executable host authoring transaction](console.md#executable-host-authoring-transaction) to publish the returned JSON as UTF-8 without BOM. Its confined, staged write and replacement/cleanup policy owns file publication; the eval stays read-only. Do not silently overwrite an existing export.

## Import JSON

`scriptableobject.json-import` owns partial serialized-field overwrite of one asset. Pass `asset`, exactly one of inline `json` or confined `jsonFilePath`, and `dryRun=false`/`confirm=false` as needed. Supplying both is refused as ambiguous; supplying neither is refused. The command calculates and returns the exact changed serialized member paths, supports a dry run, and requires `confirm=true` before a real import.

Use the Editor JSON envelope returned by the export above. A partial update has an object-valued `MonoBehaviour` root, for example `{"MonoBehaviour":{"Number":9}}`; fields omitted from that object retain their values. Flat runtime JSON, a missing or non-object root, and duplicate JSON keys are refused. Keep object-reference encodings from the Editor export and verify the selected reference after import.

Scalar token/range mismatches and nonassignable object references are refused before mutation. Rect, Bounds, AnimationCurve, Gradient, and LayerMask use their Editor-exported shapes. For these types, omitted object members retain their current exported values, including the metadata Unity needs for partial updates. Supplied arrays replace the previous array with the supplied length and order; use complete exported elements for compound array values such as curve keys.

On real import it records Undo only when serialized data changes, marks the asset dirty, saves it, and compares a reload with the proposed serialized state. Its `Persistent`, `DirtyBeforeSave`, and `DirtyAfterSave` fields describe persistence separately from Undo.
