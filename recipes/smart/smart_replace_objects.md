# smart_replace_objects

Replace selected objects with a prefab, preserving position, rotation, scale, and sibling order. Requires objects selected in Hierarchy first.

**Signature:** `SmartReplaceObjects(string prefabPath)`

**Returns:** `{ success, replaced, prefab }`

**Notes:**
- Each selected object is destroyed and replaced with an instantiated prefab
- New objects are selected after replacement
- Undoable
- `prefabPath` must be a valid `Assets/...` path

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string prefabPath = "Assets/Prefabs/MyPrefab.prefab";

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            result.SetResult(new { error = $"Prefab not found: {prefabPath}" });
            return;
        }

        var selected = Selection.gameObjects.ToArray();
        if (selected.Length == 0)
        {
            result.SetResult(new { error = "No objects selected" });
            return;
        }

        var newObjects = new List<GameObject>();
        foreach (var go in selected)
        {
            var newGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            newGo.transform.SetParent(go.transform.parent);
            newGo.transform.position = go.transform.position;
            newGo.transform.rotation = go.transform.rotation;
            newGo.transform.localScale = go.transform.localScale;
            newGo.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
            Undo.RegisterCreatedObjectUndo(newGo, "Replace Object");
            Undo.DestroyObjectImmediate(go);
            newObjects.Add(newGo);
        }

        Selection.objects = newObjects.ToArray();
        result.SetResult(new { success = true, replaced = selected.Length, prefab = prefabPath });
    }
}
```
