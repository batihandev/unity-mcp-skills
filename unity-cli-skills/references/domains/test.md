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

Use the public host gate for an exact method, exact class, exact assembly, or their intersection. Omitting
all selectors runs the entire requested mode; omitting `--mode` selects EditMode.

```text
python3 unity-cli-skills/scripts/unity_workflow.py test --project <exact-project> --route connected \
  --test-class <namespace.class> --assembly <exact-assembly> --source <file.cs> --output <new-report.json>
```

Use `--test-name <exact-FullName>` for a method, `--test-class <namespace.class>` for every discovered method
in that class, and `--assembly <exact-assembly>` for an assembly. Name and class are mutually exclusive;
either can combine with assembly. Repeat `--source` for each explicit source input. The connected route
compiles and freezes a nonzero native discovery set, then submits one asynchronous native suite and polls to
fresh terminal status. The SDK accepts one case-insensitive partial name or assembly filter. The gate refuses
any selection that this filter would widen, including identical fullnames in separate assemblies: the native
SDK passes only fullnames to its runner. Use offline execution for such a selection.

Prepare a bound discovery plan while the exact project Editor is connected, then stop that owned Editor
before offline execution:

```text
python3 unity-cli-skills/scripts/unity_workflow.py test-plan --project <exact-project> \
  --test-name <exact-FullName> --assembly <exact-assembly> --source <file.cs> --output <new-plan.json>
python3 unity-cli-skills/scripts/unity_workflow.py test --project <exact-project> --route offline \
  --test-name <exact-FullName> --assembly <exact-assembly> --discovery <new-plan.json> \
  --source <file.cs> --output <new-report.xml>
```

The plan freezes native rows, exact selected assembly/fullname pairs, project, mode, Editor identity/version,
source hashes, and the source/assembly/DLL inventory. Offline execution requires matching selectors, project,
mode, version, and unchanged inputs; a raw discovery response cannot substitute for a plan. Offline class,
assembly, combined, and whole-mode selections require this plan. An offline single exact fullname can run
without a plan and validates the sole exact leaf. Native offline names use anchored escaped regex filters;
assembly selection forwards `-assemblyNames` after the CLI's `--` separator. Whole-mode runs pass no filter.
The offline runner performs its own compile step, so no separate connected compile proof is claimed.

Both routes require the complete nonzero frozen selection, consistent summary counts, and every selected
leaf passed, with no failed, skipped, or inconclusive leaves. Offline XML identifies each leaf through its
Assembly parent suite; distinct assemblies with the same fullname remain distinct identities. Missing,
extra, or duplicate identities fail even when a failing report is retained. A successful acknowledgement is
not completion. Explicit connected tests require `--include-explicit`; the offline CLI exposes no inclusion
switch and refuses that option.

Choose an initially absent report path in a persistent directory outside the project's `Assets`, `Packages`,
`Library`, and `Temp` trees. The helper stages native output in a private sibling directory and publishes
without replacing an existing target. Connected output is sanitized JSON. Offline output is the raw NUnit XML,
which may contain test messages, stack traces, and project-local paths; treat it as local diagnostic data.
Failures with a valid report preserve that report. Do not use Unity's temporary report location or select a
newest report from a shared directory.

Connected tests have no native run ID. Freshness is established through the rewritten status file, exact
Editor/source checks, and a cooperating per-project host lock. An uncooperative writer outside that lock
cannot be ruled out. On a connected wall-clock timeout, the test may still be running; the helper does not
cancel it automatically. Inspect the request/status files before attempting another run. Offline execution
reserves time for Unity's native timeout/cleanup before the host deadline; if the host itself times out, Editor
cleanup remains unverified.

Connected `run_tests` uses the same clean-scene preflight. Set `async_tests=true`, poll `test_status` with
a bounded timeout, and compare every terminal leaf against the frozen native discovery set. The requested
mode is bound through discovery and the native acknowledgement; native connected leaves omit assembly and
mode metadata. A supplied mode that conflicts with the request fails validation.

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
