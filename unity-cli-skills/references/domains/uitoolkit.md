# UI Toolkit

Read [foundation](foundation.md), [assets](asset.md), [components](component.md), and [ScriptableObjects](scriptableobject.md) before authoring. Discover the selected project's command catalog and built-in `UnityEngine.UIElements` availability. This preview's runtime checks target Unity 6000.6.2f1; discover fields on other versions and report unavailable requested fields. UXML, USS, UIDocument and PanelSettings use the engine module. Match visible UI text to the user's language; keep query names/classes stable.

## Content proposals and publication

`python scripts/unity_uitoolkit.py --request request.json --result proposal.json` renders explicit source/data without touching the project. `--help` lists operations. A successful result contains `Ok:true` and `Result`; refusal has `Ok:false` and exits 2. Pure rendering proves syntax/structure only. The executable publisher is [the shared authoring transaction](console.md#executable-host-authoring-transaction): `python scripts/unity_authoring.py --request publication.json --result publication-result.json`. Encode proposed UTF-8 text as Base64 in its `writes[].content`. Pass the exact project identity and resolved CLI, `operation:"write"`, and explicit post-command argv arrays. Creation uses `replaceAuthorized:false`; replacement uses authorization and `expectedSha256` for every existing target. Never publish through a second writer.

Before each publication capture exact normalized destination source and `.meta` presence/bytes/hashes, GUID, loadable type, import flags and before clone/tree state. For creation require source and `.meta` absence and an unloadable main asset; record cached GUID only as an observation. Resolve every referenced stylesheet/UXML as an exact persistent asset of the requested type and validate import flags before publication. Capture the planned absent parent chain; preserve all preexisting directory/meta bytes. After import record owned folder GUID/meta hashes.

Use explicit synchronous exact-path post validation. Typed `asset.reimport` is the existing-file route; native `import_asset` copies an external file and does not implement this operation. A read-only post eval checks the freshly resolved import and throws on any violation:

```csharp
var path = "Assets/UI/Menu.uxml";
var tree = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(path);
if (tree == null || tree.importedWithErrors || tree.importedWithWarnings)
    throw new System.InvalidOperationException("UXML import failed validation: " + path);
var root = tree.CloneTree();
if (root.Q<UnityEngine.UIElements.Button>("play") == null)
    throw new System.InvalidOperationException("Expected typed play query missing");
return new { path, guid = UnityEditor.AssetDatabase.AssetPathToGUID(path),
    tree.importedWithErrors, tree.importedWithWarnings, cloneVerified = true };
```

Use the analogous `StyleSheet` type and its actual error/warning flags for USS. Require both flags false, preserve actual flags and current diagnostic slice. An accepted warning exception must name that specific warning. Returning an inner `false` does not fail the CLI envelope; throw so the publisher's `run_checked` sees immediate `success:false`. Read file bytes/hash and every requested query/attribute after import. A pair is staged together and rolls back its published prefix on failure; visibility is atomic per path, not simultaneously across all paths.

## XML edits and inspection

Requests use `operation`, explicit `source`, and these fields:

| Operation | Inputs and result |
|---|---|
| `xml-add` | `elementType`, optional `parentName`, `elementName`, `text`, `classes`, `style`, `bindingPath`, `attributes` object; returns `content` |
| `xml-modify` | exact unique `elementName`, supplied attributes only; optional `newName`; returns `content` |
| `xml-clone` | exact unique `elementName` and distinct unique `newName`; independent subtree/attributes preserved |
| `xml-remove` | exact unique `elementName`; removes that subtree; document root refuses |
| `xml-inspect` | `depth` defaults 5, nonnegative; bounded hierarchy with local type, namespace and attributes |
| `create-uxml` | `savePath`, optional verbatim `content`, `ussPath`; absent content supplies a named root and optional stylesheet dependency |

A real XML parser preserves namespaces, comments and unrelated tree structure and escapes attributes. Serialization normalizes formatting and namespace prefixes while retaining namespace URIs; compare parsed structure for unchanged siblings. Malformed XML, DOCTYPE/entity declarations, unsupported document roots and ambiguous selection refuse before publication. A clone retains descendant names, so target any subsequently duplicated descendant with a unique strategy before editing. Same-directory stylesheets use their filename; cross-directory stylesheets use their exact asset path. Custom attribute names/values are validated. `bindingPath` maps to `binding-path`.

Example proposal request:

```json
{"operation":"xml-add","source":"<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><ui:VisualElement name=\"root\"/></ui:UXML>","parentName":"root","elementType":"Button","elementName":"play","text":"Play","classes":"primary"}
```

## USS rules and variables

`uss-upsert` takes explicit `source`, exact `selector`, and `properties` as a raw declaration block or string-valued property object. It adds or replaces precisely that selector. `uss-remove` removes one exact selector. `uss-variables` returns declared custom-property name/value/selector records and sorted distinct `var()` references including fallback references. Comments and quoted lookalikes are excluded. Lexical parsing handles escaped strings, comments, braces, brackets and parentheses; duplicate selectors, malformed delimiters and nested/at-rule structures refuse. Unaffected source spans remain exact. Lexical validity does not prove Unity semantics.

`create-uss` uses caller content or useful starter tokens. Prefer flex rows/wrapping, explicit values, rounded surfaces and actual child elements for decoration. USS is a restricted stylesheet language: use Unity import flags and the selected version's supported properties before promising CSS features. Start with tokens, then component rules, then layout containers. Keep USS alongside UXML and inspect structure before complex edits.

## Ten starters and generated scripts

`template` takes one of `menu`, `hud`, `dialog`, `settings`, `inventory`, `list`, `tab-view`, `toolbar`, `card`, `notification`, a `savePath` **directory**, and optional single file `name`. Default names are title-cased template names, including `Tab-View`. It returns ordered paired USS/UXML `writes` with `text`, exact paths and role-specific unique query names. Both destinations must be absent. These are useful visual starters; attach application callbacks separately. The sole starter data owner is `scripts/unity_cli/uitoolkit_templates.py`.

`editor-window` takes `savePath`, `className`, optional `windowTitle`, `menuPath`, `uxmlPath`, `ussPath`. Defaults are title=class, menu=`Window/<class>`, minimum size 400×300. An omitted UXML builds a label/action root; supplied UXML clones and supplied USS attaches. `runtime-ui` takes `savePath`, `className`, optional comma-separated `elementQueries` of `Type:name` pairs. It emits `[RequireComponent(typeof(UIDocument))]`, typed `root.Q<T>(name)` fields and a null-root guard. Names become `m_` fields with hyphens/underscores removed; collisions refuse. Canonical non-keyword class/file identifiers, valid type tokens, unique fields and escaped C# literals are required; malformed pairs refuse as a whole. These scaffolds do not fabricate callback behavior.

Prevalidate referenced asset paths/types/flags before script publication. Use the [terminal compile boundary](script.md) after import; require true completed compilation and inspect diagnostics. Open the actual menu/window and inspect its tree; bind/enter Play for runtime queries and read actual field/query results. A host proposal or successful import acknowledgement does not prove compilation. Capture representative rendered UI through the [camera/capture owners](lighting-camera.md) and inspect the PNG.

For multiple creates, render an ordered `batch` with `items` (each explicit operation/data); it continues after an item refusal, returns requested/succeeded/failed and ordered results, and exits nonzero on any failure. This is a proposal batch. Actual file creation uses **one guarded publication per original item**, with synchronous validation/readback and recovery before the next item; keep per-item success/failure, continue after fully recovered failure, and return nonzero aggregate status. Map original file item `type:uss|uxml` to `create-uss|create-uxml` and preserve its `savePath`, `content`, `ussPath`. Never count rendered content as a created file.

## Read, find and recoverable deletion

Read UXML/USS with native `read_text_file` after exact path confinement and extension/type validation. Return actual path, inferred `uxml|uss` kind, content and line count; bound presentation separately from actual content. Find with a bounded read-only AssetDatabase query: validate `type` (`uxml`, `uss`, `all`), folder confinement/existence and filter, search VisualTreeAsset/StyleSheet as appropriate, resolve exact paths/types, filter by the requested name/path text, deduplicate and sort ordinally **before** taking `limit` (default 200, nonnegative). Return unbounded matching `total`, bounded `count` and results. The folder scope must include only its selected descendants.

Delete only after exact UI asset type/hash/GUID preflight with confirmed `asset.trash` from [assets](asset.md). Retain the recovery location and restore/read back exact source/meta/GUID. Trash is persistent and non-Undo; do not promise editor Undo for deletion.

## UIDocument authoring and live reads

Prevalidate the entire request before mutation: exact selected loaded regular scene, one unambiguous parent identity in that same scene, normalized persistent UXML and PanelSettings references with actual type/loadability/flags, and finite sorting order. Unknown/bad second references must leave no object/component mutation. Create with native `create_gameobject`, `add_component` for `UnityEngine.UIElements.UIDocument`, exact `set_serialized_field` and existing `batch` composition. Default name is `UIDocument` and sorting order is 0; preserve omitted bindings/order on set. For set, resolve one exact GameObject and ensure the component once; obtain its exact returned handle. Discover `get_serialized_fields` for source asset/PanelSettings/order paths rather than guessing them. Apply only the prevalidated fields. Follow [foundation's returned-group recovery](foundation.md) for batch failures, clean up only an owned newly created object, then verify Undo/Redo and scene save/reopen bindings.

Inspect a selected exact UIDocument with public `rootVisualElement`. Report inactive document and null live root explicitly. Recursively return type/name/classes/child count and bounded children; depth defaults 5, rejects negative values. Sort document enumeration by exact scene path and hierarchy identity before limiting. List only UIDocuments whose GameObjects belong to the exact selected loaded scene: include inactive scene objects, exclude persistent assets, prefab stages and unloaded scenes. Return actual UXML/Panel paths, sorting order and active state. Public `Resources.FindObjectsOfTypeAll<UIDocument>()` requires those filters. Compare live named typed children/classes after binding and Play lifecycle. Runtime rendering requires an available PanelSettings; world-space behavior also needs the selected version's scene-side setup.

## PanelSettings create, set and read

Use existing native `create_asset(type="UnityEngine.UIElements.PanelSettings")`, native serialized setters/batch, the public member owner and the narrowly scoped [partial JSON owner](scriptableobject.md). Create defaults: `ScaleWithScreenSize`, resolution 1920×1080 and `MatchWidthOrHeight`. Leave the built-in theme untouched unless explicitly changed. Set preserves every omitted field, including individual resolution axes and RGBA channels. Read the complete before state and resolve requested enum names/numeric values against actual public enum definitions; invalid/range-unsupported values refuse before mutation. Discover actual serialized paths/types with `get_serialized_fields`/`SerializedObject.FindProperty`. Report requested unavailable fields; never substitute guessed defaults.

| Original input | Actual semantic target / validation |
|---|---|
| `scaleMode`, `screenMatchMode` | public `scaleMode`, `screenMatchMode`; named or numeric defined enums |
| `referenceResolutionX/Y` | `referenceResolution.x/y`; positive integers, merge omitted axis |
| `themeStyleSheetPath`, `textSettingsPath`, `targetTexturePath` | exact persistent `ThemeStyleSheet`, `PanelTextSettings`, `RenderTexture` references; preserve omitted references |
| `targetDisplay`, `sortOrder`, `scale`, `match` | `targetDisplay`, `sortingOrder`, `scale`, `match`; display valid for version, finite order, positive scale, match 0..1 |
| `referenceDpi`, `fallbackDpi`, `referenceSpritePixelsPerUnit` | same public names; finite positive values |
| `dynamicAtlasMinSize`, `dynamicAtlasMaxSize`, `dynamicAtlasMaxSubTextureSize` | `dynamicAtlasSettings.minAtlasSize/maxAtlasSize/maxSubTextureSize`; positive supported sizes, min≤max and subtexture≤max |
| `dynamicAtlasFilters` | `dynamicAtlasSettings.activeFilters`; validate all public flag bits and convert combined names to underlying numeric mask |
| `clearColor`, `colorClearR/G/B/A`, `clearDepthStencil` | public `clearColor`, `colorClearValue` merged channels, `clearDepthStencil`; finite supported color values |
| `renderMode`, `colliderUpdateMode`, `colliderIsTrigger` | discover serialized `m_RenderMode`, `m_ColliderUpdateMode`, `m_ColliderIsTrigger`; actual availability and enum values |
| `forceGammaRendering`, `bindingLogLevel`, `vertexBudget`, `textureSlotCount` | public/serialized fields if available; `forceGammaRendering` boolean; `bindingLogLevel` and [`textureSlotCount`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/UIElements.PanelSettings-textureSlotCount.html) defined enums; [`vertexBudget`](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/UIElements.PanelSettings-vertexBudget.html) unsigned integer 0–4294967295, with 0 selecting the renderer default; negative or out-of-range budgets refuse |

Use `scriptableobject.member-set` for the public scalar float properties `sortingOrder`, `scale`, `match`, `referenceDpi`, `fallbackDpi` and `referenceSpritePixelsPerUnit`. Discover its schema, then pass the exact persistent PanelSettings path as `asset`, the exact public property name as `member` and an invariant scalar string as `value`. For example, `{"asset":"Assets/UI/MenuPanel.asset","member":"scale","value":"1.25","dryRun":true}` previews conversion; the applied request uses `dryRun:false,confirm:true`. Prevalidate every requested range before any setter. These properties require their public setter callbacks: a pending serialized float value can be accepted and then reset by `ApplyModifiedProperties`. Read the actual public property immediately after the command and again after selected-target save and synchronous reimport; `ValueSet` reports the requested conversion and does not prove the callback result. Keep native serialized setters for supported integer resolution axes, atlas sizes, booleans, enums, references and merged color channels.

Capture the positive finite before-state `scale`. Apply the native serialized phase first, then the requested public scalar setters; after that phase, use `scriptableobject.member-set` to restore the effective scale if readback differs: the requested scale when supplied, otherwise the captured before-state scale. Unity 6000.6.2f1 can reset omitted scale to 1 while applying other serialized fields, including resolution axes and color channels. Keep partial flags JSON last and verify every supplied and omitted value after the complete composition and selected-target save/reimport; refuse success if any callback reset remains. Compose those operations with the same owned Undo and persistent recovery boundary below.

Read every public target above plus exact path/GUID, theme/text/texture paths and per-field availability. Include `textureSlotCount` when available; do not infer support from a broad Unity 6 version label. For internal fields require `FindProperty` present and read its actual value/type. Preserve null versus unavailable distinctions.

Combined atlas masks use numeric bits through a **partial** Editor JSON payload rooted at `MonoBehaviour`, with the actual discovered atlas/filter field nesting. Dry-run first, then confirmation and readback of masks 5 and 9 after save/reimport. Full Panel JSON is not supported by this route because the Vector2Int JSON shape fails its current validator. Place partial flags JSON **last** after prevalidated native and public member setters; it saves only its resolved target. Keep one Undo boundary or foundation's exact returned-group restoration for mixed operations. Success marks/saves only the selected asset, synchronously reimports and compares all supplied and omitted fields and metadata identity. Read the actual public properties after both Undo and Redo; persistent restoration requires explicit selected-target save/reimport.

Unity 6000.6.2f1 can reset PanelSettings `scale` to 1 during Redo: an applied value of 1.25 returned to 1 after Undo and remained 1 after Redo and synchronous reimport, including with the public property and `Undo.RecordObject` in ScreenSpaceOverlay. If readback differs from the captured desired positive finite scale, restore it explicitly through `scriptableobject.member-set` under the retained GUID/source/meta hash and persistent recovery guards below, save only the selected target, synchronously reimport and reread its complete state. Report the observed engine limitation and explicit restoration; automatic Redo success requires matching readback.

## Persistent recovery

File publication rollback restores bytes only. Preserve its original error and recovery records. For existing UXML/USS restore exact owned source/meta snapshots under identity/hash guards, synchronously reimport and verify original GUID/meta/loadable type/import flags/clone state. For absent originals whose new source survives, remove only the exact task-created asset through native `delete_asset` after created GUID/source/meta hash checks. When rollback removed the source and left metadata, native deletion is not viable: use [the existing asset lifecycle](asset.md), require exact source absence and observed created GUID/meta hash, remove only owned `.meta`, then call public `AssetDatabase.Refresh(ForceSynchronousImport | ForceUpdate)`. Prove source/meta absence and unloadable main asset; cached GUID is observational. Clean owned empty parent folders in reverse order only after checking their identity/meta and proving no unrelated child appeared. Preserve preexisting parents/meta.

Generated C# recovery also runs the terminal compile lifecycle after restored/imported source. If bytes/cache/meta/compile cannot be restored, report `recovery-required`, list retained snapshot paths and stop dependent work. A semantic import failure never authorizes deletion of preexisting assets or metadata.

Before a mixed Panel set retain exact source/meta bytes/hashes, GUID and full serialized before state. After any failure, **before every recovery save or byte replacement**, compare current source/meta identity and expected post-operation hashes; another writer refuses recovery. Restore only the owned Undo group in memory, mark/save only the selected restored asset, synchronously reimport and compare full before state and GUID/meta. If serialization cannot restore exact original bytes, use the hash-bound shared publication transaction to restore opaque original bytes, then reimport and compare complete state. Failed guards or restoration return `recovery-required` and retain snapshots. No scene-only Undo claim applies to a saved asset.

Panel creation prevalidates exact source and `.meta` absence plus unloadable main asset, parent confinement, all fields/references/ranges and collision state before `create_asset`. Refuse orphan metadata. Creation is persistent/non-Undo and outside reversible native batch. Capture the newly created path/GUID/source/meta hashes before setup. After a setup failure, verify current identity and expected hashes before cleanup, remove only that created asset/owned metadata via the verified lifecycle, preserve preexisting assets/folders/meta and prove absence. Incomplete cleanup returns `recovery-required` and stops dependent work. Keep all snapshots until actual creation/import/refusal/recovery, post-save Panel recovery and concurrent-writer guards have been verified.
