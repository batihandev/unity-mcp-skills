using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace BatihanDev.UnityCliCommands.ProBuilderAuthoring
{
    [Serializable]
    public class ProBuilderResult
    {
        public string Target { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool SceneDirty { get; set; }
    }

    [Serializable]
    public sealed class ProBuilderFaceResult : ProBuilderResult
    {
        public int SelectedFaces { get; set; }
        public int OutputFaces { get; set; }
        public int TotalFaces { get; set; }
        public int TotalVertices { get; set; }
    }

    [Serializable]
    public sealed class ProBuilderEdgeResult : ProBuilderResult
    {
        public int SelectedEdges { get; set; }
        public int OutputEdges { get; set; }
        public int OutputFaces { get; set; }
        public int TotalFaces { get; set; }
        public int TotalVertices { get; set; }
    }

    [Serializable]
    public sealed class ProBuilderVertexResult : ProBuilderResult
    {
        public int SelectedVertices { get; set; }
        public int OutputVertices { get; set; }
        public int TotalVertices { get; set; }
        public float[][] Positions { get; set; }
        public float[] BoundsMin { get; set; }
        public float[] BoundsMax { get; set; }
    }

    [Serializable]
    public sealed class ProBuilderCreateResult : ProBuilderResult
    {
        public string Name { get; set; }
        public string Shape { get; set; }
        public int TotalFaces { get; set; }
        public int TotalVertices { get; set; }
    }

    [Serializable]
    public sealed class ProBuilderInspectResult : ProBuilderResult
    {
        public string Name { get; set; }
        public string Shape { get; set; }
        public int FaceCount { get; set; }
        public int VertexCount { get; set; }
        public int EdgeIncidenceCount { get; set; }
        public float[] Position { get; set; }
        public float[] BoundsMin { get; set; }
        public float[] BoundsMax { get; set; }
        public string[] Materials { get; set; }
        public int[] FacesPerMaterialSlot { get; set; }
    }

    [Serializable]
    public sealed class ProBuilderPivotResult : ProBuilderResult
    {
        public float[] Position { get; set; }
    }

    [Serializable]
    public sealed class ProBuilderMaterialResult : ProBuilderResult
    {
        public int SelectedFaces { get; set; }
        public int Slot { get; set; }
        public int MaterialSlots { get; set; }
    }

    [Serializable]
    public sealed class ProBuilderUvResult : ProBuilderResult
    {
        public int SelectedFaces { get; set; }
        public int Channel { get; set; }
        public int AssignedVertices { get; set; }
        public bool ChannelOneClearedByFutureRebuild { get; set; }
    }

    [Serializable]
    public sealed class ProBuilderCombineResult : ProBuilderResult
    {
        public string[] Outputs { get; set; }
        public int[] OutputFaces { get; set; }
        public int[] OutputVertices { get; set; }
        public string[] ConsumedInputs { get; set; }
    }

    internal static class ProBuilderCore
    {
        internal static string Id(UnityEngine.Object value) => Editor.ExactObjectReference.ExactId(value);
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static float[] Values(Vector3 value) => new[] { value.x, value.y, value.z };

        internal static T Resolve<T>(string reference) where T : UnityEngine.Object => Editor.ExactObjectReference.Resolve<T>(reference);

        internal static bool RegularScene(UnityEngine.Component component)
        {
            if (component == null || EditorUtility.IsPersistent(component)) return false;
            var scene = component.gameObject.scene;
            return scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewSceneObject(component) &&
                PrefabStageUtility.GetPrefabStage(component.gameObject) == null;
        }

        internal static bool RegularActiveScene(UnityEngine.SceneManagement.Scene scene) =>
            scene.IsValid() && scene.isLoaded && !string.IsNullOrEmpty(scene.path) &&
            !EditorSceneManager.IsPreviewScene(scene) && PrefabStageUtility.GetCurrentPrefabStage() == null;

        internal static ProBuilderMesh Mesh(string target)
        {
            var mesh = Resolve<ProBuilderMesh>(target);
            if (mesh == null) mesh = Resolve<GameObject>(target)?.GetComponent<ProBuilderMesh>();
            if (!RegularScene(mesh)) throw new ArgumentException("Select an exact ProBuilderMesh in a loaded regular scene.");
            return mesh;
        }

        internal static void EditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new ArgumentException("ProBuilder authoring requires Edit mode.");
        }

        internal static void OwnedMesh(ProBuilderMesh mesh)
        {
            var compiled = CompiledMesh(mesh);
            if (compiled == null) throw new ArgumentException("The ProBuilder mesh has no compiled Mesh.");
            if (EditorUtility.IsPersistent(compiled) || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(compiled)))
                throw new ArgumentException("The compiled Mesh is a persistent asset and cannot be edited in place.");
            var consumers = Resources.FindObjectsOfTypeAll<MeshFilter>()
                .Count(filter => filter != null && filter.sharedMesh == compiled);
            if (consumers != 1 || mesh.GetComponent<MeshFilter>()?.sharedMesh != compiled)
                throw new ArgumentException("The compiled Mesh must have exactly one regular-scene MeshFilter owner.");
        }

        internal static UnityEngine.Mesh CompiledMesh(ProBuilderMesh mesh) => mesh.GetComponent<UnityEngine.MeshFilter>()?.sharedMesh;

        internal static int[] Indexes(string csv, int count, bool required)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                if (required) throw new ArgumentException("A nonempty index selection is required.");
                return Enumerable.Range(0, count).ToArray();
            }
            var tokens = csv.Split(',');
            var result = new List<int>();
            var seen = new HashSet<int>();
            foreach (var token in tokens)
            {
                if (!int.TryParse(token.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
                    index < 0 || index >= count || !seen.Add(index))
                    throw new ArgumentException("Indexes must be unique, nonnegative integers within the current mesh.");
                result.Add(index);
            }
            return result.ToArray();
        }

        internal static Face[] Faces(ProBuilderMesh mesh, string csv, bool required = false) =>
            Indexes(csv, mesh.faceCount, required).Select(index => mesh.faces[index]).ToArray();

        internal static Edge[] Edges(ProBuilderMesh mesh, string csv, bool required)
        {
            var actual = new HashSet<string>(mesh.faces.SelectMany(face => face.edges).Select(EdgeKey));
            if (string.IsNullOrWhiteSpace(csv))
            {
                if (required) throw new ArgumentException("A nonempty edge selection is required.");
                return mesh.faces.SelectMany(face => face.edges).GroupBy(EdgeKey).Select(group => group.First()).ToArray();
            }
            var result = new List<Edge>();
            var seen = new HashSet<string>();
            foreach (var token in csv.Split(','))
            {
                var endpoints = token.Split('-');
                if (endpoints.Length != 2 || !int.TryParse(endpoints[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var a) ||
                    !int.TryParse(endpoints[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var b) ||
                    a == b || a < 0 || b < 0 || a >= mesh.vertexCount || b >= mesh.vertexCount)
                    throw new ArgumentException("Each edge must contain two distinct in-range vertex indexes as a-b.");
                var edge = new Edge(a, b);
                var key = EdgeKey(edge);
                if (!actual.Contains(key) || !seen.Add(key))
                    throw new ArgumentException("Each selected edge must be a unique current face edge.");
                result.Add(edge);
            }
            return result.ToArray();
        }

        private static string EdgeKey(Edge edge) => edge.a < edge.b ? edge.a + ":" + edge.b : edge.b + ":" + edge.a;

        internal static void Record(ProBuilderMesh mesh, string label)
        {
            OwnedMesh(mesh);
            Undo.RecordObject(mesh, label);
            Undo.RecordObject(CompiledMesh(mesh), label);
            Undo.RecordObject(mesh.transform, label);
            if (mesh.GetComponent<MeshRenderer>() is MeshRenderer renderer) Undo.RecordObject(renderer, label);
        }

        internal static void Rebuild(ProBuilderMesh mesh)
        {
            mesh.ToMesh();
            mesh.Refresh();
            EditorUtility.SetDirty(mesh);
            EditorUtility.SetDirty(CompiledMesh(mesh));
            PrefabUtility.RecordPrefabInstancePropertyModifications(mesh);
            EditorSceneManager.MarkSceneDirty(mesh.gameObject.scene);
        }

        internal static CommandResult<T> Run<T>(string schema, string target, bool confirm, bool dryRun,
            Func<ProBuilderMesh, T> prepare, Action<ProBuilderMesh, T> mutate, string label) where T : ProBuilderResult
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<T>.Failure(schema, compatibility.Error);
            try
            {
                EditMode();
                var mesh = Mesh(target);
                OwnedMesh(mesh);
                var result = prepare(mesh);
                result.Target = Id(mesh);
                result.DryRun = dryRun;
                result.SceneDirty = mesh.gameObject.scene.isDirty;
                if (dryRun) return CommandResult<T>.Success(schema, result);
                if (!confirm) return CommandResult<T>.Failure(schema, "CONFIRMATION_REQUIRED", "Set confirm=true or dryRun=true.");
                Undo.IncrementCurrentGroup();
                var group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(label);
                try
                {
                    Record(mesh, label);
                    mutate(mesh, result);
                    result.Applied = true;
                    result.SceneDirty = mesh.gameObject.scene.isDirty;
                    Undo.CollapseUndoOperations(group);
                    return CommandResult<T>.Success(schema, result);
                }
                catch
                {
                    Undo.RevertAllDownToGroup(group);
                    throw;
                }
            }
            catch (Exception error)
            {
                return CommandResult<T>.Failure(schema, "PROBUILDER_REFUSED", error.Message);
            }
        }
    }
}
