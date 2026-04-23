# prefab_create

Create a prefab asset from a scene GameObject. The source object remains in the scene connected to the new prefab asset.

**Signature:** `PrefabCreate(string name = null, int instanceId = 0, string path = null, string savePath = null)`

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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string savePath = null;

        if (Validate.Required(savePath, "savePath") is object reqErr) { result.SetResult(reqErr); return; }
        if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // 使用 SaveAsPrefabAssetAndConnect 将场景物体连接为预制体实例
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, savePath, InteractionMode.UserAction);

        // 记录新创建的预制体资产
        WorkflowManager.SnapshotCreatedAsset(prefab);

        { result.SetResult(new { success = true, prefabPath = savePath, name = prefab.name }); return; }
    }
}
```
