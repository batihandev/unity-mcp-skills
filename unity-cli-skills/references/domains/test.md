# Test discovery, execution, and templates

Read [foundation routing and safety](foundation.md) first. Only one test operation may be active for the
project. Before connected or top-level execution, confirm the exact project, compilation success, stopped
PlayMode, and that every open scene is clean. Save authorized owned scenes or refuse before submission; a
batch-mode save dialog can block the dispatcher.

## Discovery and execution

Use connected `list_tests(mode)` for live discovery. Preserve `FullName`, mode, assembly, categories, and
explicit status; apply a validated caller-side limit after discovery. Category listing is the sorted distinct
set of nonempty categories. A new test must appear after compilation and disappear after owned deletion and
reload; there is no source-regex cache.

Prefer top-level `unity test <exact-project> --mode EditMode|PlayMode --filter <fully-qualified-name>
--output <unique-exact-path> --timeout <bound>`. The Editor must be closed first. Require a fresh output file,
exact project identity, nonzero expected test IDs, zero failed/skipped/inconclusive results when success is
required, and propagation of compile failures, timeouts, and zero matches. `unity@1.0.0-beta.9` can exit zero
after a zero-match filter, so parse the fresh XML and reject `total=0` or a missing exact expected
identity even when the invocation envelope says success. Explicit tests run only when the caller deliberately
selected them. Retain the first structured invocation and exact NUnit report.

Connected `run_tests` remains valid after the same clean-scene preflight. Submit one exact discovered full
name with `async_tests=true`, then poll `test_status` with a bounded timeout to a terminal state and verify the
exact report/test identity. Zero or multiple exact-name matches refuse before submission. A request
acknowledgement is not completion.

Parse only the caller-owned report path with a standard XML parser. Return its requested mode, counts,
failures, start/end time, duration, and exact test identities. Missing or malformed XML fails. Summary accepts
an explicit report set, defines its malformed-file policy, de-duplicates failed identities, and sorts output
deterministically; never choose a newest unrelated file or fall back across modes.

## Safe template creation

New templates use the shared host authoring transaction from [console export](console.md#live-entries-and-export):
validate with `foundation.path.validate` immediately before writing, reject roots/traversal/reparse escape,
stage in the target directory, and atomically create without overwrite as UTF-8 without BOM with LF newlines.
The class must be a legal non-keyword C# identifier and match the file name. Defaults are
`Assets/Tests/Editor/<Name>.cs` and `Assets/Tests/Runtime/<Name>.cs`; an explicitly selected existing embedded-
package subfolder is also valid. Import, wait for terminal compilation, verify exact discovery, and remove
only files created by the transaction before a final reload.

Perform identifier and assembly-owner decisions before creating a directory, staging file, import, or reload.
Validate the identifier with the C# lexical categories (letter or underscore first; then letters, decimal
digits, connector, combining, or formatting characters) and reject every reserved keyword. From the target
folder walk toward the selected Assets or embedded-package root, inspecting the nearest `.asmdef` or `.asmref`;
refuse malformed or conflicting ownership. Resolve an asmref to its exact asmdef and reuse only an assembly
whose `TestAssemblies` optional reference and platform inclusion match the requested mode. If an enclosing
assembly exists but is incompatible, return its path/hash and refuse without rewriting it. If none exists,
create the one exact owned assembly beside the template, refusing an already-used assembly name.
EditMode uses `UnityCliEditModeTests.asmdef`, name `UnityCliEditModeTests`, Editor-only `includePlatforms`,
`references:[]`, `optionalUnityReferences:["TestAssemblies"]`, and `autoReferenced:false`. In Unity Test
Framework 1.6.0, `TestAssemblies` supplies both runner references; listing them again produces duplicate
assembly-reference compilation errors. PlayMode uses `UnityCliPlayModeTests.asmdef`, name
`UnityCliPlayModeTests`, empty `includePlatforms`, and the same empty references, optional reference, and
auto-reference setting. Never rewrite an unrelated asmdef. If either creation fails, remove only files this transaction
created. The template body must contain one discoverable, non-explicit passing NUnit test for the chosen mode;
PlayMode uses a `UnityTest` enumerator.
