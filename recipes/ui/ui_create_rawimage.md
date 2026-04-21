# ui_create_rawimage

Create a RawImage UI element for displaying a Texture2D or RenderTexture.

**Signature:** `UICreateRawImage(name string = "RawImage", parent string = null, texturePath string = null, width float = 100, height float = 100)`

**Returns:** `{ success, name, instanceId, parent, hasTexture }`

**Notes:**
- `texturePath` must be a project-relative path (e.g. `Assets/Textures/map.png`).
- Unlike `Image`, `RawImage` accepts any `Texture` (not just `Sprite`), including `RenderTexture`.
- `hasTexture` in the response indicates whether the texture was successfully loaded.

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "RawImage";
        string parent = null;
        string texturePath = null;
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

        var rawImage = go.AddComponent<RawImage>();

        if (!string.IsNullOrEmpty(texturePath))
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (texture != null)
                rawImage.texture = texture;
        }

        Undo.RegisterCreatedObjectUndo(go, "Create RawImage");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name, hasTexture = rawImage.texture != null });
    }
}
```
