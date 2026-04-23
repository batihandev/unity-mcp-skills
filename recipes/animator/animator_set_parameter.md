# animator_set_parameter

Set a parameter value on an Animator component at runtime. Resolves the GameObject using name, instanceId, or path. Supports float, int, bool, and trigger types.

**Signature:** `AnimatorSetParameter(string name = null, int instanceId = 0, string path = null, string paramName = null, string paramType = "float", float floatValue = 0, int intValue = 0, bool boolValue = false)`

**Returns:** `{ success, gameObject, parameter, value }` — trigger returns `{ success, gameObject, parameter, triggered: true }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

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

        string paramName = "Speed";   // Parameter name (required)
        string paramType = "float";   // float | int | bool | trigger
        float floatValue = 0f;
        int intValue = 0;
        bool boolValue = false;

        if (Validate.Required(paramName, "paramName") is object err) { result.SetResult(err); return; }

        var (animator, error) = GameObjectFinder.FindComponentOrError<Animator>(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        WorkflowManager.SnapshotObject(animator);
        Undo.RecordObject(animator, "Set Animator Parameter");

        switch (paramType.ToLower())
        {
            case "float":
                animator.SetFloat(paramName, floatValue);
                result.SetResult(new { success = true, gameObject = animator.gameObject.name, parameter = paramName, value = floatValue });
                break;
            case "int":
                animator.SetInteger(paramName, intValue);
                result.SetResult(new { success = true, gameObject = animator.gameObject.name, parameter = paramName, value = intValue });
                break;
            case "bool":
                animator.SetBool(paramName, boolValue);
                result.SetResult(new { success = true, gameObject = animator.gameObject.name, parameter = paramName, value = boolValue });
                break;
            case "trigger":
                animator.SetTrigger(paramName);
                result.SetResult(new { success = true, gameObject = animator.gameObject.name, parameter = paramName, triggered = true });
                break;
            default:
                result.SetResult(new { error = $"Unknown parameter type: {paramType}" });
                break;
        }
    }
}
```
