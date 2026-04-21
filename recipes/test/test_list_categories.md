# test_list_categories

List all NUnit `[Category]` attribute values found in discovered tests.

**Signature:** `TestListCategories(testMode string = "EditMode")`

**Returns:** `{ success, count, categories, discoveryMode, note }`

**Notes:**
- Categories are collected via source-scan; no tests are executed
- Results are deduplicated and sorted case-insensitively
- `note` is populated only when zero categories are found

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

        var categories = DiscoverTests(testMode)
            .SelectMany(test => test.Categories ?? Array.Empty<string>())
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        result.SetResult(new
        {
            success = true,
            count = categories.Length,
            categories,
            discoveryMode = "source_scan_with_file_dependencies",
            note = categories.Length == 0
                ? "No [Category] attributes were found in discovered tests."
                : null
        });
    }
}
```
