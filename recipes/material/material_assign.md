# material_assign

Assign a material asset to a renderer on a GameObject.

**Signature:** `MaterialAssign(string name = null, int instanceId = 0, string path = null, string materialPath = null)`

**Returns:** `{ success, gameObject, material }`

## Notes

- Exactly one of `name`, `instanceId`, or `path` must identify the target GameObject.
- `materialPath` is the asset path of the material to assign (e.g. `Assets/Materials/MyMat.mat`). Required.
- Fails if the GameObject has no `Renderer` component.

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

        /* Original Logic:

            if (Validate.Required(materialPath, "materialPath") is object err) return err;

            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return new { error = "No Renderer component found" };

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                return new { error = $"Material not found: {materialPath}" };

            WorkflowManager.SnapshotObject(renderer);
            Undo.RecordObject(renderer, "Assign Material");
            renderer.sharedMaterial = material;

            return new { success = true, gameObject = go.name, material = materialPath };
        */
    }
}
```
