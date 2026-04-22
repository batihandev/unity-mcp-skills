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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // sourceParams accepts a JsonUtility-parseable shape: {"velocity":{"x":0,"y":-1,"z":0}}
        string sourceParams = "{\"velocity\":{\"x\":0,\"y\":-1,\"z\":0}}";

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
                var parsed = JsonUtility.FromJson<_ImpulseParams>(sourceParams);
                if (parsed != null && parsed.velocity != null)
                    velocity = new Vector3(parsed.velocity.x, parsed.velocity.y, parsed.velocity.z);
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

// Named serializable types so JsonUtility can round-trip them (no anonymous / Newtonsoft).
[System.Serializable]
internal class _ImpulseParams { public _ImpulseVelocity velocity; }

[System.Serializable]
internal class _ImpulseVelocity { public float x; public float y; public float z; }
```
