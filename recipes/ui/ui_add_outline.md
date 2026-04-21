# ui_add_outline

Add a Shadow or Outline visual effect component to a UI element.

**Signature:** `UIAddOutline(name string = null, instanceId int = 0, path string = null, effectType string = "Outline", r float = 0, g float = 0, b float = 0, a float = 0.5, distanceX float = 1, distanceY float = -1, useGraphicAlpha bool = true)`

**Returns:** `{ success, name, effectType, effectColor, effectDistance }`

**Notes:**
- `effectType` values: `Outline` or `Shadow`.
- Each call adds a new component instance; multiple calls will stack effects.
- `distanceY` defaults to `-1` (downward for shadow convention).

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string effectType = "Outline";
        float r = 0f, g = 0f, b = 0f, a = 0.5f;
        float distanceX = 1f, distanceY = -1f;
        bool useGraphicAlpha = true;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(go, "Add Effect");

        var effectColor = new Color(r, g, b, a);
        var effectDistance = new Vector2(distanceX, distanceY);

        string applied;
        if (effectType.Equals("Shadow", StringComparison.OrdinalIgnoreCase))
        {
            var shadow = Undo.AddComponent<Shadow>(go);
            shadow.effectColor = effectColor;
            shadow.effectDistance = effectDistance;
            shadow.useGraphicAlpha = useGraphicAlpha;
            applied = "Shadow";
        }
        else
        {
            var outline = Undo.AddComponent<Outline>(go);
            outline.effectColor = effectColor;
            outline.effectDistance = effectDistance;
            outline.useGraphicAlpha = useGraphicAlpha;
            applied = "Outline";
        }

        result.SetResult(new { success = true, name = go.name, effectType = applied, effectColor = $"({r},{g},{b},{a})", effectDistance = $"({distanceX},{distanceY})" });
    }
}
```
