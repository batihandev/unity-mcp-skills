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

Cross-domain C# helpers live in [`_shared/`](./_shared/README.md) — `gameobject_finder`, `skills_common`, `validate`. Many recipes embed these; do not duplicate the helper code.

## Golden Template

```csharp
using UnityEngine;
using UnityEditor;
internal class CommandScript : IRunCommand {
    public void Execute(ExecutionResult result) {
        // logic
    }
}
```
