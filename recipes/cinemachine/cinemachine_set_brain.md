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

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

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

        var brain = CinemachineAdapter.FindBrain();
        if (brain == null) { result.SetResult(new { error = "No CinemachineBrain found. Add one to the Main Camera first." }); return; }

        Undo.RecordObject(brain, "Set Brain");

        if (updateMethod != null)
            CinemachineAdapter.SetBrainUpdateMethod(brain, updateMethod);
        if (blendUpdateMethod != null)
            CinemachineAdapter.SetBrainBlendUpdateMethod(brain, blendUpdateMethod);

        if (defaultBlendStyle != null || defaultBlendTime.HasValue)
        {
            var current = CinemachineAdapter.GetBrainDefaultBlend(brain);
            string style = defaultBlendStyle ?? CinemachineAdapter.GetBlendStyle(current);
            float blendTime = defaultBlendTime ?? CinemachineAdapter.GetBlendTime(current);
            CinemachineAdapter.SetBrainDefaultBlend(brain, CinemachineAdapter.CreateBlendDefinition(style, blendTime));
        }

        if (showDebugText.HasValue) CinemachineAdapter.SetBrainBool(brain, "ShowDebugText", showDebugText.Value);
        if (showCameraFrustum.HasValue) CinemachineAdapter.SetBrainBool(brain, "ShowCameraFrustum", showCameraFrustum.Value);
        if (ignoreTimeScale.HasValue) CinemachineAdapter.SetBrainBool(brain, "IgnoreTimeScale", ignoreTimeScale.Value);

        EditorUtility.SetDirty(brain);

        var blend = CinemachineAdapter.GetBrainDefaultBlend(brain);
        result.SetResult(new
        {
            success = true,
            settings = new
            {
                updateMethod = CinemachineAdapter.GetBrainUpdateMethod(brain),
                blendUpdateMethod = CinemachineAdapter.GetBrainBlendUpdateMethod(brain),
                defaultBlendStyle = CinemachineAdapter.GetBlendStyle(blend),
                defaultBlendTime = CinemachineAdapter.GetBlendTime(blend),
                showDebugText = CinemachineAdapter.GetBrainBool(brain, "ShowDebugText"),
                showCameraFrustum = CinemachineAdapter.GetBrainBool(brain, "ShowCameraFrustum"),
                ignoreTimeScale = CinemachineAdapter.GetBrainBool(brain, "IgnoreTimeScale")
            }
        });
    }
}
```
