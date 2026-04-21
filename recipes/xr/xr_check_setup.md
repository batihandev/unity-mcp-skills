# xr_check_setup

Validates XR package installation, rig presence, managers, event system, collider rules, and TrackedPoseDriver on controllers.

**Signature:** `XRCheckSetup(verbose bool = false)`

**Returns:** `{ xriInstalled, xriMajorVersion, interactionManagerCount, xrOriginCount, mainCamera, eventSystemCount, hasXRUIInputModule, interactorCount, interactableCount, hasTeleportation, hasContinuousMove, hasTurnProvider, issues, issueCount, success }`

**Notes:**
- Returns `{ error: "XR Interaction Toolkit package ... is not installed" }` if XRI is missing.
- Pass `verbose=true` to include `interactorDetails` and `interactableDetails` arrays.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

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
                    var issues = new List<string>();
                    var info = new Dictionary<string, object>();

                    // 1. XRI version
                    info["xriInstalled"] = XRReflectionHelper.IsXRIInstalled;
                    info["xriMajorVersion"] = XRReflectionHelper.XRIMajorVersion;

                    // 2. XRInteractionManager
                    var managers = XRReflectionHelper.FindComponentsOfXRType("XRInteractionManager");
                    info["interactionManagerCount"] = managers.Length;
                    if (managers.Length == 0)
                        issues.Add("No XRInteractionManager found in scene. Add one via xr_setup_interaction_manager.");
                    if (managers.Length > 1)
                        issues.Add($"Multiple XRInteractionManagers found ({managers.Length}). Typically only one is needed.");

                    // 3. XR Origin
                    var origins = XRReflectionHelper.FindComponentsOfXRType("XROrigin");
                    info["xrOriginCount"] = origins.Length;
                    if (origins.Length == 0)
                        issues.Add("No XR Origin found in scene. Create one via xr_setup_rig.");

                    // 4. Camera
                    var mainCam = Camera.main;
                    info["mainCamera"] = mainCam != null ? mainCam.gameObject.name : null;
                    if (mainCam == null)
                        issues.Add("No Main Camera found. XR Origin rig should include a tagged MainCamera.");

                    // 5. EventSystem
                    var eventSystems = FindHelper.FindAll<UnityEngine.EventSystems.EventSystem>();
                    info["eventSystemCount"] = eventSystems.Length;
                    if (eventSystems.Length == 0)
                        issues.Add("No EventSystem found. Create one via xr_setup_event_system.");
                    else
                    {
                        // Check for XRUIInputModule
                        bool hasXRInput = false;
                        foreach (var es in eventSystems)
                        {
                            if (XRReflectionHelper.GetXRComponent(es.gameObject, "XRUIInputModule") != null)
                            {
                                hasXRInput = true;
                                break;
                            }
                        }
                        info["hasXRUIInputModule"] = hasXRInput;
                        if (!hasXRInput)
                            issues.Add("EventSystem exists but lacks XRUIInputModule. Fix via xr_setup_event_system.");
                    }

                    // 6. Interactors & Interactables
                    var interactors = XRReflectionHelper.FindComponentsOfXRType("XRBaseInteractor");
                    var interactables = XRReflectionHelper.FindComponentsOfXRType("XRBaseInteractable");
                    info["interactorCount"] = interactors.Length;
                    info["interactableCount"] = interactables.Length;

                    // 7. Locomotion
                    var teleportProvider = XRReflectionHelper.FindFirstOfXRType("TeleportationProvider");
                    var moveProvider = XRReflectionHelper.FindFirstOfXRType("ActionBasedContinuousMoveProvider")
                                       ?? XRReflectionHelper.FindFirstOfXRType("ContinuousMoveProvider");
                    var turnProvider = XRReflectionHelper.FindFirstOfXRType("ActionBasedSnapTurnProvider")
                                       ?? XRReflectionHelper.FindFirstOfXRType("SnapTurnProvider")
                                       ?? XRReflectionHelper.FindFirstOfXRType("ActionBasedContinuousTurnProvider")
                                       ?? XRReflectionHelper.FindFirstOfXRType("ContinuousTurnProvider");
                    info["hasTeleportation"] = teleportProvider != null;
                    info["hasContinuousMove"] = moveProvider != null;
                    info["hasTurnProvider"] = turnProvider != null;

                    // 8. Collider validation — most common XR setup error
                    var colliderIssues = new List<string>();
                    foreach (var interactor in interactors)
                    {
                        var typeName = interactor.GetType().Name;
                        if (typeName.Contains("Direct") || typeName.Contains("Socket"))
                        {
                            var col = interactor.GetComponent<Collider>();
                            if (col == null)
                                colliderIssues.Add($"{interactor.gameObject.name} ({typeName}): missing trigger Collider — will not detect targets");
                            else if (!col.isTrigger)
                                colliderIssues.Add($"{interactor.gameObject.name} ({typeName}): Collider.isTrigger must be TRUE for interactors");
                        }
                    }
                    foreach (var interactable in interactables)
                    {
                        var typeName = interactable.GetType().Name;
                        if (typeName.Contains("Grab"))
                        {
                            var rb = interactable.GetComponent<Rigidbody>();
                            if (rb == null)
                                colliderIssues.Add($"{interactable.gameObject.name} ({typeName}): missing Rigidbody — grab will not work");
                            var col = interactable.GetComponent<Collider>();
                            if (col == null)
                                colliderIssues.Add($"{interactable.gameObject.name} ({typeName}): missing Collider — cannot be detected by interactors");
                            else if (col.isTrigger)
                                colliderIssues.Add($"{interactable.gameObject.name} ({typeName}): Collider.isTrigger should be FALSE for interactables");
                        }
                    }
                    if (colliderIssues.Count > 0)
                    {
                        issues.AddRange(colliderIssues);
                        info["colliderIssues"] = colliderIssues;
                    }

                    // 9. TrackedPoseDriver check
                    var tpdType = FindTrackedPoseDriverType();
                    if (tpdType != null && origins.Length > 0)
                    {
                        var originGo = origins[0].gameObject;
                        var controllers = new[] { "Left Controller", "Right Controller" };
                        foreach (var ctrlName in controllers)
                        {
                            var ctrlTransform = originGo.transform.Find(ctrlName);
                            if (ctrlTransform != null && ctrlTransform.GetComponent(tpdType) == null)
                                issues.Add($"'{ctrlName}' lacks TrackedPoseDriver — controller position will not update");
                        }
                    }

                    info["issues"] = issues;
                    info["issueCount"] = issues.Count;
                    info["success"] = true;

                    if (verbose)
                    {
                        // Add detailed component listing
                        info["interactorDetails"] = interactors.Select(c => new {
                            name = c.gameObject.name, type = c.GetType().Name,
                            instanceId = c.gameObject.GetInstanceID()
                        }).ToArray();
                        info["interactableDetails"] = interactables.Select(c => new {
                            name = c.gameObject.name, type = c.GetType().Name,
                            instanceId = c.gameObject.GetInstanceID()
                        }).ToArray();
                    }

                    { result.SetResult(info); return; }
        #endif
    }
}
```
