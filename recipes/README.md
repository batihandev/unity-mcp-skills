# Unity RunCommand Recipes

These markdown files contain raw C# templates designed to be executed via `Unity_RunCommand`.

## Directory Structure

```
recipes/<topic>/README.md
recipes/<topic>/<command>.md
```

Each `<command>.md` file corresponds to exactly one skill command. If the skill command is `gameobject_create`, the recipe path is `recipes/gameobject/gameobject_create.md`.

- command filenames must match skill command IDs exactly
- Recipe path rule: `../../recipes/<topic>/<command>.md`

## Domains

The canonical list of recipe domains is the **Domain Skill Map** in the root [`../SKILL.md`](../SKILL.md). Each `<domain>` listed there has a matching `recipes/<domain>/` directory.

## Shared Helpers

Cross-domain C# helpers live in [`_shared/`](./_shared/README.md):

- `validate.md` — parameter validation (`Validate.Required` / `.SafePath` / `.InRange` / `.RequiredJsonArray`)
- `execution_result.md` — `result.SetResult(object)` extension that serializes with `Newtonsoft.Json`
- `workflow_manager.md` — `WorkflowManager.Snapshot*` undo registration shim
- `gameobject_finder.md` — `FindHelper.FindAll<T>` + `GameObjectFinder` cached lookups
- `skills_common.md` — `SkillsCommon.Utf8NoBom` / `GetAllLoadedTypes` / `GetTriangleCount`
- `component_type_finder.md` — `ComponentSkills.FindComponentType` (Unity component type lookup by name)
- `value_converter.md` — `ComponentSkills.ConvertValue` (string → Vector/Color/enum/etc.)

A recipe that uses any of these declares the dependency in a `## Prerequisites`
section immediately before its `csharp` code block. The agent reads that section,
pastes the helper class into the same `Unity_RunCommand` code block as
`CommandScript`, then executes. Do not duplicate helper code inline.

## Golden Template

```csharp
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

internal class CommandScript : IRunCommand {
    public void Execute(ExecutionResult result) {
        // logic
        result.SetResult(new { success = true });
    }
}

// ... paste each Prerequisite helper class below, inside this same code block ...
```
