# Unity CLI Commands

An optional Editor-only package for scene, asset, component, ScriptableObject, console, and profiler
commands used by `unity-cli-skills`. Install it per project when you need those recipes. Workflows
using only Unity’s built-in commands do not require it. It is excluded from game builds.

Agent skills and Unity packages are installed separately; neither Unity’s official skill nor this
repository’s skill installs this package.

## Prerequisites

Use **Unity 6000.6.2f1**, **Unity CLI 1.0.0-beta.10**, **Pipeline 0.7.0-exp.1**, and
**Input System 1.20.0**. This preview package checks the exact Editor/Pipeline/Input versions.
Git must be available to Unity for package downloads. For CLI and agent skill setup,
start with the [main README](../../README.md).

## Install

1. Open the project's package manifest, **`Packages/manifest.json`**.
2. Merge these entries into its existing `dependencies` object, preserving other packages and valid JSON:

   ```json
   "com.unity.pipeline": "0.7.0-exp.1",
   "com.unity.inputsystem": "1.20.0",
   "com.batihandev.unity-cli-commands": "https://github.com/batihandev/unity-mcp-skills.git?path=/Packages/com.batihandev.unity-cli-commands#0f9df79a66a7fc5811d2e695cbd58fc1264fd58f"
   ```

   The URL pins the tested package revision independently of your skill checkout. Keep Pipeline and
   Input System as direct dependencies so removing this package leaves built-in CLI commands available.
3. Save the file, open the project in Unity, and wait for package installation and compilation.
4. Leave Unity open and run the check below. Replace `PROJECT_PATH` with the full project folder path
   (the folder containing `Assets`, `Packages`, and `ProjectSettings`). Keep the quotes.

   ```sh
   unity --json command unity_cli_commands_smoke --project-path "PROJECT_PATH"
   ```

   Look for `success: true`, then `Ok: true` and `Status: "ready"` inside the result.
   A compatibility error means the required versions do not match. From WSL, use the Windows project
   path and Windows Unity CLI.

Response formats and command safety rules are documented in the [agent reference](../../unity-cli-skills/references/domains/foundation.md).

## Verify the package tests

Run the tests after the first installation and when changing the package revision.

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
