# animator_get_info

Get Animator component information from a GameObject. Resolves by name, instanceId, or path. Returns controller path, speed, root motion, update mode, culling mode, layer count, and parameter count.

**Signature:** `AnimatorGetInfo(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ gameObject, instanceId, hasController, controllerPath, speed, applyRootMotion, updateMode, cullingMode, layerCount, parameterCount }`

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

        var (animator, error) = GameObjectFinder.FindComponentOrError<Animator>(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var controllerPath = animator.runtimeAnimatorController != null
            ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
            : null;

        result.SetResult(new
        {
            gameObject = animator.gameObject.name,
            instanceId = animator.gameObject.GetInstanceID(),
            hasController = animator.runtimeAnimatorController != null,
            controllerPath,
            speed = animator.speed,
            applyRootMotion = animator.applyRootMotion,
            updateMode = animator.updateMode.ToString(),
            cullingMode = animator.cullingMode.ToString(),
            layerCount = animator.layerCount,
            parameterCount = animator.parameterCount
        });
    }
}
```
