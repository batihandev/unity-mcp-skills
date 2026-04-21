# optimize_set_lod_group

Add or reconfigure an `LODGroup` component on a GameObject. LOD levels are assigned based on comma-separated screen-relative transition heights. The root LOD0 receives all child renderers; remaining levels use empty renderer arrays. The operation is recorded in the Undo stack.

**Signature:** `OptimizeSetLodGroup(string name = null, int instanceId = 0, string path = null, string lodDistances = "0.6,0.3,0.1")`

**Returns:** `{ success, gameObject, lodLevels, distances }`

- `lodDistances` — screen-relative heights in descending order, e.g. `"0.6,0.3,0.1"`. A culled LOD at `0` is appended automatically.
- `lodLevels` — total LOD count including the zero-height culled level.

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Provide at least one of: name, instanceId, or path
        string name = "Tree_01";
        int instanceId = 0;
        string path = null;

        string lodDistances = "0.6,0.3,0.1"; // Screen-relative transition heights

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var distanceParts = lodDistances.Split(',');
        var distances = new List<float>();
        foreach (var part in distanceParts)
        {
            if (!float.TryParse(part.Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float dist))
            {
                result.SetResult(new { error = $"Invalid LOD distance value: '{part.Trim()}'" });
                return;
            }
            distances.Add(dist);
        }

        var lodGroup = go.GetComponent<LODGroup>();
        if (lodGroup == null)
            lodGroup = Undo.AddComponent<LODGroup>(go);
        else
            Undo.RecordObject(lodGroup, "Set LOD Group");

        var renderers = go.GetComponentsInChildren<Renderer>();
        var lods = new LOD[distances.Count + 1];
        for (int i = 0; i < distances.Count; i++)
            lods[i] = new LOD(distances[i], i == 0 ? renderers : new Renderer[0]);
        lods[distances.Count] = new LOD(0, new Renderer[0]); // Culled level

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();

        result.SetResult(new
        {
            success = true,
            gameObject = go.name,
            lodLevels = lods.Length,
            distances = lodDistances
        });
    }
}
```
