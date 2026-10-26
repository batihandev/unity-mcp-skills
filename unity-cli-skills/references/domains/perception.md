# Scene, project, and script perception

Use this guide to inspect the selected Editor before editing a scene or project. Read [foundation](foundation.md) and discover each live command schema. Reports return a typed `data.result` envelope: require `Ok=true` and then consume `Result`; a successful CLI transport alone does not prove a successful report. Exact object references are unsigned decimal EntityId strings, GlobalObjectId strings, or exact asset paths accepted by the discovered owner. Names and hierarchy paths are discovery aids; bind a discovered exact reference before requesting a subtree.

## Seventeen report routes

| Requested report | Canonical owner and defaults |
| --- | --- |
| Describe hierarchy | Native `get_scene_hierarchy` plus the text projection below: active scene, `maxDepth=5`, `includeInactive=false`, `maxItemsPerLevel=20`. |
| Detect project stack | `analysis.project-stack`: engine/package version, pipeline/shaders, input/UI/package/test/folder signals and profile heuristics. |
| Component and facility statistics | `analysis.scene-summary`, `includeComponentStats=true`, `topComponentsLimit=15`. |
| Coding context | `analysis.scene-context`: depth10, objects200, `root=null`, valuesfalse, referencestrue, codeDepsfalse. |
| Scene conventions | `analysis.scene-contract`: roots Systems/Managers/Gameplay/UIRoot; optional string JSON arrays for roots/tags/layers, EventSystem requirementtrue. |
| Serialized dependency impact | `analysis.scene-dependencies`, optional exact `target=null`; inline Markdown. |
| Capture/compare snapshot | `analysis.scene-diff`, `snapshotJson=null` to capture, returned `SnapshotJson` to compare. |
| Export scene report | `analysis.scene-report`: depth10, objects500; inline Markdown followed by optional guarded publication below, default destination `Assets/Docs/SceneReport.md`. |
| Find hierarchy hotspots | `analysis.scene-hotspots`: deep8, children25, max20. |
| Scene health | `analysis.scene-health`: issue100, deep8, children25. |
| Scene materials | `analysis.scene-materials`, propertiesfalse; active loaded renderers, actual shared-material slots. |
| Performance hints | `analysis.scene-performance`: active loaded component heuristics. |
| Spatial proximity | `analysis.scene-spatial`: center0, radius10, exact nearObjectnull, componentFilternull, max50. |
| Quick scene summary | `analysis.scene-summary`: includeComponentStatstrue, top10. |
| Tag/layer usage | `analysis.scene-tag-layers`: loaded objects including inactive and hidden. |
| Script inspection | `analysis.script-inspect`: required unique simple or qualified scriptName, includePrivatefalse. |
| Script field-type closure | `analysis.script-graph`: required eligible scriptName, maxHops2, includeDetailstrue. |

```bash
unity --json command --project-path "$PROJECT_PATH" --query analysis.scene-context --detail full
unity --json command --project-path "$PROJECT_PATH" analysis.scene-summary
unity --json command --project-path "$PROJECT_PATH" analysis.scene-context -- \
  --root "$TARGET_REFERENCE" --includeValues true --includeReferences true --includeCodeDeps true
unity --json command --project-path "$PROJECT_PATH" analysis.script-graph -- \
  --scriptName MyGame.PlayerController --maxHops 2 --includeDetails true
unity --json command --project-path "$PROJECT_PATH" analysis.project-stack
```

Summary metrics count ordinary objects across every loaded scene, including inactive and hidden objects and disabled components. Active scene name/path/dirty/root count identify the active scene only. Preview scenes, persistent assets, and prefab-stage objects are excluded. Facility counts cover cameras/MainCamera tags, lights, canvases, EventSystems, AudioListeners, UGUI graphics and Toolkit documents; component top lists exclude Transform. Counts refresh on every call. Component-statistics and summary routes share the same facts; their top limits differ.

Context and report traverse active-scene roots in breadth-first sibling order, or one exact loaded subtree. Their `ScopeObjects` denominator is that actual scope; `TotalObjects` counts all ordinary loaded objects. `DepthOmitted` and `CountOmitted` explain omissions. Depth0 includes roots; object cap0 shows none. Hidden and inactive roots and descendants participate. Sources of exported reference/type edges are exported objects only; external targets retain actual scene, owning GameObject/component, and asset identities. Duplicate hierarchy paths in different scenes remain different objects.

