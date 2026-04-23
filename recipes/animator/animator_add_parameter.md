# animator_add_parameter

Add a parameter to an existing Animator Controller. Supports float, int, bool, and trigger types with optional default values.

**Note:** Default values are written using index-based mutation to avoid the C# value-type copy bug when modifying struct arrays.

**Signature:** `AnimatorAddParameter(string controllerPath, string paramName, string paramType = "float", float defaultFloat = 0, int defaultInt = 0, bool defaultBool = false)`

**Returns:** `{ success, controller, parameter, type }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string controllerPath = "Assets/Animations/MyController.controller";
        string paramName = "Speed"; // Parameter name
        string paramType = "float"; // float | int | bool | trigger
        float defaultFloat = 0f;
        int defaultInt = 0;
        bool defaultBool = false;

        var pathErr = Validate.SafePath(controllerPath, "controllerPath");
        if (pathErr != null) { result.SetResult(pathErr); return; }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            result.SetResult(new { error = $"Controller not found: {controllerPath}" });
            return;
        }

        AnimatorControllerParameterType type;
        switch (paramType.ToLower())
        {
            case "float":   type = AnimatorControllerParameterType.Float;   break;
            case "int":     type = AnimatorControllerParameterType.Int;     break;
            case "bool":    type = AnimatorControllerParameterType.Bool;    break;
            case "trigger": type = AnimatorControllerParameterType.Trigger; break;
            default:
                result.SetResult(new { error = $"Unknown parameter type: {paramType}. Use: float, int, bool, trigger" });
                return;
        }

        WorkflowManager.SnapshotObject(controller);
        controller.AddParameter(paramName, type);

        // Set default value — use index to modify struct in-place (value type copy bug fix)
        var parameters = controller.parameters;
        int idx = System.Array.FindIndex(parameters, p => p.name == paramName);
        if (idx >= 0)
        {
            switch (type)
            {
                case AnimatorControllerParameterType.Float:
                    parameters[idx].defaultFloat = defaultFloat;
                    break;
                case AnimatorControllerParameterType.Int:
                    parameters[idx].defaultInt = defaultInt;
                    break;
                case AnimatorControllerParameterType.Bool:
                    parameters[idx].defaultBool = defaultBool;
                    break;
                default: break;
            }
            controller.parameters = parameters;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, controller = controllerPath, parameter = paramName, type = paramType });
    }
}
```
