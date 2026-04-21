# event_set_listener_state

Set the call state of a specific persistent listener on a UnityEvent. Valid states are `"Off"`, `"RuntimeOnly"`, and `"EditorAndRuntime"`. The state string is parsed case-insensitively.

**Signature:** `EventSetListenerState(string name = null, int instanceId = 0, string path = null, string componentName = null, string eventName = null, int index = 0, string state = null)`

**Returns:** `{ success, index, state }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyButton";           // GameObject name (or use instanceId / path)
        int instanceId = 0;
        string path = null;
        string componentName = "Button";
        string eventName = "onClick";
        int index = 0;                      // Zero-based listener index
        string state = "EditorAndRuntime";  // "Off", "RuntimeOnly", or "EditorAndRuntime"

        var (evt, comp, err) = FindEvent(name, instanceId, path, componentName, eventName);
        if (err != null) { result.SetResult(err); return; }

        if (index < 0 || index >= evt.GetPersistentEventCount())
        {
            result.SetResult(new { error = "Index out of range" });
            return;
        }

        if (!System.Enum.TryParse<UnityEventCallState>(state, true, out var callState))
        {
            result.SetResult(new { error = $"Invalid state: {state}" });
            return;
        }

        WorkflowManager.SnapshotObject(comp);
        Undo.RecordObject(comp, "Set Listener State");
        evt.SetPersistentListenerState(index, callState);

        result.SetResult(new { success = true, index, state = callState.ToString() });
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
