# probuilder_set_face_material

Assign a material to specific faces of a ProBuilder mesh.

**Signature:** `ProBuilderSetFaceMaterial(string name = null, int instanceId = 0, string path = null, string faceIndexes = null, string materialPath = null, int submeshIndex = -1)`

**Returns:** `{ success, name, instanceId, affectedFaces, materialCount }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to apply to all faces.
- Provide either `materialPath` (asset path, e.g. `"Assets/Materials/MyMat.mat"`) or `submeshIndex`.
- When `materialPath` is provided, the material is added to the renderer's shared materials array if not already present.
- `submeshIndex`: use `-1` (default) when providing `materialPath`; otherwise set to an existing renderer slot index.
- Use `probuilder_set_material` to assign a single material to the whole object.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !PROBUILDER
                    { result.SetResult(NoProBuilder()); return; }
        #else
                    var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
                    if (err != null) { result.SetResult(err); return; }

                    var faces = SelectFaces(pbMesh, faceIndexes);
                    if (faces.Count == 0)
                        { result.SetResult(new { error = "No faces selected. Provide faceIndexes or omit to apply to all." }); return; }

                    // Validate inputs BEFORE Undo/Snapshot
                    if (!string.IsNullOrEmpty(materialPath))
                    {
                        if (Validate.SafePath(materialPath, "materialPath") is object pathErr) { result.SetResult(pathErr); return; }
                    }
                    else if (submeshIndex < 0)
                    {
                        { result.SetResult(new { error = "Provide either materialPath or submeshIndex" }); return; }
                    }

                    var renderer = pbMesh.GetComponent<MeshRenderer>();
                    if (renderer == null)
                        { result.SetResult(new { error = $"'{pbMesh.gameObject.name}' has no MeshRenderer component" }); return; }

                    Undo.RecordObject(pbMesh, "Set Face Material");
                    Undo.RecordObject(renderer, "Set Face Material");
                    WorkflowManager.SnapshotObject(pbMesh);

                    if (!string.IsNullOrEmpty(materialPath))
                    {
                        var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (mat == null)
                            { result.SetResult(new { error = $"Material not found: {materialPath}" }); return; }

                        // Determine submesh index for this material
                        var sharedMats = renderer.sharedMaterials;
                        int matIndex = Array.IndexOf(sharedMats, mat);

                        if (matIndex < 0)
                        {
                            // Add material to renderer
                            var newMats = new Material[sharedMats.Length + 1];
                            Array.Copy(sharedMats, newMats, sharedMats.Length);
                            newMats[sharedMats.Length] = mat;
                            renderer.sharedMaterials = newMats;
                            matIndex = sharedMats.Length;
                        }

                        foreach (var face in faces)
                            face.submeshIndex = matIndex;
                    }
                    else
                    {
                        foreach (var face in faces)
                            face.submeshIndex = submeshIndex;
                    }

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        affectedFaces = faces.Count,
                        materialCount = pbMesh.GetComponent<MeshRenderer>().sharedMaterials.Length
                    }); return; }
        #endif
    }
}
```
