# event_invoke

Invoke a UnityEvent explicitly via reflection. This fires all persistent and runtime listeners immediately. Intended for runtime use; calling from the editor outside Play Mode will only fire persistent listeners that have `EditorAndRuntime` call state.

**Signature:** `EventInvoke(string name = null, int instanceId = 0, string path = null, string componentName = null, string eventName = null)`

**Returns:** `{ success, message }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

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

        var (go, goErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (goErr != null) { result.SetResult(goErr); return; }

        var component = go.GetComponent(componentName);
        if (component == null) { result.SetResult(new { error = $"Component not found: {componentName}" }); return; }

        var type = component.GetType();
        System.Reflection.FieldInfo field = null;
        foreach (var _f in type.GetFields()) if (_f.Name == eventName) { field = _f; break; }
        System.Reflection.PropertyInfo property = null;
        foreach (var _p in type.GetProperties()) if (_p.Name == eventName) { property = _p; break; }

        UnityEventBase unityEvent = null;
        if (field != null) unityEvent = field.GetValue(component) as UnityEventBase;
        else if (property != null) unityEvent = property.GetValue(component) as UnityEventBase;

        if (unityEvent == null) { result.SetResult(new { error = "UnityEvent not found" }); return; }

        // No-arg GetMethods + filter — BindingFlags-arg overloads trip the reformatter NRE.
        System.Reflection.MethodInfo invokeMethod = null;
        foreach (var _m in unityEvent.GetType().GetMethods())
            if (_m.Name == "Invoke" && !_m.IsStatic && _m.GetParameters().Length == 0) { invokeMethod = _m; break; }
        if (invokeMethod == null) { result.SetResult(new { error = "Could not find Invoke method on event" }); return; }

        try
        {
            invokeMethod.Invoke(unityEvent, null);
        }
        catch (System.Reflection.TargetInvocationException ex)
        {
            result.SetResult(new { error = $"Event invoke failed: {(ex.InnerException ?? ex).Message}" });
            return;
        }

        result.SetResult(new { success = true, message = "Event invoked" });
    }
}
```
