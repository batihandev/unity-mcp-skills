# BatihanDev Unity CLI Commands

This optional Editor-only UPM package provides focused typed commands for standalone Unity CLI and Pipeline
workflows. Version `0.1.0` accepts exactly Unity `6000.6.2f1`, `com.unity.pipeline@0.7.0-exp.1`, and
`com.unity.inputsystem@1.20.0`; the smoke command returns a structured incompatibility error before mutation
when that contract is not met.

## Install

For the recommended full-CLI host, directly declare the exact CLI foundation before adding the optional
package:

```json
"com.unity.inputsystem": "1.20.0",
"com.unity.pipeline": "0.7.0-exp.1"
```

Add a reviewed commit from the public `wip/unity-cli` branch. Replace the placeholder with its full commit ID;
the package URL uses Unity's Git-subdirectory syntax:

```json
"com.batihandev.unity-cli-commands": "https://github.com/batihandev/unity-mcp-skills.git?path=/Packages/com.batihandev.unity-cli-commands#<reviewed-wip-commit>"
```

The package also declares both dependencies for compilation and version coherence, so a package-only host is
supported. Use exact `unity command --project-path` discovery and execution. Let Unity resolve the project,
enable its tests with this top-level entry in the consuming `Packages/manifest.json` (alongside `dependencies`):

```json
"testables": ["com.batihandev.unity-cli-commands"]
```

Preserve any existing entries in `testables`. Run the package EditMode tests, discover
`unity_cli_commands_smoke` with `unity command --query ... --detail full`, and assert its returned versions.
Package installation is explicit and independent from installing `unity-cli-skills`.

The package supplies typed commands for console settings and entries, project defines, Editor context,
scene and asset operations, component and ScriptableObject members, and profiler analysis. Discover each
command's live schema before use. It also extends `foundation.path.validate` with
`allowEmbeddedPackages=true` for a project-contained, existing embedded package. The default path policy
is Assets-only.

## Update

Replace only the revision after `#`, resolve from a clean project, and repeat the tests and live smoke check.
Do not claim a revision is supported from compilation alone.

## Remove

Remove only `com.batihandev.unity-cli-commands` from the consuming manifest, then resolve and reload. In a
full-CLI host, confirm its commands are absent while directly declared Pipeline/Input and built-ins remain;
removing that foundation is separate. In a package-only host, removal may also remove transitive
Pipeline/Input and ends Editor-command access. The package creates no project assets or migration hooks, so
no compatibility shim or cleanup command is required.
