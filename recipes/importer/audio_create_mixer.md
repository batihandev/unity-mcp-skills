# audio_create_mixer

Create a new AudioMixer asset in the project.

## Signature

```
audio_create_mixer(mixerName?: string = "NewAudioMixer", folder?: string = "Assets")
  → { success, path, name }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

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

        if (Validate.Required(mixerName, "mixerName") is object nameErr) { result.SetResult(nameErr); return; }
        if (mixerName.Contains("/") || mixerName.Contains("\\") || mixerName.Contains(".."))
            { result.SetResult(new { error = "mixerName must not contain path separators" }); return; }
        if (Validate.SafePath(folder, "folder") is object pathErr) { result.SetResult(pathErr); return; }
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        var savePath = Path.Combine(folder, mixerName + ".mixer").Replace("\\", "/");
        if (File.Exists(savePath)) { result.SetResult(new { error = $"Mixer already exists: {savePath}" }); return; }

        // Find AudioMixerController type (location varies by Unity version)
        System.Type mixerType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            mixerType = asm.GetType("UnityEditor.Audio.AudioMixerController");
            if (mixerType != null) break;
        }
        if (mixerType == null) { result.SetResult(new { error = "AudioMixerController type not found" }); return; }

        // BindingFlags unavailable in Unity_RunCommand context; GetMethods() returns public members only
        System.Reflection.MethodInfo createMethod = null;
        foreach (var m in mixerType.GetMethods())
        {
            if (m.IsStatic && m.Name == "CreateMixerControllerAtPath") { createMethod = m; break; }
        }

        if (createMethod != null)
        {
            var output = createMethod.Invoke(null, new object[] { savePath });
            if (output != null)
            {
                AssetDatabase.SaveAssets();
                { result.SetResult(new { success = true, path = savePath, name = mixerName }); return; }
            }
        }

        // Fallback: ScriptableObject.CreateInstance (may log warnings in Unity 6+)
        var mixer = ScriptableObject.CreateInstance(mixerType);
        if (mixer != null)
        {
            mixer.name = mixerName;
            AssetDatabase.CreateAsset(mixer, savePath);
            AssetDatabase.SaveAssets();
            { result.SetResult(new { success = true, path = savePath, name = mixerName }); return; }
        }

        { result.SetResult(new { error = "Failed to create AudioMixer. Use Assets > Create > Audio > Audio Mixer manually." }); return; }
    }
}
```

## Notes

- Uses reflection to call Unity's internal `CreateMixerControllerAtPath` factory for proper mixer initialisation.
- If the primary method is unavailable (older/newer Unity), falls back to `ScriptableObject.CreateInstance` which may log a warning in Unity 6+.
- The mixer is created with a single default master group.
