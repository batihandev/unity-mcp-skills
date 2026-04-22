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
        // BindingFlags-overload of GetFields trips reformatter NRE — walk inheritance chain
        // manually over no-arg GetFields() to cover inherited + NonPublic fields.
        var seen = new System.Collections.Generic.HashSet<string>();
        var events = new System.Collections.Generic.List<object>();
        for (var scan = type; scan != null; scan = scan.BaseType)
        {
            foreach (var f in scan.GetFields())
            {
                if (!seen.Add(f.Name)) continue;
                if (!typeof(UnityEventBase).IsAssignableFrom(f.FieldType)) continue;
                var e = f.GetValue(component) as UnityEventBase;
                events.Add(new { name = f.Name, type = f.FieldType.Name, listenerCount = e?.GetPersistentEventCount() ?? 0 });
            }
        }

        result.SetResult(new { success = true, component = componentName, count = events.Count, events });
    }
}
```
