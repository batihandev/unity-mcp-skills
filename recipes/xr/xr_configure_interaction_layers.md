# xr_configure_interaction_layers

Sets the InteractionLayerMask on an XR interactor or interactable for filtering which objects can interact with each other.

**Signature:** `XRConfigureInteractionLayers(name string = null, instanceId int = 0, path string = null, layers string = "Default", isInteractor bool = true)`

**Returns:** `{ success, name, instanceId, componentType, layers, isInteractor }`

**Notes:**
- `layers`: comma-separated XR interaction layer names, e.g. `"Default,Teleport"`.
- `isInteractor = true` targets an interactor component; `false` targets an interactable component.
- Uses `InteractionLayerMask.GetMask()` via reflection. Falls back to integer parsing if the type is unavailable.
- Do not confuse XR InteractionLayerMask with Unity physics Layer — they are separate systems.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !XRI
                    { result.SetResult(NoXRI()); return; }
        #else
                    var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
                    if (findErr != null) { result.SetResult(findErr); return; }

                    // Find the XR component
                    Component comp;
                    if (isInteractor)
                    {
                        comp = XRReflectionHelper.GetXRComponent(go, "XRRayInteractor")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRDirectInteractor")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRSocketInteractor")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRBaseInteractor");
                    }
                    else
                    {
                        comp = XRReflectionHelper.GetXRComponent(go, "XRGrabInteractable")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRSimpleInteractable")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRBaseInteractable");
                    }

                    if (comp == null)
                        { result.SetResult(new { error = $"No XR {(isInteractor ? "interactor" : "interactable")} found on '{go.name}'." }); return; }

                    Undo.RecordObject(comp, "Configure Interaction Layers");
                    WorkflowManager.SnapshotObject(comp);

                    // Try to set interaction layers via InteractionLayerMask
                    var ilmType = XRReflectionHelper.ResolveXRType("InteractionLayerMask");
                    if (ilmType != null)
                    {
                        var getMethod = ilmType.GetMethod("GetMask", BindingFlags.Public | BindingFlags.Static);
                        if (getMethod != null)
                        {
                            try
                            {
                                var layerNames = layers.Split(',').Select(l => l.Trim()).ToArray();
                                var mask = getMethod.Invoke(null, new object[] { layerNames });
                                XRReflectionHelper.SetProperty(comp, "interactionLayers", mask);
                            }
                            catch
                            {
                                // Fallback: try setting by integer value
                                if (int.TryParse(layers, out int layerMask))
                                    XRReflectionHelper.SetProperty(comp, "interactionLayers", layerMask);
                            }
                        }
                    }

                    { result.SetResult(new
                    {
                        success = true,
                        name = go.name,
                        instanceId = go.GetInstanceID(),
                        componentType = comp.GetType().Name,
                        layers,
                        isInteractor
                    }); return; }
        #endif
    }
}
```
