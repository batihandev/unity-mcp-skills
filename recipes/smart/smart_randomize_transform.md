# smart_randomize_transform

Randomize position, rotation, and/or scale of selected objects within specified ranges.

**Signature:** `SmartRandomizeTransform(float posRange = 0f, float rotRange = 0f, float scaleMin = 1f, float scaleMax = 1f)`

**Returns:** `{ success, randomized }`

**Notes:**
- `posRange = 0` and `rotRange = 0` skip those randomizations
- `scaleMin == scaleMax == 1` skips scale randomization
- Scale is applied uniformly (same value on X/Y/Z)
- Requires objects selected in Hierarchy first
- Undoable

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float posRange = 0.5f;   // offset radius in world units; 0 = skip
        float rotRange = 15f;    // angle range in degrees; 0 = skip
        float scaleMin = 0.8f;   // uniform scale min; 1 = skip
        float scaleMax = 1.2f;   // uniform scale max; 1 = skip

        var selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            result.SetResult(new { error = "No objects selected" });
            return;
        }

        foreach (var go in selected)
        {
            Undo.RecordObject(go.transform, "Randomize Transform");

            if (posRange > 0)
                go.transform.position += new Vector3(
                    Random.Range(-posRange, posRange),
                    Random.Range(-posRange, posRange),
                    Random.Range(-posRange, posRange));

            if (rotRange > 0)
                go.transform.eulerAngles += new Vector3(
                    Random.Range(-rotRange, rotRange),
                    Random.Range(-rotRange, rotRange),
                    Random.Range(-rotRange, rotRange));

            if (scaleMin != 1f || scaleMax != 1f)
            {
                float s = Random.Range(scaleMin, scaleMax);
                go.transform.localScale = new Vector3(s, s, s);
            }
        }

        result.SetResult(new { success = true, randomized = selected.Length });
    }
}
```
