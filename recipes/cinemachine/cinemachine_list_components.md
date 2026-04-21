# cinemachine_list_components

List all available Cinemachine component type names from the installed Cinemachine assembly.

**Signature:** `CinemachineListComponents()`

**Returns:** `{ success, count, components[] }`

**Notes:**
- Returns only public, non-abstract types whose names start with `"Cinemachine"`.
- Read-only — does not modify anything.
- Useful for discovering valid values for `componentType` in `cinemachine_set_component`, `cinemachine_add_component`, and `cinemachine_add_extension`.

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var cmAssembly = CinemachineAdapter.CmAssembly;
        System.Type[] assemblyTypes;
        try { assemblyTypes = cmAssembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { assemblyTypes = ex.Types.Where(t => t != null).ToArray(); }

        var componentTypes = assemblyTypes
            .Where(t => t.IsSubclassOf(typeof(MonoBehaviour)) && !t.IsAbstract && t.IsPublic)
            .Select(t => t.Name)
            .Where(n => n.StartsWith("Cinemachine"))
            .OrderBy(n => n)
            .ToList();

        result.SetResult(new { success = true, count = componentTypes.Count, components = componentTypes });
    }
}
```
