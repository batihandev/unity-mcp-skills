# cinemachine_impulse_generate

Trigger a Cinemachine Impulse from the first `CinemachineImpulseSource` found in the scene.

**Signature:** `CinemachineImpulseGenerate(string sourceParams)`

**Returns:** `{ success, message }` or `{ success: false, error }`

**sourceParams format:** JSON string, e.g. `{"velocity": {"x": 0, "y": -1, "z": 0}}`

**Notes:**
- Always uses the first `CinemachineImpulseSource` found in the scene. If you need a specific source, use the source's GameObject directly.
- Default velocity when `sourceParams` is empty or unparseable: `Vector3.down` (0, -1, 0).
- Requires a `CinemachineImpulseListener` on the VCam(s) to perceive the impulse.
- To configure the source definition (shape, duration, radius), use `cinemachine_configure_impulse_source`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sourceParams = "{\"velocity\": {\"x\": 0, \"y\": -1, \"z\": 0}}";

        var sources = FindHelper.FindAll<CinemachineImpulseSource>();
        if (sources.Length == 0)
        {
            result.SetResult(new { success = false, error = "No CinemachineImpulseSource found in scene." });
            return;
        }

        var source = sources[0];
        Vector3 velocity = Vector3.down;

        if (!string.IsNullOrEmpty(sourceParams))
        {
            try
            {
                var json = JObject.Parse(sourceParams);
                if (json["velocity"] != null)
                {
                    var v = json["velocity"];
                    velocity = new Vector3((float)v["x"], (float)v["y"], (float)v["z"]);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[UnitySkills] Failed to parse impulse params: " + ex.Message);
            }
        }

        source.GenerateImpulse(velocity);
        result.SetResult(new { success = true, message = "Generated Impulse from " + source.name + " with velocity " + velocity });
    }
}
```
