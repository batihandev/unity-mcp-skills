# cinemachine_set_brain

Configure `CinemachineBrain` properties: update method, default blend, and debug display options.

**Signature:** `CinemachineSetBrain(string updateMethod = null, string blendUpdateMethod = null, string defaultBlendStyle = null, float? defaultBlendTime = null, bool? showDebugText = null, bool? showCameraFrustum = null, bool? ignoreTimeScale = null)`

**Returns:** `{ success, settings }` or `{ error }`

**updateMethod values:** `"FixedUpdate"`, `"LateUpdate"`, `"SmartUpdate"`, `"ManualUpdate"`

**blendUpdateMethod values:** `"FixedUpdate"`, `"LateUpdate"`

**defaultBlendStyle values:** `"Cut"`, `"EaseInOut"`, `"EaseIn"`, `"EaseOut"`, `"HardIn"`, `"HardOut"`, `"Linear"`

**Notes:**
- Only pass parameters you want to change; omitted parameters are left unchanged.
- Returns the full resulting settings for confirmation.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string updateMethod = "SmartUpdate";      // null = no change
        string blendUpdateMethod = null;           // null = no change
        string defaultBlendStyle = "EaseInOut";   // null = no change
        float? defaultBlendTime = 1.5f;           // null = no change
        bool? showDebugText = false;
        bool? showCameraFrustum = true;
        bool? ignoreTimeScale = null;

        CinemachineBrain brain = null;
        var mainCam = Camera.main;
        if (mainCam != null) brain = mainCam.GetComponent<CinemachineBrain>();
        if (brain == null) brain = Object.FindFirstObjectByType<CinemachineBrain>();
        if (brain == null) { result.SetResult(new { error = "No CinemachineBrain found. Add one to the Main Camera first." }); return; }

        Undo.RecordObject(brain, "Set Brain");

        if (updateMethod != null &&
            System.Enum.TryParse<CinemachineBrain.UpdateMethods>(updateMethod, true, out var upd))
            brain.UpdateMethod = upd;
        if (blendUpdateMethod != null &&
            System.Enum.TryParse<CinemachineBrain.BrainUpdateMethods>(blendUpdateMethod, true, out var blUpd))
            brain.BlendUpdateMethod = blUpd;

        if (defaultBlendStyle != null || defaultBlendTime.HasValue)
        {
            var current = brain.DefaultBlend;
            string style = defaultBlendStyle ?? current.Style.ToString();
            float blendTime = defaultBlendTime ?? current.Time;
            var newBlend = new CinemachineBlendDefinition { Time = blendTime };
            if (System.Enum.TryParse<CinemachineBlendDefinition.Styles>(style, true, out var parsed))
                newBlend.Style = parsed;
            brain.DefaultBlend = newBlend;
        }

        if (showDebugText.HasValue) brain.ShowDebugText = showDebugText.Value;
        if (showCameraFrustum.HasValue) brain.ShowCameraFrustum = showCameraFrustum.Value;
        if (ignoreTimeScale.HasValue) brain.IgnoreTimeScale = ignoreTimeScale.Value;

        EditorUtility.SetDirty(brain);

        var resBlend = brain.DefaultBlend;
        result.SetResult(new
        {
            success = true,
            settings = new
            {
                updateMethod = brain.UpdateMethod.ToString(),
                blendUpdateMethod = brain.BlendUpdateMethod.ToString(),
                defaultBlendStyle = resBlend.Style.ToString(),
                defaultBlendTime = resBlend.Time,
                showDebugText = brain.ShowDebugText,
                showCameraFrustum = brain.ShowCameraFrustum,
                ignoreTimeScale = brain.IgnoreTimeScale
            }
        });
    }
}
```
