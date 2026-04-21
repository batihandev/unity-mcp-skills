# navmesh_add_agent

Add a `NavMeshAgent` component to an existing GameObject. Fails if the component already exists.

**Signature:** `NavMeshAddAgent(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, gameObject }`

```csharp
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Provide at least one of: name, instanceId, or path
        string name = "Enemy";
        int instanceId = 0;
        string path = null;

        var (go, err) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        if (go.GetComponent<NavMeshAgent>() != null)
        {
            result.SetResult(new { error = $"{go.name} already has NavMeshAgent" });
            return;
        }

        Undo.AddComponent<NavMeshAgent>(go);
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);
        result.SetResult(new { success = true, gameObject = go.name });
    }
}
```
