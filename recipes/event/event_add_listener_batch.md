# event_add_listener_batch

Add multiple persistent listeners to a UnityEvent in a single call. Each item in the JSON array specifies a target object, component, and method. Internally calls `event_add_listener` for each item with default `argType = "void"` and `mode = "RuntimeOnly"`. Returns the count of successfully added listeners.

**Signature:** `EventAddListenerBatch(string name = null, int instanceId = 0, string path = null, string componentName = null, string eventName = null, string items = null)`

**Returns:** `{ success, added, total }`

**`items` format:** JSON array of objects with fields `targetObjectName`, `targetComponentName`, `methodName`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Notes

Handles the `argType = "void"` / `mode = "RuntimeOnly"` case only. For typed-arg events (`int`, `float`, `string`, `bool`), use the single-item `event_add_listener` recipe per call.

## C# Template

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using System;
using System.Linq;
using System.Collections.Generic;

internal sealed class _BatchListenerItem
{
    public string targetObjectName;
    public string targetComponentName;
    public string methodName;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyButton";
        int instanceId = 0;
        string path = null;
        string componentName = "Button";
        string eventName = "onClick";

        var items = new[]
        {
            new _BatchListenerItem { targetObjectName = "GameManager", targetComponentName = "GameManager", methodName = "OnButtonClicked" },
            new _BatchListenerItem { targetObjectName = "AudioController", targetComponentName = "AudioController", methodName = "PlayClickSound" },
        };

        var (sourceGo, sourceErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (sourceErr != null) { result.SetResult(sourceErr); return; }

        var sourceComp = sourceGo.GetComponent(componentName);
        if (sourceComp == null) { result.SetResult(new { error = "Source component not found: " + componentName }); return; }

        var sourceType = sourceComp.GetType();
        object rawEvent = null;
        var field = sourceType.GetFields().FirstOrDefault(f => f.Name == eventName);
        var prop = field == null ? sourceType.GetProperties().FirstOrDefault(p => p.Name == eventName) : null;
        if (field != null) rawEvent = field.GetValue(sourceComp);
        else if (prop != null) rawEvent = prop.GetValue(sourceComp);
        var unityEvent = rawEvent as UnityEvent;
        if (unityEvent == null) { result.SetResult(new { error = "UnityEvent '" + eventName + "' not found or wrong type (only parameterless UnityEvent supported)" }); return; }

        WorkflowManager.SnapshotObject(sourceComp);
        Undo.RecordObject(sourceComp, "Add Event Listeners");

        var results = new List<object>();
        int addedCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var (targetGo, targetErr) = GameObjectFinder.FindOrError(item.targetObjectName);
            if (targetErr != null) { results.Add(new { target = item.targetObjectName, success = false, error = "Target GameObject not found" }); failCount++; continue; }

            UnityEngine.Object targetObj;
            Type targetType;
            if (item.targetComponentName == "GameObject" || item.targetComponentName == "UnityEngine.GameObject")
            { targetObj = targetGo; targetType = typeof(GameObject); }
            else
            {
                var targetComp = targetGo.GetComponent(item.targetComponentName);
                if (targetComp == null) { results.Add(new { target = item.targetObjectName, success = false, error = "Target component not found: " + item.targetComponentName }); failCount++; continue; }
                targetObj = targetComp;
                targetType = targetComp.GetType();
            }

            var mi = targetType.GetMethods().FirstOrDefault(m => m.Name == item.methodName && m.GetParameters().Length == 0 && m.ReturnType == typeof(void));
            if (mi == null) { results.Add(new { target = item.targetObjectName, success = false, error = "Method not found: " + item.methodName }); failCount++; continue; }

            var action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), targetObj, mi, false);
            if (action == null) { results.Add(new { target = item.targetObjectName, success = false, error = "CreateDelegate failed" }); failCount++; continue; }

            UnityEventTools.AddPersistentListener(unityEvent, action);
            results.Add(new { target = item.targetObjectName, success = true, method = item.methodName });
            addedCount++;
        }

        EditorUtility.SetDirty(sourceComp);
        result.SetResult(new { success = failCount == 0, totalItems = items.Length, added = addedCount, failCount, results });
    }
}
```
