# Importer and media operations

This guide is a draft in the WIP migration. Its importer verification is incomplete; it is not part of the accepted workflow coverage.

Read [foundation routing and safety](foundation.md) first. This reference uses the Unity `get_import_settings` and `set_import_settings` commands for importer state. Discover their schema in the target project before use. In the Unity 6.6 catalog, the getter takes `asset` and optional `platform`; the setter takes `asset`, JSON-valued `settings`, optional `platform`, and `dry_run`. It reimports on a successful write.

Importer settings are persistent asset mutations. Capture the complete applicable default and platform blocks before a write, use `dry_run` first, read the affected block after reimport, and restore the captured block when the work is complete. They are non-Undo operations and cannot enter a transactional batch. Existing task authorization covers the requested mutation; use the command's `dry_run` to inspect an intended write, not as a second user gate.

The native setter reports fields it can apply and fields it cannot. For a single-setting operation, an unknown or incompatible requested field is a failure and the prior state must remain unchanged. Do not infer undocumented importer member names from a display label.

## Canonical importer surface

```bash
unity command --project-path "$PROJECT_PATH" --format json get_import_settings -- \
  --asset 'Assets/Textures/icon.png' --platform Default

unity command --project-path "$PROJECT_PATH" --format json set_import_settings -- \
  --asset 'Assets/Textures/icon.png' \
  --settings '{"isReadable":true,"mipmapEnabled":false}' --platform Default --dry_run true
```

`asset` accepts the ObjectRef forms supported by the discovered command. Use one exact asset path, GUID, or global ID; never select the first result of a search. `platform` is `Default`, `Standalone`, `iOS`, `Android`, `WebGL`, or `tvOS` in the captured catalog. Normalize `iPhone` input to `iOS` at the public boundary.

The canonical operation is a native get or set with one of the profiles below. Minimal and rich inputs select fields from that same operation. A batch repeats the canonical setter in request order through `batch(transactional=false,on_error=continue)` when the native host accepts the operation. Retain one result per item and prove success, failure, success. Do not describe this as transactional or Undoable.

## Audio

The audio importer profile is `forceToMono`, `loadInBackground`, `ambisonic`, `loadType`, `compressionFormat`, `quality`, and `sampleRateSetting`.

`quality` in this profile is a normalized number from `0` through `1`. A `0` through `100` percentage input converts once at the route boundary by dividing by `100`; reject values outside that inclusive range. Do not send the unconverted percentage as `quality`.

Use native `get_import_settings` and `set_import_settings` for the profile. When reading or setting a non-default audio platform block, pass the exact canonical platform. Validate the importer kind and all enum/range values before applying. Reimport readback must include the values that were requested and the complete captured block is required for restoration.

### Clips and sources

AudioClip metadata has no importer-schema owner. Use a narrow read-only evaluation for `length`, `channels`, `frequency`, `samples`, `loadType`, `loadState`, and `ambisonic`. The path must resolve to exactly one `AudioClip`.

```csharp
string assetPath = "Assets/Audio/clip.wav";
var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(assetPath);
if (clip == null) throw new System.ArgumentException("assetPath must resolve to an AudioClip");
return new { path = assetPath, name = clip.name, length = clip.length, channels = clip.channels,
    frequency = clip.frequency, samples = clip.samples, loadType = clip.loadType.ToString(),
    loadState = clip.loadState.ToString(), ambisonic = clip.ambisonic };
```

For clip search, use `AssetDatabase.FindAssets("t:AudioClip " + filter)` in a bounded read-only evaluation, resolve every GUID to a path, ordinal-sort paths, compute `totalFound` before limiting, then load only the shown clips. The default `limit` is `50`; negative values refuse; `showing` equals the returned array length. Each row is `{ path, name, length }`.

