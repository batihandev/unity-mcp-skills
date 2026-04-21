# test_create_editmode

Create an EditMode test script template and return a compile-monitor job.

**Signature:** `TestCreateEditMode(testName string, folder string = "Assets/Tests/Editor")`

**Returns:** `{ success, status, path, testName, jobId, serverAvailability }`

**Notes:**
- `testName` must not contain path separators (`/`, `\`, or `..`)
- Creates the folder if it does not exist; returns an error if the file already exists
- Triggers a script mutation job (`jobId`) to monitor compile status after file creation
- `serverAvailability` always includes a transient unavailability notice due to domain reload

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
        var path = System.IO.Path.Combine(folder, testName + ".cs");
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
        var job = AsyncJobService.StartScriptMutationJob("test_create_editmode", path.Replace("\\", "/"), true, 20);
        result.SetResult(new
        {
            success = true,
            status = "accepted",
            path,
            testName,
            jobId = job.jobId,
            serverAvailability = ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                $"已创建测试脚本: {path}。Unity 可能短暂重载脚本域。",
                alwaysInclude: true)
        });
    }
}
```
