# event_clear_listeners

Remove all persistent listeners from a UnityEvent. Iterates in reverse order to avoid index shifting. Returns the count of listeners that were removed.

**Signature:** `EventClearListeners(string name = null, int instanceId = 0, string path = null, string componentName = null, string eventName = null)`

**Returns:** `{ success, removed }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;

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

        WorkflowManager.SnapshotObject(comp);
        Undo.RecordObject(comp, "Clear Listeners");
        int count = evt.GetPersistentEventCount();
        for (int i = count - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(evt, i);

        result.SetResult(new { success = true, removed = count });
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
