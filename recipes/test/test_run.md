# test_run

Kick off all tests in a mode, via the installed test runner tool.

**Prerequisite:** install [`tooling/test/test_runner_tool`](../../tooling/test/test_runner_tool.md)
once per project. Runs are started by that compiled tool, not by a callback registered from this
body. Registering the callback here cannot be made to work — it silently rewrites earlier runs'
reports, turning a red report into a copy of a later green one. The failure mode and the repairs
that do not fix it are in
[Why not inline](../../tooling/test/test_runner_tool.md#why-not-inline).

**Signature:** `ProjectTestRunner.Run(resultFileName string, editMode bool = true, fullyQualifiedTestName string = null, assemblyName string = null)`

**Returns:** the absolute report path, or an `ERROR: ` string when the run cannot start.

**Notes:**
- `assemblyName` scopes the run to one test assembly, which is how to run a whole suite without
  naming every class in it. It combines with `fullyQualifiedTestName`; leave both unset for the
  entire mode.
- Fire-and-forget. The report does not exist when the call returns; read it in a *later* call.
- Use a fresh `resultFileName` per run. A stale file from an earlier run is otherwise
  indistinguishable from this run's result.
- Unity serializes the Test Runner — one run at a time. The tool refuses a `Run` while one it
  started is still pending. Wait for the first report to be whole and fresh — the poll loop below —
  before starting the next.
- `fullyQualifiedTestName` filters to one class or method and must be `Namespace.Class` or
  `Namespace.Class.Method`. A bare class name matches zero tests, and a run of zero tests reports as
  a pass. Assert `total` is non-zero before believing any green.
- **Precondition:** the Unity Editor must be open and responsive on the intended project. The tool
  refuses to start and returns an `ERROR: ` string when the editor is compiling, or when it is in
  Play Mode and an EditMode run was asked for — that combination otherwise never starts and never
  writes a report, which looks exactly like a broken callback.
- `Run` refuses to start while `EditorUtility.scriptCompilationFailed` is true — the Test Runner
  would execute the assemblies from the last successful compile and report their numbers as this
  code's. Read the errors from `Editor.log`; `Unity_ReadConsole` can return an empty list while the
  compile is broken.
- **PlayMode caveat:** entering Play Mode can trigger a domain reload that discards the callback
  before `RunFinished` fires, so no report is written. Disable domain reload for the run or keep
  PlayMode runs on the Test Runner window. EditMode runs do not reload the domain.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using YourProject.Editor.Testing;   // namespace you installed the tool under

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Fresh name per run. Never reuse, and never use a fixed name like "EditMode-mcp.xml".
        string path = ProjectTestRunner.Run("EditMode-<task>-<red|green>.xml", editMode: true);

        result.SetResult(new
        {
            success = !path.StartsWith("ERROR:"),
            started = !path.StartsWith("ERROR:"),
            resultsPath = path
        });
    }
}
```

Then, in a later call, poll from the shell and parse. Three reports can lie, so waiting for the
file to merely exist is not enough:

- a **leftover** from an earlier run under the same name, which reads as this run's result
- a run that **matched no tests**, which NUnit reports as a pass
- a **half-written** file caught mid-flush

```bash
F=TestResults/EditMode-<task>-<red|green>.xml
MAX_AGE=120   # a report older than this belongs to an earlier run, not this one

# The closing tag is the completion signal: Unity writes the report in one pass,
# so its presence means the file is whole. stat -c is GNU, stat -f is macOS.
for i in $(seq 1 150); do
  if [ -f "$F" ] && grep -q '</test-run>' "$F" 2>/dev/null; then
    MTIME=$(stat -c %Y "$F" 2>/dev/null || stat -f %m "$F")
    [ $(( $(date +%s) - MTIME )) -le "$MAX_AGE" ] && break
  fi
  sleep 2
done

# On fall-through whatever is on disk is stale or half-written; parsing it would
# report an older run's numbers as this one's.
{ [ -f "$F" ] && grep -q '</test-run>' "$F" 2>/dev/null && \
  [ $(( $(date +%s) - $(stat -c %Y "$F" 2>/dev/null || stat -f %m "$F") )) -le "$MAX_AGE" ]; } \
  || { echo "no fresh report at $F" >&2; exit 1; }

python3 - "$F" <<'PY'
import sys, xml.etree.ElementTree as ET
a = ET.parse(sys.argv[1]).getroot().attrib
total = int(a.get("total") or a.get("testcasecount") or 0)
failed, inconc = int(a.get("failed", 0)), int(a.get("inconclusive", 0))
print("total=%d passed=%s failed=%d inconclusive=%d"
      % (total, a.get("passed"), failed, inconc))
if total == 0:
    sys.exit(3)              # matched no tests - NUnit calls this a pass
sys.exit(2 if failed or inconc else 0)
PY
```

Exit codes: `0` passed, `1` no fresh report arrived, `2` tests failed, `3` matched no tests.
