# material_set_render_queue

Set the render queue of a material.

**Signature:** `MaterialSetRenderQueue(string name = null, int instanceId = 0, string path = null, int renderQueue = -1)`

**Returns:** `{ success, target, renderQueue, queueCategory }`

## Notes
- `renderQueue = -1` restores the shader's default queue (reported as `ShaderDefault`).
- `queueCategory` is derived from the numeric value:
| Range | Category |
|-------|----------|
| -1 | ShaderDefault |
| < 2000 | Background |
| 2000–2449 | Geometry |
| 2450–2499 | AlphaTest |
| 2500–2999 | GeometryLast |
| 3000–3999 | Transparent |
| >= 4000 | Overlay |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name        = "Cube";  // target GameObject name
        int    instanceId  = 0;
        string path        = null;    // or material asset path
        int    renderQueue = 3000;    // -1 for shader default; 3000 = Transparent

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Render Queue");
        material.renderQueue = renderQueue;

        if (go == null) EditorUtility.SetDirty(material);

        string queueName = renderQueue switch
        {
            -1 => "ShaderDefault",
            < 2000 => "Background",
            < 2450 => "Geometry",
            < 2500 => "AlphaTest",
            < 3000 => "GeometryLast",
            < 4000 => "Transparent",
            _ => "Overlay"
        };

        { result.SetResult(new { 
            success = true, 
            target = go != null ? go.name : path, 
            renderQueue,
            queueCategory = queueName
        }); return; }
    }

    private static (Material mat, GameObject go, object error) FindMaterial(string name, int instanceId, string path)
    {
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".mat"))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) return (null, null, new { error = "Material asset not found: " + path });
            return (m, null, null);
        }
        var (go, err) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (err != null) return (null, null, err);
        var rdr = go.GetComponent<Renderer>();
        if (rdr == null) return (null, go, new { error = "No Renderer on " + go.name });
        var mat = rdr.sharedMaterial;
        if (mat == null) return (null, go, new { error = "No material on " + go.name });
        return (mat, go, null);
    }
}
```
