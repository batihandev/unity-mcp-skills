# test_list

List available Unity tests discovered from source files.

**Signature:** `TestList(testMode string = "EditMode", limit int = 100)`

**Returns:** `{ success, testMode, count, discoveryMode, tests }`

**Notes:**
- Discovery uses source-scan with file dependencies — no test execution occurs
- Each entry in `tests` contains `name`, `fullName`, and `runState`
- `limit` is clamped to a minimum of 1 via `Mathf.Max`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string testMode = "EditMode";
        int limit = 100;

        var tests = DiscoverTests(testMode)
            .Take(Mathf.Max(1, limit))
            .Select(test => new
            {
                name = test.Name,
                fullName = test.FullName,
                runState = test.RunState
            })
            .ToArray();

        result.SetResult(new
        {
            success = true,
            testMode,
            count = tests.Length,
            discoveryMode = "source_scan_with_file_dependencies",
            tests
        });
    }
}
```
