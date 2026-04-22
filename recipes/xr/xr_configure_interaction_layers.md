# xr_configure_interaction_layers

Sets the InteractionLayerMask on an XR interactor or interactable for filtering which objects can interact with each other.

**Signature:** `XRConfigureInteractionLayers(name string = null, instanceId int = 0, path string = null, layers string = "Default", isInteractor bool = true)`

**Returns:** `{ success, name, instanceId, componentType, layers, isInteractor }`

**Notes:**
- `layers`: comma-separated XR interaction layer names, e.g. `"Default,Teleport"`.
- `isInteractor = true` targets an interactor component; `false` targets an interactable component.
- Do not confuse XR InteractionLayerMask with Unity physics Layer — they are separate systems.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string layers = "Default";
        bool isInteractor = true;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        // Parse layer mask once — InteractionLayerMask.GetMask takes string[] and returns int.
        var layerNames = layers.Split(',').Select(l => l.Trim()).ToArray();
        int mask = InteractionLayerMask.GetMask(layerNames);
        if (mask == 0 && int.TryParse(layers, out int parsed))
            mask = parsed;

        string componentType = null;
        if (isInteractor)
        {
            XRBaseInteractor interactor = go.GetComponent<XRRayInteractor>();
            if (interactor == null) interactor = go.GetComponent<XRDirectInteractor>();
            if (interactor == null) interactor = go.GetComponent<XRSocketInteractor>();
            if (interactor == null) interactor = go.GetComponent<XRBaseInteractor>();
            if (interactor == null)
                { result.SetResult(new { error = $"No XR interactor found on '{go.name}'." }); return; }

            Undo.RecordObject(interactor, "Configure Interaction Layers");
            WorkflowManager.SnapshotObject(interactor);
            interactor.interactionLayers = mask;
            componentType = interactor.GetType().Name;
        }
        else
        {
            XRBaseInteractable interactable = go.GetComponent<XRGrabInteractable>();
            if (interactable == null) interactable = go.GetComponent<XRSimpleInteractable>();
            if (interactable == null) interactable = go.GetComponent<XRBaseInteractable>();
            if (interactable == null)
                { result.SetResult(new { error = $"No XR interactable found on '{go.name}'." }); return; }

            Undo.RecordObject(interactable, "Configure Interaction Layers");
            WorkflowManager.SnapshotObject(interactable);
            interactable.interactionLayers = mask;
            componentType = interactable.GetType().Name;
        }

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            componentType,
            layers,
            isInteractor
        }); return; }
    }
}
```
