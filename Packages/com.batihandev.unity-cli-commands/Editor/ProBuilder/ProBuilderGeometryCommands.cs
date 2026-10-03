using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace BatihanDev.UnityCliCommands.ProBuilderAuthoring
{
    public static partial class ProBuilderCommands
    {
        private static ProBuilderFaceResult FaceResult(ProBuilderMesh mesh, int selected) => new ProBuilderFaceResult
        {
            SelectedFaces = selected, TotalFaces = mesh.faceCount, TotalVertices = mesh.vertexCount
        };

        private static void Finish(ProBuilderMesh mesh, ProBuilderFaceResult result, int output)
        {
            ProBuilderCore.Rebuild(mesh);
            result.OutputFaces = output;
            result.TotalFaces = mesh.faceCount;
            result.TotalVertices = mesh.vertexCount;
        }

        private static ProBuilderEdgeResult EdgeResult(ProBuilderMesh mesh, int selected) => new ProBuilderEdgeResult
        {
            SelectedEdges = selected, TotalFaces = mesh.faceCount, TotalVertices = mesh.vertexCount
        };

        private static void Finish(ProBuilderMesh mesh, ProBuilderEdgeResult result, int edges, int faces)
        {
            ProBuilderCore.Rebuild(mesh);
            result.OutputEdges = edges;
            result.OutputFaces = faces;
            result.TotalFaces = mesh.faceCount;
            result.TotalVertices = mesh.vertexCount;
        }

        [CliCommand("probuilder.face-delete", "Delete exact selected faces.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderFaceResult> DeleteFaces(string target, string faceIndexes, bool confirm = false, bool dryRun = false)
        {
            Face[] faces = null;
            return ProBuilderCore.Run("unity.probuilder.face-delete@1", target, confirm, dryRun,
                mesh => { faces = ProBuilderCore.Faces(mesh, faceIndexes, true); return FaceResult(mesh, faces.Length); },
                (mesh, result) => { var deleted = mesh.DeleteFaces(faces); if (deleted == null) throw new InvalidOperationException("DeleteFaces failed."); Finish(mesh, result, result.SelectedFaces); }, "Delete ProBuilder Faces");
        }

        [CliCommand("probuilder.face-merge", "Merge two or more selected faces.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderFaceResult> MergeFaces(string target, string faceIndexes = null, bool confirm = false, bool dryRun = false)
        {
            Face[] faces = null;
            return ProBuilderCore.Run("unity.probuilder.face-merge@1", target, confirm, dryRun,
                mesh => { faces = ProBuilderCore.Faces(mesh, faceIndexes); if (faces.Length < 2) throw new ArgumentException("Select at least two faces."); return FaceResult(mesh, faces.Length); },
                (mesh, result) => { if (MergeElements.Merge(mesh, faces) == null) throw new InvalidOperationException("Merge failed."); Finish(mesh, result, 1); }, "Merge ProBuilder Faces");
        }

        [CliCommand("probuilder.face-flip", "Reverse winding of selected faces.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderFaceResult> FlipFaces(string target, string faceIndexes = null, bool confirm = false, bool dryRun = false)
        {
            Face[] faces = null;
            return ProBuilderCore.Run("unity.probuilder.face-flip@1", target, confirm, dryRun,
                mesh => { faces = ProBuilderCore.Faces(mesh, faceIndexes); if (faces.Length == 0) throw new ArgumentException("No faces to flip."); return FaceResult(mesh, faces.Length); },
                (mesh, result) => { foreach (var face in faces) face.Reverse(); Finish(mesh, result, faces.Length); }, "Flip ProBuilder Faces");
        }

        [CliCommand("probuilder.face-detach", "Detach selected faces from shared vertices.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderFaceResult> DetachFaces(string target, string faceIndexes = null, bool deleteSourceFaces = false, bool confirm = false, bool dryRun = false)
        {
            Face[] faces = null;
            return ProBuilderCore.Run("unity.probuilder.face-detach@1", target, confirm, dryRun,
                mesh => { faces = ProBuilderCore.Faces(mesh, faceIndexes); if (faces.Length == 0) throw new ArgumentException("No faces to detach."); return FaceResult(mesh, faces.Length); },
                (mesh, result) => { var output = mesh.DetachFaces(faces, deleteSourceFaces); if (output == null || output.Count == 0) throw new InvalidOperationException("Detach failed."); Finish(mesh, result, output.Count); }, "Detach ProBuilder Faces");
        }

        [CliCommand("probuilder.face-extrude", "Extrude selected faces.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderFaceResult> ExtrudeFaces(string target, string faceIndexes = null,
            float distance = 0.5f, string method = "FaceNormal", bool confirm = false, bool dryRun = false)
        {
            Face[] faces = null;
            ExtrudeMethod selectedMethod = ExtrudeMethod.FaceNormal;
            return ProBuilderCore.Run("unity.probuilder.face-extrude@1", target, confirm, dryRun,
                mesh =>
                {
                    if (!ProBuilderCore.Finite(distance)) throw new ArgumentException("Distance must be finite.");
                    if (!Enum.TryParse(method, false, out selectedMethod) ||
                        !Enum.GetNames(typeof(ExtrudeMethod)).Contains(method))
                        throw new ArgumentException("Use IndividualFaces, FaceNormal or VertexNormal.");
                    faces = ProBuilderCore.Faces(mesh, faceIndexes);
                    if (faces.Length == 0) throw new ArgumentException("No faces to extrude.");
                    return FaceResult(mesh, faces.Length);
                },
                (mesh, result) => { var output = mesh.Extrude(faces, selectedMethod, distance); if (output == null || output.Length == 0) throw new InvalidOperationException("Extrude failed."); Finish(mesh, result, output.Length); }, "Extrude ProBuilder Faces");
        }

        [CliCommand("probuilder.face-subdivide", "Connect selected face edge midpoints.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderFaceResult> SubdivideFaces(string target, string faceIndexes = null, bool confirm = false, bool dryRun = false)
        {
            Face[] faces = null;
            return ProBuilderCore.Run("unity.probuilder.face-subdivide@1", target, confirm, dryRun,
                mesh => { faces = ProBuilderCore.Faces(mesh, faceIndexes); if (faces.Length == 0) throw new ArgumentException("No faces to subdivide."); return FaceResult(mesh, faces.Length); },
                (mesh, result) => { var output = mesh.Connect(faces); if (output == null || output.Length == 0) throw new InvalidOperationException("Subdivision failed."); Finish(mesh, result, output.Length); }, "Subdivide ProBuilder Faces");
        }

        [CliCommand("probuilder.face-conform", "Conform selected face normals.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderFaceResult> ConformFaces(string target, string faceIndexes = null, bool confirm = false, bool dryRun = false)
        {
            Face[] faces = null;
            int[] indexes = null;
            return ProBuilderCore.Run("unity.probuilder.face-conform@1", target, confirm, dryRun,
                mesh => { indexes = ProBuilderCore.Indexes(faceIndexes, mesh.faceCount, false); faces = indexes.Select(index => mesh.faces[index]).ToArray(); if (faces.Length == 0) throw new ArgumentException("No faces to conform."); return FaceResult(mesh, faces.Length); },
                (mesh, result) =>
                {
                    var winding = indexes.Select(index => mesh.faces[index].indexes.ToArray()).ToArray();
                    var output = mesh.ConformNormals(faces);
                    if (output == null || output.status != ActionResult.Status.Success)
                        throw new InvalidOperationException(output?.notification ?? "Conform failed.");
                    var changed = indexes.Where((index, selection) => !winding[selection].SequenceEqual(mesh.faces[index].indexes)).Count();
                    Finish(mesh, result, changed);
                }, "Conform ProBuilder Normals");
        }

        [CliCommand("probuilder.edge-extrude", "Extrude exact selected face edges.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderEdgeResult> ExtrudeEdges(string target, string edgeIndexes, float distance = 0.5f,
            bool extrudeAsGroup = true, bool enableManifoldExtrude = false, bool confirm = false, bool dryRun = false)
        {
            Edge[] edges = null;
            return ProBuilderCore.Run("unity.probuilder.edge-extrude@1", target, confirm, dryRun,
                mesh => { if (!ProBuilderCore.Finite(distance)) throw new ArgumentException("Distance must be finite."); edges = ProBuilderCore.Edges(mesh, edgeIndexes, true); return EdgeResult(mesh, edges.Length); },
                (mesh, result) => { var output = mesh.Extrude(edges, distance, extrudeAsGroup, enableManifoldExtrude); if (output == null || output.Length == 0) throw new InvalidOperationException("Edge extrusion failed."); Finish(mesh, result, output.Length, mesh.faceCount - result.TotalFaces); }, "Extrude ProBuilder Edges");
        }

        [CliCommand("probuilder.edge-bevel", "Bevel exact selected face edges.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderEdgeResult> BevelEdges(string target, string edgeIndexes = null, float amount = 0.2f, bool confirm = false, bool dryRun = false)
        {
            Edge[] edges = null;
            return ProBuilderCore.Run("unity.probuilder.edge-bevel@1", target, confirm, dryRun,
                mesh => { if (!ProBuilderCore.Finite(amount) || amount <= 0 || amount > 1) throw new ArgumentException("Bevel amount must be finite and in (0, 1]."); edges = ProBuilderCore.Edges(mesh, edgeIndexes, false); if (edges.Length == 0) throw new ArgumentException("No edges to bevel."); return EdgeResult(mesh, edges.Length); },
                (mesh, result) => { var output = Bevel.BevelEdges(mesh, edges, amount); if (output == null || output.Count == 0) throw new InvalidOperationException("Bevel failed."); Finish(mesh, result, 0, output.Count); }, "Bevel ProBuilder Edges");
        }

        [CliCommand("probuilder.edge-bridge", "Bridge two exact face edges.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderEdgeResult> BridgeEdges(string target, string edgeA, string edgeB,
            bool allowNonManifold = false, bool confirm = false, bool dryRun = false)
        {
            Edge first = default, second = default;
            return ProBuilderCore.Run("unity.probuilder.edge-bridge@1", target, confirm, dryRun,
                mesh => { var a = ProBuilderCore.Edges(mesh, edgeA, true); var b = ProBuilderCore.Edges(mesh, edgeB, true); if (a.Length != 1 || b.Length != 1 || (a[0].a == b[0].a && a[0].b == b[0].b)) throw new ArgumentException("Select two different single edges."); first = a[0]; second = b[0]; return EdgeResult(mesh, 2); },
                (mesh, result) => { if (mesh.Bridge(first, second, allowNonManifold) == null) throw new InvalidOperationException("Bridge failed."); Finish(mesh, result, 0, 1); }, "Bridge ProBuilder Edges");
        }

        [CliCommand("probuilder.vertex-move", "Move exact local vertices by a finite delta.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderVertexResult> MoveVertices(string target, string vertexIndexes,
            float deltaX = 0, float deltaY = 0, float deltaZ = 0, bool confirm = false, bool dryRun = false)
        {
            int[] indexes = null;
            return ProBuilderCore.Run("unity.probuilder.vertex-move@1", target, confirm, dryRun,
                mesh => { if (!(new[] { deltaX, deltaY, deltaZ }).All(ProBuilderCore.Finite)) throw new ArgumentException("Delta must be finite."); indexes = ProBuilderCore.Indexes(vertexIndexes, mesh.vertexCount, true); return new ProBuilderVertexResult { SelectedVertices = indexes.Length, TotalVertices = mesh.vertexCount }; },
                (mesh, result) => { var positions = mesh.positions.ToArray(); var delta = new Vector3(deltaX, deltaY, deltaZ); foreach (var index in indexes) positions[index] += delta; mesh.positions = positions; ProBuilderCore.Rebuild(mesh); result.OutputVertices = indexes.Length; result.TotalVertices = mesh.vertexCount; }, "Move ProBuilder Vertices");
        }

        [CliCommand("probuilder.vertex-set", "Set exact local vertex positions from strict JSON.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderVertexResult> SetVertices(string target, string vertices, bool confirm = false, bool dryRun = false)
        {
            Dictionary<int, Vector3> updates = null;
            return ProBuilderCore.Run("unity.probuilder.vertex-set@1", target, confirm, dryRun,
                mesh =>
                {
                    JToken parsed;
                    try { parsed = JToken.Parse(vertices); }
                    catch (Exception error) when (error is JsonException || error is ArgumentNullException) { throw new ArgumentException("Vertices must be a JSON array of {index,x,y,z}."); }
                    if (!(parsed is JArray array) || array.Count == 0) throw new ArgumentException("Vertices must be a nonempty JSON array.");
                    updates = new Dictionary<int, Vector3>();
                    foreach (var token in array)
                    {
                        if (!(token is JObject item) || item.Properties().Count() != 4 ||
                            !TryInteger(item["index"], out var index) || index < 0 || index >= mesh.vertexCount ||
                            !TryFloat(item["x"], out var x) || !TryFloat(item["y"], out var y) || !TryFloat(item["z"], out var z) ||
                            !updates.TryAdd(index, new Vector3(x, y, z)))
                            throw new ArgumentException("Every vertex needs unique in-range index and finite x/y/z numbers.");
                    }
                    return new ProBuilderVertexResult { SelectedVertices = updates.Count, TotalVertices = mesh.vertexCount };
                },
                (mesh, result) => { var positions = mesh.positions.ToArray(); foreach (var item in updates) positions[item.Key] = item.Value; mesh.positions = positions; ProBuilderCore.Rebuild(mesh); result.OutputVertices = updates.Count; result.TotalVertices = mesh.vertexCount; }, "Set ProBuilder Vertices");
        }

        private static bool TryInteger(JToken token, out int value)
        {
            value = 0;
            return token != null && token.Type == JTokenType.Integer && int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryFloat(JToken token, out float value)
        {
            value = 0;
            return token != null && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float) &&
                float.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && ProBuilderCore.Finite(value);
        }

        [CliCommand("probuilder.vertex-weld", "Weld exact local vertices within a finite radius.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderVertexResult> WeldVertices(string target, string vertexIndexes,
            float radius = 0.01f, bool confirm = false, bool dryRun = false)
        {
            int[] indexes = null;
            return ProBuilderCore.Run("unity.probuilder.vertex-weld@1", target, confirm, dryRun,
                mesh => { if (!ProBuilderCore.Finite(radius) || radius <= 0) throw new ArgumentException("Weld radius must be finite and positive."); indexes = ProBuilderCore.Indexes(vertexIndexes, mesh.vertexCount, true); return new ProBuilderVertexResult { SelectedVertices = indexes.Length, TotalVertices = mesh.vertexCount }; },
                (mesh, result) => { var output = mesh.WeldVertices(indexes, radius); if (output == null || output.Length == 0) throw new InvalidOperationException("Weld failed."); ProBuilderCore.Rebuild(mesh); result.OutputVertices = output.Length; result.TotalVertices = mesh.vertexCount; }, "Weld ProBuilder Vertices");
        }

        [CliCommand("probuilder.pivot", "Center or set a world-space pivot while retaining omitted axes.", Tags = new[] { "unity-cli-commands", "probuilder" })]
        public static CommandResult<ProBuilderPivotResult> Pivot(string target, float? worldX = null, float? worldY = null, float? worldZ = null,
            bool confirm = false, bool dryRun = false)
        {
            Vector3 position = default;
            var center = !worldX.HasValue && !worldY.HasValue && !worldZ.HasValue;
            return ProBuilderCore.Run("unity.probuilder.pivot@1", target, confirm, dryRun,
                mesh => { if ((worldX.HasValue && !ProBuilderCore.Finite(worldX.Value)) || (worldY.HasValue && !ProBuilderCore.Finite(worldY.Value)) || (worldZ.HasValue && !ProBuilderCore.Finite(worldZ.Value))) throw new ArgumentException("Pivot coordinates must be finite."); position = mesh.transform.position; position.x = worldX ?? position.x; position.y = worldY ?? position.y; position.z = worldZ ?? position.z; return new ProBuilderPivotResult { Position = ProBuilderCore.Values(position) }; },
                (mesh, result) => { if (center) mesh.CenterPivot(null); else mesh.SetPivot(position); ProBuilderCore.Rebuild(mesh); result.Position = ProBuilderCore.Values(mesh.transform.position); }, "Set ProBuilder Pivot");
        }
    }
}
