# cinemachine_set_noise

Configure Noise settings on a VCam using `CinemachineBasicMultiChannelPerlin`. Adds the component if not present.

**Signature:** `CinemachineSetNoise(string vcamName = null, int instanceId = 0, string path = null, float amplitudeGain = 1f, float frequencyGain = 1f)`

**Returns:** `{ success, message }` or `{ error }`

**Notes:**
- `amplitudeGain` scales the noise magnitude. `0` = no noise, `1` = default noise profile amplitude.
- `frequencyGain` scales the noise speed. Higher values produce faster camera shake.
- The `CinemachineBasicMultiChannelPerlin` component is auto-added to the VCam if missing.
- You still need a Noise Profile asset assigned for the noise to be visible. Use `cinemachine_set_vcam_property` with `componentType="CinemachineBasicMultiChannelPerlin"`, `propertyName="NoiseProfile"` to assign an asset by name.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        float amplitudeGain = 1f;
        float frequencyGain = 1f;

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        WorkflowManager.SnapshotObject(go);

        var perlin = go.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (perlin == null)
        {
            perlin = Undo.AddComponent<CinemachineBasicMultiChannelPerlin>(go);
            WorkflowManager.SnapshotCreatedComponent(perlin);
        }

        perlin.AmplitudeGain = amplitudeGain;
        perlin.FrequencyGain = frequencyGain;
        EditorUtility.SetDirty(perlin);

        result.SetResult(new { success = true, message = "Set Noise profile." });
    }
}
```
