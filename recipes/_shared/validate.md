# Validation Recipes

These recipes were extracted from the legacy `ValidationSkills.cs` module. Use these templates in `Unity_RunCommand`.

## ValidateScene
**Signature:** `public static object ValidateScene(bool checkMissingScripts = true, bool checkMissingPrefabs = true, bool checkDuplicateNames = true, bool checkEmptyGameObjects = false)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        bool true = default; // Assign value
        bool true = default; // Assign value
        bool true = default; // Assign value
        bool false = default; // Assign value

        /* Original Logic:

            var issues = new List<ValidationIssue>();
            var scene = SceneManager.GetActiveScene();
            var allObjects = FindHelper.FindAll<GameObject>();

            // Check for missing scripts
            if (checkMissingScripts)
            {
                foreach (var go in allObjects)
                {
                    var components = go.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            issues.Add(new ValidationIssue
                            {
                                type = "MissingScript",
                                severity = "Error",
                                gameObject = go.name,
                                path = GameObjectFinder.GetPath(go),
                                message = $"Missing script at component index {i}"
                            });
                        }
                    }
                }
            }

            // Check for missing prefab references
            if (checkMissingPrefabs)
            {
                foreach (var go in allObjects)
                {
                    if (PrefabUtility.IsPrefabAssetMissing(go))
                    {
                        issues.Add(new ValidationIssue
                        {
                            type = "MissingPrefab",
                            severity = "Warning",
                            gameObject = go.name,
                            path = GameObjectFinder.GetPath(go),
                            message = "Prefab asset is missing"
                        });
                    }
                }
            }

            // Check for duplicate names
            if (checkDuplicateNames)
            {
                var nameGroups = allObjects.GroupBy(go => go.name).Where(g => g.Count() > 1);
                foreach (var group in nameGroups)
                {
                    issues.Add(new ValidationIssue
                    {
                        type = "DuplicateName",
                        severity = "Info",
                        gameObject = group.Key,
                        count = group.Count(),
                        message = $"{group.Count()} objects share the name '{group.Key}'"
                    });
                }
            }

            // Check for empty GameObjects
            if (checkEmptyGameObjects)
            {
                foreach (var go in allObjects)
                {
                    var components = go.GetComponents<Component>();
                    if (components.Length == 1 && go.transform.childCount == 0) // Only Transform
                    {
                        issues.Add(new ValidationIssue
                        {
                            type = "EmptyGameObject",
                            severity = "Info",
                            gameObject = go.name,
                            path = GameObjectFinder.GetPath(go),
                            message = "GameObject has no components (except Transform) and no children"
                        });
                    }
                }
            }

            var summary = new
            {
                errors = issues.Count(i => i.severity == "Error"),
                warnings = issues.Count(i => i.severity == "Warning"),
                info = issues.Count(i => i.severity == "Info")
            };

            return new
            {
                scene = scene.name,
                totalIssues = issues.Count,
                summary,
                issues = issues.Select(i => new { i.type, i.severity, i.gameObject, i.path, i.message, i.count }).ToArray()
            };
        */
    }
}
```

## ValidateFindMissingScripts
**Signature:** `public static object ValidateFindMissingScripts(bool searchInPrefabs = true)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        bool true = default; // Assign value

        /* Original Logic:

            var results = new List<object>();

            // Search in scene
            var sceneObjects = FindHelper.FindAll<GameObject>();
            foreach (var go in sceneObjects)
            {
                var components = go.GetComponents<Component>();
                var missingCount = components.Count(c => c == null);
                if (missingCount > 0)
                {
                    results.Add(new
                    {
                        source = "Scene",
                        gameObject = go.name,
                        path = GameObjectFinder.GetPath(go),
                        missingCount
                    });
                }
            }

            // Search in prefabs
            if (searchInPrefabs)
            {
                var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                foreach (var guid in prefabGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    var allChildren = prefab.GetComponentsInChildren<Transform>(true);
                    foreach (var t in allChildren)
                    {
                        var components = t.gameObject.GetComponents<Component>();
                        var missingCount = components.Count(c => c == null);
                        if (missingCount > 0)
                        {
                            results.Add(new
                            {
                                source = "Prefab",
                                prefabPath = path,
                                gameObject = t.name,
                                missingCount
                            });
                        }
                    }
                }
            }

            return new { totalFound = results.Count, objects = results };
        */
    }
}
```

