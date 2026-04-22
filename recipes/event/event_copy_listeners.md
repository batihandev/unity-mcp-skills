# event_copy_listeners

Copy all persistent listeners from one UnityEvent to another. Only void-argument listeners (no-parameter methods) are copied; listeners requiring typed arguments are skipped because they cannot be safely reconstructed via the persistent API without type information. The target event must be a standard `UnityEvent` (not `UnityEvent<T>`).

**Signature:** `EventCopyListeners(string sourceObject, string sourceComponent, string sourceEvent, string targetObject, string targetComponent, string targetEvent)`

**Returns:** `{ success, copied }`

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
        string sourceObject = "SourceButton";       // Source GameObject name
        string sourceComponent = "Button";          // Source component name
        string sourceEvent = "onClick";             // Source event field name
        string targetObject = "TargetButton";       // Target GameObject name
        string targetComponent = "Button";          // Target component name
        string targetEvent = "onClick";             // Target event field name

        var (srcEvt, srcComp, srcErr) = FindEvent(sourceObject, 0, null, sourceComponent, sourceEvent);
        if (srcErr != null) { result.SetResult(srcErr); return; }

        var (tgtEvt, tgtComp, tgtErr) = FindEvent(targetObject, 0, null, targetComponent, targetEvent);
        if (tgtErr != null) { result.SetResult(tgtErr); return; }

        if (!(tgtEvt is UnityEvent tgtUnityEvent))
        {
            result.SetResult(new { error = "Target must be a standard UnityEvent" });
            return;
        }

        WorkflowManager.SnapshotObject(tgtComp);
        Undo.RecordObject(tgtComp, "Copy Listeners");

        int copied = 0;
        for (int i = 0; i < srcEvt.GetPersistentEventCount(); i++)
        {
            var target = srcEvt.GetPersistentTarget(i);
            var method = srcEvt.GetPersistentMethodName(i);
            if (target == null) continue;
            var mi = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public,
                null, System.Type.EmptyTypes, null);
            if (mi != null)
            {
                var del = System.Delegate.CreateDelegate(typeof(UnityAction), target, mi) as UnityAction;
                UnityEventTools.AddPersistentListener(tgtUnityEvent, del);
                copied++;
            }
        }

        result.SetResult(new { success = true, copied });
    }

    private (UnityEventBase evt, Component comp, object error) FindEvent(
        string name, int instanceId, string path, string componentName, string eventName)
    {
        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) return (null, null, findErr);
        var component = go.GetComponent(componentName);
        if (component == null) return (null, null, new { error = $"Component not found: {componentName}" });
        var type = component.GetType();
        var field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var property = type.GetProperty(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        UnityEventBase evt = null;
        if (field != null) evt = field.GetValue(component) as UnityEventBase;
        else if (property != null) evt = property.GetValue(component) as UnityEventBase;
        if (evt == null) return (null, null, new { error = $"UnityEvent '{eventName}' not found" });
        return (evt, component, null);
    }
}
```
