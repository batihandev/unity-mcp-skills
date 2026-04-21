# scriptableobject_get

Get properties of a ScriptableObject asset.

**Signature:** `ScriptableObjectGet(string assetPath)`

**Returns:** `{ path, typeName, fields, properties }`

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Data/MyAsset.asset"; // Asset path of the ScriptableObject

        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (asset == null)
        {
            result.SetResult(new { error = $"ScriptableObject not found: {assetPath}" });
            return;
        }

        var type = asset.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(f => new { name = f.Name, type = f.FieldType.Name, value = f.GetValue(asset)?.ToString() })
            .ToArray();

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !p.GetIndexParameters().Any())
            .Select(p =>
            {
                try { return new { name = p.Name, type = p.PropertyType.Name, value = p.GetValue(asset)?.ToString() }; }
                catch { return new { name = p.Name, type = p.PropertyType.Name, value = "(error)" }; }
            })
            .ToArray();

        result.SetResult(new
        {
            path = assetPath,
            typeName = type.Name,
            fields,
            properties = props
        });
    }
}
```
