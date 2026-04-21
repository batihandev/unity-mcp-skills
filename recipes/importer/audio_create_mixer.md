# audio_create_mixer

Create a new AudioMixer asset in the project.

**Skill ID:** `audio_create_mixer`
**Source:** `AudioSkills.cs` — `AudioCreateMixer`

## Signature

```
audio_create_mixer(mixerName?: string = "NewAudioMixer", folder?: string = "Assets")
  → { success, path, name }
```

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `mixerName` | string | no | `"NewAudioMixer"` | Name of the mixer asset (no path separators) |
| `folder` | string | no | `"Assets"` | Destination folder in the project |

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string mixerName = "MusicMixer"; // Replace with desired name
        string folder = "Assets/Audio";  // Replace with destination folder

        if (Validate.Required(mixerName, "mixerName") is object nameErr) return nameErr;
        if (mixerName.Contains("/") || mixerName.Contains("\\") || mixerName.Contains(".."))
            return new { error = "mixerName must not contain path separators" };
        if (Validate.SafePath(folder, "folder") is object pathErr) return pathErr;
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        var savePath = Path.Combine(folder, mixerName + ".mixer").Replace("\\", "/");
        if (File.Exists(savePath)) return new { error = $"Mixer already exists: {savePath}" };

        // Find AudioMixerController type (location varies by Unity version)
        System.Type mixerType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            mixerType = asm.GetType("UnityEditor.Audio.AudioMixerController");
            if (mixerType != null) break;
        }
        if (mixerType == null) return new { error = "AudioMixerController type not found" };

        var createMethod = mixerType.GetMethod("CreateMixerControllerAtPath",
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (createMethod != null)
        {
            var output = createMethod.Invoke(null, new object[] { savePath });
            if (output != null)
            {
                AssetDatabase.SaveAssets();
                return new { success = true, path = savePath, name = mixerName };
            }
        }

        // Fallback: ScriptableObject.CreateInstance (may log warnings in Unity 6+)
        var mixer = ScriptableObject.CreateInstance(mixerType);
        if (mixer != null)
        {
            mixer.name = mixerName;
            AssetDatabase.CreateAsset(mixer, savePath);
            AssetDatabase.SaveAssets();
            return new { success = true, path = savePath, name = mixerName };
        }

        return new { error = "Failed to create AudioMixer. Use Assets > Create > Audio > Audio Mixer manually." };
    }
}
```

## Notes

- Uses reflection to call Unity's internal `CreateMixerControllerAtPath` factory for proper mixer initialisation.
- If the primary method is unavailable (older/newer Unity), falls back to `ScriptableObject.CreateInstance` which may log a warning in Unity 6+.
- The mixer is created with a single default master group.
