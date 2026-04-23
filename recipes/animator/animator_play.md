# animator_play

Play a specific animation state on an Animator component. Resolves the GameObject using name, instanceId, or path.

**Signature:** `AnimatorPlay(string name = null, int instanceId = 0, string path = null, string stateName = null, int layer = 0, float normalizedTime = 0)`

**Returns:** `{ success, gameObject, state, layer }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Provide at least one of: name, instanceId, or path
        string name = "Player";
        int instanceId = 0;
        string path = null;

        string stateName = "Run";   // Animation state name (required)
        int layer = 0;              // Animator layer index
        float normalizedTime = 0f;  // Start time 0–1

        if (Validate.Required(stateName, "stateName") is object err1) { result.SetResult(err1); return; }

        var (animator, error) = GameObjectFinder.FindComponentOrError<Animator>(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        animator.Play(stateName, layer, normalizedTime);

        result.SetResult(new { success = true, gameObject = animator.gameObject.name, state = stateName, layer });
    }
}
```
