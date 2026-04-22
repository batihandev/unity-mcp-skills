# test_create_playmode

Write a PlayMode test script template and import it. Synchronous — returns
after `AssetDatabase.ImportAsset`. Unity may still domain-reload after the
call, so the next MCP call can see a brief unavailability.

**Signature:** `TestCreatePlayMode(testName string, folder string = "Assets/Tests/Runtime")`

**Returns:** `{ success, path, testName }` — or `{ error }` on validation or
file-exists failure.

**Notes:**
- `testName` must not contain `/`, `\`, or `..`.
- Creates `folder` if it doesn't exist; fails if the target `.cs` already
  exists.
- The runtime test folder needs an `*.asmdef` with `UnityEngine.TestRunner`
  and `UnityEditor.TestRunner` as precompiled references for the test to be
  discovered. This recipe does not create that asmdef.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string testName = "MyPlayModeTest";
        string folder = "Assets/Tests/Runtime";

        if (Validate.Required(testName, "testName") is object nameErr)
        {
            result.SetResult(nameErr);
            return;
        }
        if (testName.Contains("/") || testName.Contains("\\") || testName.Contains(".."))
        {
            result.SetResult(new { error = "testName must not contain path separators" });
            return;
        }
        if (Validate.SafePath(folder, "folder") is object folderErr)
        {
            result.SetResult(folderErr);
            return;
        }
        if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
        var path = System.IO.Path.Combine(folder, testName + ".cs").Replace('\\', '/');
        if (System.IO.File.Exists(path))
        {
            result.SetResult(new { error = $"File already exists: {path}" });
            return;
        }
        var content = $@"using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class {testName}
{{
    [UnityTest]
    public IEnumerator SamplePlayModeTest()
    {{
        yield return null;
        Assert.Pass();
    }}
}}
";
        System.IO.File.WriteAllText(path, content, SkillsCommon.Utf8NoBom);
        AssetDatabase.ImportAsset(path);

        result.SetResult(new { success = true, path, testName });
    }
}
```
