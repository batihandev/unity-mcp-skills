# test_list_categories

List distinct NUnit `[Category]` values from the test list cache. Stateless — reads the cache produced by a prior `test_list` call.

**Signature:** `TestListCategories(testMode string = "EditMode")`

**Returns:** `{ success, count, categories, testMode }` or `{ error }` if cache is missing.

**Notes:**
- Cache path is `Temp/test-list-<testMode>.json`, written by `test_list`.
- Categories are deduplicated case-insensitively and sorted.
- If the cache doesn't exist, the caller needs to run `test_list` first and retry after ~1s.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string testMode = "EditMode";

        var cachePath = "Temp/test-list-" + testMode + ".json";
        if (!File.Exists(cachePath))
        {
            result.SetResult(new { error = "Test list cache missing: " + cachePath + ". Run test_list first and retry after ~1s." });
            return;
        }

        var raw = File.ReadAllText(cachePath);
        var cats = ExtractCategories(raw);

        result.SetResult(new
        {
            success = true,
            testMode,
            count = cats.Length,
            categories = cats,
            note = cats.Length == 0 ? "No [Category] attributes were found in discovered tests." : null
        });
    }

    // Scan the cache for every "categories":["A","B",...] array, dedup case-insensitively, sort.
    // Uses List<string> + manual Contains — HashSet<string>(StringComparer) trips the
    // ISet<> assembly-reference gotcha in the Unity_RunCommand compile context.
    private static string[] ExtractCategories(string raw)
    {
        var list = new List<string>();
        var marker = "\"categories\"";
        int i = 0;
        while (i < raw.Length)
        {
            int k = raw.IndexOf(marker, i, System.StringComparison.Ordinal);
            if (k < 0) break;
            int lb = raw.IndexOf('[', k + marker.Length);
            if (lb < 0) break;
            int rb = raw.IndexOf(']', lb);
            if (rb < 0) break;

            var slice = raw.Substring(lb + 1, rb - lb - 1);
            int j = 0;
            while (j < slice.Length)
            {
                while (j < slice.Length && slice[j] != '"') j++;
                if (j >= slice.Length) break;
                int q = j + 1;
                int end = q;
                while (end < slice.Length)
                {
                    if (slice[end] == '\\' && end + 1 < slice.Length) { end += 2; continue; }
                    if (slice[end] == '"') break;
                    end++;
                }
                var val = Unescape(slice.Substring(q, end - q));
                if (!string.IsNullOrWhiteSpace(val) && !ContainsIgnoreCase(list, val)) list.Add(val);
                j = end + 1;
            }
            i = rb + 1;
        }
        list.Sort(System.StringComparer.OrdinalIgnoreCase);
        return list.ToArray();
    }

    private static bool ContainsIgnoreCase(List<string> list, string val)
    {
        for (int k = 0; k < list.Count; k++)
            if (string.Equals(list[k], val, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static string Unescape(string s)
    {
        if (string.IsNullOrEmpty(s) || s.IndexOf('\\') < 0) return s;
        var sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length) { sb.Append(s[i + 1]); i++; }
            else sb.Append(s[i]);
        }
        return sb.ToString();
    }
}
```
