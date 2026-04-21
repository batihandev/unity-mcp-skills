# material_assign

Assign a material asset to a renderer on a GameObject.

**Signature:** `MaterialAssign(string name = null, int instanceId = 0, string path = null, string materialPath = null)`

**Returns:** `{ success, gameObject, material }`

## Notes

- Exactly one of `name`, `instanceId`, or `path` must identify the target GameObject.
- `materialPath` is the asset path of the material to assign (e.g. `Assets/Materials/MyMat.mat`). Required.
- Fails if the GameObject has no `Renderer` component.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

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
