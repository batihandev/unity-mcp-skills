# Scene operations

Read [foundation routing and safety](foundation.md) first. Discover the exact native command on the selected project before invoking it. Scene paths are authoring-root-relative; use the native command's optional `Assets/` prefix and `.unity` extension rules. A current, exact project/session authorization is sufficient for these operations. Keep all EntityId values as unsigned decimal strings.

## Native scene lifecycle

Use the native commands for ordinary scene work:

| Operation | Native command | Required request shape |
|---|---|---|
| Create | `create_scene` | `path`, optional `additive=false`, `template="empty"` or `"default"` |
| Open | `open_scene` | `path`, optional `additive=false` |
| Save | `save_scene` | optional exact path of an already-open scene; omission saves the active scene |
| List open scenes | `list_open_scenes` | no parameters |
| Set active scene | `set_active_scene` | `path` of an already-open scene |

`create_scene` is the owner for an empty saved scene and, when requested, the native default template. Confirm the returned path and the new scene's identity. `save_scene` without a path refuses for an unsaved active scene; a supplied path selects an already-open scene and is not Save As. The typed `scene.save-as` owner supplies persistent Save As after the same path preflight and target-state checks. Its request requires the exact loaded scene handle string in `scene` and a project-relative `.unity` `destination`; optional `dryRun`, `confirm`, `allowEmbeddedPackages`, and `expectedDestinationSha256` retain their native meanings. An existing destination requires `confirm=true` and its current SHA-256; a new destination rejects an expected destination hash. Read `list_open_scenes` after create, open, save, Save As, or active-scene changes when the workflow needs current load, active, or dirty state.

Every supplied path is an exact authoring-root-relative path. Before `create_scene` or supplied-path `save_scene`, validate foundation confinement and require a case-insensitive `.unity` suffix; before create, also require that the exact target is absent. Native extension normalization and replacement behavior do not satisfy these baseline safeguards. Do not select an open scene from a display name, a suffix, or the first matching path.

Before `open_scene` or `create_scene` with `additive=false`, inspect all open scenes. Existing dirty-action authorization supplies `save` or `discard` for each affected dirty scene. To save several dirty scenes, retain the original active-scene path, use `set_active_scene(path)` for each exact selected scene, call `save_scene()` with no path, then restore the original active scene when it remains open. Apply the authorized discard action only to the exact inspected scene. Then call the replacing command. Additive create and open do not replace an open scene.

`scene.unload(scene, dirtyAction=null)` is the narrow typed owner for closing one already-open scene. `scene` must identify exactly one loaded scene, it refuses when it is the only loaded scene, and a dirty scene requires the existing authorized `dirtyAction` of `"save"` or `"discard"`. `save` refuses for an unsaved scene; `discard` closes only the selected scene. A clean scene accepts the default null action. Read `list_open_scenes` afterward and require the selected scene to be absent.

## Read-only scene views

The native catalog supplies open-scene metadata but not the old root summaries, hierarchy projection, or active-scene object filter. Use the following read-only bodies with discovered `eval` or `eval_file`. They do not select a later mutation target; callers select an exact returned identity when needed.

### Active-scene information

```csharp
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (!scene.IsValid() || !scene.isLoaded)
    throw new System.InvalidOperationException("There is no loaded active scene");
var roots = scene.GetRootGameObjects();
return new {
    sceneName = scene.name,
    scenePath = scene.path,
    isDirty = scene.isDirty,
    rootObjectCount = roots.Length,
    rootObjects = roots.Select(go => new {
        name = go.name,
        instanceId = UnityEngine.EntityId.ToULong(go.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
        childCount = go.transform.childCount
    }).ToArray()
};
```

For loaded-scene root summaries, use `list_open_scenes` for each scene's native load/active/dirty state, then project roots with the same `name`, unsigned `instanceId`, and `childCount` shape. Preserve native open-scene order. An unsaved scene has an empty path.

