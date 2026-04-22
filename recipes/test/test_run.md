# test_run

Kick off all tests in a mode. Fire-and-forget: creates a `Filter` and calls
`TestRunnerApi.Execute`, then returns. Read results in a later call via
`test_get_result` once the run has produced an XML report under `TestResults/`.

**Signature:** `TestRun(testMode string = "EditMode", filter string = null)`

**Returns:** `{ success, started, mode, filter }`

**Notes:**
- `testMode` must be `"EditMode"` or `"PlayMode"`.
- `filter` is an optional test-name substring forwarded as `testNames`.
- Unity serializes the Test Runner — do not start a second run while another
  is active.
- Output XML file: `TestResults/EditMode-all-menu.xml` or
  `TestResults/PlayMode-all-menu.xml` depending on mode. `test_get_result`
  reads the newest matching XML.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

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

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var runFilter = new Filter { testMode = mode };
        if (!string.IsNullOrEmpty(filter))
            runFilter.testNames = new[] { filter };
        api.Execute(new ExecutionSettings(runFilter));

        result.SetResult(new
        {
            success = true,
            started = true,
            mode = testMode,
            filter
        });
    }
}
```
