# event_list_events

List all UnityEvent fields declared on a component (both public and non-public). Uses reflection to find fields whose type is assignable from `UnityEventBase`. Returns the field name, type name, and current persistent listener count for each event.

**Signature:** `EventListEvents(string name = null, int instanceId = 0, string path = null, string componentName = null)`

**Returns:** `{ success, component, count, events: [{ name, type, listenerCount }] }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System.Linq;
using System.Reflection;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyButton";       // GameObject name (or use instanceId / path)
        int instanceId = 0;
        string path = null;
        string componentName = "Button";

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var component = go.GetComponent(componentName);
        if (component == null) { result.SetResult(new { error = $"Component not found: {componentName}" }); return; }

        var type = component.GetType();
        var events = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => typeof(UnityEventBase).IsAssignableFrom(f.FieldType))
            .Select(f =>
            {
                var e = f.GetValue(component) as UnityEventBase;
                return new { name = f.Name, type = f.FieldType.Name, listenerCount = e?.GetPersistentEventCount() ?? 0 };
            })
            .ToArray();

        result.SetResult(new { success = true, component = componentName, count = events.Length, events });
    }
}
```
