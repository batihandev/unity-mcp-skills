# Unity CLI Skills

Agent skills for inspecting scenes, editing objects and assets, and running tests through the Unity CLI.

**Work-in-progress preview.** Coverage is incomplete and full release verification is pending.
The stable MCP version remains on `main`. See [migration status](#migration-status) for current progress.

## Requirements

| Component | Installation scope | Required? |
|---|---|---|
| Unity Editor and Unity CLI | Machine | Yes |
| `unity-cli-skills` | Agent skills directory | Yes |
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

### 2. Install the agent skill

Download a tested revision with Git:

```sh
git clone --branch wip/unity-cli --single-branch https://github.com/batihandev/unity-mcp-skills.git
cd unity-mcp-skills
git checkout 0f9df79a66a7fc5811d2e695cbd58fc1264fd58f
```

This command selects the tested preview commit. Copy the complete `unity-cli-skills` folder,
including its references, into your agent's skills directory.
The destination should contain `unity-cli-skills/SKILL.md`. Use your agent's documented skill location
and start a new agent session to load it.

Already installed through a configuration manager? Use that manager to update it; skip the manual copy.

## Project setup

### 3. Enable Unity's built-in Editor commands

Skip this step if the project already has the versions below.

Open the project's **`Packages/manifest.json`** in a text editor. This is Unity's package dependency list.
Inside its existing `dependencies` object, add or update these entries, preserving all other packages:

```json
"com.unity.pipeline": "0.7.0-exp.1",
"com.unity.inputsystem": "1.20.0"
```

Keep valid JSON: entries are separated by commas, with no comma after the final entry.
Open the project in Unity 6000.6.2f1 and let package installation and compilation finish.

### 4. Optional: install the command package

Install `com.batihandev.unity-cli-commands` for the additional scene/asset,
component, ScriptableObject, console, and profiler commands used by the skill's recipes.
Without it, those particular recipes are unavailable; Unity's built-in commands remain usable.

Follow the [command package installation guide](Packages/com.batihandev.unity-cli-commands/README.md).
It is Editor-only: it does not ship in your built game.

### 5. Check the connection

Leave Unity open. Replace `PROJECT_PATH` below with the full path to your Unity project folder
(the folder containing `Assets`, `Packages`, and `ProjectSettings`). Keep the quotes:

```sh
unity --json command list_open_scenes --project-path "PROJECT_PATH"
```

Success means the output reports `success: true` and lists your project's open scene.
From WSL, pass the Windows form of the project path to the Windows Unity CLI.

## Usage

Tell your agent which project to use and what you want, for example:

> Use unity-cli-skills with my open Unity project. Inspect the current scene and summarize its objects.

The [agent instructions](unity-cli-skills/SKILL.md) cover command discovery and execution.

## Update or uninstall

- **Update the skill:** obtain a reviewed WIP revision and replace your installed `unity-cli-skills`
  folder with its copy. Configuration-manager users should update through their manager.
- **Uninstall the skill:** remove only that installed folder. This leaves your Unity project untouched.
- **Update/remove the command package:** follow its [package guide](Packages/com.batihandev.unity-cli-commands/README.md#update-or-remove).

## Migration status

213 of 585 migration items are accepted; 372 remain open, including importer workflows.
Full release verification is pending.

## Credits and license

This project originated from [Besty0728/Unity-Skills](https://github.com/Besty0728/Unity-Skills).
The direct-CLI implementation and verification are developed in this repository.
Licensed under the [MIT License](LICENSE).
