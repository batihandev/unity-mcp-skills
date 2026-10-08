# Unity CLI Skills

Unity workflows and verified recipes for coding agents, extending Unity’s official `unity-cli` skill
with scene, asset, authoring, and testing guidance.

## Requirements

| Component | Installation scope | Required? |
|---|---|---|
| Unity Editor and Unity CLI | Machine | Yes |
| Official `unity-cli` skill | Agent skills directory | Recommended CLI reference |
| This repository’s `unity-cli-skills` | Agent skills directory | Yes, for these workflows |
| Unity Pipeline package | Unity project | Yes, for Editor commands |
| `com.batihandev.unity-cli-commands` | Unity project | Optional; required by some recipes |

Skill installation and Unity project configuration are separate steps.

## Machine setup

### 1. Check Unity and the CLI

Use **Unity 6000.6.2f1** and **Unity CLI 1.0.0-beta.12**.
If the CLI is already installed, check it:

```sh
unity --version
```

If the command is missing, follow [Unity's CLI installation instructions](https://github.com/Unity-Technologies/skills/blob/cb1dccb8f5adffcca43a5a26993fdeb8eae59433/skills/unity-cli/SKILL.md#install-the-cli-if-not-already-installed).
The compatibility table identifies verified versions and platform routes; the optional package requires the exact Editor and package versions listed below.

### 2. Install Unity's official skill

The CLI bundles the official `unity-cli` skill; installing it for your agent is a separate step.
List supported agents and their installation paths:

```sh
unity skill install --list
```

Then install for the agent you use, for example:

```sh
unity skill install codex
```

Replace `codex` with your client ID from the list, such as `claude-code` or `cursor`.
Run this in the environment where your agent runs. If a configuration manager already provides the
skill, manage it there instead.

### 3. Install this repository's skill

Clone the release branch:

```sh
git clone --branch main --single-branch https://github.com/batihandev/unity-cli-skills.git unity-cli-skills-release
cd unity-cli-skills-release
```

`unity-cli-skills-release` is the local checkout folder.
Copy the complete `unity-cli-skills` directory into your agent's documented skills directory.
The installed entry point is `unity-cli-skills/SKILL.md`. Start a new agent session after installation.
Configuration-manager users should install and update through their manager.

Record `git rev-parse HEAD` to reproduce the installed skill revision. The optional Unity package
uses a separate reviewed commit pin.

## Project setup

### 4. Enable Unity's built-in Editor commands

Skip this step if the project already has the versions below.

Open the project's **`Packages/manifest.json`** in a text editor. This is Unity's package dependency list.
Inside its existing `dependencies` object, add or update these entries, preserving all other packages:

```json
"com.unity.pipeline": "0.8.0-exp.1",
"com.unity.inputsystem": "1.20.0"
```

These are entries to merge, not a replacement manifest. Preserve valid JSON.
Open the project in Unity 6000.6.2f1 and let package installation and compilation finish.

### 5. Optional: install the command package

Install `com.batihandev.unity-cli-commands` for the additional scene/asset,
component, ScriptableObject, console, and profiler commands used by the skill's recipes.
Without it, those particular recipes are unavailable; Unity's built-in commands remain usable.

Follow the [command package installation guide](Packages/com.batihandev.unity-cli-commands/README.md).
It is Editor-only: it does not ship in your built game.

### 6. Check the connection

Leave Unity open. Replace `PROJECT_PATH` below with the full path to your Unity project folder
(the folder containing `Assets`, `Packages`, and `ProjectSettings`). Keep the quotes:

```sh
unity --json command list_open_scenes --project-path "PROJECT_PATH"
```

Success means the output reports `success: true` and lists your project's open scene.
From WSL, pass the Windows form of the project path to the Windows Unity CLI.

## Usage

Specify the target project and task:

> Use unity-cli-skills with my open Unity project. Inspect the current scene and summarize its objects.

The [agent instructions](unity-cli-skills/SKILL.md) cover command discovery and execution.
For Canvas widgets, layout and component edits, read the [uGUI guide](unity-cli-skills/references/domains/ui.md).
For UXML/USS, UIDocument, PanelSettings and UI Toolkit starters, read the [UI Toolkit guide](unity-cli-skills/references/domains/uitoolkit.md).

## Update or uninstall

- **Official skill:** after updating the CLI, run `unity skill refresh` for CLI-managed installations.
- **This skill:** run `git pull --ff-only` in the checkout, review the changes, and replace the
  installed `unity-cli-skills` directory. Use your configuration manager for managed installations.
- **Uninstall the skill:** remove only that installed folder. This leaves your Unity project untouched.
- **Update/remove the command package:** follow its [package guide](Packages/com.batihandev.unity-cli-commands/README.md#update-or-remove).

## Compatibility

Runtime evidence covers Unity **6000.6.2f1**, Pipeline **0.8.0-exp.1**, and
Input System **1.20.0**. The semantic matrix uses CLI **1.0.0-beta.11**;
Git installation lifecycle and baseline restoration use **1.0.0-beta.12**.
The exercised route is a graphics-enabled Windows Editor controlled from a WSL/Linux shell.
The host Python helpers require **Python 3.10+** and use the standard library.

| Editor / CLI / Pipeline | OS and shell route | Renderer | Status |
|---|---|---|---|
| 6000.6.2f1 / 1.0.0-beta.11 / 0.8.0-exp.1 | Windows Editor, WSL/Linux shell | Built-in core and seven isolated package integrations | semantic matrix verified |
| 6000.6.2f1 / 1.0.0-beta.11 / 0.8.0-exp.1 | Windows Editor, WSL/Linux shell | HDRP 17.6.0 and isolated URP 17.6.0 overlays | verified |
| 6000.6.2f1 / 1.0.0-beta.12 / 0.8.0-exp.1 | Windows Editor, WSL/Linux shell | Built-in; two Git installation ownership cases | lifecycle verified (22 passing tests per case) |
| Same exact versions | Native Windows shell | Same renderer versions | expected-unverified |
| Same exact versions | Native Linux or macOS Editor and shell | Same renderer versions | expected-unverified |
| Other Editor, CLI, Pipeline or renderer versions | Any | Any | expected-unverified |

The HDRP/URP row covers retained domain cases. The native fixture matrix records 40 passing core leaves and seven isolated package integration leaves under Built-in, including exact resolved pins and HDRP restoration reconciliation. The original fixture baseline restoration passed separately on CLI `1.0.0-beta.12`; the core/overlay tests retain beta.11 provenance. The Git-subdirectory installation lifecycle passed in two ownership cases on beta.12, with 21 foundation tests and one smoke test per case. The native matrix evidence records source bindings, removal behavior, and exact baseline restoration.
Optional integrations require their own package and isolated verification; a core workflow
passing does not verify an optional overlay. The optional command package's exact requirements
and version are owned by its [package guide](Packages/com.batihandev.unity-cli-commands/README.md).
No canonical renamed HTTPS package installation is claimed before the repository release.

## Credits and license

The initial skills and recipes were adapted from
[Besty0728/Unity-Skills](https://github.com/Besty0728/Unity-Skills) for direct agent use through Unity MCP.
This project is evolving that approach around Unity’s official CLI, with consolidated workflows,
additional commands, and verification.

Licensed under the [MIT License](LICENSE).
