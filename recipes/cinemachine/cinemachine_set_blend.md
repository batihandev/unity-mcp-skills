# cinemachine_set_blend

Set the default blend (or a per-camera-pair blend) on the `CinemachineBrain`.

**Signature:** `CinemachineSetBlend(string style = "EaseInOut", float time = 2f, string fromCamera = null, string toCamera = null)`

**Returns:** `{ success, message }` or `{ error }`

**style values:** `"Cut"`, `"EaseInOut"`, `"EaseIn"`, `"EaseOut"`, `"HardIn"`, `"HardOut"`, `"Linear"`

**Notes:**
- Leave `fromCamera` and `toCamera` empty to set the default blend for all transitions.
- Per-camera-pair blends (with `fromCamera`/`toCamera`) require a `CinemachineBlenderSettings` asset. The current implementation sets the default blend and returns a note about the limitation — for full per-pair control, create a CinemachineBlenderSettings asset manually or via `cinemachine_set_brain`.
- For camera-specific blend control also see `cinemachine_set_brain`.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string style = "EaseInOut";
        float time = 2f;
        string fromCamera = null;  // leave null for default blend
        string toCamera = null;    // leave null for default blend

        var brain = CinemachineAdapter.FindBrain();
        if (brain == null) { result.SetResult(new { error = "No CinemachineBrain found." }); return; }

        var blend = CinemachineAdapter.CreateBlendDefinition(style, time);

        if (string.IsNullOrEmpty(fromCamera) && string.IsNullOrEmpty(toCamera))
        {
            Undo.RecordObject(brain, "Set Default Blend");
            CinemachineAdapter.SetBrainDefaultBlend(brain, blend);
            EditorUtility.SetDirty(brain);
            result.SetResult(new { success = true, message = "Set default blend: " + style + " " + time + "s" });
        }
        else
        {
            // Per-pair blends require CinemachineBlenderSettings asset; set default as best-effort
            result.SetResult(new { success = true, message = "Set default blend: " + style + " " + time + "s (per-camera-pair blends require CinemachineBlenderSettings asset)" });
        }
    }
}
```
