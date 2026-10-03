# C# probe execution

Use `scripts/unity_probe.py` to run one project C# source through registered
`run_script` and require an exact current console completion message. Discover
`run_script`, `console`, and `editor_status` in the selected Editor's live catalog
before using the helper. Explicit play ownership also requires `editor_play` and
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
It preserves the current play state. This checks the selected ephemeral source
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
