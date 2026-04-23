# probuilder_set_material

Assign a material to an entire ProBuilder mesh, or apply a quick prototype color.

**Signature:** `ProBuilderSetMaterial(string name = null, int instanceId = 0, string path = null, string materialPath = null, float? r = null, float? g = null, float? b = null, float? a = null)`

**Returns:** `{ success, name, instanceId, material }` or `{ success, name, instanceId, materialName, color, note }` for runtime color.

## Notes

- Provide `materialPath` for a persistent material asset (e.g. `"Assets/Materials/MyMat.mat"`).
- Provide `r/g/b` (and optionally `a`) for a quick runtime color. The color is NOT saved as an asset.
- You must provide one or the other — both empty is an error.
- For per-face material assignment use `probuilder_set_face_material`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.probuilder` package.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string materialPath = null;
        float? r = null, g = null, b = null, a = null;

        var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var renderer = pbMesh.GetComponent<MeshRenderer>();
        if (renderer == null)
        { result.SetResult(new { error = "'" + pbMesh.gameObject.name + "' has no MeshRenderer component" }); return; }

        Undo.RecordObject(renderer, "Set Material");
        WorkflowManager.SnapshotObject(pbMesh.gameObject);

        if (!string.IsNullOrEmpty(materialPath))
        {
            if (Validate.SafePath(materialPath, "materialPath") is object pathErr) { result.SetResult(pathErr); return; }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            { result.SetResult(new { error = "Material not found: " + materialPath }); return; }
            renderer.sharedMaterial = mat;
        }
        else if (r.HasValue || g.HasValue || b.HasValue)
        {
            var color = new Color(r ?? 0.5f, g ?? 0.5f, b ?? 0.5f, a ?? 1f);
            var shader = FindBestShader();
            if (shader == null)
            { result.SetResult(new { error = "Cannot find a suitable shader for current render pipeline" }); return; }
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            else
                mat.color = color;
            mat.name = "PB_" + pbMesh.gameObject.name + "_" + ColorUtility.ToHtmlStringRGB(color);
            renderer.sharedMaterial = mat;

            result.SetResult(new
            {
                success = true,
                name = pbMesh.gameObject.name,
                instanceId = pbMesh.gameObject.GetInstanceID(),
                materialName = mat.name,
                color = new { r = color.r, g = color.g, b = color.b, a = color.a },
                note = "Runtime material created. Use material_create + materialPath for persistent materials."
            });
            return;
        }
        else
        { result.SetResult(new { error = "Provide materialPath or color (r,g,b)" }); return; }

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            material = renderer.sharedMaterial.name
        });
    }

    private static (ProBuilderMesh mesh, object error) FindProBuilderMesh(string fname, int fid, string fpath)
    {
        var (go, findErr) = GameObjectFinder.FindOrError(fname, fid, fpath);
        if (findErr != null) return (null, findErr);

        var pbMesh = go.GetComponent<ProBuilderMesh>();
        if (pbMesh == null)
            return (null, new { error = "GameObject '" + go.name + "' does not have a ProBuilderMesh component" });

        return (pbMesh, null);
    }

    private static Shader FindBestShader()
    {
        // Probe pipeline-specific defaults; fall back to Standard/Unlit.
        var candidates = new[]
        {
            "Universal Render Pipeline/Lit",
            "HDRP/Lit",
            "Standard",
            "Unlit/Color"
        };
        foreach (var n in candidates)
        {
            var s = Shader.Find(n);
            if (s != null) return s;
        }
        return null;
    }
}
```
