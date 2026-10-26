using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Pipeline.Commands;
using UnityTerrain = UnityEngine.Terrain;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.TerrainAuthoring
{
    internal static class TerrainCore
    {
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Unit(float value) => Finite(value) && value >= 0f && value <= 1f;
        internal static int Pixel(float value, int resolution) => Mathf.Clamp(Mathf.RoundToInt(value * (resolution - 1)), 0, resolution - 1);
        internal static CommandResult<object> Fail(string schema, string code, string message, object detail = null) =>
            CommandResult<object>.Failure(schema, code, message, new Dictionary<string, object> { ["detail"] = detail ?? string.Empty });
        internal static bool AuthoringAllowed => !EditorApplication.isPlayingOrWillChangePlaymode;

        internal static UnityTerrain Resolve(string target, string schema, out CommandResult<object> failure)
        {
            failure = null;
            var terrain = Editor.ExactObjectReference.Resolve<UnityTerrain>(target);
            if (terrain == null)
                failure = Fail(schema, "TERRAIN_NOT_FOUND", "Select an exact Terrain component EntityId or GlobalObjectId.", target);
            else if (EditorUtility.IsPersistent(terrain) || !Regular(terrain.gameObject) || terrain.terrainData == null)
                failure = Fail(schema, "TERRAIN_INVALID", "The Terrain must be in a regular loaded scene and reference TerrainData.", target);
            return failure == null ? terrain : null;
        }

        internal static bool Regular(GameObject gameObject)
        {
            var scene = gameObject.scene;
            return scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene) &&
                !EditorSceneManager.IsPreviewSceneObject(gameObject) && PrefabStageUtility.GetPrefabStage(gameObject) == null;
        }

        internal static CommandResult<object> GuardData(string schema, TerrainData data)
        {
            var path = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(path)) return null;
            var guard = ProjectPathPolicy.Validate(path);
            return guard.Ok ? null : CommandResult<object>.Failure(schema, guard.Error);
        }

        internal static object Identity(UnityTerrain terrain)
        {
            var data = terrain.terrainData;
            var path = AssetDatabase.GetAssetPath(data);
            var users = UnityObject.FindObjectsByType<UnityTerrain>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item != null && item.terrainData == data && Regular(item.gameObject))
                .Select(Editor.ExactObjectReference.ExactId).OrderBy(item => item).ToArray();
            return new { target = Editor.ExactObjectReference.ExactId(terrain), gameObject = Editor.ExactObjectReference.ExactId(terrain.gameObject),
                scenePath = terrain.gameObject.scene.path, data = Editor.ExactObjectReference.ExactId(data), dataPath = path,
                persistent = !string.IsNullOrEmpty(path), sharingCount = users.Length, sharingTerrainTargets = users };
        }

        internal static CommandResult<object> PrepareWrite(string schema, string target, bool confirm, bool dryRun,
            out UnityTerrain terrain)
        {
            terrain = null;
            if (!AuthoringAllowed) return Fail(schema, "PLAY_MODE_REFUSED", "Terrain authoring requires Edit Mode.");
            terrain = Resolve(target, schema, out var failure);
            if (failure != null) return failure;
            if (!dryRun && !confirm) return Fail(schema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
            return GuardData(schema, terrain.terrainData);
        }

        internal static CommandResult<object> WriteHeights(string schema, UnityTerrain terrain, float[,] candidate,
            int x, int z, bool dryRun, object details)
        {
            var data = terrain.terrainData;
            int rows = candidate.GetLength(0), cols = candidate.GetLength(1);
            if (!dryRun)
            {
                var guard = GuardData(schema, data);
                if (guard != null) return guard;
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Terrain heights");
                Undo.RegisterCompleteObjectUndo(data, "Terrain heights");
                data.SetHeights(x, z, candidate);
                EditorUtility.SetDirty(data);
                var path = AssetDatabase.GetAssetPath(data);
                if (!string.IsNullOrEmpty(path)) AssetDatabase.SaveAssetIfDirty(data);
            }
            return CommandResult<object>.Success(schema, new { identity = Identity(terrain), x, z, width = cols, length = rows,
                details, dryRun, applied = !dryRun, undoable = !dryRun, saved = !dryRun && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(data)) });
        }
    }

    public static class TerrainCreationCommands
    {
        private const string CreateSchema = "unity.terrain.create@1";
        private const string InspectSchema = "unity.terrain.inspect@1";
        private const string SampleSchema = "unity.terrain.sample-height@1";

        [CliCommand("terrain.create", "Create a Terrain, TerrainCollider and owned TerrainData asset.", Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> Create(
            [CliArg("nameTerrain", "Terrain object name.")] string nameTerrain = "Terrain",
            [CliArg("width", "Terrain X size.")] float width = 500f,
            [CliArg("height", "Terrain Y size.")] float height = 100f,
            [CliArg("length", "Terrain Z size.")] float length = 500f,
            [CliArg("resolution", "Heightmap resolution.")] int resolution = 513,
            [CliArg("x", "World X.")] float x = 0f,
            [CliArg("y", "World Y.")] float y = 0f,
            [CliArg("z", "World Z.")] float z = 0f,
            [CliArg("assetPath", "New TerrainData path under an existing Assets folder; omitted generates a unique name.")] string assetPath = null,
            [CliArg("confirm", "Create the asset and scene object.")] bool confirm = false,
            [CliArg("dryRun", "Validate and preview without writes.")] bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<object>.Failure(CreateSchema, compatibility.Error);
            if (!TerrainCore.AuthoringAllowed) return TerrainCore.Fail(CreateSchema, "PLAY_MODE_REFUSED", "Terrain authoring requires Edit Mode.");
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene) || PrefabStageUtility.GetCurrentPrefabStage() != null)
                return TerrainCore.Fail(CreateSchema, "REGULAR_SCENE_REQUIRED", "A regular loaded active scene is required.");
            if (string.IsNullOrWhiteSpace(nameTerrain) || nameTerrain.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                nameTerrain.Contains("/") || nameTerrain.Contains("\\") || nameTerrain == "." || nameTerrain == "..")
                return TerrainCore.Fail(CreateSchema, "NAME_INVALID", "Use a nonempty filename-safe Terrain name.");
            if (!TerrainCore.Finite(width) || width <= 0 || !TerrainCore.Finite(height) || height <= 0 ||
                !TerrainCore.Finite(length) || length <= 0 || !TerrainCore.Finite(x) || !TerrainCore.Finite(y) || !TerrainCore.Finite(z))
                return TerrainCore.Fail(CreateSchema, "DIMENSIONS_INVALID", "Size must be positive and finite; position must be finite.");
            if (!(new[] { 33, 65, 129, 257, 513, 1025, 2049, 4097 }).Contains(resolution))
                return TerrainCore.Fail(CreateSchema, "RESOLUTION_INVALID", "Resolution must be 33, 65, 129, 257, 513, 1025, 2049, or 4097.");
            if (!dryRun && !confirm) return TerrainCore.Fail(CreateSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
            var explicitPath = !string.IsNullOrWhiteSpace(assetPath);
            var path = explicitPath ? assetPath : AssetDatabase.GenerateUniqueAssetPath("Assets/" + nameTerrain + "_Data.asset");
            var guard = ProjectPathPolicy.Validate(path);
            if (!guard.Ok) return CommandResult<object>.Failure(CreateSchema, guard.Error);
            path = guard.Result.Path;
            if (!string.Equals(Path.GetExtension(path), ".asset", StringComparison.OrdinalIgnoreCase))
                return TerrainCore.Fail(CreateSchema, "ASSET_EXTENSION_INVALID", "TerrainData asset path must end in .asset.", path);
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
                return TerrainCore.Fail(CreateSchema, "PARENT_MISSING", "The asset parent folder must already exist.", path);
            if (guard.Result.Exists || AssetDatabase.LoadMainAssetAtPath(path) != null)
                return TerrainCore.Fail(CreateSchema, "ASSET_EXISTS", "The asset path already exists.", path);
            if (dryRun) return CommandResult<object>.Success(CreateSchema, new { name = nameTerrain, path, size = new[] { width, height, length },
                position = new[] { x, y, z }, resolution, dryRun = true, applied = false, assetUndoable = false });
            TerrainData data = null;
            GameObject gameObject = null;
            bool assetCreated = false;
            try
            {
                data = new TerrainData { heightmapResolution = resolution, size = new Vector3(width, height, length) };
                var freshGuard = ProjectPathPolicy.Validate(path);
                if (!freshGuard.Ok || freshGuard.Result.Exists) throw new InvalidOperationException("The asset path changed before creation.");
                AssetDatabase.CreateAsset(data, path);
                assetCreated = AssetDatabase.LoadAssetAtPath<TerrainData>(path) == data;
                if (!assetCreated) throw new InvalidOperationException("The TerrainData asset did not read back after creation.");
                gameObject = UnityTerrain.CreateTerrainGameObject(data);
                gameObject.name = nameTerrain;
                gameObject.transform.position = new Vector3(x, y, z);
                SceneManager.MoveGameObjectToScene(gameObject, scene);
                var terrain = gameObject.GetComponent<UnityTerrain>();
                var collider = gameObject.GetComponent<TerrainCollider>();
                if (terrain == null || collider == null || terrain.terrainData != data || collider.terrainData != data ||
                    data.heightmapResolution != resolution || data.size != new Vector3(width, height, length))
                    throw new InvalidOperationException("Terrain, collider or data readback did not match creation parameters.");
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Create Terrain");
                Undo.RegisterCreatedObjectUndo(gameObject, "Create Terrain");
                return CommandResult<object>.Success(CreateSchema, new { identity = TerrainCore.Identity(terrain),
                    collider = Editor.ExactObjectReference.ExactId(collider), name = gameObject.name, path,
                    size = new[] { data.size.x, data.size.y, data.size.z }, position = new[] { x, y, z }, resolution = data.heightmapResolution,
                    applied = true, dryRun = false, sceneUndoable = true, assetUndoable = false, saved = true });
            }
            catch (Exception exception)
            {
                var cleanup = new List<string>();
                try { if (gameObject != null) UnityObject.DestroyImmediate(gameObject); } catch (Exception e) { cleanup.Add("scene:" + e.GetType().Name); }
                try { if (assetCreated) { var freshGuard = ProjectPathPolicy.Validate(path); if (freshGuard.Ok && !AssetDatabase.DeleteAsset(path)) cleanup.Add("asset:DeleteAsset returned false"); } }
                catch (Exception e) { cleanup.Add("asset:" + e.GetType().Name); }
                if (!assetCreated && data != null) UnityObject.DestroyImmediate(data);
                return CommandResult<object>.Failure(CreateSchema, "CREATE_FAILED", "Terrain creation failed; inspect cleanup details.",
                    new Dictionary<string, object> { ["path"] = path, ["exceptionType"] = exception.GetType().Name,
                        ["cleanupIssues"] = cleanup.ToArray(), ["assetStillExists"] = AssetDatabase.LoadMainAssetAtPath(path) != null,
                        ["sceneObjectStillExists"] = gameObject != null });
            }
        }

        [CliCommand("terrain.inspect", "Inspect one exact Terrain and its shared TerrainData.", Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> Inspect([CliArg("target", "Exact Terrain component EntityId or GlobalObjectId.", Required = true)] string target)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<object>.Failure(InspectSchema, compatibility.Error);
            var terrain = TerrainCore.Resolve(target, InspectSchema, out var failure);
            if (failure != null) return failure;
            var data = terrain.terrainData;
            return CommandResult<object>.Success(InspectSchema, new { identity = TerrainCore.Identity(terrain), name = terrain.gameObject.name,
                position = new[] { terrain.transform.position.x, terrain.transform.position.y, terrain.transform.position.z },
                size = new[] { data.size.x, data.size.y, data.size.z }, data.heightmapResolution, data.alphamapResolution,
                data.baseMapResolution, data.detailResolution, layerCount = data.terrainLayers.Length,
                layers = data.terrainLayers.Select((layer, index) => new { index, present = layer != null,
                    name = layer == null ? null : layer.name, diffuseTexture = layer?.diffuseTexture?.name,
                    tileSize = layer == null ? null : new { x = layer.tileSize.x, y = layer.tileSize.y },
                    path = layer == null ? null : AssetDatabase.GetAssetPath(layer),
                    identity = layer == null ? null : Editor.ExactObjectReference.ExactId(layer) }).ToArray() });
        }

        [CliCommand("terrain.sample-height", "Sample one exact Terrain at world X/Z; Unity clamps samples to bounds.", Tags = new[] { "unity-cli-commands", "terrain" })]
        public static CommandResult<object> SampleHeight(
            [CliArg("target", "Exact Terrain component EntityId or GlobalObjectId.", Required = true)] string target,
            [CliArg("worldX", "World X coordinate.")] float worldX = 0f,
            [CliArg("worldZ", "World Z coordinate.")] float worldZ = 0f)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<object>.Failure(SampleSchema, compatibility.Error);
            var terrain = TerrainCore.Resolve(target, SampleSchema, out var failure);
            if (failure != null) return failure;
            if (!TerrainCore.Finite(worldX) || !TerrainCore.Finite(worldZ))
                return TerrainCore.Fail(SampleSchema, "COORDINATE_INVALID", "World X and Z must be finite.");
            var localHeight = terrain.SampleHeight(new Vector3(worldX, terrain.transform.position.y, worldZ));
            return CommandResult<object>.Success(SampleSchema, new { identity = TerrainCore.Identity(terrain), worldX, worldZ,
                height = localHeight, worldY = localHeight + terrain.transform.position.y });
        }
    }
}
