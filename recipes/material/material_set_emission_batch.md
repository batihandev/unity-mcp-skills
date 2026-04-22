# material_set_emission_batch

Set emission color on multiple objects in a single call (efficient batch operation).

**Signature:** `MaterialSetEmissionBatch(string items)`

**`items`:** JSON array of `{ name?, instanceId?, path?, r?, g?, b?, intensity?, enableEmission? }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, emissionEnabled }] }`

## Notes

- Each item delegates to `material_set_emission` internally; all the same rules apply (HDR, keyword enable/disable).
- `intensity` defaults to `1.0` for items that omit it (if `<= 0`, falls back to `1.0`).
- `enableEmission` defaults to `true`.
- Prefer this over calling `material_set_emission` repeatedly when operating on 2+ objects.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchEmissionItem
{
    public string name;
    public int instanceId;
    public string path;
    public float r = 1f, g = 1f, b = 1f;
    public float intensity = 1f;
    public bool enableEmission = true;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchEmissionItem { name = "Lantern1", r = 1.0f, g = 0.8f, b = 0.2f, intensity = 3.0f },
            new _BatchEmissionItem { name = "Lantern2", r = 1.0f, g = 0.8f, b = 0.2f, intensity = 3.0f },
            new _BatchEmissionItem { name = "Screen", r = 0.2f, g = 0.8f, b = 1.0f, intensity = 2.0f },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            Material material = null;
            GameObject go = null;

            if (!string.IsNullOrEmpty(item.path) && item.path.EndsWith(".mat"))
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(item.path);
            }
            else
            {
                var (foundGo, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
                if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }
                go = foundGo;
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null) { results.Add(new { target, success = false, error = "No Renderer" }); failCount++; continue; }
                material = renderer.sharedMaterial;
            }

            if (material == null) { results.Add(new { target, success = false, error = "No material" }); failCount++; continue; }

            WorkflowManager.SnapshotObject(material);
            Undo.RecordObject(material, "Batch Set Emission");

            float intensity = item.intensity > 0 ? item.intensity : 1f;
            var hdrColor = new Color(item.r * intensity, item.g * intensity, item.b * intensity, 1f);

            string emissionProperty = null;
            foreach (var prop in new[] { "_EmissionColor", "_Emission" })
            {
                if (material.HasProperty(prop))
                {
                    material.SetColor(prop, hdrColor);
                    emissionProperty = prop;
                    break;
                }
            }

            if (emissionProperty == null)
            { results.Add(new { target, success = false, error = "Material does not support emission (shader: " + material.shader.name + ")" }); failCount++; continue; }

            bool on = item.enableEmission && intensity > 0;
            if (on)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            if (go == null) EditorUtility.SetDirty(material);

            results.Add(new { target = go?.name ?? item.path, success = true, emissionEnabled = on });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
