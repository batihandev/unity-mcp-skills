# Optimization reports and authoring

Read [foundation](foundation.md), [importer capture and restoration](importer.md), and [editor Undo](editor.md) first. Discover the live schema before each command. Rendering and material reports are heuristics; they do not measure GPU time, overdraw, or runtime asset reachability.

## Operation map

| Requested operation | Canonical owner and defaults |
| --- | --- |
| Analyze overdraw candidates | `analysis.scene-transparent`, `limit=50`: active ordinary loaded Renderer objects, including disabled Renderer components; first shared material with queue >=2500 in slot order. |
| Analyze scene rendering | `analysis.scene-rendering`, `polyThreshold=10000`, `materialThreshold=5`: active Renderer and MeshFilter triangles/material-slot totals; strict greater-than findings. |
| Compress audio | Read-only plan below, existing ImportWorkflow/native importer; Vorbis, CompressedInMemory, quality .5, raw filter empty. |
| Find duplicate materials | `analysis.materials-equivalent`, `limit=50` groups: approximate shader name, `_Color` then `_BaseColor` then no-color, and queue equality; all textures and other properties ignored. |
| Find large assets | `analysis.assets-large`, `searchPath=Assets`, `minSizeBytes=1048576`, `inclusive=true`, `sort=false`, `assetDatabaseOrder=true`, `assetType=""`, `limit=50`; integer KB >=1024 and AssetDatabase discovery order. |
| Compress meshes | Read-only plan below, existing ImportWorkflow/native ModelImporter; Medium, raw filter empty; unknown level explicitly becomes Medium. |
| Get static flags | Bounded exact-target public API evaluation below. |
| Set static flags | `optimization.static-set`, required exact `target`, `flags=Everything`, `includeChildren=false`, `confirm=false`, `dryRun=false`; named comma-separated flags, full subtree preflight and one Undo group. |
| Set LOD group | `optimization.lod-setup`, required exact `target`, `lodDistances=0.6,0.3,0.1`, `confirm=false`, `dryRun=false`; culled zero appended. |
| Optimize textures | Read-only plan below, existing ImportWorkflow/native TextureImporter; max2048, crunch true, quality50, raw filter empty, changed-only limit0 unlimited. |

```bash
unity --json command --project-path "$PROJECT_PATH" --query optimization.lod-setup --detail full
unity --json command --project-path "$PROJECT_PATH" analysis.scene-rendering -- \
  --polyThreshold 10000 --materialThreshold 5
unity --json command --project-path "$PROJECT_PATH" analysis.assets-large -- \
  --searchPath Assets --minSizeBytes 1048576 --inclusive true --sort false \
  --assetDatabaseOrder true --limit 50
unity --json command --project-path "$PROJECT_PATH" optimization.static-set -- \
  --target "$TARGET_REFERENCE" --flags BatchingStatic,OccluderStatic --includeChildren true --dryRun true
unity --json command --project-path "$PROJECT_PATH" optimization.lod-setup -- \
  --target "$TARGET_REFERENCE" --lodDistances 0.6,0.3,0.1 --dryRun true
```

Rendering and transparent reports use public native component discovery with unsorted native order across active ordinary loaded scenes. Native GameObject and component flag eligibility is preserved, including eligible hidden targets; persistent, preview and prefab-stage objects are excluded. Triangle totals sum every submesh index count in checked long arithmetic, then divide the aggregate by3. Mixed-topology meshes therefore contribute an index-derived estimate; non-readable meshes remain observable without triangle-array allocation. Rendering issues and aggregate totals retain long values. Threshold comparisons are strict.

Review the preview, then send `confirm=true,dryRun=false` to the same typed route. Both mutators prevalidate the selected set and refuse persistent or preview objects. Static subtree targets include inactive children and deduplicate the root. LOD0 receives the native `GetComponentsInChildren<Renderer>()` result for the selected root, preserving the native inactive-root query behavior and attached renderer eligibility; later levels are empty. Bounds are recalculated. This setup creates no simplified mesh: visible lower-detail geometry requires separately authored renderers and an explicit richer mapping. Undo/Redo must restore an existing group's complete configuration or remove/recreate a newly added group. Save and reopen the affected scene only when persistence is requested.

For static inspection, get one exact EntityId from discovered `find_gameobjects`, then use this public read-only `eval` body. Supply the selected unsigned decimal identity as `target`; do not convert it to a signed instance ID.

