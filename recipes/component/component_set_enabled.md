# component_set_enabled

Enable or disable a component. Works on Behaviour (MonoBehaviour, Animator, etc.), Renderer, and Collider types.

**Signature:** `ComponentSetEnabled(string name = null, int instanceId = 0, string path = null, string componentType = null, bool enabled = true)`

## Returns

```json
{
  "success": true,
  "gameObject": "Player",
  "componentType": "AudioSource",
  "enabled": false
}
```

If the component type does not support `enabled`:
```json
{ "error": "Rigidbody does not have an enabled property" }
```

## Notes

- Only Behaviour, Renderer, and Collider types have an `enabled` flag. Components like `Rigidbody` or `Transform` do not — use their own properties instead.
- Use this instead of `component_set_property` with `propertyName: "enabled"` — this path is safer and uses explicit type checks.
- Uses `Undo.RecordObject` — operation is undoable.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`component_type_finder`](../_shared/component_type_finder.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null; int instanceId = 0; string path = null;
        string componentType = "AudioSource";
        bool enabled = true;

        if (Validate.Required(componentType, "componentType") is object err) { result.SetResult(err); return; }
        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var type = ComponentSkills.FindComponentType(componentType);
        if (type == null) { result.SetResult(new { error = $"Component type not found: {componentType}" }); return; }

        var comp = go.GetComponent(type);
        if (comp == null) { result.SetResult(new { error = $"No {componentType} on {go.name}" }); return; }

        Undo.RecordObject(comp, "Set Component Enabled");
        if (comp is Behaviour behaviour) behaviour.enabled = enabled;
        else if (comp is Renderer renderer) renderer.enabled = enabled;
        else if (comp is Collider collider) collider.enabled = enabled;
        else { result.SetResult(new { error = $"{componentType} does not have an enabled property" }); return; }

        result.SetResult(new { success = true, gameObject = go.name, componentType, enabled });
    }
}
```
