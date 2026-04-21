# project_add_tag

Add a new custom tag to the project's TagManager. Mutates `ProjectSettings/TagManager.asset`.

**Signature:** `ProjectAddTag(string tagName)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `tagName` | string | Yes | The tag name to add (case-sensitive) |

## Returns

```json
{ "success": true, "tag": "Enemy" }
```

On duplicate:
```json
{ "error": "Tag 'Enemy' already exists" }
```

## Notes

- `tagName` is required; the call returns an error object if omitted or empty.
- The tag is written directly to `TagManager.asset` via `SerializedObject`; the change is immediately visible in the editor.
- Does not support adding layers; for layers open Project Settings manually.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string tagName = "MyTag"; // replace with desired tag name

        if (string.IsNullOrEmpty(tagName))
        {
            result.SetResult(new { error = "tagName is required" });
            return;
        }

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                result.SetResult(new { error = $"Tag '{tagName}' already exists" });
                return;
            }
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();

        result.SetResult(new { success = true, tag = tagName });
    }
}
```
