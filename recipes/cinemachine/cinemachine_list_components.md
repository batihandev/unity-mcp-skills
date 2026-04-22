# cinemachine_list_components

List all available Cinemachine component type names from the installed Cinemachine assembly.

**Signature:** `CinemachineListComponents()`

**Returns:** `{ success, count, components[] }`

**Notes:**
- Returns only public, non-abstract types whose names start with `"Cinemachine"`.
- Read-only — does not modify anything.
- Useful for discovering valid values for `componentType` in `cinemachine_set_component`, `cinemachine_add_component`, and `cinemachine_add_extension`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var cmAssembly = typeof(CinemachineCamera).Assembly;
        System.Type[] assemblyTypes;
        // Fully-qualify the catch type — short-name `ReflectionTypeLoadException` imported
        // via `using System.Reflection;` trips the Unity_RunCommand reformatter NRE.
        try { assemblyTypes = cmAssembly.GetTypes(); }
        catch (System.Reflection.ReflectionTypeLoadException ex) { assemblyTypes = ex.Types.Where(t => t != null).ToArray(); }

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
