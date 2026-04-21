# material_set_colors_batch

Set colors on multiple GameObjects or material assets in a single call (efficient batch operation).

**Signature:** `MaterialSetColorsBatch(string items = null, string propertyName = null)`

**`items`:** JSON array of `{ name?, instanceId?, path?, r?, g?, b?, a? }` objects. Color defaults are 1.0 for all channels.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target }] }`

## Notes

- `propertyName` applies to all items; auto-detected from the active render pipeline if omitted.
- Per-item fallback: tries `propertyName → _BaseColor → _Color` in order.
- Color channels are in the **0–1** range (not 0–255).
- Prefer this over calling `material_set_color` repeatedly when setting colors on 2+ objects.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string propertyName = null; // null → auto-detect per render pipeline
        string items = @"[
            { ""name"": ""Cube"",   ""r"": 1.0, ""g"": 0.0, ""b"": 0.0, ""a"": 1.0 },
            { ""name"": ""Sphere"", ""r"": 0.0, ""g"": 1.0, ""b"": 0.0, ""a"": 1.0 },
            { ""name"": ""Plane"",  ""r"": 0.0, ""g"": 0.0, ""b"": 1.0, ""a"": 1.0 }
        ]";

        /* Original Logic:

            if (string.IsNullOrEmpty(propertyName))
                propertyName = ProjectSkills.GetColorPropertyName();

            return BatchExecutor.Execute<BatchColorItem>(items, item =>
            {
                var (material, go, error) = FindMaterial(item.name, item.instanceId, item.path);
                if (error != null) throw new System.Exception("Material not found");

                var color = new Color(item.r, item.g, item.b, item.a);
                WorkflowManager.SnapshotObject(material);
                Undo.RecordObject(material, "Batch Set Color");

                bool colorSet = false;
                var propertiesToTry = new[] { propertyName, "_BaseColor", "_Color" };
                foreach (var prop in propertiesToTry)
                {
                    if (material.HasProperty(prop))
                    {
                        material.SetColor(prop, color);
                        colorSet = true;
                        break;
                    }
                }

                if (!colorSet) throw new System.Exception("No color property found");
                if (go == null) EditorUtility.SetDirty(material);
                return new { target = go?.name ?? item.path, success = true };
            }, item => item.name ?? item.path);
        */
    }
}
```
