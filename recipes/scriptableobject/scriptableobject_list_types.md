# scriptableobject_list_types

List available ScriptableObject types in the project.

**Signature:** `ScriptableObjectListTypes(string filter = null, int limit = 50)`

**Returns:** `{ count, types }`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `filter` | string | No | null | Filter by name substring |
| `limit` | int | No | 50 | Maximum number of types to return |

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filter = null; // Optional name filter (e.g. "Item", "Config")
        int limit = 50; // Maximum results

        var types = SkillsCommon.GetAllLoadedTypes()
            .Where(t => t.IsSubclassOf(typeof(ScriptableObject)) && !t.IsAbstract)
            .Where(t => string.IsNullOrEmpty(filter) || t.Name.Contains(filter))
            .Take(limit)
            .Select(t => new { name = t.Name, fullName = t.FullName })
            .ToArray();

        result.SetResult(new { count = types.Length, types });
    }
}
```
