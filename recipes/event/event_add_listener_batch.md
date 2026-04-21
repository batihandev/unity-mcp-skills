# event_add_listener_batch

Add multiple persistent listeners to a UnityEvent in a single call. Each item in the JSON array specifies a target object, component, and method. Internally calls `event_add_listener` for each item with default `argType = "void"` and `mode = "RuntimeOnly"`. Returns the count of successfully added listeners.

**Signature:** `EventAddListenerBatch(string name = null, int instanceId = 0, string path = null, string componentName = null, string eventName = null, string items = null)`

**Returns:** `{ success, added, total }`

**`items` format:** JSON array of objects with fields `targetObjectName`, `targetComponentName`, `methodName`.

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System.Collections.Generic;
using Newtonsoft.Json;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyButton";       // GameObject name (or use instanceId / path)
        int instanceId = 0;
        string path = null;
        string componentName = "Button";
        string eventName = "onClick";
        // Each item: targetObjectName, targetComponentName, methodName
        string items = "[{\"targetObjectName\":\"GameManager\",\"targetComponentName\":\"GameManager\",\"methodName\":\"OnButtonClicked\"},{\"targetObjectName\":\"AudioController\",\"targetComponentName\":\"AudioController\",\"methodName\":\"PlayClickSound\"}]";

        var list = JsonConvert.DeserializeObject<List<BatchListenerItem>>(items);
        if (list == null || list.Count == 0) { result.SetResult(new { error = "No items provided" }); return; }

        int added = 0;
        foreach (var item in list)
        {
            var addResult = EventAddListener(name, instanceId, path, componentName, eventName,
                item.targetObjectName, item.targetComponentName, item.methodName);
            if (!SkillResultHelper.TryGetError(addResult, out _))
                added++;
        }

        result.SetResult(new { success = true, added, total = list.Count });
    }

    // Delegates to event_add_listener logic with void/RuntimeOnly defaults
    private object EventAddListener(string name, int instanceId, string path,
        string componentName, string eventName,
        string targetObjectName, string targetComponentName, string methodName)
    {
        // See event_add_listener.md for full implementation
        // This batch command calls it with argType = "void", mode = "RuntimeOnly"
        throw new System.NotImplementedException("Delegate to EventSkills.EventAddListener");
    }

    private class BatchListenerItem
    {
        public string targetObjectName { get; set; }
        public string targetComponentName { get; set; }
        public string methodName { get; set; }
    }
}
```
