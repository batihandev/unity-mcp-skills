# Package inspection and lifecycle

Read [foundation routing and safety](foundation.md) first. Target one exact project and discover each native
or optional typed command before invoking it. Package changes are non-Undo and can compile or reload the
Editor. Installing this skill does not install the optional command package or change a project manifest.

Use the native owners for general package management:

- `package_list` supplies installed name, version, source, direct-dependency status and resolved path. An
  installed check selects one exact name from this result.
- Exact-name `package_search` supplies available versions and dependencies. Select a version returned by
  that command; do not infer unpublished versions.
- `package_add` and `package_remove` own their confirmation and dry-run contracts.
  Capture the original manifest, lock and installed package metadata before mutation. Retain the request
  acknowledgement, poll `package_status` to a terminal result, verify the exact manifest and fresh package
  inventory, then use the [terminal compile workflow](script.md).
- `package_resolve` takes no arguments. Its completed response records that resolution was requested;
  verify the resulting package inventory after the terminal compile workflow.

Only one cooperating package workflow runs under the existing `SessionController.workflow_session` lease.
Native `package_status` supplies add/remove/resolve preflight. The optional Embed observer guards its own
requests and unresolved journals. These checks cannot observe every uncooperative public UPM caller;
retain exact SDK busy or terminal errors.

## Embed an exact installed package

The optional `package_embed` command uses public `Client.Embed` and observes its returned `EmbedRequest`.
It does not supply replacement list/search/add/remove/resolve operations. Choose an installed package by
exact name and version, preserve its source and direct-dependency status, and require an absent
`Packages/<name>` destination. The operation implementation package is protected from Embed and inverse
removal.

Start with a fresh lowercase hexadecimal operation ID and a dry-run preview. Dry-run wins over
confirmation. Preview returns `planSha256`, command-source hash, complete source hashes, manifest/lock
snapshots, destination inventory, and the observed native PID/process-start ticks. Mutating requests must
supply that exact plan hash plus `expectedNativePid` and `expectedNativeProcessStartTicks`; a changed source,
destination, manifest, lock or Editor refuses before mutation. Without confirmation, the command refuses.

The command owns `Library/unity-cli-package-operations/<operationId>.json` in the exact project. Publication is
atomic and checks physical ancestors without following links. Journals persist across Editor restarts while
the project Library remains intact; deleting Library or losing storage removes this recovery evidence. The journal binds operation, project,
command source, selected package/source/version, original hashes, native PID/start ticks, timestamps,
acknowledgement, and the SDK's terminal status/error/result. A host caller additionally supplies its leased
`editorStartedAt` identity; that OS-specific token is distinct from native UTC process-start ticks.

An acknowledgement is a started request. Success requires all of:

1. The exact journal records SDK `Success`, `nativeCompleted=true`, the selected name/version/result, and detached
   observer with no unresolved request.
2. Fresh native `package_list` returns exactly that name/version with source `Embedded`.
3. A fresh terminal compile succeeds, followed by another exact journal/package readback.
4. `package_embed_reconcile` with `mode=completed-readback` seals `completed` against the exact current
   receipt SHA and original native PID/start ticks. Its read-only dry-run supplies the reviewed plan hash.

Directory existence, acknowledgement, package inventory alone, or reloaded state do not prove SDK success.
SDK Success records `awaiting-readback` and detaches without capturing a final destination, manifest or lock.
Registered readback verifies the complete embedded file bytes, metadata and directory inventory against the
captured source, an absent original destination, unchanged manifest bytes and only the exact selected lock
node transition. Any foreign change refuses without journal publication. Exact completed seals verify
idempotently against current state. Known terminal Success survives reload in the original native process;
`completed-readback` uses its persisted SDK evidence without a request handle.

The observer detaches on SDK completion, native error, timeout, observation/startup exception, host interruption,
Editor shutdown and assembly reload. Detachment does not cancel the SDK request. An unresolved journal
blocks further cooperating package mutation; preserve it for exact reconciliation or a lifecycle-bound
inverse. Never replace an interrupted outcome with inferred success.

`package_embed_reconcile` requires the exact current receipt SHA and a confirmed native PID/start-tick
binding. With `mode=sdk-terminal`, it checks the retained same-process public request's actual completion,
status, error and result. A pending SDK request refuses. The original timeout/interruption receipt remains
in `detachedEvidence`; this path can record actual SDK success or failure.

