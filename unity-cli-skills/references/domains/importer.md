# Importer and media operations

Read [foundation routing and safety](foundation.md) before importer work. Import settings are persistent asset mutations: a successful write reimports the asset, has no Undo, and does not participate in transactional batches. Use the package-free host workflow below. It resolves the exact asset with the native getter, checks its importer kind, captures the selected block, reads actual enum names and omitted public fields, validates the whole request, runs a native dry run, writes through the native setter, and verifies every changed field after reimport. A successful dry run proves native conversion only; host preflight enforces semantic ranges that the native command accepts.

Use an exact existing asset path, GUID, or global ID. Do not select the first search result. A successful native response is not a substitute for post-reimport readback.

## Executable host workflow

The apply response includes the full capture, native dry-run result, write result, and readback. Keep that capture until the requested work is accepted. Restoration is a separate explicit action and runs through the same native setter; a failed or uncertain write never triggers automatic rollback. For a dry run without a write, pass --dry-run. If preflight refuses a request, no native setter call is made.

```bash
RESULT_FILE=texture-write.json
python scripts/unity_workflow.py importer --project "$PROJECT_PATH" \
  --asset 'Assets/Textures/icon.png' --kind texture --profile rich \
  --settings '{"isReadable":true,"mipmapEnabled":false}' > "$RESULT_FILE"

CAPTURE="$(python -c 'import json,sys; print(json.dumps(json.load(open(sys.argv[1], encoding="utf-8"))["capture"], separators=(",",":")))' "$RESULT_FILE")"
python scripts/unity_workflow.py importer-restore --project "$PROJECT_PATH" --capture "$CAPTURE"
```

Audio sample fields use a complete nested defaultSampleSettings object. The route merges the complete captured flat getter projection before sending the native request:

```bash
python scripts/unity_workflow.py importer --project "$PROJECT_PATH" \
  --asset 'Assets/Audio/ambience.wav' --kind audio --profile rich \
  --settings '{"quality":0.75,"sampleRateOverride":22050}'
```

The rich audio profile accepts normalized quality from 0 through 1. The minimal bridge accepts integer percentage quality and converts it once:

```bash
python scripts/unity_workflow.py importer --project "$PROJECT_PATH" \
  --asset 'Assets/Audio/ambience.wav' --kind audio --profile minimal --quality-unit percent \
  --settings '{"quality":75}'
```

Sprite fields require an existing Sprite importer or an explicit Sprite type transition in the same request. The response includes public TextureImporter pivot readback, which the fixed getter may omit:

```bash
python scripts/unity_workflow.py importer --project "$PROJECT_PATH" \
  --asset 'Assets/Textures/icon.png' --kind texture --profile rich \
  --settings '{"textureType":"Sprite","spriteImportMode":"Single","spritePixelsPerUnit":64,"spritePivot":{"x":0.25,"y":0.75}}'
```

Clip arrays retain input order and must name an actual take with finite, ordered frame bounds inside that take. The response includes public definitions and generated clip readback:

```bash
python scripts/unity_workflow.py importer --project "$PROJECT_PATH" \
  --asset 'Assets/Characters/Stance.fbx' --kind model --profile rich \
  --settings '{"clipAnimations":[{"name":"Walk","takeName":"Stance","firstFrame":0,"lastFrame":150,"loopTime":true}]}'
```

Independent assets can be submitted in one ordered nontransactional batch. Every outcome is attributed to its index, failures do not suppress later requests, and successful item captures are returned for separate restoration:

```bash
python scripts/unity_workflow.py importer-batch --project "$PROJECT_PATH" --items \
  '[{"asset":"Assets/Textures/a.png","kind":"texture","settings":{"filterMode":"Point"}},{"asset":"Assets/Textures/b.png","kind":"texture","settings":{"maxTextureSize":-1}},{"asset":"Assets/Textures/c.png","kind":"texture","settings":{"filterMode":"Bilinear"}}]'
```

Unknown fields, unknown enum names, wrong scalar types, nonfinite values, invalid ranges, incompatible rig/avatar state, and out-of-source clip ranges fail host preflight before any setter call. Do not infer importer member names from display labels.

## Canonical profiles

Minimal bridge inputs and rich inputs use the same native getter and setter. The profile names below describe the fields used by the former workflows; they do not introduce separate command owners.

The audio profile is `forceToMono`, `loadInBackground`, `ambisonic`, `loadType`, `compressionFormat`, `quality`, and `sampleRateSetting`. The getter exposes sample fields as flat projections. For a write, the native `AudioImporter` expects those sample fields under `defaultSampleSettings`, alongside any supplied top-level flags. Merge the complete captured sample-settings block before changing one sample field so values such as `sampleRateOverride` survive. `quality` is normalized from `0` through `1`. Convert a percentage input from `0` through `100` once at the route boundary by dividing by `100`, and refuse values outside the inclusive range.

