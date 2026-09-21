# Script authoring, search, and validation

Read [foundation routing and safety](foundation.md) first. Keep the legacy script entrypoint while its active
component, ScriptableObject, asset, validation, UI, and tooling consumers migrate. The routes below define
the direct CLI/host workflow; no generic script wrapper is added.

## Create and delete

Create MonoBehaviour, ScriptableObject, Editor, and plain-class roles from reviewed exact templates. Validate
a legal non-keyword C# identifier and a matching file name. Use the shared authoring transaction described in
[test templates](test.md#safe-template-creation): immediate `ProjectPathPolicy` validation, Assets default or
explicit existing embedded-package option, no-overwrite atomic publication, owned-partial cleanup, import,
and terminal compilation. New templates use UTF-8 without BOM and LF; return actual verified paths.

Delete only through a complete guarded native `delete_asset` after exact path, SHA-256, and confirmation,
with its actual Undo behavior stated. If live discovery does not prove that whole contract, retain the
capability gap and do not substitute destructive eval. Never delete a preexisting target during failure
cleanup.

## List, search, and edit

List with confined host `rg --files` over the selected `Assets` or `Packages` root and a `.cs` suffix. Search
with host `rg --json --line-number --column` and a caller regex; report normalized project-relative path,
one-based line, one-based UTF-8 byte column, and bounded excerpt. Do not follow links.

Hash actual bytes with SHA-256 before editing. Pre-edit validation records the current source and compiler
state. A structured insert/replace/remove requires one reviewed method/type context, expected current hash,
and a previewed host patch. Refuse zero/multiple contexts or stale content; never regex-replace every match.
A full write uses the same expected-hash and explicit overwrite authorization. Preserve existing encoding and
newlines. Stage in the target directory and replace atomically only after rechecking the hash immediately
before publication.

Group related edits before one compile boundary. Post-edit verification uses terminal compiler diagnostics,
exact source hash/content, and test/discovery results where relevant. Do not begin another mutation while
compilation or reload is active. Retain failed invocations and remove only owned staging files.
