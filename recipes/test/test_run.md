# test_run

Kick off all tests in a mode, via the installed test runner tool.

**Prerequisite:** install [`tooling/test/test_runner_tool`](../../tooling/test/test_runner_tool.md)
once per project. Runs are started by that compiled tool, not by a callback registered from this
body. Registering the callback here cannot be made to work — it silently rewrites earlier runs'
reports, turning a red report into a copy of a later green one. The failure mode and the repairs
that do not fix it are in
[Why not inline](../../tooling/test/test_runner_tool.md#why-not-inline).

**Signature:** `ProjectTestRunner.Run(resultFileName string, editMode bool = true, fullyQualifiedTestName string = null)`

**Returns:** the absolute report path, or an `ERROR: ` string when the run cannot start.

**Notes:**
- Fire-and-forget. The report does not exist when the call returns; read it in a *later* call.
- Use a fresh `resultFileName` per run. A stale file from an earlier run is otherwise
  indistinguishable from this run's result.
- Unity serializes the Test Runner — one run at a time. The tool refuses a `Run` while one it
  started is still pending. Wait for the first report to appear *and* for its mtime to stop
  changing before starting the next.
- `fullyQualifiedTestName` filters to one class or method and must be `Namespace.Class` or
  `Namespace.Class.Method`. A bare class name matches zero tests, and a run of zero tests reports as
  a pass. Assert `total` is non-zero before believing any green.
- **Precondition:** the Unity Editor must be open and responsive on the intended project. The tool
  refuses to start and returns an `ERROR: ` string when the editor is compiling, or when it is in
  Play Mode and an EditMode run was asked for — that combination otherwise never starts and never
  writes a report, which looks exactly like a broken callback.
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

Then, in a later call, poll from the shell and parse:

```bash
F=TestResults/EditMode-<task>-<red|green>.xml
for i in $(seq 1 12); do [ -f "$F" ] && break; sleep 8; done
# stat -c is GNU, stat -f is macOS
prev=""; for i in $(seq 1 6); do cur=$(stat -c %Y "$F" 2>/dev/null || stat -f %m "$F"); [ "$cur" = "$prev" ] && break; prev=$cur; sleep 5; done
python3 -c "import xml.etree.ElementTree as ET;r=ET.parse('$F').getroot();print(r.attrib)"
```
