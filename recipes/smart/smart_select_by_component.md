# smart_select_by_component

Select all GameObjects in the scene that have a specific component.

**Signature:** `SmartSelectByComponent(string componentName = null, string componentType = null)`

**Returns:** `{ success, selected, component }`

**Notes:**
- `componentName` and `componentType` are aliases; provide one
- After execution the selected objects appear in the Hierarchy
- Works with any component type name (e.g. `Light`, `Camera`, `MeshRenderer`, custom scripts)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string componentName = "Light"; // Component type name to search for

        var type = GetTypeByName(componentName);
        if (type == null)
        {
            result.SetResult(new { error = $"Component type '{componentName}' not found" });
            return;
        }

        var components = Object.FindObjectsOfType(type);
        var gameObjects = components.OfType<Component>().Select(c => c.gameObject).Distinct().ToArray();
        Selection.objects = gameObjects;

        result.SetResult(new { success = true, selected = gameObjects.Length, component = componentName });
    }

    private static System.Type GetTypeByName(string name) =>
        System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == name);
}
```
