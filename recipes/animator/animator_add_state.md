# animator_add_state

Add a new state to an Animator Controller layer. Optionally assigns an AnimationClip to the state's motion. The clip is loaded silently — if `clipPath` is provided but the asset is not found, the state is still created without a motion.

**Signature:** `AnimatorAddState(string controllerPath, string stateName, string clipPath = null, int layer = 0)`

**Returns:** `{ success, controller, stateName, layer }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string controllerPath = "Assets/Animations/MyController.controller";
        string stateName = "Run";          // Name for the new state
        string clipPath = null;            // Optional: AnimationClip asset path
        int layer = 0;                     // Layer index

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            result.SetResult(new { error = $"Controller not found: {controllerPath}" });
            return;
        }
        if (layer < 0 || layer >= controller.layers.Length)
        {
            result.SetResult(new { error = $"Invalid layer: {layer}" });
            return;
        }

        var sm = controller.layers[layer].stateMachine;
        var state = sm.AddState(stateName);
        if (!string.IsNullOrEmpty(clipPath))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip != null) state.motion = clip;
        }
        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, controller = controllerPath, stateName, layer });
    }
}
```
