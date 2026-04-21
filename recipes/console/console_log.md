# console_log

Write a custom message to the Unity console.

**Signature:** `ConsoleLog(string message, string type = "log")`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `message` | string | Yes | — | Text to log |
| `type` | string | No | `"log"` | `log`, `warning`, or `error` (case-insensitive) |

**Returns:** `{ success, logged }`

## Notes

- `type` matching is case-insensitive: `"Log"`, `"log"`, and `"LOG"` all produce `Debug.Log`.
- Any unrecognized type value defaults to `Debug.Log`.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string message = "Hello from AI agent";
        string type = "log";  // log | warning | error

        switch (type.ToLower())
        {
            case "warning":
                Debug.LogWarning(message);
                break;
            case "error":
                Debug.LogError(message);
                break;
            default:
                Debug.Log(message);
                break;
        }
        result.Return(new { success = true, logged = message });
    }
}
```
