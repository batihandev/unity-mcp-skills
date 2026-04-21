# gameobject_get_info

Get detailed information about a GameObject.

**Signature:** `GameObjectGetInfo(string name = null, int instanceId = 0, string path = null)`

**Returns:**
```
{
  name, instanceId, path, tag, layer,
  isActive,          // bool — note: key is "isActive", not "active"
  position: { x, y, z },
  rotation: { x, y, z },   // euler angles
  scale: { x, y, z },
  parent,            // parent name or null
  parentPath,        // parent hierarchy path or null
  childCount,
  children: [{ name, instanceId, path }],
  components: string[]
}
```

## Notes

- At least one identifier (`name`, `instanceId`, or `path`) is required.
- The active state field is named `isActive` (not `active`).
- `components` lists only the component type names for each component on the object.
- `children` lists only direct children (not recursive descendants).

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;       // provide at least one identifier
        int instanceId = 0;
        string path = null;

        /* Original Logic:

            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var componentBuffer = new List<Component>(8);
            go.GetComponents(componentBuffer);
            var components = new List<string>(componentBuffer.Count);
            foreach (var component in componentBuffer)
            {
                if (component != null)
                    components.Add(component.GetType().Name);
            }

            var children = new List<object>(go.transform.childCount);
            foreach (Transform child in go.transform)
            {
                children.Add(new
                {
                    name = child.name,
                    instanceId = child.gameObject.GetInstanceID(),
                    path = GameObjectFinder.GetCachedPath(child.gameObject)
                });
            }

            return new
            {
                name = go.name,
                instanceId = go.GetInstanceID(),
                path = GameObjectFinder.GetCachedPath(go),
                tag = go.tag,
                layer = LayerMask.LayerToName(go.layer),
                isActive = go.activeSelf,
                position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                rotation = new { x = go.transform.eulerAngles.x, y = go.transform.eulerAngles.y, z = go.transform.eulerAngles.z },
                scale = new { x = go.transform.localScale.x, y = go.transform.localScale.y, z = go.transform.localScale.z },
                parent = go.transform.parent?.name,
                parentPath = go.transform.parent != null ? GameObjectFinder.GetCachedPath(go.transform.parent.gameObject) : null,
                childCount = go.transform.childCount,
                children,
                components = components.ToArray()
            };
        */
    }
}
```