```csharp
var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scenes = new System.Collections.Generic.List<object>();
for (var index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++) {
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(index);
    var roots = scene.GetRootGameObjects();
    scenes.Add(new { name = scene.name, path = scene.path, isLoaded = scene.isLoaded,
        isDirty = scene.isDirty, isActive = scene == active, rootCount = roots.Length,
        rootObjects = roots.Select(go => new { name = go.name,
            instanceId = UnityEngine.EntityId.ToULong(go.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
            childCount = go.transform.childCount }).ToArray() });
}
return new { success = true, count = scenes.Count, scenes };
```

### Hierarchy

Call native `get_scene_hierarchy(path?)`; its only catalog parameter is the optional path of an open scene, with omission selecting the active scene. It returns the complete native tree. The grouped hierarchy workflow then projects that returned tree with `maxDepth=3` by default; an explicit depth of 10 is supported, and any nonnegative requested depth is projected without a baseline cap. Do not make a second Unity hierarchy traversal.

The projection preserves native root order, sibling order, component order, and hierarchy paths. Parse the native response without rounding integer identities, then expose them as unsigned decimal strings. At the requested depth, return `children: null` with `truncated: true` when descendants exist; a leaf remains an empty collection.

Run this host Python body on the captured JSON response. Set `max_depth` to the requested nonnegative depth; it is not a native command parameter.

```python
import json
import sys

max_depth = 3
if type(max_depth) is not int or max_depth < 0:
    raise ValueError("max_depth must be a nonnegative integer")
response = json.load(sys.stdin)
if response.get("success") is not True:
    raise ValueError("Native hierarchy capture failed")
scene = response["data"]["result"]

def project(node, depth):
    result = dict(node)
    identity = node["instanceId"]
    if type(identity) is not int or not 0 <= identity < 2**64:
        raise ValueError("Expected an exact unsigned integer instanceId")
    result["instanceId"] = str(identity)
    children = node["children"]
    result["truncated"] = bool(children) and depth >= max_depth
    result["children"] = None if result["truncated"] else [project(child, depth + 1) for child in children]
    return result

print(json.dumps({"sceneName": scene["sceneName"],
                  "hierarchy": [project(root, 0) for root in scene["roots"]]}))
```

### Active-scene object-filter read-only evaluation

This read-only evaluation searches the active scene only. `namePattern` is a case-insensitive substring. `tag`, `componentType`, and `namePattern` combine with AND. Validate a tag before traversal; component type accepts a simple or fully-qualified name only when it resolves to exactly one non-generic `Component` type. Invalid tag, invalid/ambiguous type, or a negative limit refuses. Traversal includes inactive objects and preserves root/sibling preorder. `count` is the returned count after limiting.

```csharp
string namePattern = null, tag = null, componentType = null;
int limit = 50;
if (limit < 0) throw new System.ArgumentOutOfRangeException(nameof(limit));
if (!string.IsNullOrEmpty(tag)) UnityEngine.GameObject.FindGameObjectsWithTag(tag);
System.Type requiredType = null;
if (!string.IsNullOrEmpty(componentType)) {
    var matches = UnityEditor.TypeCache.GetTypesDerivedFrom<UnityEngine.Component>()
        .Concat(new[] { typeof(UnityEngine.Component) })
        .Where(type => !type.ContainsGenericParameters && (type.Name == componentType || type.FullName == componentType))
        .Distinct().ToArray();
    if (matches.Length != 1) throw new System.ArgumentException("componentType must resolve to exactly one Component type");
    requiredType = matches[0];
}
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (!scene.IsValid() || !scene.isLoaded) throw new System.InvalidOperationException("There is no loaded active scene");
string PathOf(UnityEngine.Transform value) { var names = new System.Collections.Generic.List<string>(); for (; value != null; value = value.parent) names.Add(value.name); names.Reverse(); return string.Join("/", names); }
var ordered = new System.Collections.Generic.List<UnityEngine.GameObject>();
void Visit(UnityEngine.Transform value) { ordered.Add(value.gameObject); for (var i = 0; i < value.childCount; i++) Visit(value.GetChild(i)); }
foreach (var root in scene.GetRootGameObjects()) Visit(root.transform);
var objects = ordered.Where(go =>
    (string.IsNullOrEmpty(namePattern) || go.name.IndexOf(namePattern, System.StringComparison.OrdinalIgnoreCase) >= 0) &&
    (string.IsNullOrEmpty(tag) || go.CompareTag(tag)) &&
    (requiredType == null || go.GetComponent(requiredType) != null))
    .Take(limit).Select(go => new { name = go.name, path = PathOf(go.transform),
        instanceId = UnityEngine.EntityId.ToULong(go.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture),
        active = go.activeInHierarchy, tag = go.tag }).ToArray();
return new { success = true, count = objects.Length, objects };
```

