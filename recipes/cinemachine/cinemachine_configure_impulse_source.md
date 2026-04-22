# cinemachine_configure_impulse_source

Configure a `CinemachineImpulseSource` definition: amplitude gain, frequency gain, impact radius, signal duration, and dissipation rate. If no source is specified, the first source found in the scene is used.

**Signature:** `CinemachineConfigureImpulseSource(string sourceName = null, int sourceInstanceId = 0, string sourcePath = null, float? amplitudeGain = null, float? frequencyGain = null, float? impactRadius = null, float? duration = null, float? dissipationRate = null)`

**Returns:** `{ success, source, changes }` or `{ success, message }` (no changes) or `{ error }`

**Notes:**
- CM3 field paths: `ImpulseDefinition.AmplitudeGain`, `ImpulseDefinition.FrequencyGain`, `ImpulseDefinition.ImpactRadius`, `ImpulseDefinition.TimeEnvelope.Duration`, `ImpulseDefinition.DissipationRate`.
- To trigger the impulse at runtime, use `cinemachine_impulse_generate`.
- Receiving cameras need a `CinemachineImpulseListener` extension (added via `cinemachine_add_extension`).

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sourceName = null;  // null = find first in scene
        int sourceInstanceId = 0;
        string sourcePath = null;
        float? amplitudeGain = 1f;
        float? frequencyGain = 1f;
        float? impactRadius = 5f;
        float? duration = 0.5f;
        float? dissipationRate = 0.25f;

        CinemachineImpulseSource source = null;
        if (!string.IsNullOrEmpty(sourceName) || sourceInstanceId != 0 || !string.IsNullOrEmpty(sourcePath))
        {
            var (go, err) = GameObjectFinder.FindOrError(sourceName, sourceInstanceId, sourcePath);
            if (err != null) { result.SetResult(err); return; }
            source = go.GetComponent<CinemachineImpulseSource>();
        }
        else
        {
            var sources = FindHelper.FindAll<CinemachineImpulseSource>();
            source = sources.Length > 0 ? sources[0] : null;
        }
        if (source == null) { result.SetResult(new { error = "No CinemachineImpulseSource found." }); return; }

        WorkflowManager.SnapshotObject(source.gameObject);
        Undo.RecordObject(source, "Configure Impulse Source");
        var changes = new List<string>();

        // NOTE: BindingFlags.Public|Instance trips the Unity_RunCommand reformatter NRE.
        // Use parameterless GetFields()/GetField(name) instead.
        System.Reflection.FieldInfo FindField(System.Type t, string name)
            => t.GetFields().FirstOrDefault(f => f.Name == name);

        void TrySetNested(string propPath, object val, string label)
        {
            if (val == null) return;
            var parts = propPath.Split('.');
            object obj = source;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var f = FindField(obj.GetType(), parts[i]);
                if (f == null) return;
                obj = f.GetValue(obj);
                if (obj == null) return;
            }
            var last = FindField(obj.GetType(), parts[parts.Length - 1]);
            if (last != null) { last.SetValue(obj, System.Convert.ChangeType(val, last.FieldType)); changes.Add(label + "=" + val); }
        }

        TrySetNested("ImpulseDefinition.AmplitudeGain", amplitudeGain, "amplitudeGain");
        TrySetNested("ImpulseDefinition.FrequencyGain", frequencyGain, "frequencyGain");
        TrySetNested("ImpulseDefinition.ImpactRadius", impactRadius, "impactRadius");
        TrySetNested("ImpulseDefinition.DissipationRate", dissipationRate, "dissipationRate");

        EditorUtility.SetDirty(source);
        if (changes.Count == 0)
            result.SetResult(new { success = true, message = "No changes applied." });
        else
            result.SetResult(new { success = true, source = source.gameObject.name, changes = string.Join(", ", changes) });
    }
}
```