```bash
STATIC_INSPECT_CODE="$(python3 - "$TARGET_REFERENCE" <<'PYCODE'
import sys
reference = sys.argv[1]
if not reference.isdecimal():
    raise SystemExit("An exact unsigned EntityId is required")
print('var go=UnityEditor.EditorUtility.EntityIdToObject(UnityEngine.EntityId.FromULong('+reference+'UL)) as UnityEngine.GameObject;'
      'if(go==null || !BatihanDev.UnityCliCommands.Analysis.AnalysisSceneObjects.Loaded().Contains(go))throw new System.ArgumentException("Select an exact ordinary loaded scene GameObject");'
      'return new{target="'+reference+'",flags=UnityEditor.GameObjectUtility.GetStaticEditorFlags(go).ToString(),isStatic=go.isStatic};')
PYCODE
)"
unity --json command --project-path "$PROJECT_PATH" --query eval --detail full
unity --json command --project-path "$PROJECT_PATH" eval -- --code "$STATIC_INSPECT_CODE"
```

## Read-only importer planning

The three bulk operations compose uncapped AssetDatabase discovery, public importer facts, and the existing guarded host importer owner. No scene Undo applies. Importer changes do not participate in transactional rollback; every successful item retains its own explicit restore capture.

Save this method body to a caller-owned `importer-plan.cs`. Set `kind` to `audio`, `model`, or `texture` and supply the requested parameter declarations. Invoke discovered `eval_file` with its actual `path` parameter. Every candidate is inspected in AssetDatabase order without native `find_assets` caps. The result contains exact `items`, skipped reasons and counts. Invalid audio enum names and nonfinite quality refuse before a plan is produced. Audio skips matching format+loadType even when quality differs; a needed write sends all three desired sample fields, and ImportWorkflow merges complete original sample settings. Model names are case-insensitive; an invalid name explicitly selects Medium.

Texture size decreases apply to every texture type before the Default-only compression branch. Default textures become Compressed. Crunch true enables it only if absent and writes quality only in that branch; crunch false keeps existing crunch, and an existing crunch setting keeps its quality. Unchanged candidates do not consume the positive limit. A nonDefault importer platform override is not a target for this Default-only plan.

```csharp
string kind = "texture";
string filter = "";
string compressionLevel = "Medium";
string compressionFormat = "Vorbis";
string loadType = "CompressedInMemory";
float quality = .5f;
int maxTextureSize = 2048;
bool enableCrunch = true;
int compressionQuality = 50;
int limit = 0;
if (kind != "texture" && kind != "audio" && kind != "model") throw new System.ArgumentException("Choose audio, model or texture");
if (float.IsNaN(quality) || float.IsInfinity(quality) || maxTextureSize < 1 || compressionQuality < 0 || compressionQuality > 100 || limit < 0)
    throw new System.ArgumentException("Use finite quality, positive max size, quality0..100 and nonnegative limit");
string Named(System.Type type, string value) => System.Enum.GetNames(type).FirstOrDefault(name => string.Equals(name,value,System.StringComparison.OrdinalIgnoreCase));
var formatName = Named(typeof(UnityEngine.AudioCompressionFormat),compressionFormat);
var loadName = Named(typeof(UnityEngine.AudioClipLoadType),loadType);
if (kind == "audio" && (formatName == null || loadName == null)) throw new System.ArgumentException("Choose actual named audio format and load type");
var modelName = Named(typeof(UnityEditor.ModelImporterMeshCompression),compressionLevel) ?? "Medium";
var format = formatName == null ? default(UnityEngine.AudioCompressionFormat) : (UnityEngine.AudioCompressionFormat)System.Enum.Parse(typeof(UnityEngine.AudioCompressionFormat),formatName);
var load = loadName == null ? default(UnityEngine.AudioClipLoadType) : (UnityEngine.AudioClipLoadType)System.Enum.Parse(typeof(UnityEngine.AudioClipLoadType),loadName);
var model = (UnityEditor.ModelImporterMeshCompression)System.Enum.Parse(typeof(UnityEditor.ModelImporterMeshCompression),modelName);
var assetType = kind == "texture" ? "Texture2D" : kind == "audio" ? "AudioClip" : "Model";
var items = new System.Collections.Generic.List<object>();
var skipped = new System.Collections.Generic.List<object>();
var guids = UnityEditor.AssetDatabase.FindAssets("t:" + assetType + " " + filter);
foreach (var guid in guids)
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var safe = BatihanDev.UnityCliCommands.Foundation.ProjectPathPolicy.Validate(path);
    if (!safe.Ok) { skipped.Add(new { path, reason = safe.Error.Code }); continue; }
    var importer = UnityEditor.AssetImporter.GetAtPath(path);
    var settings = new System.Collections.Generic.Dictionary<string,object>();
    if (kind == "audio")
    {
        var audio = importer as UnityEditor.AudioImporter;
        if (audio == null) { skipped.Add(new { path, reason = "not AudioImporter" }); continue; }
        var sample = audio.defaultSampleSettings;
        if (sample.compressionFormat == format && sample.loadType == load) { skipped.Add(new { path, reason = "matching format and load type" }); continue; }
        settings["compressionFormat"] = format.ToString(); settings["loadType"] = load.ToString(); settings["quality"] = UnityEngine.Mathf.Clamp01(quality);
    }
    else if (kind == "model")
    {
        var source = importer as UnityEditor.ModelImporter;
        if (source == null) { skipped.Add(new { path, reason = "not ModelImporter" }); continue; }
        if (source.meshCompression == model) { skipped.Add(new { path, reason = "unchanged" }); continue; }
        settings["meshCompression"] = model.ToString();
    }
    else
    {
        var texture = importer as UnityEditor.TextureImporter;
        if (texture == null) { skipped.Add(new { path, reason = "not TextureImporter" }); continue; }
        if (texture.maxTextureSize > maxTextureSize) settings["maxTextureSize"] = maxTextureSize;
        if (texture.textureType == UnityEditor.TextureImporterType.Default)
        {
            if (texture.textureCompression != UnityEditor.TextureImporterCompression.Compressed) settings["textureCompression"] = "Compressed";
            if (enableCrunch && !texture.crunchedCompression) { settings["crunchedCompression"] = true; settings["compressionQuality"] = compressionQuality; }
        }
        if (settings.Count == 0) { skipped.Add(new { path, reason = "unchanged" }); continue; }
        if (limit > 0 && items.Count >= limit) { skipped.Add(new { path, reason = "changed-item limit" }); continue; }
    }
    items.Add(new { asset = path, kind, settings });
}
return new { kind, filter, discovered = guids.Length, planned = items.Count, skippedCount = skipped.Count, skipped, items,
    modelCompression = modelName, audioQuality = UnityEngine.Mathf.Clamp01(quality), platform = "Default", transactional = false };
```

