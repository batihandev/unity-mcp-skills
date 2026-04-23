# probuilder_project_uv

Box-project UVs onto faces of a ProBuilder mesh.

**Signature:** `ProBuilderProjectUV(string name = null, int instanceId = 0, string path = null, string faceIndexes = null, int channel = 0)`

**Returns:** `{ success, name, instanceId, projectedFaceCount, channel, method }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to project all faces.
- `channel`: UV channel `0`–`3` (default `0`). Channel `1` is the lightmap UV.
- Only box projection is supported; other UV projection modes are not available.
- Uses reflection to access `UVEditing.ProjectFacesBox` (internal in ProBuilder 5.x).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.probuilder` package.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string faceIndexes = null;
        int channel = 0;

        var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var faces = SelectFaces(pbMesh, faceIndexes);
        if (faces.Count == 0)
        { result.SetResult(new { error = "No faces selected. Provide faceIndexes or omit to project all." }); return; }

        if (channel < 0 || channel > 3)
        { result.SetResult(new { error = "UV channel must be 0-3 (0=primary, 1=lightmap)" }); return; }

        Undo.RecordObject(pbMesh, "Project UV");
        WorkflowManager.SnapshotObject(pbMesh);

        if (!InvokeProjectFacesBox(pbMesh, faces.ToArray(), channel))
        { result.SetResult(new { error = "Failed to project UVs. UVEditing.ProjectFacesBox is not accessible in this ProBuilder version." }); return; }

        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            projectedFaceCount = faces.Count,
            channel,
            method = "Box"
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

    private static List<Face> SelectFaces(ProBuilderMesh mesh, string faceIndexes)
    {
        var allFaces = mesh.faces;
        if (string.IsNullOrEmpty(faceIndexes))
            return allFaces.ToList();

        var indices = ParseIntList(faceIndexes);
        if (indices == null) return new List<Face>();

        return indices
            .Where(i => i >= 0 && i < allFaces.Count)
            .Select(i => allFaces[i])
            .ToList();
    }

    private static List<int> ParseIntList(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return null;
        var list = new List<int>();
        foreach (var part in csv.Split(','))
        {
            if (int.TryParse(part.Trim(), out var val))
                list.Add(val);
        }
        return list.Count > 0 ? list : null;
    }

    // Field fully-qualified and GetMethod args kept to the simple (name, types) overload —
    // passing BindingFlags to GetMethod triggers the Unity_RunCommand reformatter NRE.
    private static System.Reflection.MethodInfo _projectFacesBoxMethod;

    private static bool InvokeProjectFacesBox(ProBuilderMesh mesh, Face[] faces, int channel)
    {
        if (_projectFacesBoxMethod == null)
        {
            var uvType = typeof(ProBuilderMesh).Assembly.GetType("UnityEngine.ProBuilder.MeshOperations.UVEditing");
            if (uvType == null) return false;
            foreach (var mi in uvType.GetMethods())
            {
                if (mi.Name != "ProjectFacesBox" || !mi.IsStatic) continue;
                var ps = mi.GetParameters();
                if (ps.Length != 3) continue;
                if (ps[0].ParameterType != typeof(ProBuilderMesh)) continue;
                if (ps[1].ParameterType != typeof(Face[])) continue;
                if (ps[2].ParameterType != typeof(int)) continue;
                _projectFacesBoxMethod = mi;
                break;
            }
        }
        if (_projectFacesBoxMethod == null) return false;
        _projectFacesBoxMethod.Invoke(null, new object[] { mesh, faces, channel });
        return true;
    }
}
```
