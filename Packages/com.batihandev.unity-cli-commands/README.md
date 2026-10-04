# Unity CLI Commands

An optional Editor-only package for scene, asset, component, ScriptableObject, lighting, camera,
Animator runtime, UnityEvent listener, physics layer collision, NavMesh surface and area-cost, global shader keyword, console, and profiler commands used by `unity-cli-skills`. It includes Sprite Atlas V2 creation, membership
and packing plus focused Scene View framing owners. Install it per project when you need those
recipes. Workflows using only Unity’s built-in commands do not require it. It is excluded from game
builds.

Timeline binding, source-free clip, track removal, fixed duration, and Director transport commands are available when `com.unity.timeline` 6.6.x is installed. See the [Timeline guide](../../unity-cli-skills/references/domains/timeline.md) for native composition and exact command boundaries.

Cinemachine stage replacement, target-group membership, and transient impulse commands are available when `com.unity.cinemachine` 6.6.x is installed. See the [Cinemachine guide](../../unity-cli-skills/references/domains/cinemachine.md) for native composition and exact command boundaries.

ProBuilder shape creation, inspection, topology and vertex editing, face materials, UV box projection, and mesh combination are available when `com.unity.probuilder` 6.1.2 through 6.x is installed. See the [ProBuilder guide](../../unity-cli-skills/references/domains/probuilder.md) for exact targets, confirmation, Undo, and scene-save steps.

XR scene authoring commands are available with XRI 3.6.1 through 3.x and XR Core Utils 2.6 through 2.x. The XR assembly is optional and the core package has no XR reference. See the [XR guide](../../unity-cli-skills/references/domains/xr.md) for exact scene targets, input/provider prerequisites, locomotion, tracked UI, event feedback, layers, Undo and persistence.

NavMesh surface commands are available when the project has `com.unity.ai.navigation` 2.x.
The package's other commands do not require AI Navigation. The surface build produces
session NavMesh data and does not save a NavMesh asset; surface data removal unregisters
the runtime instance while retaining the surface's data reference.

Agent skills and Unity packages are installed separately; neither Unity’s official skill nor this
repository’s skill installs this package.

## Prerequisites

Use **Unity 6000.6.2f1**, **Unity CLI 1.0.0-beta.12**, **Pipeline 0.8.0-exp.1**, and
**Input System 1.20.0**. This package checks the exact Editor/Pipeline/Input versions.
Git must be available to Unity for package downloads. For CLI and agent skill setup,
start with the [main README](../../README.md).

## Install

1. Open the project's package manifest, **`Packages/manifest.json`**.
2. Merge these entries into its existing `dependencies` object, preserving other packages and valid JSON:

   ```json
   "com.unity.pipeline": "0.8.0-exp.1",
   "com.unity.inputsystem": "1.20.0",
   "com.batihandev.unity-cli-commands": "https://github.com/batihandev/unity-cli-skills.git?path=/Packages/com.batihandev.unity-cli-commands#RELEASE_COMMIT"
   ```

   Replace `RELEASE_COMMIT` with the published, reviewed package commit matching the versions above. Keep Pipeline and
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

Terrain creation, heightmap editing, and existing-layer painting are documented in the [Terrain guide](../../unity-cli-skills/references/domains/terrain.md).
Shader source, templates, inspection, recoverable removal, and `shader.global-keyword-set` are documented in the [Shader guide](../../unity-cli-skills/references/domains/shader.md). The keyword command previews with `dryRun=true`, requires `confirm=true` to change global state, reports before and after, and is non-Undo.

Response formats and command safety rules are documented in the [agent reference](../../unity-cli-skills/references/domains/foundation.md). UnityEvent inspection, persistent listener edits, and confirmed invocation are documented in the [Event guide](../../unity-cli-skills/references/domains/event.md).

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

**Update:** replace the commit ID after `#` in the package URL with a reviewed release revision.
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

## Independent probe compilation

`cli_compile_probes` accepts a SHA-256-bound JSON manifest (`schemaVersion: 1`,
`sources: [{id, path, sha256}]`) and a whole-catalogue `time_budget_ms`.
Manifest and source paths must be absolute. The command invokes native
`run_script` independently for each source with ephemeral compile-only settings.
Its response retains every native result and the Editor PID, start identity and
project. Use the skill's `ProbeWorkflow.check_many` to stage sources, validate
session and source freshness, and publish durable per-source reports.