## Screenshot workflow

The screenshot workflow defaults to `source="screen"`, `filename="screenshot.png"`, `width=1920`, and `height=1080`, and publishes a PNG at `Assets/Screenshots/<basename>`. `filename` is a basename; normalize an extensionless name to `.png` and refuse separators or a non-PNG extension. Validate positive requested dimensions and verify the PNG IHDR width and height equal the request before publication.

For `source="screen"`, call native `capture_game_view` with `source="screen"`, the requested `width` and `height`, and an inline-image result. Screen capture is available only when Play Mode is already authorized and active; this operation never enters Play Mode. It includes composited overlay UI when the Editor has a functioning Game View compositor. Verify the returned PNG pixels when overlay inclusion matters; a camera `RenderTexture` remains a separate batch-capable path and does not prove screen composition.

For `source="camera"`, resolve an exact `Camera` ObjectRef before rendering. The body below renders to a temporary `RenderTexture`, encodes the requested pixels, and restores both `Camera.targetTexture` and `RenderTexture.active` in `finally`. It does not claim overlay UI capture.

```csharp
string cameraEntityId = "REPLACE_WITH_SELECTED_CAMERA_DECIMAL_ID";
int width = 1920, height = 1080;
if (width <= 0 || height <= 0) throw new System.ArgumentOutOfRangeException("width and height must be positive");
if (!ulong.TryParse(cameraEntityId, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var rawId))
    throw new System.ArgumentException("camera must be an unsigned decimal EntityId string");
var camera = UnityEditor.EditorUtility.EntityIdToObject(UnityEngine.EntityId.FromULong(rawId)) as UnityEngine.Camera;
if (camera == null) throw new System.ArgumentException("camera is stale or does not resolve to a Camera");
var previousTarget = camera.targetTexture;
var previousActive = UnityEngine.RenderTexture.active;
var target = UnityEngine.RenderTexture.GetTemporary(width, height, 24, UnityEngine.RenderTextureFormat.ARGB32);
try {
    camera.targetTexture = target;
    camera.Render();
    UnityEngine.RenderTexture.active = target;
    var pixels = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGBA32, false);
    try { pixels.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0); pixels.Apply(false, false); return new { pngBase64 = System.Convert.ToBase64String(UnityEngine.ImageConversion.EncodeToPNG(pixels)), width, height }; }
    finally { UnityEngine.Object.DestroyImmediate(pixels); }
} finally { camera.targetTexture = previousTarget; UnityEngine.RenderTexture.active = previousActive; UnityEngine.RenderTexture.ReleaseTemporary(target); }
```

Decode the inline Base64 PNG and pass its actual bytes to the existing [host authoring transaction](console.md#executable-host-authoring-transaction) as `operation="write"` with `writes: [{ path, content: Base64 }]` for the normalized project-relative path. Use its normal confinement, staged publication, replacement authorization, and hash-bound replacement policy. Do not publish through a second transaction. The writer executes only explicitly supplied `postCommands`; it does not itself imply asset import. If import or AssetDatabase readback is needed, discover and invoke the actual owner as a separate step, then report the project-relative path and requested dimensions after IHDR verification.

Report screenshot dimensions from the PNG itself. Unity's texture importer can rescale the imported `Texture2D`; report that size separately and use the importer workflow when a runtime texture must preserve the original pixel dimensions.
