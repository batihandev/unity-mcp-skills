# test_get_summary

Aggregate every XML report under `TestResults/` into a single summary.
Stateless; no job history required.

**Signature:** `TestGetSummary()`

**Returns:** `{ success, totalRuns, totalPassed, totalFailed, totalSkipped, totalInconclusive, allFailedTests, files }`

**Notes:**
- `totalRuns` is the count of readable `<test-run>` XML files in
  `TestResults/`.
- `allFailedTests` is a deduplicated union of failed test `fullname`s across
  every report.
- Reports that don't parse or have no `<test-run>` root are skipped.
- Parsed with `IndexOf` scans because `System.Xml` and
  `System.Text.RegularExpressions` are both unavailable in the
  `Unity_RunCommand` dynamic compile context.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

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
            result.SetResult(new
            {
                success = true,
                totalRuns = 0,
                totalPassed = 0,
                totalFailed = 0,
                totalSkipped = 0,
                totalInconclusive = 0,
                allFailedTests = new string[0],
                files = new string[0]
            });
            return;
        }
        var files = System.IO.Directory.GetFiles(dir, "*.xml");

        int totalRuns = 0, totalPassed = 0, totalFailed = 0, totalSkipped = 0, totalInconclusive = 0;
        var failedSet = new System.Collections.Generic.HashSet<string>();
        var readFiles = new System.Collections.Generic.List<string>();

        foreach (var f in files)
        {
            string xml;
            try { xml = System.IO.File.ReadAllText(f); }
            catch { continue; }

            int runStart, runEnd;
            if (!_NUnitXml.FindTag(xml, "test-run", 0, out runStart, out runEnd)) continue;

            totalRuns++;
            readFiles.Add(f.Replace('\\', '/'));
            totalPassed += _NUnitXml.GetInt(xml, runStart, runEnd, "passed");
            totalFailed += _NUnitXml.GetInt(xml, runStart, runEnd, "failed");
            totalSkipped += _NUnitXml.GetInt(xml, runStart, runEnd, "skipped");
            totalInconclusive += _NUnitXml.GetInt(xml, runStart, runEnd, "inconclusive");

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
                if (!string.IsNullOrEmpty(full)) failedSet.Add(full);
            }
        }

        var allFailed = new string[failedSet.Count];
        failedSet.CopyTo(allFailed);

        result.SetResult(new
        {
            success = true,
            totalRuns,
            totalPassed,
            totalFailed,
            totalSkipped,
            totalInconclusive,
            allFailedTests = allFailed,
            files = readFiles.ToArray()
        });
    }
}
```