The texture profile is `textureType`, `textureShape`, `sRGBTexture`, `alphaSource`, `alphaIsTransparency`, `isReadable`, `mipmapEnabled`, `filterMode`, `wrapMode`, `maxTextureSize`, `textureCompression`, `spriteImportMode`, `spritePixelsPerUnit`, and `npotScale`. Use enum spellings from the discovered command and the current `TextureImporter` API. A texture type change can reset dependent fields, so restore the complete captured block after checking its post-reimport values.

The model profile is `globalScale`, `useFileScale`, `importBlendShapes`, `importVisibility`, `importCameras`, `importLights`, `meshCompression`, `isReadable`, `optimizeMeshPolygons`, `optimizeMeshVertices`, `generateSecondaryUV`, `keepQuads`, `weldVertices`, `importNormals`, `importTangents`, `animationType`, `importAnimation`, and `materialImportMode`. Use current API enum names: for example, the human animation type is `Human`. Check the exact importer state after reimport, including any avatar setup associated with a rig change.

For platform overrides, pass the canonical platform accepted by the discovered schema and change only fields in that returned platform block, such as `maxTextureSize`, `format`, `compressionFormat`, `quality`, and `overridden`. Capture and restore the full platform block, including the original `overridden` value. Normalize legacy `iPhone` input to `iOS` at the public boundary.

## Sprite settings

Sprite settings are meaningful for a Sprite importer. A non-Sprite texture should refuse sprite-only changes unless the same request changes its type to Sprite. Apply and read back `spriteImportMode`, `spritePixelsPerUnit`, and named `{ "x": number, "y": number }` `spritePivot` values together when supplied. The fixed native getter may omit pivot; read it through `TextureImporter.spritePivot` after reimport and preserve its prior value for restoration. Unity 6 ignores `TextureImporter.spritePackingTag` writes, so the importer workflow refuses this field before writing any settings. Assign packing through an explicitly selected Sprite Atlas asset, verify exact packable membership, and restore that atlas membership separately.

### Sprite Atlas V2 membership and packing

Keep sprite packing intent in an explicit Sprite Atlas V2 asset. Create a new `.spriteatlasv2`
asset through the optional package's `atlas.create` with an explicit `atlas` path. Create its parent
folder through native `create_folder` first. Use `dryRun=true` to validate the vacant destination,
then `confirm=true` to save and import the source; an existing file or meta refuses without replacement.
The result includes the imported GUID and reports that creation is not Undoable. This owner uses the
public Sprite Atlas source writer: Unity 6.6 reports an error when the generic asset creator writes
this file format. Membership edits use `atlas.set-packables`; selected-atlas packing uses `atlas.pack`.

`atlas.set-packables` takes an existing master atlas path and exactly one complete `packables` list
or `restore` list. References may identify a Sprite subasset, a Texture2D imported as a Sprite, or a
project asset folder. Use an exact Sprite ObjectRef when a texture contains multiple sprites. Missing,
duplicate, unsupported, ambiguous, variant-atlas, and non-V2 inputs refuse before writing. Start
with `dryRun=true` and retain the complete `before` capture. Apply a reviewed replacement with
`confirm=true`; the command rereads ordered persisted membership after synchronous import. Asset
writes have no Undo. Restore the complete capture explicitly by passing its references through the
same command's `restore` input.

```json
{
  "atlas": "Assets/Atlases/Environment.spriteatlasv2",
  "packables": ["GlobalObjectId_V1-...", "Assets/Art/UI"],
  "dryRun": true
}
```

`atlas.pack` accepts one explicit V2 atlas and an exact valid `BuildTarget` name, or the active
target when omitted. Sprite Atlas V2 must already be enabled; packing does not change project
settings. Supply exact Sprite ObjectRefs in `sprites` for binding observations. The result reports
the measured packed-sprite count and `CanBindTo` readback for every requested Sprite after the atlas
is reloaded. A successful response with no packed sprites or missing expected bindings is not proof
of packing. Restore any fixture-only packer settings and atlas membership after verification.

## Model clips and metadata

After external authoring, wait for import and compilation to finish before loading the asset or
running a scene builder. Preserve its `.meta` identity when replacing source bytes. Read the
imported GUID, rig mapping, exporter axes, generated clips, and saved importer state back; an
asset-refresh acknowledgement alone does not prove the new source was imported. When validating
animation, use actual Animator playback with the project's real domain/scene reload settings and
record the measurement phase. Translate every path argument passed to a Windows authoring process
at the host boundary, including output and auxiliary file arguments.

Model mesh, rig, animation, and embedded-material information is read-only public API composition because the importer getter does not contain that metadata. Keep each evaluation bounded to one exact asset or one already-selected scene component, verify its type first, and return plain projection values. Do not use a metadata evaluation to change importer state.

