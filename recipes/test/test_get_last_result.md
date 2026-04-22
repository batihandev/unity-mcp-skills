# test_get_last_result

Read the newest XML report anywhere under `TestResults/` (any mode). Stateless
— no job ID required. Alias of `test_get_result` with the "newest across all
modes" policy.

**Signature:** `TestGetLastResult()`

**Returns:** `{ success, file, mode, total, passed, failed, skipped, inconclusive, failedNames, startTime, endTime, durationSeconds }` — or `{ error }` if no report exists.

**Notes:**
- `mode` is inferred from the filename prefix (`EditMode-*`, `PlayMode-*`),
  `"unknown"` otherwise.
- Picks the single file with the newest `LastWriteTimeUtc`.
- Parsed with `IndexOf` scans because `System.Xml` and
  `System.Text.RegularExpressions` are both unavailable in the
  `Unity_RunCommand` dynamic compile context.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal static class _NUnitXml
{
    public static bool FindTag(string xml, string tag, int start, out int tagStart, out int tagEnd)
    {
        tagStart = -1; tagEnd = -1;
        var open = "<" + tag;
        int i = xml.IndexOf(open, start, System.StringComparison.Ordinal);
        if (i < 0) return false;
        int next = i + open.Length;
        if (next >= xml.Length) return false;
        char c = xml[next];
        if (c != ' ' && c != '>' && c != '\t' && c != '\r' && c != '\n' && c != '/') return false;
        int gt = xml.IndexOf('>', next);
        if (gt < 0) return false;
        tagStart = i;
        tagEnd = gt;
        return true;
    }

    public static string GetAttr(string xml, int tagStart, int tagEnd, string name)
    {
        string key = " " + name + "=\"";
        int i = xml.IndexOf(key, tagStart, tagEnd - tagStart, System.StringComparison.Ordinal);
        if (i < 0) return null;
        int v = i + key.Length;
        int q = xml.IndexOf('"', v);
        if (q < 0 || q > tagEnd) return null;
        return xml.Substring(v, q - v);
    }

    public static int GetInt(string xml, int tagStart, int tagEnd, string name)
    {
        var s = GetAttr(xml, tagStart, tagEnd, name);
        int n;
        return (s != null && int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out n)) ? n : 0;
    }
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var root = System.IO.Directory.GetParent(Application.dataPath).FullName;
        var dir = System.IO.Path.Combine(root, "TestResults");
        if (!System.IO.Directory.Exists(dir))
        {
            result.SetResult(new { error = $"TestResults directory not found: {dir}" });
            return;
        }
        var files = System.IO.Directory.GetFiles(dir, "*.xml");
        if (files.Length == 0)
        {
            result.SetResult(new { error = $"No test result XML in {dir}" });
            return;
        }

        string newest = null;
        var newestTime = System.DateTime.MinValue;
        foreach (var f in files)
        {
            var t = System.IO.File.GetLastWriteTimeUtc(f);
            if (t > newestTime) { newestTime = t; newest = f; }
        }

        var fileName = System.IO.Path.GetFileName(newest);
        string mode = "unknown";
        if (fileName.StartsWith("EditMode-", System.StringComparison.OrdinalIgnoreCase)) mode = "EditMode";
        else if (fileName.StartsWith("PlayMode-", System.StringComparison.OrdinalIgnoreCase)) mode = "PlayMode";

        var xml = System.IO.File.ReadAllText(newest);
        int runStart, runEnd;
        if (!_NUnitXml.FindTag(xml, "test-run", 0, out runStart, out runEnd))
        {
            result.SetResult(new { error = $"XML has no <test-run> root: {newest}" });
            return;
        }

        int total = _NUnitXml.GetInt(xml, runStart, runEnd, "total");
        int passed = _NUnitXml.GetInt(xml, runStart, runEnd, "passed");
        int failed = _NUnitXml.GetInt(xml, runStart, runEnd, "failed");
        int skipped = _NUnitXml.GetInt(xml, runStart, runEnd, "skipped");
        int inconclusive = _NUnitXml.GetInt(xml, runStart, runEnd, "inconclusive");
        double duration = 0;
        var durStr = _NUnitXml.GetAttr(xml, runStart, runEnd, "duration");
        if (durStr != null)
            double.TryParse(durStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out duration);

        var failedNames = new System.Collections.Generic.List<string>();
        int cursor = 0;
        while (true)
        {
            int cStart, cEnd;
            if (!_NUnitXml.FindTag(xml, "test-case", cursor, out cStart, out cEnd)) break;
            cursor = cEnd + 1;
            var res = _NUnitXml.GetAttr(xml, cStart, cEnd, "result");
            if (!string.Equals(res, "Failed", System.StringComparison.Ordinal)) continue;
            var full = _NUnitXml.GetAttr(xml, cStart, cEnd, "fullname")
                       ?? _NUnitXml.GetAttr(xml, cStart, cEnd, "name");
            if (!string.IsNullOrEmpty(full)) failedNames.Add(full);
        }

        result.SetResult(new
        {
            success = true,
            file = newest.Replace('\\', '/'),
            mode,
            total,
            passed,
            failed,
            skipped,
            inconclusive,
            failedNames = failedNames.ToArray(),
            startTime = _NUnitXml.GetAttr(xml, runStart, runEnd, "start-time"),
            endTime = _NUnitXml.GetAttr(xml, runStart, runEnd, "end-time"),
            durationSeconds = duration
        });
    }
}
```
