# timeline_play

Play, pause, or stop a Timeline in the Editor preview.

**Signature:** `TimelinePlay(name string = null, instanceId int = 0, path string = null, action string = "play")`

**Returns:** `{ success, action, time }`

**Notes:**
- Valid `action` values: `play`, `pause`, `stop` (case-insensitive)
- This controls the Editor-mode preview; it is not the same as runtime playback
- `time` reflects `director.time` immediately after the action

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string action = "play";

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var director = go.GetComponent<PlayableDirector>();
        if (director == null) { result.SetResult(new { error = "PlayableDirector not found" }); return; }

        switch (action.ToLower())
        {
            case "play":  director.Play();  break;
            case "pause": director.Pause(); break;
            case "stop":  director.Stop();  break;
            default:
                result.SetResult(new { error = $"Unknown action: {action}. Use play/pause/stop" });
                return;
        }

        result.SetResult(new { success = true, action, time = director.time });
    }
}
```
