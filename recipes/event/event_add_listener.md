# event_add_listener

Add a persistent listener to a UnityEvent at editor time. Supports void, int, float, string, and bool argument types. Handles property setters via `set_PropertyName` method name convention. Only works with standard `UnityEvent` (not generic `UnityEvent<T>`).

**Signature:** `EventAddListener(string name = null, int instanceId = 0, string path = null, string componentName = null, string eventName = null, string targetObjectName = null, string targetComponentName = null, string methodName = null, string mode = "RuntimeOnly", string argType = "void", float floatArg = 0, int intArg = 0, string stringArg = null, bool boolArg = false)`

**Returns:** `{ success, message, index }`

**Notes:**
- `argType`: `"void"`, `"int"`, `"float"`, `"string"`, or `"bool"` (generic `UnityEvent<T>` not supported)
- `mode`: `"RuntimeOnly"` (default), `"EditorAndRuntime"`, or `"Off"`
- `targetComponentName`: use `"GameObject"` to target a GameObject method directly

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

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
        string name = "MyButton";               // Source GameObject
        int instanceId = 0;
        string path = null;
        string componentName = "Button";
        string eventName = "onClick";
        string targetObjectName = "GameManager"; // Target GameObject
        string targetComponentName = "GameManager";
        string methodName = "OnButtonClicked";
        string mode = "RuntimeOnly";            // "RuntimeOnly", "EditorAndRuntime", or "Off"
        string argType = "void";                // "void", "int", "float", "string", or "bool"
        float floatArg = 0;
        int intArg = 0;
        string stringArg = null;
        bool boolArg = false;

        var (go, goErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (goErr != null) { result.SetResult(goErr); return; }

        var component = go.GetComponent(componentName);
        if (component == null) { result.SetResult(new { error = $"Source Component not found: {componentName}" }); return; }

        var (targetGo, tgtErr) = GameObjectFinder.FindOrError(name: targetObjectName);
        if (tgtErr != null) { result.SetResult(tgtErr); return; }

        UnityEngine.Object targetObj = null;
        System.Type targetType = null;
        if (targetComponentName == "GameObject" || targetComponentName == "UnityEngine.GameObject")
        {
            targetObj = targetGo;
            targetType = typeof(GameObject);
        }
        else
        {
            var targetComponent = targetGo.GetComponent(targetComponentName);
            if (targetComponent == null) { result.SetResult(new { error = $"Target Component not found: {targetComponentName}" }); return; }
            targetObj = targetComponent;
            targetType = targetComponent.GetType();
        }

        var type = component.GetType();
        var field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var property = type.GetProperty(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        object rawEvent = null;
        if (field != null) rawEvent = field.GetValue(component);
        else if (property != null) rawEvent = property.GetValue(component);

        if (rawEvent == null) { result.SetResult(new { error = $"UnityEvent '{eventName}' not found on {componentName}" }); return; }

        var unityEvent = rawEvent as UnityEvent;
        if (unityEvent == null) { result.SetResult(new { error = $"Field '{eventName}' is not a standard UnityEvent. Generic events (UnityEvent<T>) not yet supported in this version." }); return; }

        WorkflowManager.SnapshotObject(component);
        Undo.RecordObject(component, "Add Event Listener");

        MethodInfo FindMethodOnTarget(System.Type[] paramTypes)
        {
            var mi = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, null, paramTypes, null);
            if (mi != null) return mi;
            if (methodName.StartsWith("set_"))
            {
                var prop2 = targetType.GetProperty(methodName.Substring(4), BindingFlags.Instance | BindingFlags.Public);
                if (prop2 != null && prop2.CanWrite)
                {
                    var setter = prop2.GetSetMethod();
                    if (setter != null && paramTypes.Length == 1 && setter.GetParameters()[0].ParameterType == paramTypes[0])
                        return setter;
                }
            }
            return null;
        }

        MethodInfo methodInfo = null;
        switch (argType.ToLower())
        {
            case "void":
                methodInfo = FindMethodOnTarget(System.Type.EmptyTypes);
                if (methodInfo == null) { result.SetResult(new { error = $"Method '{methodName}()' not found on {targetComponentName}" }); return; }
                var voidDelegate = System.Delegate.CreateDelegate(typeof(UnityAction), targetObj, methodInfo) as UnityAction;
                UnityEventTools.AddPersistentListener(unityEvent, voidDelegate);
                break;
            case "float":
                methodInfo = FindMethodOnTarget(new[] { typeof(float) });
                if (methodInfo == null) { result.SetResult(new { error = $"Method '{methodName}(float)' not found" }); return; }
                var floatDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<float>), targetObj, methodInfo) as UnityAction<float>;
                UnityEventTools.AddFloatPersistentListener(unityEvent, floatDelegate, floatArg);
                break;
            case "int":
                methodInfo = FindMethodOnTarget(new[] { typeof(int) });
                if (methodInfo == null) { result.SetResult(new { error = $"Method '{methodName}(int)' not found" }); return; }
                var intDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<int>), targetObj, methodInfo) as UnityAction<int>;
                UnityEventTools.AddIntPersistentListener(unityEvent, intDelegate, intArg);
                break;
            case "string":
                methodInfo = FindMethodOnTarget(new[] { typeof(string) });
                if (methodInfo == null) { result.SetResult(new { error = $"Method '{methodName}(string)' not found" }); return; }
                var stringDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<string>), targetObj, methodInfo) as UnityAction<string>;
                UnityEventTools.AddStringPersistentListener(unityEvent, stringDelegate, stringArg);
                break;
            case "bool":
                methodInfo = FindMethodOnTarget(new[] { typeof(bool) });
                if (methodInfo == null) { result.SetResult(new { error = $"Method '{methodName}(bool)' not found" }); return; }
                var boolDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<bool>), targetObj, methodInfo) as UnityAction<bool>;
                UnityEventTools.AddBoolPersistentListener(unityEvent, boolDelegate, boolArg);
                break;
            default:
                result.SetResult(new { error = $"Unsupported argType: {argType}" }); return;
        }

        int index = unityEvent.GetPersistentEventCount() - 1;
        UnityEventCallState callState = UnityEventCallState.RuntimeOnly;
        if (mode.ToLower() == "editorandruntime") callState = UnityEventCallState.EditorAndRuntime;
        else if (mode.ToLower() == "off") callState = UnityEventCallState.Off;
        unityEvent.SetPersistentListenerState(index, callState);

        result.SetResult(new
        {
            success = true,
            message = $"Added listener {targetComponentName}.{methodName} to {componentName}.{eventName}",
            index
        });
    }
}
```
