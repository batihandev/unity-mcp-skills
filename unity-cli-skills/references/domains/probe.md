# C# probe execution

Use `scripts/unity_probe.py` to run one project C# source through registered
`run_script` and require an exact current console completion message. Discover
`run_script`, `console`, and `editor_status` in the selected Editor's live catalog
at the workflow boundary and after package/domain changes. Reuse that metadata while
the selected session and command contract remain current. Explicit play ownership also requires `editor_play` and
`editor_stop`.

```bash
python3 scripts/unity_probe.py --project "$PROJECT" \
  --source AgentScripts/Probe.cs --entry Probe.Run \
  --completion-text 'PROBE_DONE {run_id}' \
  --output "$REPORT_DIR/probe.json" --timeout 120
```

The exact completion template must appear literally in the C# source, such as
`UnityEngine.Debug.Log("PROBE_DONE {run_id}");`. The helper generates a unique run
ID and replaces that entire template in a private source copy. Other `{run_id}`
occurrences remain unchanged. Project source stays untouched. The staged path is
converted for the selected native CLI host.

A plain literal completion message is also accepted. Its evidence correlates the
message with the current console sequence after invocation; it cannot establish
exclusive authorship when other scripts can emit the same message. Prefer the
run ID template for probes that need invocation-specific completion evidence.

Execution requires an exact running Editor in playing state. `--enter-play`
authorizes a stopped-to-playing transition owned by this helper. It restores
stopped state only for that transition and only while the leased Editor identity
remains current. Existing playing state remains caller-owned. Paused state requires
the caller to choose the desired state before execution. The helper never launches
or exits the Editor and never clears the console.

Use compile-only checks without an entry point or completion marker:

```bash
python3 scripts/unity_probe.py --project "$PROJECT" \
  --source AgentScripts/Probe.cs --dry-run \
  --output "$REPORT_DIR/compile-check.json"
```

Compile-only mode invokes `run_script` with `dry_run=true` and `mode=ephemeral`.
It requires structured success, zero execution time, and no loaded assembly.
Native failed compilations can report a generated `PipelineRunScript_<file>_<guid>`
assembly name before any assembly is loaded; a successful dry-run must report null.
Both cases require zero execution time and valid diagnostics. It preserves the
current play state. This checks the selected ephemeral source
against the Editor's loaded references and defines; project compilation is owned
by the [compile workflow](editor.md).

The report path must be absent and its parent directory must exist. Reports belong
outside `Assets`, `Packages`, `Library`, and `Temp`. Each report retains complete
native command responses, source and staged hashes, exact Editor identity, fresh
console cursor, run ID, correlation mode, completion entry, and restoration status.
Successful stdout includes the report path and SHA-256. Failure returns a nonzero
exit status with a retained report when report publication is possible.

The helper refuses unsuccessful or malformed command results, console reset or
dropped history, Error/Assert/Exception entries after the initial cursor, source
hash changes, Editor replacement, play-state changes, and deadline expiry. A stale,
seeded, partial, or wrong completion message cannot establish success. Each poll
uses the shared [console validator](console.md). Console evidence remains bounded
by the native buffer; a reported gap prevents acceptance. Probe code must emit
completion only after its own assertions and asynchronous work finish. Exceptions
or messages produced after that terminal boundary require a separate observation.

Python callers use `ProbeWorkflow(project, session).run(source, entry,
completion_text, output, enter_play=False)` or `.check(source, output)` with a
`SessionController`. The session timeout bounds the operation. `ProbeRefusal`
provides a structured code and retained artifact details.

## Compile an independent catalogue

`ProbeWorkflow(project, session).check_many(source_paths, output_dir)` requires
`cli_compile_probes` from `com.batihandev.unity-cli-commands`. Select explicit
project-contained C# sources and an existing empty plain report directory outside
project-owned data directories. Discover the typed command once in the selected
Editor before adopting this route.

One request invokes the registered native `run_script` handler separately for every
source with `mode=ephemeral` and `dry_run=true`. Each source uses the Editor's
loaded references and defines; probes do not supply references to one another.
The hash-bound manifest and staged sources use absolute native-host paths.
The checker does not launch the Editor, enter play, load probe assemblies or run
entry points. It refuses missing, duplicated, malformed or mismatched results.

The session timeout covers preparation, discovery, compilation and acceptance of
the whole catalogue. Each source keeps its complete native result in an individual
report; the summary retains all command responses and report hashes. Ordinary
compiler failures retain the remaining source outcomes and make the aggregate
unsuccessful. Source, session, console or deadline failures refuse acceptance with
the available raw evidence retained. There is no fallback to individual requests.

Run the active probe for normal iteration: its execution includes compilation.
Use the single-file dry-run for affected probes that will not execute. Run the
catalogue once at final integration when changed APIs or references could break
other probes. Repeated full checks add no evidence when source, references and
Editor identity have not changed.
