# test_create_editmode

Write an EditMode test script template and import it. Synchronous — returns
after `AssetDatabase.ImportAsset`. Unity may still domain-reload after the
call, so the next MCP call can see a brief unavailability.

**Signature:** `TestCreateEditMode(testName string, folder string = "Assets/Tests/Editor")`

**Returns:** `{ success, path, testName }` — or `{ error }` on validation or
file-exists failure.

**Notes:**
- `testName` must not contain `/`, `\`, or `..`.
- Creates `folder` if it doesn't exist; fails if the target `.cs` already
  exists.
- No job ID — domain reload is Unity's business, not this recipe's.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code
block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/skills_common.md` — for `SkillsCommon.Utf8NoBom`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string testName = "MyEditModeTest";
        string folder = "Assets/Tests/Editor";

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
        var content = $@"using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class {testName}
{{
    [Test]
    public void SampleTest()
    {{
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