## ValidateCleanupEmptyFolders
**Signature:** `public static object ValidateCleanupEmptyFolders(string rootPath = "Assets", bool dryRun = true)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        string "Assets" = default; // Assign value
        bool true = default; // Assign value

        /* Original Logic:

            if (Validate.SafePath(rootPath, "rootPath") is object pathErr) return pathErr;

            var emptyFolders = new List<string>();
            FindEmptyFolders(rootPath, emptyFolders);

            if (!dryRun && emptyFolders.Count > 0)
            {
                // Delete in reverse order (deepest first) to handle nested empty folders
                var sorted = emptyFolders.OrderByDescending(f => f.Length).ToList();
                foreach (var folder in sorted)
                {
                    if (Directory.Exists(folder))
                    {
                        AssetDatabase.DeleteAsset(folder);
                    }
                }
                AssetDatabase.Refresh();
            }

            return new
            {
                success = true,
                dryRun,
                emptyFolderCount = emptyFolders.Count,
                folders = emptyFolders,
                message = dryRun ? "Dry run - no folders deleted" : $"Deleted {emptyFolders.Count} empty folders"
            };
        */
    }
}
```

## ValidateFindUnusedAssets
**Signature:** `public static object ValidateFindUnusedAssets(string assetType = "Material", int limit = 100)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        string "Material" = default; // Assign value
        int 100 = default; // Assign value

        /* Original Logic:

            var filter = $"t:{assetType}";
            var guids = AssetDatabase.FindAssets(filter);
            var candidatePaths = new HashSet<string>(
                guids.Select(AssetDatabase.GUIDToAssetPath).Where(p => !string.IsNullOrEmpty(p)),
                System.StringComparer.OrdinalIgnoreCase);

            // Pre-build dependency index: collect all paths that are depended upon by any asset
            var allGuids = AssetDatabase.FindAssets("t:Object", new[] { "Assets" });
            var referencedPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var g in allGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(g);
                if (string.IsNullOrEmpty(assetPath)) continue;
                foreach (var dep in AssetDatabase.GetDependencies(assetPath, true))
                {
                    if (dep != assetPath && candidatePaths.Contains(dep))
                        referencedPaths.Add(dep);
                }
            }

            // Collect candidates not found in the referenced set
            var potentiallyUnused = new List<object>();
            foreach (var path in candidatePaths)
            {
                if (potentiallyUnused.Count >= limit) break;
                if (referencedPaths.Contains(path)) continue;

                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                potentiallyUnused.Add(new
                {
                    path,
                    name = asset?.name,
                    type = asset?.GetType().Name
                });
            }

            return new
            {
                assetType,
                potentiallyUnusedCount = potentiallyUnused.Count,
                note = "These assets may still be used via scripts or Resources.Load",
                assets = potentiallyUnused
            };
        */
    }
}
```

## ValidateTextureSizes
**Signature:** `public static object ValidateTextureSizes(int maxRecommendedSize = 2048, int limit = 50)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        int 2048 = default; // Assign value
        int 50 = default; // Assign value

        /* Original Logic:

            var largeTextures = new List<object>();
            var guids = AssetDatabase.FindAssets("t:Texture2D");

            foreach (var guid in guids)
            {
                if (largeTextures.Count >= limit) break;

                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;

                if (texture.width > maxRecommendedSize || texture.height > maxRecommendedSize)
                {
                    largeTextures.Add(new
                    {
                        path,
                        name = texture.name,
                        width = texture.width,
                        height = texture.height,
                        maxTextureSize = importer.maxTextureSize,
                        format = texture.format.ToString(),
                        recommendation = $"Consider reducing to {maxRecommendedSize}x{maxRecommendedSize} or smaller"
                    });
                }
            }

            return new
            {
                maxRecommendedSize,
                largeTextureCount = largeTextures.Count,
                textures = largeTextures
            };
        */
    }
}
```