For an unresolved receipt after reload loses the request object, use `mode=lifecycle-inverse` only after stopping the exact original
Editor and its owned auxiliary workers under the existing lifecycle owner, proving their absence, reopening
the same project, and verifying fresh stopped compilation and registered package identity. The native
planner also refuses a live original PID/start identity. Its dry-run must prove full embedded bytes/meta/
directories equal the captured source, original manifest bytes are unchanged, and lock changes affect only
the selected package's exact embedded representation. Confirm its reviewed `planSha256` and fresh native
identity to authorize `package_operation_recover`. This records an observed-state inverse proof and keeps
`requestUnresolved=true`; it supplies no original SDK success. Exact inverse hash checks still apply.
A terminal SDK Success awaiting readback in a replaced native process can use this explicit lifecycle
inverse after the same original-process absence barrier; its terminal SDK evidence and
`requestUnresolved=false` remain recorded. Normal completion readback binds the original native identity.

If the exact original registered package/source hashes remain, the embedded destination is absent, and
manifest/lock bytes are unchanged, the same lifecycle preview can authorize an observed no-mutation
inverse. Recovery rechecks that complete original state and closes the receipt without deleting files or
requesting resolution. SDK uncertainty remains recorded.

Do not use another SDK List/Resolve call as a barrier for an unfinished request: the public
[Client API](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/packagemanager/client)
requires sequential operations, and
[Client.List](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/packagemanager/client/list)
requires other operations to have completed already.

`unity_cli.package.PackageWorkflow` composes these owners under the existing session lease. From Python
with the skill's `scripts` directory on `PYTHONPATH`, construct the existing session and inspect a preview:

```python
from unity_cli.lifecycle import SessionController
from unity_cli.platforms import current_platform
from unity_cli.transport import CliTransport
from unity_cli.package import PackageWorkflow

adapter = current_platform()
session = SessionController(project, CliTransport(adapter), adapter, timeout=120)
workflow = PackageWorkflow(project, session, source_paths=command_sources)
preview = workflow.embed(name=package_name, version=installed_version, dry_run=True)
```

`command_sources` names explicit project-contained command source inputs for the compile gate. After owner
approval of the preview, apply its exact identity and hash:

```python
result = workflow.embed(
    name=package_name, version=installed_version,
    operation_id=preview["preview"]["operationId"],
    expected_plan_sha256=preview["preview"]["planSha256"], confirm=True,
)
```

The host refuses confirmation without a reviewed plan hash. A timeout or interruption retains the journal
and attempts observer detachment only under the same verified Editor identity; it never reports SDK
cancellation.

## Import an exact package sample

`package_sample` uses public `Sample.FindByPackage(name, version)` and requires exactly one matching
`displayName`. Missing, duplicate, and wrong-version selections refuse. It calls public `Sample.Import`
with `HideImportWindow`. On Editor `6000.6.2f1`, that flag suppresses the package item-selection window;
archive signature/trust decisions may still require a native modal. An unsigned archive can block the call
at a **Missing signature** dialog even when selection is hidden. Inspect the exact owned Editor and
archive before an explicitly authorized one-time trust decision. Preserve the active operation and its
journal while the call is pending; do not repeat import or change global trust settings. A CLI timeout
does not cancel the native call.

On Editor `6000.6.2f1`, public sample discovery depends on the Package Manager UI's installed-package
cache. An empty `Sample.FindByPackage` result does not prove that a freshly registered package declares no
samples. Before preview, verify the exact registered name/version/resolved path and the package's declared
sample set. In a GUI Editor with all package requests complete, initialize that native UI through public
`UnityEditor.PackageManager.UI.Window.Open(packageName)` at an explicit owned session boundary, allow
Editor updates to attach it, then read fresh registration and public discovery again. Compare the exact
version's complete display-name/resolved-path/import-path set with its declaration; duplicate names remain
ambiguous. Opening initializes eligible pages; it does not guarantee refresh of an initialized stale cache.
If the sets disagree, retain the mismatch and stop before import.

Use the discovered `eval` or `eval_file` route for this bounded public API operation and its readback.
Discover its actual schema and the optional sample command separately:

```bash
unity --json command --project-path "$PROJECT_PATH" --query eval --detail full
unity --json command --project-path "$PROJECT_PATH" --query package_sample --detail full
```

Capture the existing Editor window identities and focus before initialization. `Window.Open` may create a
`UnityEditor.PackageManager.UI.PackageManagerWindow`; track it and close only a window created by this
operation after import/readback/inverse completes. Preserve existing windows and restore focus. Keep this
prerequisite visible to the caller; discovery failure must not trigger a silent automatic window fallback.
Internal service/cache reflection, UI clicks, repeated Resolve, and an unrelated SDK List request do not
provide this verified discovery prerequisite. Batch mode and other Editor versions require their own
supported discovery evidence.

