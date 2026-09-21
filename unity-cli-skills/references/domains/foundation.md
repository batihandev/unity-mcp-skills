# Foundation routing and safety

This baseline reference is verified against Unity `6000.6.2f1`, Unity CLI `1.0.0-beta.10`, Pipeline
`0.7.0-exp.1`, and Input System `1.20.0`. Live discovery wins when a later installed surface differs. This
baseline gate does not reverify the separate profiler, test, or optional-overlay matrices.

## Exact project targeting

Pass `--project-path` on every connected invocation and verify the returned target. From WSL, keep Windows
Unity projects on a drive-backed path and pass the Windows form; do not launch the Windows Editor against a
`\\wsl.localhost\...` project. Stop before a risky transition when the target is ambiguous, importing,
compiling, modal-blocked, or dirty in a way that could discard work.

```bash
unity --json status --project-path "$PROJECT_PATH"
```

## Live discovery

Discover the exact registered command before calling it:

```bash
unity --json command --project-path "$PROJECT_PATH" \
  --query find_gameobjects --detail full
```

In CLI beta.10, `--detail full` supplies the exact parameter metadata and JSON schema. The schema may describe
Newtonsoft `JObject`, `JArray`, `JToken`, or a structured Pipeline input as a JSON-valued CLI string. Pass the
raw JSON value required by the discovered argument; do not reconstruct Pipeline's internal DTO or command
handler types in a public package.

Use this route order:

1. A registered built-in that owns the complete behavior and safety contract.
2. A tested typed command for repeated or correctness-sensitive project behavior.
3. Registered `unity command eval` or `eval_file` for a narrow one-off operation.
4. Registered `run_script` for bounded, reviewable multi-statement authoring with a static entry point.

Pass method-body statements and local functions to `eval`/`eval_file`. Put class declarations, including TestRunner callbacks, in a source file and invoke its static entry point with `run_script`.

There is no top-level `unity eval` verb. If no discovered route owns the whole operation, report the missing
capability instead of composing a weaker approximation.

## Results and argument failures

CLI beta.10 wraps connected Pipeline results under `data.result` and reports transport `errors` and `warnings`
beside `data`. A public `CommandResult<T>` at `data.result` uses the established PascalCase properties
`Schema`, `Ok`, `Result`, and `Error`; its versioned `CommandError` is distinct from a transport or argument
failure. Keep transport warnings separate from the domain result.

Raw positional values, named flags, and JSON-valued structured inputs are bound by the CLI/Pipeline boundary.
For beta.10, locally invalid arguments are reported in the top-level `errors` array with
`INVALID_COMMAND_ARGS`. Discover the schema separately. Do not parse error prose or require fields absent from
the returned beta.10 envelope.

## Object and type discovery

Use `find_gameobjects` and the relevant component read command with exact identities returned by Pipeline.
Set `include_inactive` deliberately. Prefer an exact hierarchy path, instance ID, or global ID whenever names
have multiple matches. A short component type is acceptable only when it resolves uniquely; qualify the
namespace when two loaded types share a short name. Unknown or ambiguous types are errors, never first-match
selection. Before any mutation, enumerate every ambiguous GameObject candidate and every ambiguous loaded
component-type candidate, present the complete candidate lists to the user, and obtain the user's explicit
selection of the exact object identity and fully qualified component type. Do not infer either selection.

## Typed values

Use the consuming built-in's discovered schema. `set_serialized_field`, component/material operations,
`set_transform`, `set_layer`, and `set_animation_curve` own different formats; there is no universal string
converter. Invalid booleans, enum names, vectors, colors, layers, and curves must fail without changing state.
Read the value back through a registered read command after mutation.

## Path contract

Public custom authoring commands accept project-relative `Assets/...` paths by default. They reject root targets,
rooted outside paths, traversal, canonical escape, and a reparse point at the project root, an ancestor, an
intermediate segment, the final target, or the nearest existing parent of a nonexistent target. The optional
package's `ProjectPathPolicy` is the single owner, and every public custom path mutation calls it inside the
same command immediately before mutation.

