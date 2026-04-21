# event_remove_listener

Remove a persistent listener from a UnityEvent by its zero-based index. The index is validated against the current listener count before removal.

**Signature:** `EventRemoveListener(string name = null, int instanceId = 0, string path = null, string componentName = null, string eventName = null, int index = 0)`

**Returns:** `{ success, remainingCount }`

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using System.Reflection;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyButton";       // GameObject name (or use instanceId / path)
        int instanceId = 0;
        string path = null;
        string componentName = "Button";
        string eventName = "onClick";
        int index = 0;                  // Zero-based listener index to remove

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var component = go.GetComponent(componentName);
        if (component == null) { result.SetResult(new { error = $"Component not found: {componentName}" }); return; }

        var type = component.GetType();
        var field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var property = type.GetProperty(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        UnityEventBase unityEvent = null;
        if (field != null) unityEvent = field.GetValue(component) as UnityEventBase;
        else if (property != null) unityEvent = property.GetValue(component) as UnityEventBase;

        if (unityEvent == null) { result.SetResult(new { error = "UnityEvent not found" }); return; }

        if (Validate.InRange(index, 0, unityEvent.GetPersistentEventCount() - 1, "index") is object rangeErr) { result.SetResult(rangeErr); return; }

        WorkflowManager.SnapshotObject(component);
        Undo.RecordObject(component, "Remove Event Listener");
        UnityEventTools.RemovePersistentListener(unityEvent, index);

        result.SetResult(new { success = true, remainingCount = unityEvent.GetPersistentEventCount() });
    }
}
```
