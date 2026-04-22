# material_assign

Assign a material asset to a renderer on a GameObject.

**Signature:** `MaterialAssign(string name = null, int instanceId = 0, string path = null, string materialPath = null)`

**Returns:** `{ success, gameObject, material }`

## Notes

- Exactly one of `name`, `instanceId`, or `path` must identify the target GameObject.
- `materialPath` is the asset path of the material to assign (e.g. `Assets/Materials/MyMat.mat`). Required.
- Fails if the GameObject has no `Renderer` component.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Cube";                            // target GameObject name
        int instanceId = 0;                              // or use instanceId
        string path = null;                              // or use hierarchy path
        string materialPath = "Assets/Materials/Red.mat"; // required

        if (Validate.Required(materialPath, "materialPath") is object err) { result.SetResult(err); return; }

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            { result.SetResult(new { error = "No Renderer component found" }); return; }

        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
            { result.SetResult(new { error = $"Material not found: {materialPath}" }); return; }

        WorkflowManager.SnapshotObject(renderer);
        Undo.RecordObject(renderer, "Assign Material");
        renderer.sharedMaterial = material;

        { result.SetResult(new { success = true, gameObject = go.name, material = materialPath }); return; }
    }
}
```
