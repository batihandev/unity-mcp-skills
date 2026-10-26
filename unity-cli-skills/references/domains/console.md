# Console capture, settings, and export

Read [foundation routing and safety](foundation.md) first. Pipeline capture and the live Editor Console are
different data owners. Preserve their distinct counts and never substitute one for the other.

## Capture, logging, and statistics

Use the bounded host read for one console snapshot or one follow read:

```text
python3 unity-cli-skills/scripts/unity_workflow.py console --project <exact-project> \
  --output <new-persistent-json-path> [--level log|warn|error] [--tail 1..1000] \
  [--since <cursor> --since-session <source-session>]
```

Without cursor arguments, native `since=-1` returns a snapshot. For follow reads, pass both the prior
`cursor` and `sourceSession` from the result; keep that Pipeline source session separate from the Editor's
OS process identity. The native request always captures up to the bounded 2,000-entry callback buffer. The
`--tail` value only limits entries returned on stdout; the persistent JSON artifact retains the full sanitized
native response. `dropped`/`bufferGap` and `reset` describe native cursor history, while `displayTruncated`
describes only stdout clipping. Neither state implies the other.

The callback buffer is bounded and can miss entries across overflow or reload; do not claim a complete Editor
history. Retained buffer `counts` and Editor `groundTruth` totals have different owners. Ground truth may be
unknown or stale when no main-thread sample exists or the Editor is blocked. The result labels it `unknown`,
`stale`, or `fresh`; null or stale data is diagnostic information, not zero counts or a console command
failure. A clean compile flag does not imply that the callback buffer contains all Editor records. This read
is useful during compilation, imports, and PlayMode, and does not clear or mutate either console.

The host report path must be initially absent, persistent, and outside the project's `Assets`, `Packages`,
`Library`, and `Temp` directories. The artifact is local diagnostic data and may contain full messages, stack
traces, and project-local paths. Do not select a newest report from a shared directory. Call `clear_console`
only on an explicit clear request.

For a bounded log request, map case-insensitive `log`, `warning`, and `error` to the corresponding
`Debug.Log*` call in a narrow `eval`; unknown type preserves the baseline log fallback. Generate only a
quoted literal, including Unicode and multiline text, and verify the unique message through the retained
session/cursor. Never interpolate message text as C# code.

## Effective Console settings

The optional `console.settings` command reads or patches `collapse`, `clearOnPlay`, and `errorPause` through
Unity's effective Console flags. No supplied nullable Boolean means read. `dryRun=true` previews and wins over
`confirm=true`; every supplied patch, including an idempotent one, otherwise requires `confirm=true`.
The typed result contains `Before`, `Proposed`, `After`, `Applied`, `DryRun`, and `Undoable=false`. The command
preserves unrelated flag bits and reads the effective flags back. Capture the original snapshot and restore
it with the same guarded route.

## Live entries and export

The optional read-only `console.entries(limit=1000)` command reads the live Editor Console in Editor index
order. Valid limits are 0 through 1000. It returns `Source="editor-console"`, `TotalAvailable`, `Count`,
`Truncated`, and ordered entries with index, full original message, integer mode, and tested `Log`, `Warning`,
or `Error` classification. It always ends the LogEntries read session in `finally`; any failed entry read is
an error with no successful prefix. Collapse state can change the Editor's visible entry set, so retain it
with the result.

Export the returned prefix as UTF-8 without BOM with one LF-terminated line per entry:
`[Log|Warning|Error] <first physical message line>`. Strip a CR only when it immediately precedes the first
stripped LF; preserve an empty first line. Limit zero creates an empty file. Report the verified project-
relative path, exported count, `editor-console` source, and import completion.

Validate the exact destination immediately before writing. `Assets/...` is the default; an existing,
project-contained embedded `Packages/<package>/...` target requires `allowEmbeddedPackages=true`.
`foundation.path.validate` reports confinement, link/reparse-point, and read-only-attribute diagnostics. It is
read-only and does not prove that the operating system will allow a later write. The transaction below checks
existing parent components without following links and proves create permission by staging in the destination
directory before it publishes any target. Existing output refuses unless replacement was explicitly authorized
and its current SHA-256 still matches immediately before atomic replacement. On failure it attempts to restore
preexisting targets and remove only files and directories the transaction created. If the OS prevents recovery,
`TRANSACTION_RECOVERY_REQUIRED` identifies retained paths and preserves any needed old-content backup;
`Committed` distinguishes cleanup after completed commands from failed publication or rollback. Import an asset destination
and wait for completion; delete only the owned export during cleanup.

## Executable host authoring transaction

Invoke `scripts/unity_authoring.py` with `--request <json>`. Its shipped module is the canonical host transaction for console export, test templates, script writes, and one external asset
publication. The request names the
exact project root, Windows project path, resolved Unity CLI application, operation, and post-publication CLI
commands. Each post command is an argv array; no shell evaluates caller text. `test-template` accepts `mode`,
`className`, and `folder`. `write` accepts `writes` with project-relative `path` and Base64 `content`, plus
optional replacement authorization and an expected SHA-256 per existing target. `replaceAuthorized` and
`allowEmbeddedPackages` accept JSON booleans only; malformed values refuse before any diagnostic or write.

`asset-import` publishes one exact external byte snapshot. It requires an absolute `sourcePath`,
`destinationPath`, a
64-digit hexadecimal `expectedSourceSha256`, and `expectedDestinationState`. The destination state is either
`{"kind":"absent"}` with `replaceAuthorized=false`, or
`{"kind":"existing","sha256":"<64-digit hexadecimal SHA-256>"}` with `replaceAuthorized=true`. The
operation does not accept `writes` or `content`. It opens and reads the source once, verifies that snapshot,
then passes the same bytes through this transaction's normal confined staging and recovery. It checks the
approved destination state before staging and immediately before publication; appearance, disappearance, or
hash change refuses. The result retains `Writes` and adds `AssetImport` with the source path, destination path,
and verified source and destination hashes. This host result proves file publication only. A caller that needs
a Unity asset must separately run and verify the AssetDatabase import lifecycle; this transaction does not
report import completion or an asset GUID.

The executable owner ships at `scripts/unity_cli/authoring.py`; invoke its entrypoint:

```bash
python3 <skill-root>/scripts/unity_authoring.py --help
python3 <skill-root>/scripts/unity_authoring.py --request request.json --result result.json
```


## Defines and recompilation

Use [project defines](project.md#scripting-defines) for exact-group define reads/replacement. For a forced
reload, call `recompile(focus=false)`, then poll `recompile_status`; `up_to_date` is terminal, otherwise require
completed status, `failed:false`, empty errors, and `compilationFailed:false`. A request acknowledgement is
not compilation completion.
