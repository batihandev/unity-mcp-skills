# test_run

Kick off all tests in a mode. Fire-and-forget: registers a result callback,
creates a `Filter`, and calls `TestRunnerApi.Execute`, then returns. When the
run finishes the callback writes the NUnit XML report; read it in a later call
via `test_get_result`.

**Signature:** `TestRun(testMode string = "EditMode", filter string = null)`

**Returns:** `{ success, started, mode, filter, resultsPath }`

**Notes:**
- `testMode` must be `"EditMode"` or `"PlayMode"`.
- `filter` is an optional test-name substring forwarded as `testNames`.
- Unity serializes the Test Runner — do not start a second run while another
  is active.
- **`TestRunnerApi.Execute` writes no file on its own.** This recipe registers
  an `ICallbacks` whose `RunFinished` calls `TestRunnerApi.SaveResultToFile`,
  writing `TestResults/<mode>-mcp.xml`. The api + callback are parked in a
  `static` so they survive the async run — without it the callback is collected
  before `RunFinished` and no XML is ever written. `test_get_result` reads the
  newest `TestResults/<mode>-*.xml`.
- **PlayMode caveat:** entering Play Mode may trigger a domain reload (Project
  Settings → Editor → "Enter Play Mode Options" / Reload Domain), which discards
  the in-memory callback before `RunFinished` fires, so no XML is written. For
  reliable PlayMode reports either disable domain reload for the run or use a
  persistent (compiled) editor runner. EditMode runs do not reload the domain
  and are unaffected.
- **Precondition:** the Unity Editor must be open and responsive on the intended
  project before triggering. Trigger the run in one call; read results in a
  *later* call once the run has finished.
- **PlayMode-blocks-EditMode gotcha:** if the editor is already in (or
  entering) Play Mode, an EditMode run silently never starts and no XML is
  written — indistinguishable from a broken callback. This recipe guards for it
  and returns an error so you exit Play Mode first.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

// Keeps the api + callback alive for the async run. TestRunnerApi.Execute is
// fire-and-forget; a local callback would be GC'd before RunFinished and no XML
// would ever be written. A static survives until the next domain reload, which
// outlives an EditMode run.
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
        string testMode = "EditMode";
        string filter = null;

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

        // A stuck/leftover PlayMode blocks an EditMode run from ever starting —
        // it fails silently (no run, no XML), looking exactly like a broken
        // callback. Surface it instead.
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

        var runFilter = new Filter { testMode = mode };
        if (!string.IsNullOrEmpty(filter))
            runFilter.testNames = new[] { filter };
        api.Execute(new ExecutionSettings(runFilter));

        result.SetResult(new
        {
            success = true,
            started = true,
            mode = testMode,
            filter,
            resultsPath = resultsPath.Replace('\\', '/')
        });
    }
}
```
