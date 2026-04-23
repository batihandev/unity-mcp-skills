# xr_add_interaction_event

Wires a persistent UnityEvent listener from an XR interactable's interaction event to a MonoBehaviour method on another GameObject.

**Signature:** `XRAddInteractionEvent(name string = null, instanceId int = 0, path string = null, eventType string = "selectEntered", targetName string, targetMethod string)`

**Returns:** `{ success, name, instanceId, eventType, targetObject, targetMethod, interactableType }`

**Notes:**
- `targetName` and `targetMethod` are required.
- `eventType` options: `selectEntered`, `selectExited`, `hoverEntered`, `hoverExited`, `firstSelectEntered`, `lastSelectExited`, `firstHoverEntered`, `lastHoverExited`, `activated`, `deactivated`.
- The target method must be public, instance, and parameterless (void) on a MonoBehaviour component of `targetName`.
- Uses `UnityEventTools.AddVoidPersistentListener` — the listener persists across play mode.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string eventType = "selectEntered";
        string targetName = null;
        string targetMethod = null;

        if (Validate.Required(targetName, "targetName") is object err1) { result.SetResult(err1); return; }
        if (Validate.Required(targetMethod, "targetMethod") is object err2) { result.SetResult(err2); return; }

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        // Find interactable (XRGrabInteractable first, then XRSimpleInteractable, then any XRBaseInteractable)
        Component comp = go.GetComponent<XRGrabInteractable>();
        if (comp == null) comp = go.GetComponent<XRSimpleInteractable>();
        if (comp == null) comp = go.GetComponent<XRBaseInteractable>();

        if (comp == null)
            { result.SetResult(new { error = $"No XR interactable found on '{go.name}'." }); return; }

        // Find target object
        var targetGo = GameObjectFinder.Find(targetName);
        if (targetGo == null)
            { result.SetResult(new { error = $"Target GameObject '{targetName}' not found." }); return; }

        // Find the event property (no BindingFlags — reformatter gotcha).
        var eventProp = comp.GetType().GetProperty(eventType);
        if (eventProp == null)
            { result.SetResult(new
            {
                error = $"Event '{eventType}' not found on {comp.GetType().Name}.",
                availableEvents = new[] { "selectEntered", "selectExited", "hoverEntered", "hoverExited",
                    "firstSelectEntered", "lastSelectExited", "firstHoverEntered", "lastHoverExited",
                    "activated", "deactivated" }
            }); return; }

        Undo.RecordObject(comp, "Add XR Interaction Event");
        WorkflowManager.SnapshotObject(comp);

        var eventObj = eventProp.GetValue(comp);
        if (eventObj == null)
            { result.SetResult(new { error = $"Event '{eventType}' returned null." }); return; }

        // Find target method on any component of the target (GetMethod without flags to avoid reformatter NRE).
        MonoBehaviour targetComponent = null;
        System.Reflection.MethodInfo targetMethodInfo = null;
        foreach (var targetComp in targetGo.GetComponents<MonoBehaviour>())
        {
            var method = targetComp.GetType().GetMethod(targetMethod);
            if (method != null)
            {
                targetComponent = targetComp;
                targetMethodInfo = method;
                break;
            }
        }

        if (targetComponent == null || targetMethodInfo == null)
            { result.SetResult(new { error = $"Method '{targetMethod}' not found on any component of '{targetName}'." }); return; }

        // Use UnityEventTools to add persistent listener
        var addMethod = typeof(UnityEditor.Events.UnityEventTools).GetMethods()
            .FirstOrDefault(m => m.Name == "AddVoidPersistentListener" && m.GetParameters().Length == 2);

        if (addMethod != null)
        {
            var action = Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), targetComponent, targetMethodInfo, false);
            if (action != null)
            {
                addMethod.Invoke(null, new object[] { eventObj, action });
            }
        }

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            eventType,
            targetObject = targetName,
            targetMethod,
            interactableType = comp.GetType().Name
        }); return; }
    }
}
```