Folder previews inventory every source file/meta/GUID and the full affected destination, including prior
sample versions and ancestor metadata. An existing destination requires `allowOverwrite=true` in the
preview and apply, plus the exact reviewed plan hash. The command validates source and destination physical
paths and hashes again, and invokes `ProjectPathPolicy` immediately before every actual Assets import.

The package's sample declaration points to a containing **directory**, which public `Sample.resolvedPath`
returns. For an archive sample, that directory contains exactly one top-level `*.unitypackage` regular file;
do not set the declaration path to the archive file itself. Zero top-level archives selects folder import,
and more than one refuses as ambiguous. Bind preview to the unique archive's content hash and the complete
containing-directory inventory. Read the registered package's actual resolved source; a vendored or other
cached copy cannot substitute for it.

For `.unitypackage` samples, Unity ignores `Sample.importPath`. The command validates the gzip/tar members,
actual `pathname` targets, asset/meta payloads and GUID identities. It rejects traversal, rooted/non-Assets
paths, links/special or duplicate members, mismatched GUID metadata, and GUID relocation to an existing asset
at another path before mutation. A target with different GUID metadata refuses even with overwrite approval.
The preview includes all target and metadata hashes and source GUID identities. Safe archives
remain importable through the same public Sample API. The structural inventory also recognizes the exact
regular-file envelope `package/.attestation.p7m` emitted by Unity's
[`6000.6.2f1` signed-package implementation](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6.2f1/Modules/AssetPackageEditor/Managed/SignedAssetPackage.cs).
It binds the complete archive hash, checks the envelope's header, duplicate identity, size and payload,
and excludes the envelope from Assets targets and recovery files. Envelope presence proves no valid
signature or trusted publisher: native Unity owns those checks. That SDK source requests signing only
with a nonempty owner organization ID; default export with an empty ID is unsigned, and signing errors
must be retained.

A successful import Boolean is retained as acknowledgement. Exact file bytes and registered GUID/path/type
readback, full destination/meta inventory, and fresh terminal compilation establish completion. Unity may
normalize metadata during import; the journal retains source hashes and actual imported metadata bytes.
`package_sample_readback` verifies the exact hash-bound journal and current inventory after compilation.

The host workflow shares the same confirmation contract:

```python
preview = workflow.sample(name=package_name, version=installed_version,
                          sample_name=exact_display_name, dry_run=True)
result = workflow.sample(name=package_name, version=installed_version,
                         sample_name=exact_display_name,
                         operation_id=preview["preview"]["operationId"],
                         expected_plan_sha256=preview["preview"]["planSha256"], confirm=True)
```

Retain `operationId` and `journalSha256` from the host result for an owned inverse.

## Hash-bound inverse and failed operations

`package_operation_recover` requires the exact current journal hash and a confirmed native PID/start-tick
binding. Dry-run previews recovery and performs no writes. Sample recovery restores prior file/meta bytes
and GUIDs, removes only captured operation-created files and empty directories, refreshes Unity, and verifies
the full prior inventory. Changed files, metadata, GUID identities or destination inventory refuse recovery
without deleting external changes.

An interrupted sample still in `starting`, with acknowledgement false, import acknowledgement absent or
false, and no after-inventory or completion fields, supports a no-mutation recovery only after the exact
original PID/process-start identity is absent. Recovery validates the retained plan digest and current
command source, the complete installed SDK package and selected sample binding, source inventory, and
unchanged destination files, metadata, GUIDs, directories and parents. The current Editor must be stopped
and idle for the command's observed Editor state and owned Embed observer; the host workflow also checks
native `package_status`. Dry-run writes nothing. Confirmation rechecks the proof and fresh native identity,
then atomically marks only the journal restored with `observedNoMutation=true`. It preserves false/absent
acknowledgements and SDK cancellation truth, changes no Assets bytes, and requests no resolution. Missing,
malformed or contradictory proof and any partial destination mutation refuse and retain the journal.

Embed recovery checks the original manifest/lock/registered package identity and complete new-root inventory.
Only an unchanged root absent before the operation can be removed. It restores exact manifest/lock bytes;
the host then invokes native resolve, requires terminal compilation, and checks the original installed
name/version/source/direct-dependency identity. The operation implementation cannot remove itself.

```python
restored = workflow.recover(operation_id=result["operationId"],
                            expected_journal_sha256=result["journalSha256"], confirm=True)
```

If an SDK request remains unresolved, retain its recovery journal and snapshots. Do not start another package
mutation or delete its destination while the request can still write it. Recovery refusal and incomplete
native evidence are explicit failures, never a successful inverse. Preserve exact SDK codes/messages and
partial-import snapshots when reporting failure.
