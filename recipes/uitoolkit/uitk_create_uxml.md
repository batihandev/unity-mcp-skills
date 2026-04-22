# uitk_create_uxml

Create a new UXML layout file for UI Toolkit.

**Signature:** `UitkCreateUxml(savePath string, content string = null, ussPath string = null)`

**Returns:** `{ success, path, lines }`

**Notes:**
- Fails if the file already exists.
- Creates intermediate directories automatically.
- When `ussPath` is in the same directory as `savePath`, a relative `<Style src="..."/>` reference is used.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string savePath = "Assets/UI/MyLayout.uxml";
        string content = null;
        string ussPath = null;

        if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (File.Exists(savePath)) { result.SetResult(new { error = $"File already exists: {savePath}" }); return; }

        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string relUss = null;
        if (!string.IsNullOrEmpty(ussPath))
        {
            var uxmlDir = Path.GetDirectoryName(savePath)?.Replace('\\', '/') ?? "";
            var ussDir  = Path.GetDirectoryName(ussPath)?.Replace('\\', '/') ?? "";
            relUss = (uxmlDir == ussDir) ? Path.GetFileName(ussPath) : ussPath;
        }

        string fileContent;
        if (content != null)
            fileContent = content;
        else if (relUss != null)
            fileContent = $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<engine:UXML xmlns:engine=\"UnityEngine.UIElements\">\n    <Style src=\"{relUss}\" />\n</engine:UXML>\n";
        else
            fileContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<engine:UXML xmlns:engine=\"UnityEngine.UIElements\">\n</engine:UXML>\n";

        File.WriteAllText(savePath, fileContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(savePath);

        result.SetResult(new { success = true, path = savePath, lines = fileContent.Split('\n').Length });
    }
}
```
