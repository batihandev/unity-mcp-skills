# animator_get_parameters

List all parameters defined in an Animator Controller asset. Returns name, type string, and all default values for each parameter.

**Signature:** `AnimatorGetParameters(string controllerPath)`

**Returns:** `{ controller, parameters: [{ name, type, defaultFloat, defaultInt, defaultBool }] }`

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

        var pathErr = Validate.SafePath(controllerPath, "controllerPath");
        if (pathErr != null) { result.SetResult(pathErr); return; }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            result.SetResult(new { error = $"Controller not found: {controllerPath}" });
            return;
        }

        var parameters = controller.parameters.Select(p => new
        {
            name = p.name,
            type = p.type.ToString(),
            defaultFloat = p.defaultFloat,
            defaultInt = p.defaultInt,
            defaultBool = p.defaultBool
        }).ToArray();

        result.SetResult(new { controller = controllerPath, parameters });
    }
}
```
