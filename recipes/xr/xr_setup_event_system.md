# xr_setup_event_system

Finds or creates an EventSystem, removes StandaloneInputModule if present, and adds XRUIInputModule for XR-compatible UI input.

**Signature:** `XRSetupEventSystem()`

**Returns:** `{ success, name, instanceId, created, removedStandaloneInputModule, addedXRUIInputModule }`

**Notes:**
- Safe to call even if EventSystem already exists — it only modifies the input modules.
- Required before XR UI Canvas interactions work correctly.

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
                    var xrInputType = XRReflectionHelper.ResolveXRType("XRUIInputModule");
                    if (xrInputType == null)
                        { result.SetResult(new { error = "XRUIInputModule type not found in current XRI version." }); return; }

                    // Find or create EventSystem
                    var eventSystems = FindHelper.FindAll<UnityEngine.EventSystems.EventSystem>();
                    GameObject esGo;
                    bool created = false;

                    if (eventSystems.Length > 0)
                    {
                        esGo = eventSystems[0].gameObject;
                    }
                    else
                    {
                        esGo = new GameObject("EventSystem");
                        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        created = true;
                    }

                    Undo.RecordObject(esGo, "Setup XR EventSystem");

                    // Remove StandaloneInputModule if present
                    var standalone = esGo.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    bool removedStandalone = false;
                    if (standalone != null)
                    {
                        Undo.DestroyObjectImmediate(standalone);
                        removedStandalone = true;
                    }

                    // Add XRUIInputModule if not present
                    bool addedXRInput = false;
                    var xrInput = esGo.GetComponent(xrInputType);
                    if (xrInput == null)
                    {
                        xrInput = esGo.AddComponent(xrInputType);
                        addedXRInput = true;
                    }

                    if (created)
                        Undo.RegisterCreatedObjectUndo(esGo, "Create XR EventSystem");

                    WorkflowManager.SnapshotObject(esGo, created ? SnapshotType.Created : SnapshotType.Modified);

                    { result.SetResult(new
                    {
                        success = true,
                        name = esGo.name,
                        instanceId = esGo.GetInstanceID(),
                        created,
                        removedStandaloneInputModule = removedStandalone,
                        addedXRUIInputModule = addedXRInput
                    }); return; }
        #endif
    }
}
```
