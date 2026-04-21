# animator_list_states

List all states in a specific layer of an Animator Controller. Returns name, tag, speed, and whether each state has a motion assigned.

**Signature:** `AnimatorListStates(string controllerPath, int layer = 0)`

**Returns:** `{ controller, layer, layerName, stateCount, states: [{ name, tag, speed, hasMotion }] }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string controllerPath = "Assets/Animations/MyController.controller";
        int layer = 0; // Layer index

        var pathErr = Validate.SafePath(controllerPath, "controllerPath");
        if (pathErr != null) { result.SetResult(pathErr); return; }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            result.SetResult(new { error = $"Controller not found: {controllerPath}" });
            return;
        }

        if (layer >= controller.layers.Length)
        {
            result.SetResult(new { error = $"Layer {layer} does not exist. Controller has {controller.layers.Length} layers." });
            return;
        }

        var stateMachine = controller.layers[layer].stateMachine;
        var states = stateMachine.states.Select(s => new
        {
            name = s.state.name,
            tag = s.state.tag,
            speed = s.state.speed,
            hasMotion = s.state.motion != null
        }).ToArray();

        result.SetResult(new
        {
            controller = controllerPath,
            layer,
            layerName = controller.layers[layer].name,
            stateCount = states.Length,
            states
        });
    }
}
```
