# Unity CLI Skills

`unity-cli-skills` teaches coding agents to inspect, test, and author Unity projects through the standalone
Unity CLI and the Editor's live Pipeline command catalog. The skill itself is Markdown-only and has no Unity
package, server, or managed-client dependency.

This is a work-in-progress preview. The migration has accepted 213 of 585 tracked artifacts; 372 remain open,
including incomplete importer workflows. Release acceptance gates for the whole migration have not run.

The current baseline compatibility stack is Unity `6000.6.2f1`, Unity CLI `1.0.0-beta.10`,
`com.unity.pipeline@0.7.0-exp.1`, and `com.unity.inputsystem@1.20.0`. Discover the live surface before use;
exact compatibility is intentionally revalidated at each migration or release gate.

## Install the skill

Clone the public WIP branch, record the reviewed commit, and copy only the skill directory into your agent's
skill home:

```bash
git clone --branch wip/unity-cli --single-branch https://github.com/batihandev/unity-mcp-skills.git
cd unity-mcp-skills
git rev-parse HEAD
# For a repeatable install, check out that reviewed commit before copying.
git checkout <reviewed-wip-commit>
cp -a unity-cli-skills "$AGENT_SKILLS_HOME/unity-cli-skills"
```

Installing the skill does not edit `Packages/manifest.json`. Update it by replacing that directory from a
reviewed checkout, and remove it by deleting only `$AGENT_SKILLS_HOME/unity-cli-skills`.

## Use

Name the target project and discover exact arguments before execution:

```bash
unity --json command --project-path "$PROJECT_PATH" \
  --query find_gameobjects --detail full
unity --json command find_gameobjects --project-path "$PROJECT_PATH" \
  --name Player --include_inactive true
```

CLI beta.10 wraps a connected Pipeline response as
`{"success":true,"data":{"command":...,"parameters":...,"result":...,"target":...},"errors":[],"warnings":[]}`.
A public package `CommandResult<T>` appears at `data.result` with established PascalCase
`Schema`/`Ok`/`Result`/`Error` properties. Verify the returned identity and resulting Unity state.

## Optional typed-command package

`Packages/com.batihandev.unity-cli-commands/` is an optional, Editor-only UPM package for reusable typed
commands. It is not installed with the skill. After explicit authorization, add a reviewed WIP commit
with Unity's Git-subdirectory syntax. The recommended full-CLI host declares
`com.unity.pipeline@0.7.0-exp.1` and `com.unity.inputsystem@1.20.0` directly first, so removing only the optional
package leaves Pipeline built-ins installed. A package-only host is also supported; use exact
`unity command --project-path` discovery and execution. Removing the optional package from that shape may also remove its transitive
Pipeline/Input dependencies and ends Editor-command access. Verify `unity_cli_commands_smoke` and the package
tests after installation. See the [package README](Packages/com.batihandev.unity-cli-commands/README.md).

Start with [the skill router](unity-cli-skills/SKILL.md) and its
[foundation reference](unity-cli-skills/references/domains/foundation.md).

## Credits and license

The original skill-library structure and source material were curated by
[Besty0728/Unity-Skills](https://github.com/Besty0728/Unity-Skills). This migration's direct-CLI contracts and
verification are maintained in this repository. Licensed under the [MIT License](LICENSE).
