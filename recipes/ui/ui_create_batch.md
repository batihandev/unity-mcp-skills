# ui_create_batch

Create multiple UI elements in one call by dispatching to individual create skills.

**Signature:** `UICreateBatch(items string)`

**Returns:** `{ totalRequested, succeeded, failed, results }`

**Notes:**
- `items` is a JSON string array; each element is an object with a `type` field plus the parameters of the matching create skill.
- Supported types: `canvas`, `panel`, `button`, `text`, `image`, `inputfield`, `slider`, `toggle`, `dropdown`, `scrollview`, `rawimage`, `scrollbar`.
- Unknown `type` values throw and are counted as failures.
- Prefer this over N sequential create calls when building menus, HUDs, or repeated widget groups.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string items = "[]";

        result.SetResult(BatchExecutor.Execute<BatchUIItem>(items, item =>
        {
            object itemResult;
            switch ((item.type ?? "").ToLower())
            {
                case "canvas":
                    itemResult = UICreateCanvas(item.name, item.renderMode ?? "ScreenSpaceOverlay");
                    break;
                case "panel":
                    itemResult = UICreatePanel(item.name, item.parent, item.r, item.g, item.b, item.a);
                    break;
                case "button":
                    itemResult = UICreateButton(item.name, item.parent, item.text ?? "Button", item.width, item.height);
                    break;
                case "text":
                    itemResult = UICreateText(item.name, item.parent, item.text ?? "Text", (int)item.fontSize, item.r, item.g, item.b);
                    break;
                case "image":
                    itemResult = UICreateImage(item.name, item.parent, item.spritePath, item.width, item.height);
                    break;
                case "inputfield":
                    itemResult = UICreateInputField(item.name, item.parent, item.placeholder ?? "Enter text...", item.width, item.height);
                    break;
                case "slider":
                    itemResult = UICreateSlider(item.name, item.parent, item.minValue, item.maxValue, item.value, item.width, item.height);
                    break;
                case "toggle":
                    itemResult = UICreateToggle(item.name, item.parent, item.label ?? "Toggle", item.isOn);
                    break;
                case "dropdown":
                    itemResult = UICreateDropdown(item.name, item.parent, item.options, item.width, item.height);
                    break;
                case "scrollview":
                    itemResult = UICreateScrollview(item.name, item.parent, item.width, item.height);
                    break;
                case "rawimage":
                    itemResult = UICreateRawImage(item.name, item.parent, item.texturePath, item.width, item.height);
                    break;
                case "scrollbar":
                    itemResult = UICreateScrollbar(item.name, item.parent, item.direction ?? "BottomToTop", item.value, item.size, (int)item.numberOfSteps);
                    break;
                default:
                    throw new System.Exception($"Unknown UI type: {item.type}");
            }
            return itemResult;
        }, item => item.type));
    }
}
```
