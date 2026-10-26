using System;
using System.Collections.Generic;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.AI.Navigation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BatihanDev.UnityCliCommands.Navigation
{
    public static class NavigationSurfaceCommands
    {
        private const string BuildSchema = "unity.navmesh.surface-build@1";
        private const string RemoveSchema = "unity.navmesh.surface-remove-data@1";

        [CliCommand("navmesh.surface-build", "Build session NavMesh data for one exact scene NavMeshSurface.",
            Tags = new[] { "unity-cli-commands", "navmesh" })]
        public static CommandResult<NavigationSurfaceResult> Build(
            [CliArg("target", "Exact NavMeshSurface component EntityId or GlobalObjectId.", Required = true)] string target,
            [CliArg("confirm", "Run the synchronous build.")] bool confirm = false,
            [CliArg("dryRun", "Preview without building; wins over confirmation.")] bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<NavigationSurfaceResult>.Failure(BuildSchema, compatibility.Error);
            var surface = Resolve(target, BuildSchema, out var failure);
            if (surface == null) return failure;
            if (!surface.isActiveAndEnabled)
                return Failure(BuildSchema, "SURFACE_INACTIVE", "The selected surface and GameObject must be active and enabled for a build.", target);
            if (!dryRun && !confirm)
                return Failure(BuildSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.", target);
            var before = surface.navMeshData;
            if (dryRun) return CommandResult<NavigationSurfaceResult>.Success(BuildSchema, Result(surface, before, before, false, true, false));

            try { surface.BuildNavMesh(); }
            catch (Exception exception)
            {
                return Failure(BuildSchema, "BUILD_FAILED", "The surface build threw; inspect the surface and current data before retrying.",
                    target, exception, true, before, surface.navMeshData);
            }
            var after = surface.navMeshData;
            if (after == null || ReferenceEquals(before, after))
                return Failure(BuildSchema, "BUILD_DATA_NOT_REPLACED", "The build returned without replacing the surface data with a new non-null reference.",
                    target, null, true, before, after);
            return CommandResult<NavigationSurfaceResult>.Success(BuildSchema, Result(surface, before, after, true, false, true));
        }

        [CliCommand("navmesh.surface-remove-data", "Unregister one exact scene NavMeshSurface instance while retaining its data reference.",
            Tags = new[] { "unity-cli-commands", "navmesh" })]
        public static CommandResult<NavigationSurfaceResult> RemoveData(
            [CliArg("target", "Exact NavMeshSurface component EntityId or GlobalObjectId.", Required = true)] string target,
            [CliArg("confirm", "Call RemoveData on the selected surface.")] bool confirm = false,
            [CliArg("dryRun", "Preview without removing; wins over confirmation.")] bool dryRun = false)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<NavigationSurfaceResult>.Failure(RemoveSchema, compatibility.Error);
            var surface = Resolve(target, RemoveSchema, out var failure);
            if (surface == null) return failure;
            if (!dryRun && !confirm)
                return Failure(RemoveSchema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.", target);
            var before = surface.navMeshData;
            if (dryRun) return CommandResult<NavigationSurfaceResult>.Success(RemoveSchema, Result(surface, before, before, false, true, false));

            try { surface.RemoveData(); }
            catch (Exception exception)
            {
                return Failure(RemoveSchema, "REMOVE_DATA_FAILED", "RemoveData threw; inspect this surface's runtime registration before retrying.",
                    target, exception, true, before, surface.navMeshData);
            }
            var after = surface.navMeshData;
            if (!ReferenceEquals(before, after))
                return Failure(RemoveSchema, "DATA_REFERENCE_CHANGED", "RemoveData returned but the retained data reference changed unexpectedly.",
                    target, null, true, before, after);
            return CommandResult<NavigationSurfaceResult>.Success(RemoveSchema, Result(surface, before, after, true, false, true));
        }

        private static NavMeshSurface Resolve(string target, string schema, out CommandResult<NavigationSurfaceResult> failure)
        {
            failure = null;
            var surface = Editor.ExactObjectReference.Resolve<NavMeshSurface>(target);
            if (surface == null)
            {
                failure = Failure(schema, "SURFACE_NOT_FOUND", "The exact reference is stale or does not identify a NavMeshSurface component.", target);
                return null;
            }
            var scene = surface.gameObject.scene;
            if (EditorUtility.IsPersistent(surface) || !scene.IsValid() || !scene.isLoaded ||
                EditorSceneManager.IsPreviewSceneObject(surface) || PrefabStageUtility.GetPrefabStage(surface.gameObject) != null)
            {
                failure = Failure(schema, "REGULAR_SCENE_REQUIRED", "Select a surface in a regular loaded scene, outside prefab editing and preview scenes.", target);
                return null;
            }
            return surface;
        }

        private static NavigationSurfaceResult Result(NavMeshSurface surface, UnityEngine.AI.NavMeshData before,
            UnityEngine.AI.NavMeshData after, bool applied, bool dryRun, bool dispatched) =>
            new NavigationSurfaceResult
            {
                Target = Editor.ExactObjectReference.ExactId(surface), ScenePath = surface.gameObject.scene.path,
                DataBefore = Editor.ExactObjectReference.ExactId(before), DataAfter = Editor.ExactObjectReference.ExactId(after),
                DataReferenceChanged = !ReferenceEquals(before, after), DataRetained = ReferenceEquals(before, after),
                Applied = applied, DryRun = dryRun, Dispatched = dispatched, Undoable = false,
                SavedAsset = false
            };

        private static CommandResult<NavigationSurfaceResult> Failure(string schema, string code, string message,
            string target, Exception exception = null, bool dispatched = false,
            UnityEngine.AI.NavMeshData before = null, UnityEngine.AI.NavMeshData after = null) =>
            CommandResult<NavigationSurfaceResult>.Failure(schema, code, message, new Dictionary<string, object>
            {
                ["target"] = target ?? string.Empty, ["dispatched"] = dispatched,
                ["dataBefore"] = Editor.ExactObjectReference.ExactId(before),
                ["dataAfter"] = Editor.ExactObjectReference.ExactId(after),
                ["exceptionType"] = exception?.GetType().Name ?? string.Empty
            });
    }

    [Serializable]
    public sealed class NavigationSurfaceResult
    {
        public string Target { get; set; }
        public string ScenePath { get; set; }
        public string DataBefore { get; set; }
        public string DataAfter { get; set; }
        public bool DataReferenceChanged { get; set; }
        public bool DataRetained { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool Dispatched { get; set; }
        public bool Undoable { get; set; }
        public bool SavedAsset { get; set; }
    }
}
