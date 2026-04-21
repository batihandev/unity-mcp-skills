# prefab_create

Create a prefab asset from a scene GameObject. The source object remains in the scene connected to the new prefab asset.

**Signature:** `PrefabCreate(string name = null, int instanceId = 0, string path = null, string savePath = null)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Source GameObject name |
| `instanceId` | int | No* | Instance ID (preferred) |
| `path` | string | No* | Hierarchy path |
| `savePath` | string | Yes | Asset save path (e.g. `Assets/Prefabs/Enemy.prefab`) |

*At least one object identifier required alongside `savePath`.

## Returns

```json
{
  "success": true,
  "prefabPath": "Assets/Prefabs/Enemy.prefab",
  "name": "Enemy"
}
```

## Notes

- Uses `PrefabUtility.SaveAsPrefabAssetAndConnect` — the scene object becomes a connected prefab instance.
- Intermediate directories are created automatically if they do not exist.
- `savePath` must end in `.prefab`.
- The created prefab asset is recorded in the workflow snapshot.
- Do NOT use `prefab_create_from_object` — that command does not exist.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            if (Validate.Required(savePath, "savePath") is object reqErr) return reqErr;
            if (Validate.SafePath(savePath, "savePath") is object pathErr) return pathErr;

            var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
            if (findErr != null) return findErr;

            var dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, savePath, InteractionMode.UserAction);

            WorkflowManager.SnapshotCreatedAsset(prefab);

            return new { success = true, prefabPath = savePath, name = prefab.name };
        */
    }
}
```
