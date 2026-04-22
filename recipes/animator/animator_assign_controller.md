# animator_assign_controller

Assign an Animator Controller to a GameObject. If the GameObject has no Animator component, one is added automatically via Undo. Resolves the GameObject using name, instanceId, or path.

**Signature:** `AnimatorAssignController(string name = null, int instanceId = 0, string path = null, string controllerPath = null)`

**Returns:** `{ success, gameObject, controller }`

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

        string controllerPath = "Assets/Animations/MyController.controller"; // Required

        if (Validate.Required(controllerPath, "controllerPath") is object err2) { result.SetResult(err2); return; }
        var pathErr = Validate.SafePath(controllerPath, "controllerPath");
        if (pathErr != null) { result.SetResult(pathErr); return; }

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var animator = go.GetComponent<Animator>();
        if (animator == null)
        {
            animator = Undo.AddComponent<Animator>(go);
            WorkflowManager.SnapshotCreatedComponent(animator);
        }

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller == null)
        {
            result.SetResult(new { error = $"Controller not found: {controllerPath}" });
            return;
        }

        Undo.RecordObject(animator, "Assign Animator Controller");
        animator.runtimeAnimatorController = controller;

        result.SetResult(new { success = true, gameObject = go.name, controller = controllerPath });
    }
}
```
