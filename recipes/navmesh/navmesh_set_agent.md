# navmesh_set_agent

Set `NavMeshAgent` properties on an existing agent. Pass only the parameters you want to change; omitted parameters are left unchanged.

**Signature:** `NavMeshSetAgent(string name = null, int instanceId = 0, string path = null, float? speed = null, float? acceleration = null, float? angularSpeed = null, float? radius = null, float? height = null, float? stoppingDistance = null)`

**Returns:** `{ success, gameObject, speed, radius }`

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

        // Only set the values you want to change; leave others null
        float? speed = 3.5f;
        float? acceleration = null;
        float? angularSpeed = null;
        float? radius = null;
        float? height = null;
        float? stoppingDistance = 1f;

        var (go, err) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var agent = go.GetComponent<NavMeshAgent>();
        if (agent == null) { result.SetResult(new { error = $"No NavMeshAgent on {go.name}" }); return; }

        WorkflowManager.SnapshotObject(agent);
        Undo.RecordObject(agent, "Set NavMeshAgent");

        if (speed.HasValue) agent.speed = speed.Value;
        if (acceleration.HasValue) agent.acceleration = acceleration.Value;
        if (angularSpeed.HasValue) agent.angularSpeed = angularSpeed.Value;
        if (radius.HasValue) agent.radius = radius.Value;
        if (height.HasValue) agent.height = height.Value;
        if (stoppingDistance.HasValue) agent.stoppingDistance = stoppingDistance.Value;

        result.SetResult(new { success = true, gameObject = go.name, speed = agent.speed, radius = agent.radius });
    }
}
```
