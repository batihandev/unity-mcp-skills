# project_stack_detect

**Skill:** `project_stack_detect`
**C# method:** `PerceptionSkills.ProjectStackDetect`

## Signature

```
ProjectStackDetect()
```

## Parameters

None.

## Return Shape

Returns `success`, `unityVersion`, `renderPipeline` (type, defaultShader, unlitShader), `input` (mode, inputSystemInstalled, legacyInputManagerAvailable), `ui` (route, uguiDetected, uiToolkitDetected), `packages` (cinemachine, timeline, navMesh, xr, proBuilder, inputSystem), `tests` (detected, nunitLoaded), `projectFolders` (scripts, scenes, prefabs, materials, tests), and `projectProfile`.

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var metrics = CollectSceneMetrics(includeComponentStats: true);
        var packageIds = ReadInstalledPackageIds();
        var hasUiToolkitAssets = AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { "Assets" }).Length > 0
            || AssetDatabase.FindAssets("t:PanelSettings", new[] { "Assets" }).Length > 0;

        var cinemachineDetected = packageIds.Contains("com.unity.cinemachine")
            || FindTypeInAssemblies("Cinemachine.CinemachineBrain") != null
            || FindTypeInAssemblies("Unity.Cinemachine.CinemachineBrain") != null;
        var timelineDetected = packageIds.Contains("com.unity.timeline")
            || AssetDatabase.FindAssets("t:TimelineAsset", new[] { "Assets" }).Length > 0;
        var navMeshDetected = packageIds.Contains("com.unity.ai.navigation")
            || FindTypeInAssemblies("Unity.AI.Navigation.NavMeshSurface") != null;
        var xrDetected = packageIds.Contains("com.unity.xr.interaction.toolkit")
            || packageIds.Contains("com.unity.xr.management")
            || FindTypeInAssemblies("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager") != null;
        var proBuilderDetected = packageIds.Contains("com.unity.probuilder")
            || FindTypeInAssemblies("UnityEngine.ProBuilder.ProBuilderMesh") != null;
        var inputSystemDetected = packageIds.Contains("com.unity.inputsystem");
        var uiRoute = DetermineUiRoute(metrics, hasUiToolkitAssets);
        var inputHandling = DetectInputHandling(packageIds);
        var testAsmdefs = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .ToArray();

        result.SetValue(new
        {
            success = true,
            unityVersion = Application.unityVersion,
            renderPipeline = new
            {
                type = ProjectSkills.DetectRenderPipeline().ToString(),
                defaultShader = ProjectSkills.GetDefaultShaderName(),
                unlitShader = ProjectSkills.GetUnlitShaderName()
            },
            input = new
            {
                mode = inputHandling,
                inputSystemInstalled = inputSystemDetected,
                legacyInputManagerAvailable = !inputSystemDetected || inputHandling.IndexOf("Both", StringComparison.OrdinalIgnoreCase) >= 0
            },
            ui = new
            {
                route = uiRoute,
                uguiDetected = metrics.Canvases > 0 || metrics.HasUiGraphic || packageIds.Contains("com.unity.ugui"),
                uiToolkitDetected = metrics.HasUiToolkitDocument || hasUiToolkitAssets
            },
            packages = new
            {
                cinemachine = cinemachineDetected,
                timeline = timelineDetected,
                navMesh = navMeshDetected,
                xr = xrDetected,
                proBuilder = proBuilderDetected,
                inputSystem = inputSystemDetected
            },
            tests = new
            {
                detected = Directory.Exists("Assets/Tests") || testAsmdefs.Any(path => Path.GetFileNameWithoutExtension(path).IndexOf("Test", StringComparison.OrdinalIgnoreCase) >= 0),
                nunitLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name.IndexOf("nunit", StringComparison.OrdinalIgnoreCase) >= 0)
            },
            projectFolders = new
            {
                scripts = Directory.Exists("Assets/Scripts"),
                scenes = Directory.Exists("Assets/Scenes"),
                prefabs = Directory.Exists("Assets/Prefabs"),
                materials = Directory.Exists("Assets/Materials"),
                tests = Directory.Exists("Assets/Tests")
            },
            projectProfile = DetermineProjectProfile(metrics, xrDetected, uiRoute)
        });
    }
}
```

## Notes

- Detects packages both from `manifest.json` (`ReadInstalledPackageIds`) and via reflection on loaded assemblies.
- `projectProfile` is a synthesized label (e.g. `"2D"`, `"3D"`, `"XR"`, `"Mobile"`) based on detected stack.
- Run this before writing any generation or setup code to avoid wrong shader/input-system assumptions.
