# event_get_listeners

Get the persistent listeners of a UnityEvent on a component field or property. Returns index, target name, target type, method name, and call state for each listener.

**Signature:** `EventGetListeners(string name = null, int instanceId = 0, string path = null, string componentName = null, string eventName = null)`

**Returns:** `{ success, gameObject, component, eventName, listenerCount, listeners: [{ index, target, targetType, method, state }] }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyButton";           // GameObject name (or use instanceId / path)
        int instanceId = 0;
        string path = null;
        string componentName = "Button";
        string eventName = "onClick";

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var component = go.GetComponent(componentName);
        if (component == null) { result.SetResult(new { error = $"Component not found: {componentName} on {go.name}" }); return; }

        var type = component.GetType();
        var field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var property = type.GetProperty(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        UnityEventBase unityEvent = null;
        if (field != null) unityEvent = field.GetValue(component) as UnityEventBase;
        else if (property != null) unityEvent = property.GetValue(component) as UnityEventBase;

        if (unityEvent == null) { result.SetResult(new { error = $"UnityEvent field/property '{eventName}' not found or null on {componentName}" }); return; }

        int count = unityEvent.GetPersistentEventCount();
        var listeners = new List<object>();
        for (int i = 0; i < count; i++)
        {
            var target = unityEvent.GetPersistentTarget(i);
            var methodName = unityEvent.GetPersistentMethodName(i);
            var state = unityEvent.GetPersistentListenerState(i);
            listeners.Add(new
            {
                index = i,
                target = target != null ? target.name : "null",
                targetType = target != null ? target.GetType().Name : "null",
                method = methodName,
                state = state.ToString()
            });
        }

        result.SetResult(new
        {
            success = true,
            gameObject = go.name,
            component = componentName,
            eventName = eventName,
            listenerCount = count,
            listeners
        });
    }
}
```
