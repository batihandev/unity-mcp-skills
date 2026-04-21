# light_add_probe_group

Add a Light Probe Group component to a GameObject. Optionally populates a 3-D grid of probe positions.

**Signature:** `LightAddProbeGroup(string name = null, int instanceId = 0, string path = null, int gridX = 0, int gridY = 0, int gridZ = 0, float spacingX = 2f, float spacingY = 1.5f, float spacingZ = 2f)`

**Returns:** `{ success, gameObject, probeCount, existed, hasGrid }`

**Notes:**
- If the component already exists on the object, it is reused (`existed = true`).
- Grid is only generated when all three of `gridX`, `gridY`, `gridZ` are > 0.
- Probes are centered on the object (offset applied on X and Z axes).

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Room Floor";
        int instanceId = 0;
        string path = null;
        int gridX = 4;
        int gridY = 2;
        int gridZ = 4;
        float spacingX = 2f;
        float spacingY = 1.5f;
        float spacingZ = 2f;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var lpg = go.GetComponent<LightProbeGroup>();
        bool existed = lpg != null;
        if (!existed)
            lpg = Undo.AddComponent<LightProbeGroup>(go);

        if (gridX > 0 && gridY > 0 && gridZ > 0)
        {
            Undo.RecordObject(lpg, "Set Light Probe Positions");
            var probes = new Vector3[gridX * gridY * gridZ];
            int idx = 0;
            float offsetX = (gridX - 1) * spacingX * 0.5f;
            float offsetZ = (gridZ - 1) * spacingZ * 0.5f;
            for (int iy = 0; iy < gridY; iy++)
                for (int ix = 0; ix < gridX; ix++)
                    for (int iz = 0; iz < gridZ; iz++)
                        probes[idx++] = new Vector3(ix * spacingX - offsetX, iy * spacingY, iz * spacingZ - offsetZ);
            lpg.probePositions = probes;
            EditorUtility.SetDirty(lpg);
        }

        result.SetResult(new
        {
            success = true,
            gameObject = go.name,
            probeCount = lpg.probePositions.Length,
            existed,
            hasGrid = gridX > 0 && gridY > 0 && gridZ > 0
        });
    }
}
```