An authoring workflow may opt into `Packages/<package>/...` only for an existing embedded package inside this
project's `Packages` directory with its own `package.json`. `foundation.path.validate` exposes this as
`allowEmbeddedPackages=true`; its default is false. It reports path structure, link/reparse-point state, and
read-only attributes. Package roots, `package.json`, registry/cache packages, external links, and every
reparse escape are refused. Effective create/write permission is established only by the authoring mutator's
owned staging operation. This option does not authorize package embedding or any mutation by itself.

`foundation.path.validate` is read-only diagnostic evidence. It does not make a separate Pipeline asset or
file mutation safe: route that built-in only when its own atomic path handling has independently passed the
required confinement and sentinel-preservation cases.

## Mutation contract

One reversible scene change stays with its Pipeline built-in and must create one Unity Undo group. Use
Pipeline 0.7 `batch` for supported multi-operation scene transactions: transactional mode is the default,
groups the operations into one Undo step, and rolls back applied operations on failure, cancellation, or time
budget exhaustion.

Do not place asset, file, package, build, test, play-mode, scene-open, settings, or other declared
non-revertible/excluded commands in a transactional batch. Such operations retain their own confirmation,
dry-run, confinement, cleanup, and explicit non-Undo contract. Verify exact before/after state; do not infer
Undo from a successful envelope.

### Typed results inside a batch

Pipeline 0.7.0-exp.1 does not interpret a custom command's nested `Ok:false` as a batch
failure. An outer item can report `success:true`, increment `applied`, and allow later
items to run even with `transactional=true`. Derive each item's success from both the
outer result and its typed `Ok`; compute counts from those combined outcomes.

For a continuation request, retain successful items and report the nested failures. For
rollback of a batch containing only verified Undo-compatible scene changes, retain its
returned `undoGroup`. If any nested result fails and `reverted` is false, immediately call
`UnityEditor.Undo.RevertAllDownToGroup(undoGroup)` through `eval`, before another Editor
mutation, then verify the affected state matches its before values. Later items may have
executed before this restoration; it is not preflight rejection. Nonserialized fields,
assets, and other non-Undo effects require their own explicit restoration and cannot use
this rollback workflow.

## Rendering compatibility

Read `get_graphics_settings`, then compare it with the actual default, Quality override, effective/current
asset, and active pipeline instance. Discover a shader with `list_shaders` and inspect it with
`get_shader_properties` before using its property names. Core, URP, and HDRP have different live catalogs.
For an unknown or custom SRP, report its actual type/state and make no default-shader or property-name guess.

## Tests and retained evidence

Run `unity test` with an exact output path and parse its NUnit XML. Require the expected nonzero test count and
zero failed, skipped, and inconclusive tests. For connected probes, verify both the structured response and
Unity's resulting state. Keep sanitized schemas, results, versions, totals, and hashes; discard raw Editor logs
and machine-specific paths.

## Optional package boundary

The skill has no Unity dependency and never edits a manifest. If live discovery lacks a required reusable
typed command, the optional `com.batihandev.unity-cli-commands` package may be proposed with its exact tested
revision and compatibility. Wait for explicit authorization. The recommended full-CLI host first declares
Pipeline `0.7.0-exp.1` and Input System `1.20.0` directly, then adds the optional Git-subdirectory package.
A package-only host is also supported because the package declares both dependencies. Require exact
`unity command --project-path` discovery/execution. Run package tests and
`unity_cli_commands_smoke`. On full-CLI removal, delete only the optional package and prove Pipeline built-ins
remain reachable. On package-only removal, state beforehand that transitive Pipeline/Input may disappear and
Editor-command access ends. Verify a public WIP Git package URL by resolving it in the consuming Unity host
before relying on it; a documented URL alone is not an installation proof.
