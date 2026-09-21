# Asset operations

Read [foundation routing and safety](foundation.md) before an Asset operation. Foundation owns project targeting, confinement, exact references, confirmations, and the caller's current authorization. An existing authorization remains in force for the requested operation; do not ask the person to repeat it. Asset files, folders, labels, import state, and importer effects are persistent and non-Undo operations. Capture the exact GUID and relevant file hashes before a write, read the result back, and restore owned state during cleanup.

All custom paths are project-relative `Assets/...` paths unless the caller explicitly opts into an existing embedded package. Each mutator performs its own foundation validation immediately before mutation. Read-only preflight never authorizes a later write. Discover every native command in the selected project before invoking it; the captured Unity 6.6 forms below are the baseline.

## Operation map

| Operation | Owner and route |
| --- | --- |
| `asset_create_folder` | Native `create_folder` |
| `asset_delete` | Typed `asset.trash` |
| `asset_delete_batch` | Ordered typed `asset.trash` calls |
| `asset_duplicate` | Native `copy_asset` after unique-path preview |
| `asset_find` | Read-only raw `AssetDatabase.FindAssets` evaluation |
| `asset_get_info` | Read-only AssetDatabase evaluation |
| `asset_get_labels` | Read-only AssetDatabase evaluation |
| `asset_import` | Hash-bound host publication followed by explicit Unity import lifecycle |
| `asset_import_batch` | Ordered hash-bound publications and import lifecycles |
| `asset_move` | Native `move_asset` |
| `asset_move_batch` | Native `batch(transactional=false,on_error=continue)` |
| `asset_refresh` | Authorized, non-Undo public evaluation calling `AssetDatabase.Refresh` |
| `asset_reimport` | Typed `asset.reimport` |
| `asset_reimport_batch` | Ordered typed `asset.reimport` calls |
| `asset_set_labels` | Typed `asset.labels` |

`asset_get_info` and `asset_get_labels` share the exact-path metadata evaluation; callers select the fields required by their operation.

## Folders, copies, and moves

Create a folder with native `create_folder` using its one required `path` parameter. The path is relative to the authoring root, permits an optional `Assets/` prefix, and creates intermediate folders. Native creation is idempotent for an existing folder, so a create-only request first runs this read-only check on the normalized, confined destination. A file collision also refuses. Read the returned folder path and GUID back. Cleanup removes only the owned empty folder and `.meta` state through its owning route.

```csharp
string path = "Assets/NewFolder";
if (UnityEditor.AssetDatabase.IsValidFolder(path) || UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null)
    throw new System.ArgumentException("Destination already exists");
return new { available = true, path };
```

Copy an exact selected asset with native `copy_asset`. Its captured schema is `asset` (ObjectRef), `destination` (string), optional `confirm=false`, and optional `dry_run=false`. The destination includes an extension and is relative to the authoring root. Preview a deterministic unused destination first, then call `copy_asset` with that path. A successful copy must have equal source bytes and main type, a distinct GUID, and a loadable result. Record the copied path, GUID, and hash so cleanup can remove only that copy.

```csharp
string sourceAsset = "Assets/Materials/Example.mat";
string preferredDestination = "Assets/Materials/Example Copy.mat";
var source = UnityEditor.AssetDatabase.LoadMainAssetAtPath(sourceAsset);
if (source == null) throw new System.ArgumentException("sourceAsset must identify a main asset");
var destination = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(preferredDestination);
return new {
    source = UnityEditor.AssetDatabase.GetAssetPath(source),
    destination,
    sourceGuid = UnityEditor.AssetDatabase.AssetPathToGUID(sourceAsset),
    sourceEntityId = UnityEngine.EntityId.ToULong(source.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture)
};
```

Move or rename an exact selected asset with native `move_asset`: `asset` (ObjectRef), `destination` (string), and optional `dry_run=false`. The command preserves the GUID. Read the old and new paths, GUID, main type, and file hash around the operation; cleanup performs the inverse move only for the owned result. For file or folder moves, missing sources, destination collisions, roots, escapes, and reparse points refuse.

