# model_get_mesh_info

Get vertex, triangle, and submesh statistics for a mesh.

## Signature

```
model_get_mesh_info(name?: string, instanceId?: int, path?: string, assetPath?: string)
  → { success, name, vertexCount, triangles, subMeshCount, bounds,
      hasNormals, hasTangents, hasUV, hasUV2, hasColors, blendShapeCount, isReadable }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Use this for project assets
        string name = null;       // OR use scene target
        int instanceId = 0;
        string path = null;

        Mesh mesh = null;

        if (!string.IsNullOrEmpty(assetPath))
        {
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (mesh == null)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (go != null)
                {
                    var mf = go.GetComponentInChildren<MeshFilter>();
                    if (mf != null) mesh = mf.sharedMesh;
                }
            }
        }
        else
        {
            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) { result.SetResult(error); return; }
            var mf = go.GetComponent<MeshFilter>();
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            mesh = mf != null ? mf.sharedMesh : smr != null ? smr.sharedMesh : null;
        }

        if (mesh == null) { result.SetResult(new { error = "No mesh found" }); return; }

        { result.SetResult(new
        {
            success = true,
            name = mesh.name,
            vertexCount = mesh.vertexCount,
            triangles = SkillsCommon.GetTriangleCount(mesh),
            subMeshCount = mesh.subMeshCount,
            bounds = new { center = $"{mesh.bounds.center}", size = $"{mesh.bounds.size}" },
            hasNormals = mesh.normals.Length > 0,
            hasTangents = mesh.tangents.Length > 0,
            hasUV = mesh.uv.Length > 0,
            hasUV2 = mesh.uv2.Length > 0,
            hasColors = mesh.colors.Length > 0,
            blendShapeCount = mesh.blendShapeCount,
            isReadable = mesh.isReadable
        }); return; }
    }
}
```

## Notes

- When `assetPath` points to an FBX, the first Mesh sub-asset found in the hierarchy is used.
- For embedded materials and animation clips in the asset, use `model_get_materials_info` and `model_get_animations_info`.
