# test_list_read

Read the cached test list produced by a prior `test_list` call. Stateless — no test discovery happens here.

**Signature:** `TestListRead(testMode string = "EditMode", limit int = 100)`

**Returns:** `{ success, testMode, count, tests }` or `{ error }` if cache is missing.

**Notes:**
- Cache path is `Temp/test-list-<testMode>.json`, written by `test_list`.
- If the cache doesn't exist, the caller needs to run `test_list` first and wait one or two seconds for the async callback to write the file.
- `limit` is clamped to a minimum of 1.

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
        int limit = 100;

        var cachePath = "Temp/test-list-" + testMode + ".json";
        if (!File.Exists(cachePath))
        {
            result.SetResult(new { error = "Test list cache missing: " + cachePath + ". Run test_list first and retry after ~1s." });
            return;
        }

        var raw = File.ReadAllText(cachePath);
        var tests = ParseTestArray(raw);
        var clamped = Mathf.Max(1, limit);
        var truncated = tests.Count > clamped ? tests.GetRange(0, clamped) : tests;

        result.SetResult(new
        {
            success = true,
            testMode,
            count = truncated.Count,
            totalAvailable = tests.Count,
            tests = truncated
        });
    }

    // Minimal hand-rolled parser for the flat JSON shape produced by test_list:
    // { "mode": "...", "tests": [ { "name", "fullName", "runState", "categories": [...] } ] }
    private static List<object> ParseTestArray(string raw)
    {
        var list = new List<object>();
        int i = raw.IndexOf("\"tests\"");
        if (i < 0) return list;
        i = raw.IndexOf('[', i);
        if (i < 0) return list;
        i++;

        while (i < raw.Length)
        {
            while (i < raw.Length && (raw[i] == ' ' || raw[i] == ',' || raw[i] == '\n' || raw[i] == '\r' || raw[i] == '\t')) i++;
            if (i >= raw.Length || raw[i] == ']') break;
            if (raw[i] != '{') { i++; continue; }
            int end = FindObjectEnd(raw, i);
            if (end < 0) break;
            list.Add(ParseOne(raw.Substring(i, end - i + 1)));
            i = end + 1;
        }
        return list;
    }

    private static int FindObjectEnd(string s, int start)
    {
        int depth = 0;
        bool inStr = false;
        for (int i = start; i < s.Length; i++)
        {
            char c = s[i];
            if (inStr)
            {
                if (c == '\\' && i + 1 < s.Length) { i++; continue; }
                if (c == '"') inStr = false;
                continue;
            }
            if (c == '"') { inStr = true; continue; }
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    private static object ParseOne(string obj)
    {
        return new
        {
            name = ReadStringField(obj, "name"),
            fullName = ReadStringField(obj, "fullName"),
            runState = ReadStringField(obj, "runState"),
            categories = ReadStringArray(obj, "categories"),
        };
    }

    private static string ReadStringField(string obj, string key)
    {
        var marker = "\"" + key + "\"";
        int k = obj.IndexOf(marker);
        if (k < 0) return null;
        int q = obj.IndexOf('"', k + marker.Length);
        if (q < 0) return null;
        int end = q + 1;
        while (end < obj.Length)
        {
            if (obj[end] == '\\' && end + 1 < obj.Length) { end += 2; continue; }
            if (obj[end] == '"') break;
            end++;
        }
        return Unescape(obj.Substring(q + 1, end - q - 1));
    }

    private static string[] ReadStringArray(string obj, string key)
    {
        var marker = "\"" + key + "\"";
        int k = obj.IndexOf(marker);
        if (k < 0) return System.Array.Empty<string>();
        int lb = obj.IndexOf('[', k + marker.Length);
        if (lb < 0) return System.Array.Empty<string>();
        int rb = obj.IndexOf(']', lb);
        if (rb < 0) return System.Array.Empty<string>();
        var slice = obj.Substring(lb + 1, rb - lb - 1);
        var results = new List<string>();
        int i = 0;
        while (i < slice.Length)
        {
            while (i < slice.Length && slice[i] != '"') i++;
            if (i >= slice.Length) break;
            int q = i + 1;
            int end = q;
            while (end < slice.Length)
            {
                if (slice[end] == '\\' && end + 1 < slice.Length) { end += 2; continue; }
                if (slice[end] == '"') break;
                end++;
            }
            results.Add(Unescape(slice.Substring(q, end - q)));
            i = end + 1;
        }
        return results.ToArray();
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
