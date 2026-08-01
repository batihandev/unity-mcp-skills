# test_run_by_name

Kick off a specific test class or method, via the installed test runner tool.

**Prerequisite:** install [`tooling/test/test_runner_tool`](../../tooling/test/test_runner_tool.md)
once per project. Same reason as [`test_run`](test_run.md): a result callback registered from a
`Unity_RunCommand` body cannot be cleaned up and silently rewrites earlier runs' reports. See
[Why not inline](../../tooling/test/test_runner_tool.md#why-not-inline).

**Signature:** `ProjectTestRunner.Run(resultFileName string, editMode bool = true, fullyQualifiedTestName string = null, assemblyName string = null)`

**Returns:** the absolute report path, or an `ERROR: ` string when the run cannot start.

**Notes:**
- **`fullyQualifiedTestName` must be fully qualified** — `Namespace.Class` or
  `Namespace.Class.Method`. A bare class name such as `MyTestClass` matches **zero** tests, and a
  run of zero tests reports as a pass. This is the single most common false green here.
- Always assert the report's `total` is non-zero before believing a green result.
- Fire-and-forget. Read the report in a *later* call, after it appears and its mtime settles.
- Use a fresh `resultFileName` per run; never reuse one.
- Only one active Test Runner run at a time.
- See [`test_run`](test_run.md) for the editor-open precondition and the PlayMode domain-reload
  caveat.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using YourProject.Editor.Testing;   // namespace you installed the tool under

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Fully qualified. "MyTestClass" alone would match nothing and report a pass.
        string testName = "MyNamespace.MyTestClass";

        if (Validate.Required(testName, "testName") is object err)
        {
            result.SetResult(err);
            return;
        }

        string path = ProjectTestRunner.Run(
            "EditMode-<task>-<red|green>.xml",
            editMode: true,
            fullyQualifiedTestName: testName);

        result.SetResult(new
        {
            success = !path.StartsWith("ERROR:"),
            started = !path.StartsWith("ERROR:"),
            testName,
            resultsPath = path
        });
    }
}
```
