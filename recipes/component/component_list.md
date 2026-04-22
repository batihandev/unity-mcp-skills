# component_list

List all components on a GameObject. Optionally includes key property summaries for each component.

**Signature:** `ComponentList(string name = null, int instanceId = 0, string path = null, bool includeProperties = false)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | No* | null | GameObject name |
| `instanceId` | int | No* | 0 | Instance ID |
| `path` | string | No* | null | Hierarchy path |
| `includeProperties` | bool | No | false | Include key property summaries per component |

*At least one object identifier required.

## Returns

```json
{
  "gameObject": "Player",
  "instanceId": 12345,
  "path": "Scene/Player",
  "componentCount": 3,
  "components": [
    { "type": "Transform", "fullType": "UnityEngine.Transform", "enabled": true },
    { "type": "Rigidbody", "fullType": "UnityEngine.Rigidbody", "enabled": true },
    { "type": "BoxCollider", "fullType": "UnityEngine.BoxCollider", "enabled": true }
  ]
}
```

With `includeProperties: true`, components that have key properties (Transform, RectTransform, Camera) include a `keyProperties` map:

```json
{
  "type": "Transform",
  "fullType": "UnityEngine.Transform",
  "enabled": true,
  "keyProperties": {
    "position": "(0, 1, 0)",
    "rotation": "(0, 0, 0)",
    "scale": "(1, 1, 1)"
  }
}
```

## Notes

- `enabled` reflects `Behaviour.enabled`; non-Behaviour components always report `true`.
- Use this command to discover component type names before calling `component_set_property` or `component_get_properties`.
- `path` in the response is the full hierarchy path as returned by `GameObjectFinder.GetPath`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null; int instanceId = 0; string path = null;
        bool includeProperties = false;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var components = go.GetComponents<Component>()
            .Where(c => c != null)
            .Select(c => {
                var info = new Dictionary<string, object>
                {
                    { "type", c.GetType().Name },
                    { "fullType", c.GetType().FullName },
                    { "enabled", (object)((c as Behaviour)?.enabled ?? true) }
                };

                if (includeProperties)
                {
                    var props = GetKeyProperties(c);
                    if (props.Count > 0)
                        info["keyProperties"] = props;
                }

                return (object)info;
            })
            .ToArray();

        result.SetResult(new {
            gameObject = go.name,
            instanceId = go.GetInstanceID(),
            path = GameObjectFinder.GetPath(go),
            componentCount = components.Length,
            components
        });
    }

    private static Dictionary<string, string> GetKeyProperties(Component c)
    {
        var d = new Dictionary<string, string>();
        if (c is Transform t)
        {
            d["position"] = $"({t.position.x}, {t.position.y}, {t.position.z})";
            d["rotation"] = $"({t.eulerAngles.x}, {t.eulerAngles.y}, {t.eulerAngles.z})";
            d["scale"] = $"({t.localScale.x}, {t.localScale.y}, {t.localScale.z})";
        }
        else if (c is Camera cam)
        {
            d["fieldOfView"] = cam.fieldOfView.ToString();
            d["orthographic"] = cam.orthographic.ToString();
        }
        return d;
    }
}
```
