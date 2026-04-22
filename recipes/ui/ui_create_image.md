# ui_create_image

Create an Image UI element, optionally loading a sprite from an asset path.

**Signature:** `UICreateImage(name string = "Image", parent string = null, spritePath string = null, width float = 100, height float = 100)`

**Returns:** `{ success, name, instanceId, parent }`

**Notes:**
- `spritePath` must be a project-relative path (e.g. `Assets/Sprites/icon.png`).
- If the sprite is not found at the given path, the Image is created without a sprite (no error).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Image";
        string parent = null;
        string spritePath = null;
        float width = 100f, height = 100f;

        var parentGo = FindOrCreateCanvas(parent);
        if (parentGo == null)
        {
            result.SetResult(new { error = "Parent not found and could not create Canvas" });
            return;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parentGo.transform, false);

        var rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);

        var image = go.AddComponent<Image>();

        if (!string.IsNullOrEmpty(spritePath))
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
                image.sprite = sprite;
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Image");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name });
    }
}
```
