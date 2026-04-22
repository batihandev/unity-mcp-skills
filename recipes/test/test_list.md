# test_list

Kick off test discovery via `TestRunnerApi.RetrieveTestList` and cache the result under `Temp/test-list-<mode>.json`. Fire-and-forget — the callback fires asynchronously via `EditorApplication.delayCall`.

**Signature:** `TestList(testMode string = "EditMode")`

**Returns:** `{ success, started, mode, cachePath, note }`

**Notes:**
- `testMode` must be `"EditMode"` or `"PlayMode"`.
- Read results via `test_list_read` (full list) or `test_list_categories` (distinct `[Category]` values) in a subsequent call.
- Previous cache is overwritten.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using System.IO;
using System.Text;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string testMode = "EditMode";

        TestMode mode;
        if (string.Equals(testMode, "EditMode", System.StringComparison.OrdinalIgnoreCase))
            mode = TestMode.EditMode;
        else if (string.Equals(testMode, "PlayMode", System.StringComparison.OrdinalIgnoreCase))
            mode = TestMode.PlayMode;
        else
        {
            result.SetResult(new { error = "testMode must be EditMode or PlayMode, got " + testMode });
            return;
        }

        var cachePath = "Temp/test-list-" + testMode + ".json";
        Directory.CreateDirectory("Temp");

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RetrieveTestList(mode, root =>
        {
            var sb = new StringBuilder();
            sb.Append("{\"mode\":\"").Append(testMode).Append("\",\"tests\":[");
            bool first = true;
            WalkTests(root, sb, ref first);
            sb.Append("]}");
            File.WriteAllText(cachePath, sb.ToString());
        });

        result.SetResult(new
        {
            success = true,
            started = true,
            mode = testMode,
            cachePath,
            note = "Callback writes the cache asynchronously. Call test_list_read or test_list_categories in a later Unity_RunCommand."
        });
    }

    private static void WalkTests(ITestAdaptor node, StringBuilder sb, ref bool first)
    {
        if (!node.IsSuite)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append("{\"name\":\"").Append(Esc(node.Name))
              .Append("\",\"fullName\":\"").Append(Esc(node.FullName))
              .Append("\",\"runState\":\"").Append(Esc(node.RunState.ToString()))
              .Append("\",\"categories\":[");
            var cats = node.Categories;
            if (cats != null)
                for (int i = 0; i < cats.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append("\"").Append(Esc(cats[i])).Append("\"");
                }
            sb.Append("]}");
        }
        if (node.Children != null)
            foreach (var c in node.Children) WalkTests(c, sb, ref first);
    }

    private static string Esc(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c == '"') sb.Append("\\\"");
            else if (c == '\\') sb.Append("\\\\");
            else if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
```
