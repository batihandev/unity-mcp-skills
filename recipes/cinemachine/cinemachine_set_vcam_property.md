# cinemachine_set_vcam_property

Set any property on a VCam or one of its pipeline components. Also routes to `cinemachine_set_lens` when only lens shorthand params are supplied.

**Signature:** `CinemachineSetVCamProperty(string vcamName = null, int instanceId = 0, string path = null, string componentType = null, string propertyName = null, object value = null, float? fov = null, float? nearClip = null, float? farClip = null, float? orthoSize = null)`

**Returns:** `{ success, message }` or `{ error }`

**componentType values:**
- `"Main"` (or empty) — the VCam component itself
- `"Lens"` — the LensSettings struct
- Any pipeline component type name, e.g. `"OrbitalFollow"`, `"RotationComposer"`

**Notes:**
- Supports dot-notation paths for nested fields, e.g. `propertyName = "TrackerSettings.BindingMode"`.
- When `propertyName` is omitted and any of `fov`/`nearClip`/`farClip`/`orthoSize` are provided, the call is automatically forwarded to `CinemachineSetLens`.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        string componentType = "Main";  // "Main", "Lens", or e.g. "OrbitalFollow"
        string propertyName = "Priority";
        object value = 20;

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var vcam = CinemachineAdapter.GetVCam(go);
        if (CinemachineAdapter.VCamOrError(vcam) is object vcamErr) { result.SetResult(vcamErr); return; }

        WorkflowManager.SnapshotObject(go);

        // Locate target object for the given componentType
        object target = vcam;
        bool isLens = false;
        var normalized = componentType?.Trim();
        if (string.IsNullOrEmpty(normalized) ||
            normalized.Equals("Main", System.StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals(CinemachineAdapter.VCamTypeName, System.StringComparison.OrdinalIgnoreCase))
        {
            target = vcam;
        }
        else if (normalized.Equals("Lens", System.StringComparison.OrdinalIgnoreCase))
        {
            target = CinemachineAdapter.GetLens(vcam);
            isLens = true;
        }
        else
        {
            var comps = go.GetComponents<MonoBehaviour>();
            target = System.Array.Find(comps, c => c.GetType().Name.Equals(normalized, System.StringComparison.OrdinalIgnoreCase))
                  ?? System.Array.Find(comps, c => c.GetType().Name.Equals("Cinemachine" + normalized, System.StringComparison.OrdinalIgnoreCase));
        }

        if (target == null) { result.SetResult(new { error = "Component " + normalized + " not found." }); return; }

        // For Lens (struct), we need to box/unbox
        if (isLens)
        {
            object boxedLens = CinemachineAdapter.GetLens(vcam);
            // Use reflection to set field on the boxed struct, then write back
            var field = boxedLens.GetType().GetField(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(boxedLens, System.Convert.ChangeType(value, field.FieldType));
                CinemachineAdapter.SetLens(vcam, (LensSettings)boxedLens);
                result.SetResult(new { success = true, message = "Set Lens." + propertyName });
            }
            else
            {
                result.SetResult(new { error = "Property " + propertyName + " not found on Lens" });
            }
            return;
        }

        // Generic property set via reflection
        var t = target.GetType();
        var f = t.GetField(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (f != null)
        {
            f.SetValue(target, System.Convert.ChangeType(value, f.FieldType));
            if (target is UnityEngine.Object uObj) EditorUtility.SetDirty(uObj);
            result.SetResult(new { success = true, message = "Set " + t.Name + "." + propertyName });
        }
        else
        {
            result.SetResult(new { error = "Property " + propertyName + " not found on " + t.Name });
        }
    }
}
```
