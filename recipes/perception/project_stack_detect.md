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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`project_skills`](../_shared/project_skills.md), [`perception_helpers`](../_shared/perception_helpers.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

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
        var metrics = PerceptionHelpers.CollectSceneMetrics(includeComponentStats: true);
        var packageIds = PerceptionHelpers.ReadInstalledPackageIds();
        var hasUiToolkitAssets = AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { "Assets" }).Length > 0
            || AssetDatabase.FindAssets("t:PanelSettings", new[] { "Assets" }).Length > 0;

        bool Has(string id) => PerceptionHelpers.ContainsIgnoreCase(packageIds, id);

        var cinemachineDetected = Has("com.unity.cinemachine")
            || PerceptionHelpers.FindTypeInAssemblies("Cinemachine.CinemachineBrain") != null
            || PerceptionHelpers.FindTypeInAssemblies("Unity.Cinemachine.CinemachineBrain") != null;
        var timelineDetected = Has("com.unity.timeline")
            || AssetDatabase.FindAssets("t:TimelineAsset", new[] { "Assets" }).Length > 0;
        var navMeshDetected = Has("com.unity.ai.navigation")
            || PerceptionHelpers.FindTypeInAssemblies("Unity.AI.Navigation.NavMeshSurface") != null;
        var xrDetected = Has("com.unity.xr.interaction.toolkit")
            || Has("com.unity.xr.management")
            || PerceptionHelpers.FindTypeInAssemblies("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager") != null;
        var proBuilderDetected = Has("com.unity.probuilder")
            || PerceptionHelpers.FindTypeInAssemblies("UnityEngine.ProBuilder.ProBuilderMesh") != null;
        var inputSystemDetected = Has("com.unity.inputsystem");

        var uiRoute = PerceptionHelpers.DetermineUiRoute(metrics, hasUiToolkitAssets);
        var inputHandling = PerceptionHelpers.DetectInputHandling(packageIds);

        var testAsmdefs = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        result.SetResult(new
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
                uguiDetected = metrics.Canvases > 0 || metrics.HasUiGraphic || Has("com.unity.ugui"),
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
                detected = Directory.Exists("Assets/Tests") || testAsmdefs.Any(p => Path.GetFileNameWithoutExtension(p).IndexOf("Test", StringComparison.OrdinalIgnoreCase) >= 0),
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
            projectProfile = PerceptionHelpers.DetermineProjectProfile(metrics, xrDetected, uiRoute)
        });
    }
}
```

## Notes

- Detects packages from `manifest.json` and via reflection on loaded assemblies.
- `projectProfile` is a synthesized label (`"2D"`, `"3D"`, `"XR"`, `"UI"`) based on detected stack.
- Run this before writing any generation or setup code to avoid wrong shader/input-system assumptions.
