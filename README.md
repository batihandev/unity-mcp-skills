# Unity CLI Skills

Unity workflows and verified recipes for coding agents, extending Unity’s official `unity-cli` skill
with scene, asset, authoring, and testing guidance.

**Work-in-progress preview.** Coverage is incomplete and full release verification is pending.
The stable MCP version remains on `main`. See [migration status](#migration-status) for current progress.

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

This preview is tested with **Unity 6000.6.2f1** and **Unity CLI 1.0.0-beta.10**.
If the CLI is already installed, check it:

```sh
unity --version
```

If the command is missing, follow [Unity's CLI installation instructions](https://github.com/Unity-Technologies/skills/blob/950064eb1de5c62e68909cbbc7e416dddaa29567/skills/unity-cli/SKILL.md#install-the-cli-if-not-already-installed).
Other versions have not been verified for this preview; the optional package requires the exact versions listed below.

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

Clone the current WIP branch:

```sh
git clone --branch wip/unity-cli --single-branch https://github.com/batihandev/unity-mcp-skills.git unity-cli-preview
cd unity-cli-preview
```

The repository retains its MCP name during migration; `unity-cli-preview` is the local checkout folder.
Copy the complete `unity-cli-skills` directory into your agent's documented skills directory.
The installed entry point is `unity-cli-skills/SKILL.md`. Start a new agent session after installation.
Configuration-manager users should install and update through their manager.

This checkout follows the evolving WIP branch. Record `git rev-parse HEAD` if you need to reproduce
an installation. The optional Unity package uses a separate, tested commit pin.

## Project setup

### 4. Enable Unity's built-in Editor commands

Skip this step if the project already has the versions below.

Open the project's **`Packages/manifest.json`** in a text editor. This is Unity's package dependency list.
Inside its existing `dependencies` object, add or update these entries, preserving all other packages:

```json
"com.unity.pipeline": "0.7.0-exp.1",
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

## Update or uninstall

- **Official skill:** after updating the CLI, run `unity skill refresh` for CLI-managed installations.
- **This skill:** run `git pull --ff-only` in the preview checkout, review the changes, and replace the
  installed `unity-cli-skills` directory. Use your configuration manager for managed installations.
- **Uninstall the skill:** remove only that installed folder. This leaves your Unity project untouched.
- **Update/remove the command package:** follow its [package guide](Packages/com.batihandev.unity-cli-commands/README.md#update-or-remove).

## Migration status

213 of 585 migration items are accepted; 372 remain open, including importer workflows.
Full release verification is pending.

## Credits and license

This project originated from [Besty0728/Unity-Skills](https://github.com/Besty0728/Unity-Skills).
The direct-CLI implementation and verification are developed in this repository.
Licensed under the [MIT License](LICENSE).
