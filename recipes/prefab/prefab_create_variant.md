# prefab_create_variant

Create a prefab variant from an existing prefab. The variant inherits the source prefab and can override individual properties.

**Signature:** `PrefabCreateVariant(string sourcePrefabPath, string variantPath)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sourcePrefabPath` | string | Yes | Path to the source prefab asset |
| `variantPath` | string | Yes | Save path for the new variant (must end in `.prefab`) |

## Returns

```json
{
  "success": true,
  "sourcePath": "Assets/Prefabs/Enemy.prefab",
  "variantPath": "Assets/Prefabs/EnemyElite.prefab",
  "name": "EnemyElite"
}
```

## Notes

- Internally instantiates the source prefab temporarily, saves it as a connected variant, then destroys the temp instance.
- Intermediate directories are created automatically.
- The variant maintains a parent-child relationship with the source — changes to the source propagate unless overridden.
- Workflow snapshot records the created variant asset.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (Validate.Required(sourcePrefabPath, "sourcePrefabPath") is object err) { result.SetResult(err); return; }
        if (Validate.SafePath(variantPath, "variantPath") is object pathErr) { result.SetResult(pathErr); return; }

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
        if (source == null) { result.SetResult(new { error = $"Prefab not found: {sourcePrefabPath}" }); return; }

        var dir = Path.GetDirectoryName(variantPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        var variant = PrefabUtility.SaveAsPrefabAssetAndConnect(
            instance, variantPath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(instance);

        { result.SetResult(new { success = true, sourcePath = sourcePrefabPath, variantPath, name = variant.name }); return; }
    }
}
```
