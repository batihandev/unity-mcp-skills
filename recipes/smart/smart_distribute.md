# smart_distribute

Evenly distribute selected objects between the first and last positions along an axis. Requires at least 3 objects selected in the Hierarchy.

**Signature:** `SmartDistribute(string axis = "X")`

**Returns:** `{ success, distributed, axis }`

**Notes:**
- Objects are ordered by their sibling index
- First and last objects stay in place; intermediate objects are evenly spaced
- `axis`: X, Y, Z, -X, -Y, -Z
- Requires 3+ objects selected

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string axis = "X";

        var selected = Selection.gameObjects.OrderBy(g => g.transform.GetSiblingIndex()).ToList();
        if (selected.Count < 3)
        {
            result.SetResult(new { error = "Need at least 3 selected objects" });
            return;
        }

        Vector3 axisVec = ParseAxis(axis);
        Undo.RecordObjects(selected.Select(g => g.transform).ToArray(), "Smart Distribute");

        float startVal = Vector3.Dot(selected[0].transform.position, axisVec);
        float endVal = Vector3.Dot(selected[selected.Count - 1].transform.position, axisVec);

        for (int i = 1; i < selected.Count - 1; i++)
        {
            float t = i / (float)(selected.Count - 1);
            float targetVal = Mathf.Lerp(startVal, endVal, t);
            float currentVal = Vector3.Dot(selected[i].transform.position, axisVec);
            selected[i].transform.position += axisVec * (targetVal - currentVal);
        }

        result.SetResult(new { success = true, distributed = selected.Count, axis });
    }

    private static Vector3 ParseAxis(string axis) =>
        axis?.ToUpper() switch
        {
            "X" => Vector3.right,
            "-X" => Vector3.left,
            "Y" => Vector3.up,
            "-Y" => Vector3.down,
            "Z" => Vector3.forward,
            "-Z" => Vector3.back,
            _ => Vector3.right
        };
}
```