For model animation metadata, load clips at the exact model path, omit names beginning `__preview__`, and return each remaining clip's name, length, frame rate, wrap mode, and looping state. For rig information, read the exact `ModelImporter` and its avatar state. For mesh information, load the exact mesh subasset or require an exact selected `MeshFilter` or `SkinnedMeshRenderer` component handle. For material information, distinguish embedded material subassets from remapped materials; include mesh association and stable local file identifiers where available.

Ordered clip definitions use the native `clipAnimations` setting. Preserve each definition's `name`, `takeName`, `firstFrame`, `lastFrame`, and `loopTime` in order. Validate take and frame choices against the model's actual source clips. After reimport, read back `ModelImporter.clipAnimations` and inspect generated clips through the public API; do not assume the native settings getter contains generated clip details. Restore the full captured clip array and the other captured importer settings.

## Media searches and inspectors

Search filters are the raw suffix passed to `AssetDatabase.FindAssets`: use `t:AudioClip`, `t:Texture2D`, or `t:Model`, followed by the requested filter text. Resolve every GUID to a path, remove duplicate paths, sort paths ordinally, count the full set before applying the limit, and only then load the shown objects. The default limit is `50`; reject a negative limit. Return `totalFound`, `showing`, and the exact rows: audio clips `{path,name,length}` in seconds, textures `{path,name,width,height}`, and models `{path,name}` where `name` is the filename without its extension.

For texture-size search, filter the larger of width and height using inclusive `minSize` and `maxSize` bounds. The defaults are `0` and `99999`. Refuse negative bounds or `minSize > maxSize`; apply sorting and limiting after filtering and preserve the pre-limit total.

When importer settings do not provide the requested media metadata, use a bounded read-only public API projection. An exact `AudioClip` read can return `length`, `channels`, `frequency`, `samples`, `loadType`, `loadState`, and `ambisonic`. An exact `Texture2D` read can return dimensions, format, mipmap count, readability, filter/wrap modes, and a labelled runtime memory estimate. Wrong-type and missing paths fail before projection.

## AudioSource and AudioMixer workflows

For an AudioSource, select an exact GameObject and retain the exact component handle returned by the native component command. Add the component with `add_component`, then use `set_component_properties` to establish the baseline `playOnAwake=false`, `loop=false`, and `volume=1` values and any supplied options. Use `get_component_properties` for fields present in its serialized property map. Read every changed field back and use native Undo to reverse a scene/component change when needed. When a writable public member such as `spatialBlend` has no serialized owner, use the [component public-member workflow](component.md#remove-enabled-state-and-property-setting) and read the typed value back.

Before assigning a clip, resolve the exact requested asset as an `AudioClip` in a bounded read-only evaluation. A missing asset or another asset type must refuse the workflow before adding or changing the source. The native serialized reference setter can accept an incompatible object, so its success envelope alone is not a type check. For an explicit asset path, the preflight is:

```csharp
var path = "Assets/Audio/Footstep.wav";
var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(path);
if (clip == null) throw new System.ArgumentException("The selected path must contain an AudioClip.");
return new { assetPath = UnityEditor.AssetDatabase.GetAssetPath(clip),
    reference = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(clip).ToString() };
```

Only after this succeeds, pass its exact reference to the discovered clip property (`m_Resource` in Unity 6.6) through `set_component_properties`. Read back the selected component and verify the same clip identity. Resolve every supplied clip before any mutation in that item; independent later items may continue after a refusal.

To find sources, traverse the active scene roots and active descendants in hierarchy preorder, retaining each GameObject's AudioSource component order. Include disabled components, exclude inactive hierarchy objects and additive scenes, compute `totalFound` before limiting, and return `{gameObject,path,clip,volume,loop,enabled}`. An unassigned clip is the string `"null"`; `showing` equals the number of returned source rows.

`audio.mixer-create` is the typed owner for mixer creation. Discover its own schema; its current fields are `mixerName`, `folder`, `dryRun`, `confirm`, and `allowEmbeddedPackages`. It uses Unity's AudioMixer factory, checks for one `Master` group, returns the created path and GUID, and is not Undoable. Check the target for collision first and remove only a mixer the command created for the authorized request. Do not create a mixer through a generic asset command or substitute a `ScriptableObject` factory.

## Batches and ownership

Importer changes are not transactional. For a multi-asset workflow, repeat the native setter in request order and retain each item's outcome. Prove success, failure, success on three separate owned or explicitly selected assets; a failed item must remain unchanged, and later independent items must still run. Restore every successful item's complete original block. Do not call this an atomic batch or claim Undo support for importer writes.

Use read-only evaluations only where importer metadata is absent from native settings. Keep fixture creation and cleanup scoped to owned test assets; never turn fixture setup code into a generic importer implementation fallback. If the discovered native and existing typed owners cannot safely meet a required behavior, record the precise field, request, response, and readback as an owner decision.
