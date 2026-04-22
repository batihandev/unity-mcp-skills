# test_run_by_name

Kick off a specific test class or fully-qualified method via a
`TestRunnerApi` filter. Fire-and-forget; read results via `test_get_result`
after the run completes.

**Signature:** `TestRunByName(testName string, testMode string = "EditMode")`

**Returns:** `{ success, started, testName, mode }`

**Notes:**
- `testName` is required. Pass an exact class name (e.g. `MyTestClass`) or
  a fully qualified method name (e.g. `MyNamespace.MyTestClass.MyTest`).
- Only one active Test Runner run at a time is safe.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

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

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
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
            mode = testMode
        });
    }
}
```
