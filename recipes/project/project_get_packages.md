# project_get_packages

Read the project's UPM `Packages/manifest.json` and return its raw text + a hand-parsed `dependencies` map.

**Signature:** `ProjectGetPackages()`

**Returns:** `{ success, dependencies, manifestJson }`

## Notes
- Returns `{ error: "manifest.json not found" }` when `Packages/manifest.json` does not exist.
- `manifestJson` is the raw file text, for callers who need `scopedRegistries` / `lock` / etc.
- No package resolution is performed; this mirrors the on-disk manifest.

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
        var manifestPath = "Packages/manifest.json";
        if (!File.Exists(manifestPath))
        {
            result.SetResult(new { error = "manifest.json not found" });
            return;
        }

        var json = File.ReadAllText(manifestPath, System.Text.Encoding.UTF8);
        var deps = ParseDependencies(json);

        result.SetResult(new { success = true, dependencies = deps, manifestJson = json });
    }

    // Hand-parse the "dependencies": { "id": "version", ... } section.
    // Newtonsoft.Json is unavailable in the Unity_RunCommand dynamic compile context.
    private static Dictionary<string, string> ParseDependencies(string json)
    {
        var result = new Dictionary<string, string>();
        int i = json.IndexOf("\"dependencies\"");
        if (i < 0) return result;
        int lb = json.IndexOf('{', i);
        if (lb < 0) return result;
        int depth = 1;
        int j = lb + 1;
        int rb = -1;
        while (j < json.Length && depth > 0)
        {
            char c = json[j];
            if (c == '"')
            {
                j++;
                while (j < json.Length)
                {
                    if (json[j] == '\\' && j + 1 < json.Length) { j += 2; continue; }
                    if (json[j] == '"') break;
                    j++;
                }
            }
            else if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) { rb = j; break; } }
            j++;
        }
        if (rb < 0) return result;

        var slice = json.Substring(lb + 1, rb - lb - 1);
        int k = 0;
        while (k < slice.Length)
        {
            while (k < slice.Length && slice[k] != '"') k++;
            if (k >= slice.Length) break;
            int ks = k + 1;
            int ke = ks;
            while (ke < slice.Length)
            {
                if (slice[ke] == '\\' && ke + 1 < slice.Length) { ke += 2; continue; }
                if (slice[ke] == '"') break;
                ke++;
            }
            string key = slice.Substring(ks, ke - ks);
            k = ke + 1;

            while (k < slice.Length && slice[k] != ':') k++;
            if (k >= slice.Length) break;
            k++;
            while (k < slice.Length && slice[k] != '"') k++;
            if (k >= slice.Length) break;
            int vs = k + 1;
            int ve = vs;
            while (ve < slice.Length)
            {
                if (slice[ve] == '\\' && ve + 1 < slice.Length) { ve += 2; continue; }
                if (slice[ve] == '"') break;
                ve++;
            }
            string val = slice.Substring(vs, ve - vs);
            result[key] = val;
            k = ve + 1;
        }
        return result;
    }
}
```
