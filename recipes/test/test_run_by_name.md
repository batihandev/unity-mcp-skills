# test_run_by_name

Kick off a specific test class or fully-qualified method via a
`TestRunnerApi` filter. Fire-and-forget; the registered callback writes the
NUnit XML report when the run finishes — read it via `test_get_result`.

**Signature:** `TestRunByName(testName string, testMode string = "EditMode")`

**Returns:** `{ success, started, testName, mode, resultsPath }`

**Notes:**
- `testName` is required. Pass an exact class name (e.g. `MyTestClass`) or
  a fully qualified method name (e.g. `MyNamespace.MyTestClass.MyTest`).
- Only one active Test Runner run at a time is safe.
- **`TestRunnerApi.Execute` writes no file on its own.** Like `test_run`, this
  recipe registers an `ICallbacks` whose `RunFinished` calls
  `TestRunnerApi.SaveResultToFile`, writing `TestResults/<mode>-mcp.xml`; the
  api + callback are held in a `static` so they survive the async run. See
  `test_run` for the PlayMode domain-reload caveat and the editor-open
  precondition.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

// Keeps the api + callback alive for the async run (see test_run for rationale).
internal static class _TestRunHold
{
    public static TestRunnerApi Api;
    public static ResultWriter Writer;

    internal sealed class ResultWriter : ICallbacks
    {
        private readonly string _path;
        public ResultWriter(string path) { _path = path; }
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, _path);
        }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
    }
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string testName = "MyTestClass";
        string testMode = "EditMode";

        if (Validate.Required(testName, "testName") is object err)
        {
            result.SetResult(err);
            return;
        }

        TestMode mode;
        if (string.Equals(testMode, "EditMode", System.StringComparison.OrdinalIgnoreCase))
            mode = TestMode.EditMode;
        else if (string.Equals(testMode, "PlayMode", System.StringComparison.OrdinalIgnoreCase))
            mode = TestMode.PlayMode;
        else
        {
            result.SetResult(new { error = $"testMode must be EditMode or PlayMode, got {testMode}" });
            return;
        }

        // A stuck/leftover PlayMode blocks an EditMode run from ever starting
        // (fails silently — no run, no XML). Surface it instead.
        if (mode == TestMode.EditMode && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            result.SetResult(new { error = "Editor is in PlayMode; exit PlayMode before running EditMode tests (the run would silently never start)." });
            return;
        }

        var dir = System.IO.Path.Combine(
            System.IO.Directory.GetParent(Application.dataPath).FullName, "TestResults");
        System.IO.Directory.CreateDirectory(dir);
        var resultsPath = System.IO.Path.Combine(dir, mode + "-mcp.xml");

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var writer = new _TestRunHold.ResultWriter(resultsPath);
        api.RegisterCallbacks(writer);
        _TestRunHold.Api = api;       // survive the async run
        _TestRunHold.Writer = writer;

        var runFilter = new Filter
        {
            testMode = mode,
            testNames = new[] { testName }
        };
        api.Execute(new ExecutionSettings(runFilter));

        result.SetResult(new
        {
            success = true,
            started = true,
            testName,
            mode = testMode,
            resultsPath = resultsPath.Replace('\\', '/')
        });
    }
}
```