Requested values contain `EditorJsonUtility` serialized JSON and labeled scalar records. Unsupported serialized kinds are explicit; no reflected property getter runs. Missing components have explicit records. `References=null` or `CodeDependencies=null` means the section was omitted; an empty requested list means collection succeeded with no matching facts. Reference records distinguish legitimate null from unresolved nonzero-ID references. Dependency reports separately inspect the complete ordinary loaded source graph and partition selected-subtree incoming, outgoing, internal, and boundary relations by owning GameObject. This graph describes serialized fields; arbitrary runtime calls, dynamic binding, and complete delete effects remain outside its scope.

Script inspection includes declared member metadata. `includePrivate=true` adds declared private fields, properties, and methods; private lifecycle callbacks appear independently. `SerializationCandidate` excludes static/readonly/NonSerialized fields and does not prove a SerializedObject property exists. Simple names match case-insensitively only if unique; ambiguous names and duplicate qualified type identities refuse. Inspection's plain classes exclude Unity/System namespaces; graph plain classes also exclude Microsoft/Mono and all abstract classes. Graph fields include inherited public/static fields and private serialized instance fields, unwrap one array layer then the first generic argument, omit selfedges, and expand incoming/outgoing BFS. Qualified endpoints retain source/declaring-type field facts. Source paths come from actual Assets MonoScript.GetClass mappings; unmapped classes have null file paths. Kahn read order follows dependent-to-dependency direction, with a labeled cyclic remainder; it is a reading suggestion.

Snapshots are caller-held version1 data with the actual Editor PID and process-start UTC ticks. Retain the returned complete `SnapshotJson` string. Compare within that Editor process, including domain reload; another process refuses. Snapshots include all ordinary loaded exact IDs, including hidden and inactive hierarchy objects, names, scene paths, ordered qualified component names/missing markers, world position/Euler and local scale. Changes emit name/path/components/position/rotation/scale; vectors change when any axis differs by strictly more than0.0001. Added/removed loaded scenes contribute added/removed objects. Malformed/version-mismatched snapshots, missing/duplicate/zero IDs and nonfinite coordinates refuse. No snapshot is stored by the command.

Hotspots use inclusive depth/child thresholds. Deep severity becomes Warning at threshold+3, child severity at2x; exact-case duplicate names exceed1 and warn at5. Transform-only empty leaves cluster by exact scoped parent at3. Ordering is severity, count descending, depth descending, then ordinal ties. Health retains duplicate clusters among hotspots and excludes them from findings, reports complete and shown severity totals, deduplicates exact scoped facts, and derives next-guide routes from shown findings. All capped reports disclose full totals; cap0 shows0. Conventions always check camera/light/UI infrastructure, with only the EventSystem check controlled by its toggle. JSON arrays must be well formed strings.

Spatial queries inspect all ordinary loaded transforms including inactive and hidden objects without colliders. Distance is inclusive; invalid finite coordinates/radius/caps, missing exact near references, and absent/ambiguous component types refuse. Collect all hits: discovery order when total<=cap, otherwise nearest first. Materials and performance use public native component discovery with unsorted native order across active ordinary loaded scenes. Native GameObject and component flag eligibility is preserved, including eligible hidden targets; persistent, preview and prefab-stage objects are excluded. Materials group distinct material identities by shader; `UserCount` counts slot occurrences and `Users` shows the first5. Optional properties are actual shader name/type metadata. Shared material reads do not instantiate materials.

Performance hints preserve static thresholds: shadows>4, nonstatic Renderers>100, MeshFilter triangles>10000 with no same-object LODGroup, duplicate material-slot references>10, ParticleSystems>20. Priorities1/2/2/3/3 preserve check order; a clean heuristic result has one priority0 OK entry. Counts include disabled components on active objects. Mesh triangle estimates sum all submesh index counts in checked long arithmetic, then divide the aggregate by3; mixed-topology estimates include every topology and do not allocate triangle arrays. These are suggestions, not measured GPU costs. Project-stack package evidence distinguishes manifest entries, registered packages, loaded types and asset signals. UI/profile labels are heuristics with evidence.

Input mode reads the public PlayerSettings asset property `ProjectSettings/ProjectSettings.asset:activeInputHandler` and reports that owner in `InputModeSource`. Values0/1/2 mean LegacyInputManager/InputSystem/Both; other numeric values remain `UnknownSetting:<value>`. An unavailable observation is `Unknown` with source `Unavailable`. `LegacyInputManagerAvailable` is null for unknown observations; installed Input System package evidence remains separate. Absent optional ProjectAuditor is unavailable and is never installed by inspection.

## Native hierarchy text projection

Capture native `get_scene_hierarchy` once, then run this host body on its JSON. It derives ancestor activity from `activeSelf`, preserves root/sibling/component order, excludes Transform labels, caps each sibling set, and reports shown count and omitted markers. These limits are projection inputs, not native parameters.

