# Shader source, inspection, and global keywords

Read [foundation routing and safety](foundation.md) first. Target the exact Editor and discover each command with `--query NAME --detail full`. [Project rendering](project.md) owns the active pipeline check; [materials](material.md) own material shader assignment, values, and local keyword changes. Shader asset writes are persistent and non-Undo. The optional package adds only `shader.global-keyword-set`.

## Route map

| Intent | Owner |
| --- | --- |
| List by name, including built-ins | Native `list_shaders` |
| Find by exact internal name | Exact inspection block below (`Shader.Find`) |
| Property definitions for exact internal name or selected material | Native `get_shader_properties` |
| List asset-backed shaders by path substring and property count | Read-only evaluation below |
| Inspect an exact name or an exact `.shader` asset path, including properties, keywords, passes and compiler messages | Read-only evaluation below |
| Read UTF-8 source and newline-derived line count | Confined host file read |
| Create from Built-in Unlit, URP Unlit, URP Lit, or custom source | [Canonical console authoring transaction](console.md#executable-host-authoring-transaction), then exact import/readback |
| Recoverably delete a shader asset | [`asset.trash`](asset.md#recoverable-removal-labels-reimport-and-refresh) |
| Toggle a global keyword | Typed `shader.global-keyword-set` |

`list_shaders` matches the shader's **name**, accepts `filter`, `includeBuiltin`, and `limit` (default 200), and reports support and path metadata. It does not implement the original path-substring list, and its result may truncate before an exact name. `get_shader_properties` accepts exactly one of `shader` (internal name) or `material`; it reports declared definitions and is not a material-value read. For exact name lookup, use the inspection block below. Built-ins may have no asset path.

## Asset-path list

This read preserves the former default limit of 100, path-substring filtering, and `propertyCount`. It sorts the complete asset-backed set by ordinal path before limiting. It never represents a built-in as a project file. Replace the literals with caller values; a negative limit refuses.

```csharp
string pathFilter = null;
int limit = 100;
if (limit < 0) throw new System.ArgumentOutOfRangeException("limit");
var paths = UnityEditor.AssetDatabase.FindAssets("t:Shader")
    .Select(guid => UnityEditor.AssetDatabase.GUIDToAssetPath(guid))
    .Where(path => !string.IsNullOrEmpty(path))
    .Distinct(System.StringComparer.Ordinal)
    .Where(path => string.IsNullOrEmpty(pathFilter) || path.Contains(pathFilter))
    .OrderBy(path => path, System.StringComparer.Ordinal)
    .ToArray();
var shaders = paths.Take(limit).Select(path => {
    var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(path);
    return new { path, name = shader == null ? null : shader.name,
        propertyCount = shader == null ? 0 : UnityEditor.ShaderUtil.GetPropertyCount(shader) };
}).ToArray();
return new { count = shaders.Length, totalFound = paths.Length, shaders };
```

## Exact shader inspection

Choose exactly one `selector`: a whole internal shader name, or an exact `.shader` asset path under `Assets/` or `Packages/`. A path is loaded at that path and never redirected through `Shader.Find(shader.name)`; this matters when two assets declare the same name. For name selection, `Shader.Find` requires the exact case-sensitive internal name and can resolve built-ins. The read-only path branch rejects traversal, backslashes, and empty segments, then requires `AssetDatabase` to return that exact path. A registered package shader can be inspected without treating the package as an embedded authoring root. Host source reads and shader writes retain their separate `Assets/` confinement. The following block returns the actual asset path/GUID, original-style `TexEnv` property type, declared local keyword space, subshader and pass counts, and all reported compiler message severities. `hasErrors` counts **Error** messages only; warnings stay visible. Pass counts do not enumerate compiled variants.

```csharp
string selector = "Assets/Shaders/Example.shader";
if (string.IsNullOrWhiteSpace(selector)) throw new System.ArgumentException("Select an exact shader name or shader asset path");
bool byPath = selector.StartsWith("Assets/", System.StringComparison.Ordinal)
    || selector.StartsWith("Packages/", System.StringComparison.Ordinal);
if (byPath && (!selector.EndsWith(".shader", System.StringComparison.OrdinalIgnoreCase)
    || selector.Contains('\\') || selector.Split('/').Any(part => part == ".." || part == "." || part.Length == 0)))
    throw new System.ArgumentException("Select one exact Assets or Packages .shader path");
var shader = byPath ? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(selector)
    : UnityEngine.Shader.Find(selector);
if (shader == null || (!byPath && shader.name != selector))
    throw new System.ArgumentException("Exact shader was not found");
string actualPath = UnityEditor.AssetDatabase.GetAssetPath(shader);
if (byPath && actualPath != selector) throw new System.ArgumentException("Path did not resolve to the selected shader asset");
int propertyCount = UnityEditor.ShaderUtil.GetPropertyCount(shader);
var properties = Enumerable.Range(0, propertyCount).Select(i => new {
    name = UnityEditor.ShaderUtil.GetPropertyName(shader, i),
    type = UnityEditor.ShaderUtil.GetPropertyType(shader, i).ToString(),
    description = UnityEditor.ShaderUtil.GetPropertyDescription(shader, i)
}).ToArray();
var keywords = shader.keywordSpace.keywords.Select(keyword => new {
    name = keyword.name, type = keyword.type.ToString()
}).ToArray();
var data = UnityEditor.ShaderUtil.GetShaderData(shader);
int subshaderCount = data.SubshaderCount;
int totalPasses = 0;
for (int index = 0; index < subshaderCount; index++) totalPasses += data.GetSubshader(index).PassCount;
var messages = UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(message => new {
    severity = message.severity.ToString(), message = message.message,
    file = message.file, line = message.line, platform = message.platform.ToString()
}).ToArray();
int errorCount = messages.Count(message => message.severity == "Error");
return new {
    requested = selector, name = shader.name, path = actualPath,
    guid = string.IsNullOrEmpty(actualPath) ? null : UnityEditor.AssetDatabase.AssetPathToGUID(actualPath),
    supported = shader.isSupported, propertyCount, properties,
    keywordCount = keywords.Length, keywords, subshaderCount, totalPasses,
    messageCount = messages.Length, errorCount, hasErrors = errorCount > 0, messages
};
```

The returned `keywordCount` is the declared local keyword inventory. It does not say which keyword is enabled on a material. The compiler message set is Editor/platform dependent; repeat inspection after import or a compile state change. An intentionally broken fixture may leave Editor errors until its asset is removed and compilation settles.

## Source read

Use the host's canonical confined path check for one existing `Assets/... .shader` file. Read its exact bytes as UTF-8, retain the content verbatim, and compute `lines = content.Split('\n').Length`, including the empty segment after a final newline. Reject an undecodable file and report the path and line count. A shader internal name is not a disk path; only an exact asset path can be read as source. Do not normalize newlines before counting.

## Create a shader asset

Use the [Built-in Unlit](../templates/shaders/builtin-unlit.shader.txt), [URP Unlit](../templates/shaders/urp-unlit.shader.txt), or [URP Lit](../templates/shaders/urp-lit.shader.txt) template. URP Unlit is the default URP type. Each has exactly one `{{SHADER_NAME}}` placeholder. Supply a nonblank internal name without `"`, newline, or any control character before substituting once; assert that one placeholder existed. A caller-supplied custom source is written **verbatim** and its imported name is read back, even if it differs from an external requested label. Require an `Assets/... .shader` destination, no overwrite, and a confined path. The canonical [console authoring transaction](console.md#executable-host-authoring-transaction) `write` operation accepts a Base64 UTF-8 payload, supports created parents, and refuses a destination whose state changes. Use `replaceAuthorized=false` and no expected old hash for creation. Inspect its returned `Writes` and then explicitly run `AssetDatabase.ImportAsset(path, ForceSynchronousImport | ForceUpdate)` in the exact Editor. Require a loadable Shader, nonempty GUID, actual imported name, source hash/readback, and compiler messages from the inspection block. Host publication alone is not import proof. If post-publication import/readback fails, retain the owned file and recovery details; cleanup removes only the owned asset and created empty parents.

The Built-in template uses `UnityCG.cginc` and is for the Built-in renderer. The URP templates require an installed, active Universal Render Pipeline. URP Unlit samples `_BaseMap` and multiplies `_BaseColor`; it does not respond to lighting. URP Lit is a small opaque shader: `_BaseMap` and `_BaseColor`, main directional diffuse plus SH ambient, main-light shadow reception, ShadowCaster and DepthOnly passes. It is not full URP Lit/PBR and does not implement additional lights. Verify template compilation in the target Editor and judge lighting and shadows from an actual camera image, not only import success.

## Recoverable removal

Use `asset.trash` with the exact `.shader` asset reference and `confirm=true` for authorized removal. Before it, snapshot exact source and `.meta` bytes, hashes, and GUID. Verify both files absent and the asset unloadable afterward. Restore through OS trash or the saved exact bytes, then import and verify original GUID, bytes, and loadability. `dryRun=true` previews without removal. This is a non-Undo asset operation; native permanent `delete_asset` does not preserve this contract.

## Global keyword

Discover `shader.global-keyword-set(name,enabled=true,confirm=false,dryRun=false)`. It validates a nonblank name without control characters, reads the current global enabled state, requires `confirm=true` for mutation, and gives `dryRun=true` precedence. Its `Result` includes `Name`, `Before`, `Requested`, `After`, `Applied`, `DryRun`, and `Undoable=false`. Capture the prior enabled state and restore it through this same guarded command. A previously unknown name may remain **registered** after it is disabled; restoration promises only the prior enabled state. This command changes global shader state, not a material's local keywords or property values. Verify an unrelated keyword stays unchanged.
