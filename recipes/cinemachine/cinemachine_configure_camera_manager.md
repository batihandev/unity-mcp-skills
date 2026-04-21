# cinemachine_configure_camera_manager

Configure ClearShot, StateDriven, or Sequencer camera-manager properties in one call. Only the properties whose matching component exists on the target are applied.

**Signature:** `CinemachineConfigureCameraManager(string cameraName = null, int cameraInstanceId = 0, string cameraPath = null, float? activateAfter = null, float? minDuration = null, bool? randomizeChoice = null, string animatorName = null, int? layerIndex = null, string defaultBlendStyle = null, float? defaultBlendTime = null, bool? loop = null)`

**Returns:** `{ success, message }` or `{ error }`

**Parameter applicability:**
- `activateAfter`, `minDuration`, `randomizeChoice` — ClearShot only (CM3: `ActivateAfter`/`MinDuration`/`RandomizeChoice`; CM2: `m_ActivateAfter`/`m_MinDuration`/`m_RandomizeChoice`)
- `animatorName`, `layerIndex` — StateDriven only
- `defaultBlendStyle`, `defaultBlendTime` — ClearShot and StateDriven
- `loop` — Sequencer only

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string cameraName = "My Clear Shot";
        int cameraInstanceId = 0;
        string cameraPath = null;
        float? activateAfter = 0.5f;
        float? minDuration = 1f;
        bool? randomizeChoice = false;
        string animatorName = null;
        int? layerIndex = null;
        string defaultBlendStyle = "EaseInOut";
        float? defaultBlendTime = 1f;
        bool? loop = null;

        var (go, err) = GameObjectFinder.FindOrError(cameraName, cameraInstanceId, cameraPath);
        if (err != null) { result.SetResult(err); return; }

        WorkflowManager.SnapshotObject(go);
        var changes = new List<string>();

        // ClearShot
        var clearShot = go.GetComponent<CinemachineClearShot>();
        if (clearShot != null)
        {
            Undo.RecordObject(clearShot, "Configure ClearShot");
#if CINEMACHINE_3
            if (activateAfter.HasValue) { clearShot.ActivateAfter = activateAfter.Value; changes.Add("activateAfter=" + activateAfter.Value); }
            if (minDuration.HasValue) { clearShot.MinDuration = minDuration.Value; changes.Add("minDuration=" + minDuration.Value); }
            if (randomizeChoice.HasValue) { clearShot.RandomizeChoice = randomizeChoice.Value; changes.Add("randomize=" + randomizeChoice.Value); }
            if (defaultBlendStyle != null || defaultBlendTime.HasValue)
            {
                string style = defaultBlendStyle ?? CinemachineAdapter.GetBlendStyle(clearShot.DefaultBlend);
                float t = defaultBlendTime ?? CinemachineAdapter.GetBlendTime(clearShot.DefaultBlend);
                clearShot.DefaultBlend = CinemachineAdapter.CreateBlendDefinition(style, t);
                changes.Add("blend=" + style + " " + t + "s");
            }
#else
            if (activateAfter.HasValue) { clearShot.m_ActivateAfter = activateAfter.Value; changes.Add("activateAfter=" + activateAfter.Value); }
            if (minDuration.HasValue) { clearShot.m_MinDuration = minDuration.Value; changes.Add("minDuration=" + minDuration.Value); }
            if (randomizeChoice.HasValue) { clearShot.m_RandomizeChoice = randomizeChoice.Value; changes.Add("randomize=" + randomizeChoice.Value); }
#endif
            EditorUtility.SetDirty(clearShot);
        }

        // StateDriven
        var stateDriven = go.GetComponent<CinemachineStateDrivenCamera>();
        if (stateDriven != null)
        {
            Undo.RecordObject(stateDriven, "Configure StateDriven");
            if (!string.IsNullOrEmpty(animatorName))
            {
                var animGo = GameObjectFinder.Find(animatorName);
                if (animGo != null)
                {
                    var animator = animGo.GetComponent<Animator>();
                    if (animator != null)
                    {
#if CINEMACHINE_3
                        stateDriven.AnimatedTarget = animator;
#else
                        stateDriven.m_AnimatedTarget = animator;
#endif
                        changes.Add("animator=" + animatorName);
                    }
                }
            }
            EditorUtility.SetDirty(stateDriven);
        }

        // Sequencer
        var seq = CinemachineAdapter.GetSequencer(go);
        if (seq != null && loop.HasValue)
        {
            Undo.RecordObject(seq, "Configure Sequencer");
            CinemachineAdapter.SetSequencerLoop(seq, loop.Value);
            changes.Add("loop=" + loop.Value);
            EditorUtility.SetDirty(seq);
        }

        if (changes.Count == 0)
        {
            result.SetResult(new { error = "No matching camera manager found or no properties to change." });
            return;
        }
        result.SetResult(new { success = true, message = "Configured " + go.name + ": " + string.Join(", ", changes) });
    }
}
```
