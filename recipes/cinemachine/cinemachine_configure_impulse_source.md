# cinemachine_configure_impulse_source

Configure a `CinemachineImpulseSource` definition: amplitude gain, frequency gain, impact radius, signal duration, and dissipation rate. If no source is specified, the first source found in the scene is used.

**Signature:** `CinemachineConfigureImpulseSource(string sourceName = null, int sourceInstanceId = 0, string sourcePath = null, float? amplitudeGain = null, float? frequencyGain = null, float? impactRadius = null, float? duration = null, float? dissipationRate = null)`

**Returns:** `{ success, source, changes }` or `{ success, message }` (no changes) or `{ error }`

**Notes:**
- CM3 field paths: `ImpulseDefinition.AmplitudeGain`, `ImpulseDefinition.FrequencyGain`, `ImpulseDefinition.ImpactRadius`, `ImpulseDefinition.TimeEnvelope.Duration`, `ImpulseDefinition.DissipationRate`
- CM2 field paths: `m_ImpulseDefinition.m_AmplitudeGain`, etc. (same structure with `m_` prefix)
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
using System.Reflection;

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

        MonoBehaviour source = null;
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

        void TrySetNested(string propPath, object val, string label)
        {
            if (val == null) return;
            var parts = propPath.Split('.');
            object obj = source;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var f = obj.GetType().GetField(parts[i], BindingFlags.Public | BindingFlags.Instance);
                if (f == null) return;
                obj = f.GetValue(obj);
                if (obj == null) return;
            }
            var last = obj.GetType().GetField(parts[parts.Length - 1], BindingFlags.Public | BindingFlags.Instance);
            if (last != null) { last.SetValue(obj, System.Convert.ChangeType(val, last.FieldType)); changes.Add(label + "=" + val); }
        }

#if CINEMACHINE_3
        TrySetNested("ImpulseDefinition.AmplitudeGain", amplitudeGain, "amplitudeGain");
        TrySetNested("ImpulseDefinition.FrequencyGain", frequencyGain, "frequencyGain");
        TrySetNested("ImpulseDefinition.ImpactRadius", impactRadius, "impactRadius");
        TrySetNested("ImpulseDefinition.DissipationRate", dissipationRate, "dissipationRate");
#else
        TrySetNested("m_ImpulseDefinition.m_AmplitudeGain", amplitudeGain, "amplitudeGain");
        TrySetNested("m_ImpulseDefinition.m_FrequencyGain", frequencyGain, "frequencyGain");
        TrySetNested("m_ImpulseDefinition.m_ImpactRadius", impactRadius, "impactRadius");
        TrySetNested("m_ImpulseDefinition.m_DissipationRate", dissipationRate, "dissipationRate");
#endif

        EditorUtility.SetDirty(source);
        if (changes.Count == 0)
            result.SetResult(new { success = true, message = "No changes applied." });
        else
            result.SetResult(new { success = true, source = source.gameObject.name, changes = string.Join(", ", changes) });
    }
}
```