For two or more moves, use native `batch(transactional=false,on_error=continue)` in request order only when discovery shows it accepts `move_asset`. Inspect both each outer batch item and its nested typed `Ok`/result status. Keep one result per request item, allow a later item after a failed one, and reverse each successful owned move during cleanup. This continuation workflow is not transactional and does not claim Undo.

## Find and inspect

`find_assets` is not the route for `asset_find`: its captured filters are separate type/name/label fields, require at least one filter, default to 200, and do not preserve the raw `AssetDatabase.FindAssets` expression. Use a bounded, read-only evaluation instead. It resolves every GUID, ordinal-sorts paths, computes `totalFound` before applying the default limit of 50, and returns path, name, type, and GUID for each shown asset. A negative limit refuses.

```csharp
string rawFilter = "t:Texture2D player";
int limit = 50;
if (limit < 0) throw new System.ArgumentOutOfRangeException("limit");
var matches = UnityEditor.AssetDatabase.FindAssets(rawFilter)
    .Select(guid => new { guid, path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid) })
    .OrderBy(match => match.path, System.StringComparer.Ordinal)
    .ToArray();
var assets = matches.Take(limit).Select(match => {
    var value = UnityEditor.AssetDatabase.LoadMainAssetAtPath(match.path);
    return new { path = match.path, name = value == null ? null : value.name,
        type = value == null ? null : value.GetType().FullName, guid = match.guid };
}).ToArray();
return new { count = assets.Length, totalFound = matches.Length, assets };
```

Use the following read-only body for exact-path metadata and labels. It supports folders, main assets, and subassets by inspecting the main asset; a missing path refuses. `entityId` uses Unity 6.6's static `EntityId` API and is an unsigned decimal string.

```csharp
string assetPath = "Assets/Textures/player.png";
var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath);
if (asset == null) throw new System.ArgumentException("assetPath must identify a main asset or folder");
var labels = UnityEditor.AssetDatabase.GetLabels(asset).OrderBy(label => label, System.StringComparer.Ordinal).ToArray();
return new {
    path = UnityEditor.AssetDatabase.GetAssetPath(asset), name = asset.name, type = asset.GetType().FullName,
    guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath), labels,
    entityId = UnityEngine.EntityId.ToULong(asset.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture)
};
```

## Recoverable removal, labels, reimport, and refresh

Use `asset.trash` for a recoverable deletion. Its exact captured input is `asset`, optional `dryRun=false`, optional `confirm=false`, and optional `allowEmbeddedPackages=false`. A real removal requires `confirm=true`; it moves the exact asset and meta file to OS trash and returns the pre-removal GUID. Verify that the asset is absent after the call. Capture file and meta bytes, hashes, and GUID before removal. Restore through OS trash or the exact captured file/meta snapshot, then import and verify the original GUID and loadability. It is non-Undo.

For multiple removals, use native `batch(transactional=false,on_error=continue)` with ordered `asset.trash` items after discovery confirms the typed command is accepted. Inspect the outer item and nested `Ok` for every result; a later item proceeds after an earlier failure. Each successful owned removal retains its own recovery record. This file-operation batch is nontransactional and non-Undo.

Use `asset.labels` to replace labels on one exact asset. Its schema is `asset`, `labels` (`string[]`), optional `dryRun=false`, `confirm=false`, and `allowEmbeddedPackages=false`. It drops blank labels and normalizes retained labels to ordinal distinct sorted values. Capture the precise `Before` array, use `dryRun` to inspect the normalized proposal, then send `confirm=true` for the authorized write. Require `After` to equal `Proposed`; restore the captured array with the same route. This operation is non-Undo.

Use `asset.reimport` for one exact project-relative existing path. Its schema is `asset`, optional `dryRun=false`, `confirm=false`, and `allowEmbeddedPackages=false`. It forces synchronous reimport, reports `BeforeGuid` and `AfterGuid`, and is non-Undo. Reject absolute paths. Capture the complete applicable importer state before reimport when it matters, inspect that state after reimport, and restore it through the importer owner when cleanup requires it.

