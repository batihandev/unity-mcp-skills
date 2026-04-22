# animator_add_transition

Add a transition between two existing states in an Animator Controller layer. Both states must already exist; the command returns an error if either is not found.

**Signature:** `AnimatorAddTransition(string controllerPath, string fromState, string toState, int layer = 0, bool hasExitTime = true, float duration = 0.25f)`

**Returns:** `{ success, from, to, layer, hasExitTime, duration }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string controllerPath = "Assets/Animations/MyController.controller";
        string fromState = "Idle";   // Source state name
        string toState = "Run";      // Destination state name
        int layer = 0;               // Layer index
        bool hasExitTime = true;     // Whether transition waits for exit time
        float duration = 0.25f;      // Transition duration in seconds

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
        var states = sm.states;
        AnimatorState src = null, dst = null;
        foreach (var s in states)
        {
            if (s.state.name == fromState) src = s.state;
            if (s.state.name == toState) dst = s.state;
        }
        if (src == null)
        {
            result.SetResult(new { error = $"State not found: {fromState}" });
            return;
        }
        if (dst == null)
        {
            result.SetResult(new { error = $"State not found: {toState}" });
            return;
        }

        var transition = src.AddTransition(dst);
        transition.hasExitTime = hasExitTime;
        transition.duration = duration;
        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, from = fromState, to = toState, layer, hasExitTime, duration });
    }
}
```
