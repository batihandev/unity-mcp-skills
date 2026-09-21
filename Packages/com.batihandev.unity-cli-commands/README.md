# Unity CLI Commands

This optional Unity package adds reusable Editor commands for scenes, assets, components,
ScriptableObjects, console settings, and profiler analysis. Some recipes in `unity-cli-skills` use them.
It is optional for workflows that use only Unity's built-in commands.

Installing the agent skill does not install this package. Install it once in each project that needs it.
It runs only in the Editor and does not ship in your game.

## Prerequisites

Use **Unity 6000.6.2f1**, **Unity CLI 1.0.0-beta.10**, **Pipeline 0.7.0-exp.1**, and
**Input System 1.20.0**. This preview package checks the exact Editor/Pipeline/Input versions.
For initial CLI and skill setup, start with the [main README](../../README.md).

## Install

1. Open **`Packages/manifest.json`** in your Unity project's folder. It lists the project's packages.
2. Add or update these three entries **inside the existing `dependencies` object**. Keep all other
   entries. Separate entries with commas; do not add a comma after the last entry.

   ```json
   "com.unity.pipeline": "0.7.0-exp.1",
   "com.unity.inputsystem": "1.20.0",
   "com.batihandev.unity-cli-commands": "https://github.com/batihandev/unity-mcp-skills.git?path=/Packages/com.batihandev.unity-cli-commands#0f9df79a66a7fc5811d2e695cbd58fc1264fd58f"
   ```

   The commit ID pins the tested WIP revision. Listing Pipeline and Input System separately keeps them
   installed if you later remove this command package. Git must be installed and available to Unity
   so it can download the package.
3. Save the file, open the project in Unity, and wait for package installation and compilation.
4. Leave Unity open and run the check below. Replace `PROJECT_PATH` with the full project folder path
   (the folder containing `Assets`, `Packages`, and `ProjectSettings`). Keep the quotes.

   ```sh
   unity --json command unity_cli_commands_smoke --project-path "PROJECT_PATH"
   ```

   Look for `success: true`, then `Ok: true` and `Status: "ready"` inside the result.
   A compatibility error means the required versions do not match. From WSL, use the Windows project
   path and Windows Unity CLI.

After this check, your agent can discover and use the package commands. The JSON response format and
command safety rules are documented in the [agent reference](../../unity-cli-skills/references/domains/foundation.md).

## Verify the package tests

Run these when validating a first installation or changing the package revision. They are not a step
for every agent session.

Add the following property at the **top level** of `Packages/manifest.json`, alongside `dependencies`:

```json
"testables": ["com.batihandev.unity-cli-commands"]
```

If `testables` already exists, add the package name to that array, preserving its other entries.
Save your scene and close the project's Editor before running:

```sh
unity --json test "PROJECT_PATH" --editor-version 6000.6.2f1 --mode EditMode --output "package-tests.xml" --timeout 600
```

Replace `PROJECT_PATH` as above. The command writes `package-tests.xml`; verify that it includes the
package tests and has a nonzero test count with no failures, skipped, or inconclusive tests.
Reopen Unity afterward. A successful compilation alone does not verify the commands.

## Update or remove

**Update:** replace the commit ID after `#` in the package URL with a reviewed WIP revision.
Let Unity resolve it, then repeat the connection check and package tests.

**Remove:** delete the `com.batihandev.unity-cli-commands` entry from `dependencies` and, if added,
from `testables`. Keep other entries. Let Unity finish resolving the change. Its commands should
be absent; Pipeline's built-in commands should remain available with the recommended setup above.
The package creates no project assets that require cleanup.

<details>
<summary>Advanced: projects that only declare this package</summary>

The package also declares Pipeline and Input System as dependencies, so this arrangement is supported.
However, removing it may also remove those dependencies and end CLI access to Editor commands.
List them directly as shown in the recommended installation if you want them to remain installed.

</details>
