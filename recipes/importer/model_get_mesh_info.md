# model_get_mesh_info

Get vertex, triangle, and submesh statistics for a mesh.

**Skill ID:** `model_get_mesh_info`
**Source:** `ModelSkills.cs` — `ModelGetMeshInfo`

## Signature

```
model_get_mesh_info(name?: string, instanceId?: int, path?: string, assetPath?: string)
  → { success, name, vertexCount, triangles, subMeshCount, bounds,
      hasNormals, hasTangents, hasUV, hasUV2, hasColors, blendShapeCount, isReadable }
```

## Parameters

Provide either `assetPath` (project asset) or a scene target (`name`/`instanceId`/`path`):

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | no | Project-relative path to the model asset |
| `name` | string | no | Scene GameObject name |
| `instanceId` | int | no | Scene GameObject instance ID |
| `path` | string | no | Scene hierarchy path |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/skills_common.md` — for `SkillsCommon.*`

## Unity_RunCommand Template

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
            if (error != null) return error;
            var mf = go.GetComponent<MeshFilter>();
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            mesh = mf != null ? mf.sharedMesh : smr != null ? smr.sharedMesh : null;
        }

        if (mesh == null) return new { error = "No mesh found" };

        return new
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
        };
    }
}
```

## Notes

- When `assetPath` points to an FBX, the first Mesh sub-asset found in the hierarchy is used.
- For embedded materials and animation clips in the asset, use `model_get_materials_info` and `model_get_animations_info`.
