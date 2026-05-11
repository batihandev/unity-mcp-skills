# Shared Recipe Helpers

Paste-in C# helper classes used across many domain recipes. A recipe that
depends on one of these files declares it in its `## Prerequisites` section;
the agent then concatenates the helper class into the same `Unity_RunCommand`
code block as `CommandScript`. None of these are shipped as asmdefs — the repo
stays documentation-only.

## Files

| File | Exposes | Used by recipes that call |
|---|---|---|
| [`validate.md`](./validate.md) | `public static class Validate` | `Validate.Required`, `Validate.SafePath`, `Validate.InRange`, `Validate.RequiredJsonArray` |
| [`execution_result.md`](./execution_result.md) | `internal static class ExecutionResultExtensions` | `result.SetResult(new { ... })` |
| [`workflow_manager.md`](./workflow_manager.md) | `internal enum SnapshotType` + `internal static class WorkflowManager` | `WorkflowManager.SnapshotObject`, `.SnapshotCreatedAsset`, `.SnapshotCreatedComponent`, `.SnapshotCreatedGameObject`, `.IsRecording` |
| [`gameobject_finder.md`](./gameobject_finder.md) | `internal static class FindHelper` + `public static class GameObjectFinder` | `FindHelper.FindAll<T>`, `GameObjectFinder.Find`, `.FindByPath`, `.FindOrError`, `.FindComponentOrError`, `.GetPath`, `.GetCachedPath`, `.GetDepth`, `.GetSceneObjects` |
| [`skills_common.md`](./skills_common.md) | `public static class SkillsCommon` | `SkillsCommon.Utf8NoBom`, `.GetAllLoadedTypes`, `.GetTriangleCount` |
| [`component_type_finder.md`](./component_type_finder.md) | `internal static class ComponentSkills` (type-lookup surface only) | `ComponentSkills.FindComponentType` |
| [`value_converter.md`](./value_converter.md) | `internal static class ComponentSkills` (value-parsing surface only) | `ComponentSkills.ConvertValue` |
| [`project_skills.md`](./project_skills.md) | `internal static class ProjectSkills` | `ProjectSkills.DetectRenderPipeline`, `.GetDefaultShaderName`, `.GetUnlitShaderName`, `.GetColorPropertyName`, `.GetMainTexturePropertyName` |
| [`perception_helpers.md`](./perception_helpers.md) | `internal static class PerceptionHelpers` + `_SceneMetricsSnapshot` | `PerceptionHelpers.*`, `_SceneMetricsSnapshot` (perception domain recipes) |

> `component_type_finder.md` and `value_converter.md` both declare
> `internal static class ComponentSkills`. A recipe must paste **at most one**
> of them per `Unity_RunCommand` call. No current recipe needs both; if that
> changes, merge the two files at that time.

## Missing prerequisite symptoms

If `Unity_RunCommand` fails with `CS0103` or `CS7036` on `SetResult`, `Validate`, `FindHelper`, `WorkflowManager`, `SkillsCommon`, or `ComponentSkills` — you missed pasting one or more prerequisite helpers. Re-read the recipe's `**Prerequisites:**` line and paste every listed `_shared/*.md` `## Paste-in` block into the same code block as `CommandScript`.

## Paste pattern

```csharp
using UnityEngine;
using UnityEditor;
// ... any other usings the recipe needs

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // ... recipe body ...
        result.SetResult(new { success = true });
    }
}

// Paste the contents of each file listed in the recipe's ## Prerequisites
// section *after* CommandScript, inside the same code block.
internal static class Validate { /* ... contents of validate.md ... */ }
internal static class ExecutionResultExtensions { /* ... contents of execution_result.md ... */ }
// etc.
```