AudioSource state belongs to the existing component commands. Select an exact GameObject and exact AudioSource handle. Use `add_component` followed by `set_component_properties` to create a source with `playOnAwake=false`, `loop=false`, and `volume=1` unless supplied otherwise. Use `get_component_properties`/`set_component_properties` for native serialized fields. Route writable public `minDistance` and `maxDistance` through the [component public-member workflow](component.md#remove-enabled-state-and-property-setting) when verified serialized ownership is absent. Component mutations use their native Undo behavior; importer changes do not.

To list active-scene sources, traverse active-scene roots and active descendants in hierarchy preorder, retain `AudioSource` component order, compute the full eligible set before limiting, and return `{ gameObject, path, clip, volume, loop, enabled }`. Disabled components remain eligible. An unassigned clip is the string `"null"`. Do not include inactive hierarchy objects or additive scenes.

`audio.mixer-create` is the existing typed owner for AudioMixer asset creation. Its actual fields are `mixerName`, `folder`, `dryRun`, `confirm`, and `allowEmbeddedPackages`. It creates a non-Undo asset, validates one Master group, and returns its path, GUID, factory, and creation state. Use its own discovered schema; do not create a mixer with a generic asset command.

## Models

The model importer profile is `globalScale`, `useFileScale`, `importBlendShapes`, `importVisibility`, `importCameras`, `importLights`, `meshCompression`, `isReadable`, `optimizeMeshPolygons`, `optimizeMeshVertices`, `generateSecondaryUV`, `keepQuads`, `weldVertices`, `importNormals`, `importTangents`, `animationType`, `importAnimation`, and `materialImportMode`.

Use native import settings for that complete field set. Model searches use `AssetDatabase.FindAssets("t:Model " + filter)`, ordinal paths, and the same `limit=50`, pre-limit `totalFound`, and negative-limit refusal contract as audio search. Return `{ path, name }`, where `name` is the basename without its extension.

Model metadata is read-only public API composition:

- Mesh information returns vertex count, triangle count, submesh count, bounds center and size, normals/tangents/UV/UV2/colors presence, blend-shape count, and readability. An asset branch loads its exact mesh; a scene branch requires an exact selected `MeshFilter` or `SkinnedMeshRenderer` component.
- Rig information returns animation type, avatar setup, source avatar, optimize-game-objects, and human status from the exact ModelImporter/avatar state.
- Animation information excludes clips named `__preview__` and returns each remaining clip's name, length, frame rate, wrap mode, and looping state.
- Material information returns embedded material name and shader plus mesh name, vertices, and triangles. It does not claim that a remapped material is an embedded subasset.

Use this bounded public read for exact model asset metadata. It verifies the importer kind before projecting loaded sub-assets and does not write or reimport.

```csharp
string assetPath = "Assets/Models/hero.fbx";
var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.ModelImporter;
if (importer == null) throw new System.ArgumentException("assetPath must resolve to a model importer");
var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
var clips = System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(
    System.Linq.Enumerable.OfType<UnityEngine.AnimationClip>(assets),
    clip => !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal)),
    clip => new { name = clip.name, length = clip.length, frameRate = clip.frameRate,
        wrapMode = clip.wrapMode.ToString(), looping = clip.isLooping }).ToArray();
var definitions = importer.clipAnimations == null ? System.Array.Empty<object>() :
    System.Linq.Enumerable.Select(importer.clipAnimations,
        clip => (object)new { name = clip.name, takeName = clip.takeName, firstFrame = clip.firstFrame,
            lastFrame = clip.lastFrame, loopTime = clip.loopTime }).ToArray();
var materials = System.Linq.Enumerable.Select(System.Linq.Enumerable.OfType<UnityEngine.Material>(assets),
    material => new { name = material.name, shader = material.shader == null ? null : material.shader.name }).ToArray();
var meshes = System.Linq.Enumerable.Select(System.Linq.Enumerable.OfType<UnityEngine.Mesh>(assets),
    mesh => new { name = mesh.name, vertexCount = mesh.vertexCount,
        triangleCount = mesh.isReadable ? mesh.triangles.Length / 3 : (int?)null,
        subMeshCount = mesh.subMeshCount, bounds = new { center = mesh.bounds.center, size = mesh.bounds.size },
        hasNormals = mesh.isReadable && mesh.normals.Length > 0, hasTangents = mesh.isReadable && mesh.tangents.Length > 0,
        hasUV = mesh.isReadable && mesh.uv.Length > 0, hasUV2 = mesh.isReadable && mesh.uv2.Length > 0,
        hasColors = mesh.isReadable && mesh.colors.Length > 0,
        blendShapeCount = mesh.blendShapeCount, isReadable = mesh.isReadable }).ToArray();
return new { path = assetPath, animationType = importer.animationType.ToString(),
    avatarSetup = importer.avatarSetup.ToString(), sourceAvatar = importer.sourceAvatar == null ? null : importer.sourceAvatar.name,
    optimizeGameObjects = importer.optimizeGameObjects,
    isHuman = importer.animationType == UnityEditor.ModelImporterAnimationType.Human,
    clips, clipDefinitions = definitions, materials, meshes };
```

Ordered clip definitions use the native `clipAnimations` setting. Each definition retains `name`, `takeName`, `firstFrame`, `lastFrame`, and `loopTime`, in that order. Capture and restore the full array.

## Textures and sprites

The texture importer profile is `textureType`, `textureShape`, `sRGBTexture`, `alphaSource`, `alphaIsTransparency`, `isReadable`, `mipmapEnabled`, `filterMode`, `wrapMode`, `maxTextureSize`, `textureCompression`, `spriteImportMode`, `spritePixelsPerUnit`, and `npotScale`. The sprite profile adds `spritePackingTag` and `spritePivot` as a named `{ "x": number, "y": number }` value.

Use native import settings for the complete profile. For a platform override, pass the canonical platform and set only fields supported by the discovered platform block, such as `maxTextureSize`, `format`, `compressionFormat`, `quality`, and `overridden`. Validate the platform before mutation. Capture and restore the entire relevant platform block, including whether it is overridden.

Texture type changes can reset dependent importer fields. Capture the complete default block before the change, read back after reimport, and restore the complete captured block. Sprite-only fields refuse on a non-Sprite importer unless the same authorized request explicitly changes the type to Sprite first. A sprite update includes every supplied `spriteImportMode`, `spritePixelsPerUnit`, `spritePackingTag`, and `spritePivot` value in its pre/post readback and restoration set.

Texture search uses `AssetDatabase.FindAssets("t:Texture2D " + filter)`, ordinal path sort, total before limit, and `limit=50`. Return `{ path, name, width, height }` for shown assets. The size search defaults to inclusive `minSize=0`, `maxSize=99999`, and filters on the larger dimension; refuse negative values and `minSize > maxSize`.

Use this narrow public read when importer metadata is insufficient for texture information. `memorySizeKB` is a runtime estimate and must be labelled as such.

```csharp
string assetPath = "Assets/Textures/icon.png";
var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(assetPath);
if (texture == null) throw new System.ArgumentException("assetPath must resolve to a Texture2D");
return new { path = assetPath, name = texture.name, width = texture.width, height = texture.height,
    format = texture.format.ToString(), mipmapCount = texture.mipmapCount, readability = texture.isReadable,
    filterMode = texture.filterMode.ToString(), wrapMode = texture.wrapMode.ToString(),
    memorySizeKB = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture) / 1024L };
```

## Shared execution rules

Read before and after every mutation. `dry_run` leaves importer state unchanged and validates only the native command's known fields; it does not prove a later write or reimport. A failed item leaves its item unchanged. Successful importer items retain their captured state for explicit inverse restoration. Importer writes are persistent and non-Undo even when a scene or component operation in the same workflow has Undo support.

Use read-only evaluation only for metadata absent from the native schema. Keep its body bounded to one exact asset or selected object and return plain projection data. For a concrete behavior with no discovered native or existing typed owner, report the gap; do not introduce a generic recipe interpreter or a generic command framework.