```bash
unity --json command --project-path "$PROJECT_PATH" --query eval_file --detail full
unity --json command --project-path "$PROJECT_PATH" eval_file -- --path "$PLAN_SOURCE" > "$PLAN_RESULT"
```

The following host composition previews every planned item individually through existing `importer --dry-run`, then applies the reviewed plan only when `APPLY=true`. A zero-change plan returns success without calling the refusing empty batch. Preserve every item capture. Inspect `ok` and every ordered outcome; successful transport alone does not prove the batch succeeded.

```python
import json, os, pathlib, subprocess, sys
reply = json.loads(pathlib.Path(os.environ["PLAN_RESULT"]).read_text(encoding="utf-8"))
if reply.get("errors"):
    raise SystemExit("Planner transport failed")
evaluation = reply["data"]["result"]
if not evaluation.get("success",False) or evaluation.get("diagnostics"):
    raise SystemExit("Planner evaluation failed")
plan = evaluation["result"]
items = plan["items"]
workflow = pathlib.Path(os.environ["SKILL_ROOT"]) / "scripts" / "unity_workflow.py"
base = [sys.executable, str(workflow), "importer", "--project", os.environ["PROJECT_PATH"]]
previews = []
for item in items:
    argv = base + ["--asset", item["asset"], "--kind", item["kind"], "--platform", "Default", "--settings", json.dumps(item["settings"]), "--dry-run"]
    run = subprocess.run(argv, capture_output=True, text=True)
    preview = json.loads(run.stdout)
    previews.append({"asset": item["asset"], "ok": run.returncode == 0, "reply": preview})
    if run.returncode:
        print(json.dumps({"planned": len(items), "previews": previews, "applied": 0}))
        sys.exit(1)
if not items or os.environ.get("APPLY","false").lower() != "true":
    print(json.dumps({"planned": len(items), "previews": previews, "applied": 0}))
    sys.exit(0)
argv = [sys.executable, str(workflow), "importer-batch", "--project", os.environ["PROJECT_PATH"], "--items", json.dumps(items)]
run = subprocess.run(argv, capture_output=True, text=True)
result = json.loads(run.stdout)
pathlib.Path(os.environ["BATCH_RESULT"]).write_text(json.dumps(result,indent=2),encoding="utf-8")
print(json.dumps(result))
sys.exit(run.returncode)
```

A write changes persistent importer state and reimports through the native owner. Verify actual compression/crunch/quality, all untouched settings, original bytes and GUID after each item. If any item fails, distinguish attempted, succeeded and failed outcomes and retain its `writeMayHaveApplied` recovery capture. Restore each successful or uncertain selected item explicitly with existing `importer-restore --capture` using its complete captured object; inspect full readback after restoration. Do not retry uncertain writes automatically. The [importer guide](importer.md) supplies the executable single-item restore route and platform ownership rules.
