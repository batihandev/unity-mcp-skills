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

## Paste pattern

```csharp
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
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

## Newtonsoft.Json

`execution_result.md` and `validate.md` (`RequiredJsonArray`) depend on
`Newtonsoft.Json`. The Unity package `com.unity.nuget.newtonsoft-json` provides
it and ships as a transitive dependency of `com.unity.ai.assistant`, so most
target projects already have it. If a recipe errors with "The type or namespace
name 'Newtonsoft' could not be found", add the package via the Package Manager.