## ValidateProjectStructure
**Signature:** `public static object ValidateProjectStructure(string rootPath = "Assets", int maxDepth = 2)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        string "Assets" = default; // Assign value
        int 2 = default; // Assign value

        /* Original Logic:

            var structure = GetFolderStructure(rootPath, 0, maxDepth);
            
            // Count assets by type
            var assetCounts = new Dictionary<string, int>();
            var commonTypes = new[] { "Material", "Prefab", "Script", "Texture2D", "AudioClip", "Scene", "Shader" };
            
            foreach (var type in commonTypes)
            {
                var count = AssetDatabase.FindAssets($"t:{type}", new[] { rootPath }).Length;
                assetCounts[type] = count;
            }

            return new
            {
                rootPath,
                assetCounts,
                structure
            };
        */
    }
}
```

## ValidateFixMissingScripts
**Signature:** `public static object ValidateFixMissingScripts(bool dryRun = true)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        bool true = default; // Assign value

        /* Original Logic:

            var fixedObjects = new List<object>();
            var sceneObjects = FindHelper.FindAll<GameObject>();

            foreach (var go in sceneObjects)
            {
                var missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missingCount > 0)
                {
                    fixedObjects.Add(new
                    {
                        gameObject = go.name,
                        path = GameObjectFinder.GetPath(go),
                        missingCount
                    });

                    if (!dryRun)
                    {
                        WorkflowManager.SnapshotObject(go);
                        Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    }
                }
            }

            return new
            {
                success = true,
                dryRun,
                fixedCount = fixedObjects.Count,
                message = dryRun ? "Dry run - no scripts removed" : $"Removed missing scripts from {fixedObjects.Count} objects",
                objects = fixedObjects
            };
        */
    }
}
```

## ValidateMissingReferences
**Signature:** `public static object ValidateMissingReferences(int limit = 50)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        int 50 = default; // Assign value

        /* Original Logic:

            var results = new List<object>();
            foreach (var go in FindHelper.FindAll<GameObject>())
            {
                if (results.Count >= limit) break;
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    var so = new SerializedObject(comp);
                    var prop = so.GetIterator();
                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                            prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                        {
                            results.Add(new { gameObject = go.name, path = GameObjectFinder.GetPath(go),
                                component = comp.GetType().Name, property = prop.propertyPath });
                            break;
                        }
                    }
                }
            }
            return new { success = true, count = results.Count, issues = results };
        */
    }
}
```

## ValidateMeshColliderConvex
**Signature:** `public static object ValidateMeshColliderConvex(int limit = 50)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        int 50 = default; // Assign value

        /* Original Logic:

            var colliders = FindHelper.FindAll<MeshCollider>()
                .Where(mc => !mc.convex)
                .Take(limit)
                .Select(mc => new { gameObject = mc.gameObject.name, path = GameObjectFinder.GetPath(mc.gameObject),
                    vertexCount = mc.sharedMesh != null ? mc.sharedMesh.vertexCount : 0 })
                .ToArray();
            return new { success = true, count = colliders.Length, nonConvexColliders = colliders };
        */
    }
}
```

## ValidateShaderErrors
**Signature:** `public static object ValidateShaderErrors(int limit = 50)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // TODO: Replace parameters with your actual logic
        int 50 = default; // Assign value

        /* Original Logic:

            var guids = AssetDatabase.FindAssets("t:Shader");
            var errors = new List<object>();
            foreach (var guid in guids)
            {
                if (errors.Count >= limit) break;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) continue;
                int msgCount = UnityEditor.ShaderUtil.GetShaderMessageCount(shader);
                if (msgCount > 0)
                    errors.Add(new { name = shader.name, path, errorCount = msgCount });
            }
            return new { success = true, count = errors.Count, shaders = errors };
        */
    }
}
```
