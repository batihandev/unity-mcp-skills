# event_get_listener_count

Get the number of persistent listeners currently registered on a UnityEvent. Useful as a quick check before indexing into listeners or deciding whether to clear.

**Signature:** `EventGetListenerCount(string name = null, int instanceId = 0, string path = null, string componentName = null, string eventName = null)`

**Returns:** `{ success, count }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyButton";       // GameObject name (or use instanceId / path)
        int instanceId = 0;
        string path = null;
        string componentName = "Button";
        string eventName = "onClick";

        var (evt, comp, err) = FindEvent(name, instanceId, path, componentName, eventName);
        if (err != null) { result.SetResult(err); return; }

        result.SetResult(new { success = true, count = evt.GetPersistentEventCount() });
    }

    private (UnityEventBase evt, Component comp, object error) FindEvent(
        string name, int instanceId, string path, string componentName, string eventName)
    {
        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) return (null, null, findErr);
        var component = go.GetComponent(componentName);
        if (component == null) return (null, null, new { error = $"Component not found: {componentName}" });
        var type = component.GetType();
        var field = type.GetField(eventName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var property = type.GetProperty(eventName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        UnityEventBase evt = null;
        if (field != null) evt = field.GetValue(component) as UnityEventBase;
        else if (property != null) evt = property.GetValue(component) as UnityEventBase;
        if (evt == null) return (null, null, new { error = $"UnityEvent '{eventName}' not found" });
        return (evt, component, null);
    }
}
```
