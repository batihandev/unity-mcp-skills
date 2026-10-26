# Script authoring, search, and validation

Read [foundation routing and safety](foundation.md) first. Use the reviewed host authoring transaction for
script publication and editing, terminal compilation for readiness, and the exact asset removal owner for deletion.

## Create and delete

Create MonoBehaviour, ScriptableObject, Editor, and plain-class roles from reviewed exact templates. Validate
a legal non-keyword C# identifier and a matching file name. Use the shared authoring transaction described in
[test templates](test.md#safe-template-creation): immediate `ProjectPathPolicy` validation, Assets default or
explicit existing embedded-package option, no-overwrite atomic publication, owned-partial cleanup, import,
and terminal compilation. New templates use UTF-8 without BOM and LF; return actual verified paths.

Delete an exact script through the optional `asset.trash` command, with `expectedSha256` required for this
script route. Capture the source bytes, encoding/newlines, SHA-256, exact project path, `.meta` bytes/hash,
and GUID outside the project before removal. Preview with `dryRun=true` and the captured source hash, verify
that both files are unchanged, then use the same hash and explicit `confirm=true` for the authorized deletion.
A malformed hash, changed source, or changed path confinement refuses. The command hashes the exact regular
file again and revalidates source/meta paths immediately before moving both files to recoverable OS trash.
The public AssetDatabase operation is not an atomic compare-and-delete; an uncooperative filesystem writer
can still race the final checks. Serialize cooperating authoring workflows and retain the snapshots.

Removal is non-Undo. Verify source/meta absence and an unloadable script, then wait for terminal compilation
and verify affected component/type/menu postconditions. Recover through the captured file/meta snapshots or
OS trash only after proving the destination source and meta are absent; refuse a new or changed external file.
Publish the exact source bytes through the confined authoring owner, restore the exact captured meta, import
synchronously, and require identical source/meta hashes, original GUID, correct MonoScript/type resolution,
and terminal compilation. Use [asset metadata recovery](asset.md#external-import) for the metadata lifecycle.
If recovery cannot finish, retain every external snapshot and report the incomplete state. Never delete a
preexisting target during failure cleanup.

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

## Script structure

Choose a meaningful class name, its MonoBehaviour, ScriptableObject, Editor, or plain-class role, and a
feature folder such as `Assets/Scripts/<Feature>/`. Keep the structure small, make dependencies explicit,
and use events for notifications. Avoid Update polling, repeated GameObject.Find calls, reflection in hot
paths, and avoidable allocations. Class renames require the matching file rename in the reviewed operation.
