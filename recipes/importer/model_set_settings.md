# model_set_settings

Set one or more importer settings on a model asset and reimport.

## Signature

```
model_set_settings(
  assetPath: string,
  globalScale?: float,
  useFileScale?: bool,
  importBlendShapes?: bool,
  importVisibility?: bool,
  importCameras?: bool,
  importLights?: bool,
  meshCompression?: string,
  isReadable?: bool,
  optimizeMeshPolygons?: bool,
  optimizeMeshVertices?: bool,
  generateSecondaryUV?: bool,
  keepQuads?: bool,
  weldVertices?: bool,
  importNormals?: string,
  importTangents?: string,
  animationType?: string,
  importAnimation?: bool,
  materialImportMode?: string
) → { success, path, changesApplied, changes }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace
        float? globalScale = null;
        bool? useFileScale = null;
        bool? importBlendShapes = null;
        bool? importVisibility = null;
        bool? importCameras = false;
        bool? importLights = false;
        string meshCompression = null;
        bool? isReadable = null;
        bool? optimizeMeshPolygons = null;
        bool? optimizeMeshVertices = null;
        bool? generateSecondaryUV = null;
        bool? keepQuads = null;
        bool? weldVertices = null;
        string importNormals = null;
        string importTangents = null;
        string animationType = "Humanoid";
        bool? importAnimation = null;
        string materialImportMode = null;

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            { result.SetResult(new { error = $"Not a model file or asset not found: {assetPath}" }); return; }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        var changes = new List<string>();

        if (globalScale.HasValue)        { importer.globalScale        = globalScale.Value;        changes.Add($"globalScale={globalScale.Value}"); }
        if (useFileScale.HasValue)       { importer.useFileScale       = useFileScale.Value;       changes.Add($"useFileScale={useFileScale.Value}"); }
        if (importBlendShapes.HasValue)  { importer.importBlendShapes  = importBlendShapes.Value;  changes.Add($"importBlendShapes={importBlendShapes.Value}"); }
        if (importVisibility.HasValue)   { importer.importVisibility   = importVisibility.Value;   changes.Add($"importVisibility={importVisibility.Value}"); }
        if (importCameras.HasValue)      { importer.importCameras      = importCameras.Value;      changes.Add($"importCameras={importCameras.Value}"); }
        if (importLights.HasValue)       { importer.importLights       = importLights.Value;       changes.Add($"importLights={importLights.Value}"); }
        if (isReadable.HasValue)         { importer.isReadable         = isReadable.Value;         changes.Add($"isReadable={isReadable.Value}"); }
        if (optimizeMeshPolygons.HasValue){ importer.optimizeMeshPolygons = optimizeMeshPolygons.Value; changes.Add($"optimizeMeshPolygons={optimizeMeshPolygons.Value}"); }
        if (optimizeMeshVertices.HasValue){ importer.optimizeMeshVertices = optimizeMeshVertices.Value; changes.Add($"optimizeMeshVertices={optimizeMeshVertices.Value}"); }
        if (generateSecondaryUV.HasValue){ importer.generateSecondaryUV = generateSecondaryUV.Value; changes.Add($"generateSecondaryUV={generateSecondaryUV.Value}"); }
        if (keepQuads.HasValue)          { importer.keepQuads          = keepQuads.Value;          changes.Add($"keepQuads={keepQuads.Value}"); }
        if (weldVertices.HasValue)       { importer.weldVertices       = weldVertices.Value;       changes.Add($"weldVertices={weldVertices.Value}"); }
        if (importAnimation.HasValue)    { importer.importAnimation    = importAnimation.Value;    changes.Add($"importAnimation={importAnimation.Value}"); }

        if (!string.IsNullOrEmpty(meshCompression))
        {
            if (System.Enum.TryParse<ModelImporterMeshCompression>(meshCompression, true, out var mc))
            { importer.meshCompression = mc; changes.Add($"meshCompression={mc}"); }
            else { result.SetResult(new { error = $"Invalid meshCompression: {meshCompression}. Valid: Off, Low, Medium, High" }); return; }
        }

        if (!string.IsNullOrEmpty(importNormals) &&
            System.Enum.TryParse<ModelImporterNormals>(importNormals, true, out var normals))
        { importer.importNormals = normals; changes.Add($"importNormals={normals}"); }

        if (!string.IsNullOrEmpty(importTangents) &&
            System.Enum.TryParse<ModelImporterTangents>(importTangents, true, out var tangents))
        { importer.importTangents = tangents; changes.Add($"importTangents={tangents}"); }

        if (!string.IsNullOrEmpty(animationType))
        {
            if (System.Enum.TryParse<ModelImporterAnimationType>(animationType, true, out var at))
            { importer.animationType = at; changes.Add($"animationType={at}"); }
            else { result.SetResult(new { error = $"Invalid animationType: {animationType}. Valid: None, Legacy, Generic, Humanoid" }); return; }
        }

        if (!string.IsNullOrEmpty(materialImportMode) &&
            System.Enum.TryParse<ModelImporterMaterialImportMode>(materialImportMode, true, out var mim))
        { importer.materialImportMode = mim; changes.Add($"materialImportMode={mim}"); }

        importer.SaveAndReimport();

        { result.SetResult(new { success = true, path = assetPath, changesApplied = changes.Count, changes }); return; }
    }
}
```

## Notes

- For static props, typically disable `importCameras`, `importLights`, `importAnimation`.
- After changing `animationType` to `Humanoid`, Unity may prompt for avatar setup — follow with `asset_reimport`.
- `SaveAndReimport()` is always called.
