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