For multiple reimports, use native `batch(transactional=false,on_error=continue)` with ordered `asset.reimport` items after discovery confirms the typed command is accepted. Check each outer command result and nested `Ok`; each later item remains eligible after a failure. A successful item retains its pre-reimport state for restoration. This is nontransactional and non-Undo.

`asset_refresh` is a bounded public mutation covered by the caller's existing authorization. It imports staged files and changes AssetDatabase state, is non-Undo, and acknowledges completion. Use it after staging an owned file, then read the exact expected asset path.

```csharp
UnityEditor.AssetDatabase.Refresh();
return new { success = true, refreshed = true };
```

## External import

External import has two owners. The canonical host authoring transaction publishes bytes to a confined project path; Unity's explicit `AssetDatabase.ImportAsset(destination)` owns the later import lifecycle. The native `import_asset` command is not a substitute because its captured inputs are `source`, `path`, optional `confirm`, and optional `dry_run`; it cannot bind the required source or destination hashes.

The host request operation is `asset-import`. It provides an absolute `sourcePath`, `expectedSourceSha256`, `destinationPath`, `expectedDestinationState`, and `replaceAuthorized`, with the common project and CLI fields unchanged. Relative source paths refuse before the source is read or the destination is validated. It reads the selected source exactly once into a byte snapshot, hashes that snapshot, refuses a mismatch, and publishes those same bytes. The destination expectation is either `{kind:"absent"}` or `{kind:"existing",sha256:"..."}`. An absent target requires `replaceAuthorized=false`; an existing target requires `replaceAuthorized=true` and must still have the expected hash immediately before publication. The transaction applies its normal confinement, staging, reparse-point, and recovery rules.

Before publication, capture the destination file and `.meta` bytes and hashes plus the AssetDatabase GUID,
main type, and loadability. An expected-new destination requires both files to be absent and its main asset
unloadable; record any cached GUID without treating it as live state. An expected replacement requires a
loadable main asset, a nonempty GUID, and exact file and `.meta` snapshots. Keep those snapshots outside the
project until the lifecycle and any recovery have been verified.

Pass one explicit post command to the host transaction. That command calls
`AssetDatabase.ImportAsset(destination, ForceSynchronousImport | ForceUpdate)`, requires a loadable main
asset, and reads the project file back to verify its SHA-256, expected main type, and nonempty GUID. A
replacement also requires the GUID to equal the captured prior GUID. The transaction restores the prior file
or removes its newly published file when this command fails. The lifecycle then owns `.meta` recovery: restore
the exact captured `.meta` bytes for a replacement, or remove only a newly created `.meta` after verifying its
observed hash. Refresh and reimport as applicable, then require the complete prior state for a replacement or
file/meta absence and an unloadable main asset for a new destination. `AssetPathToGUID` may retain a cached
value after deletion, so record it as an observation but never use it as absence proof. Retain the external
snapshots and identify their paths if any recovery or verification step fails.

A successful item returns `{success:true, imported:<destination>}` plus `filePublished=true`,
`importComplete=true`, source and destination hashes, the verified GUID and main type,
`partialFailure=false`, and an empty retained-path list. A publication or import failure returns one failed
item with `filePublished` and `importComplete` set to the observed stages. A fully recovered failure has
`partialFailure=false`; incomplete recovery sets it true and lists every retained recoverable path. Never
report import completion or a GUID from host publication alone.

For a batch, perform this complete lifecycle serially per item. Each item has its own source snapshot hash,
destination expectation, replacement authorization, and recovery record. Return
`{success,totalItems,successCount,failCount,results}` with exactly one ordered result per input and count each
failed item once, regardless of whether publication, import, or recovery exposed the failure. A destination
with incomplete recovery cannot be reused in that run; unrelated later items still proceed. No batch is
transactional and no asset import claims Undo.