```python
import json, sys
max_depth, include_inactive, max_items = 5, False, 20
if type(max_depth) is not int or type(max_items) is not int or min(max_depth, max_items) < 0:
    raise ValueError("Depth and sibling limit must be nonnegative integers")
reply = json.load(sys.stdin)
if reply.get("success") is not True:
    raise ValueError("Native hierarchy capture failed")
scene = reply["data"]["result"]
lines, shown, omitted = ["Scene: " + scene["sceneName"]], 0, 0
def siblings(nodes, depth, ancestors_active=True):
    global shown, omitted
    eligible = [n for n in nodes if include_inactive or (ancestors_active and n["activeSelf"])]
    if depth > max_depth:
        omitted += len(eligible)
        if eligible: lines.append("  " * depth + "... depth limit")
        return
    for node in eligible[:max_items]:
        components = [c for c in node["components"] if c and c != "Transform"]
        lines.append("  " * depth + node["name"] + (" [" + ", ".join(components) + "]" if components else ""))
        shown += 1
        siblings(node["children"], depth + 1, ancestors_active and node["activeSelf"])
    if len(eligible) > max_items:
        omitted += len(eligible) - max_items
        lines.append("  " * depth + "... " + str(len(eligible) - max_items) + " more siblings")
siblings(scene["roots"], 0)
print(json.dumps({"sceneName": scene["sceneName"], "hierarchy": "\n".join(lines),
                  "totalObjectsShown": shown, "truncated": omitted > 0}, ensure_ascii=False))
```

## Guarded Markdown publication

Scene commands return inline Markdown without saving or importing. An explicit `savePath` requests publication; `savePath=null` and dry-run return inline output and perform neither publication nor import. Use UTF-8 without BOM at an exact Assets child `.md` path. Capture destination file/meta bytes, hashes, GUID and observed main type outside the project. New publication requires file/meta absence and unloadable main asset; replacement requires explicit authorization, the current expected file hash, exact meta hash and a loadable main asset.

Use the existing [host authoring transaction](console.md#executable-host-authoring-transaction), `operation="write"`, Base64 bytes in `writes[].content`, `replaceAuthorized` and `expectedSha256` for replacement. Bind the same exact project identity and resolved CLI. Supply one `postCommands` argv array invoking `eval --code <body>` with the semantic validator below. Substitute literals using JSON string escaping, never shell interpolation of report text. The validator throws on failure so canonical publication rollback runs; a nested ignored false result is insufficient.

```csharp
string path = "Assets/Docs/SceneReport.md";
string expectedHash = "<SHA256 of exact UTF8-noBOM report bytes>";
string expectedText = "<exact report text>";
string priorGuid = "<captured replacement GUID, or empty for creation>";
UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport | UnityEditor.ImportAssetOptions.ForceUpdate);
var main = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
if (main == null || string.IsNullOrEmpty(guid) || UnityEditor.AssetDatabase.GetAssetPath(main) != path)
    throw new System.InvalidOperationException("Report import did not yield the exact loadable path/GUID");
var observedType = main.GetType().FullName;
if (!(main is UnityEditor.DefaultAsset) && !(main is UnityEngine.TextAsset))
    throw new System.InvalidOperationException("Unexpected imported report main type: " + observedType);
if (!string.IsNullOrEmpty(priorGuid) && guid != priorGuid)
    throw new System.InvalidOperationException("Replacement changed the report GUID");
var bytes = System.IO.File.ReadAllBytes(path);
string actualHash;
using (var hash = System.Security.Cryptography.SHA256.Create())
    actualHash = System.BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
if (actualHash != expectedHash || System.Text.Encoding.UTF8.GetString(bytes) != expectedText ||
    (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191))
    throw new System.InvalidOperationException("Imported report bytes/text differ from publication");
return new { success = true, path, guid, observedType, actualHash, text = expectedText };
```

Observe the actual main type; Markdown can import as DefaultAsset. Require exact host bytes/hash/text and the validator's GUID/path/type, plus unchanged captured replacement meta hash. Publication and import are persistent/non-Undo. On failure retain the canonical transaction result and apply the [asset import lifecycle's metadata recovery](asset.md#external-import): replacement restores captured file/meta and GUID; creation removes only newly created metadata with its observed hash guard, then verifies file/meta absence and unloadable main asset. Reimport restored replacement content and verify all prior bytes/type/GUID. A failed hash/identity recovery guard preserves external snapshots and reports recovery-required. Review creation, authorized replacement, changed-hash refusal, and actual semantic-import failure/restoration before claiming publication complete.
