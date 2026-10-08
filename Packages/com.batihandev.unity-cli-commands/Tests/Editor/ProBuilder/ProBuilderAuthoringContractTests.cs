using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.ProBuilderAuthoring;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.SceneManagement;

namespace BatihanDev.UnityCliCommands.Tests.ProBuilder
{
    public sealed class ProBuilderAuthoringContractTests
    {
        private UnityEngine.SceneManagement.Scene previous;
        private SceneSetup[] previousSetup;
        private string primaryScenePath;
        private UnityEngine.SceneManagement.Scene scene;
        private ProBuilderMesh mesh;
        private Material sourceMaterial;

        private static string Id(UnityEngine.Object value) => EntityId.ToULong(value.GetEntityId()).ToString();

        [SetUp]
        public void SetUp()
        {
            previous = SceneManager.GetActiveScene();
            previousSetup = EditorSceneManager.GetSceneManagerSetup();
            primaryScenePath = AssetDatabase.GenerateUniqueAssetPath("Assets/ProBuilderContractTest.unity");
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, primaryScenePath));
            SceneManager.SetActiveScene(scene);
            mesh = ShapeFactory.Instantiate(typeof(Cube));
            Assert.IsNotNull(mesh);
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (sourceMaterial != null) UnityEngine.Object.DestroyImmediate(sourceMaterial);
            if (previousSetup != null && previousSetup.Length > 0 && previousSetup.All(item => !string.IsNullOrEmpty(item.path)))
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!string.IsNullOrEmpty(primaryScenePath)) AssetDatabase.DeleteAsset(primaryScenePath);
        }

        [Test]
        public void InspectReadsShapeAndActualMeshCounts()
        {
            var result = ProBuilderCommands.Inspect(Id(mesh));
            Assert.IsTrue(result.Ok, result.Error?.Message);
            Assert.AreEqual("Cube", result.Result.Shape);
            Assert.AreEqual(mesh.faceCount, result.Result.FaceCount);
            Assert.AreEqual(mesh.vertexCount, result.Result.VertexCount);
            Assert.AreEqual(mesh.faces.Count(face => face.submeshIndex == 0), result.Result.FacesPerMaterialSlot[0]);
        }

        [Test]
        public void VertexReadHonorsExactFilter()
        {
            var result = ProBuilderCommands.Vertices(Id(mesh), "0,2", false);
            Assert.IsTrue(result.Ok, result.Error?.Message);
            Assert.AreEqual(2, result.Result.Positions.Length);
            Assert.AreEqual(mesh.positions[2].x, result.Result.Positions[1][0]);
        }

        [Test]
        public void DuplicateVertexSelectionRefusesBeforeWrite()
        {
            var before = mesh.positions.ToArray();
            var result = ProBuilderCommands.MoveVertices(Id(mesh), "0,0", 1, confirm: true);
            Assert.IsFalse(result.Ok);
            CollectionAssert.AreEqual(before, mesh.positions);
        }

        [Test]
        public void MalformedVertexJsonRefusesWithoutMovingAnyVertex()
        {
            var before = mesh.positions.ToArray();
            var result = ProBuilderCommands.SetVertices(Id(mesh), "[{\"index\":0,\"x\":1,\"y\":2,\"z\":3},{\"index\":1,\"x\":\"bad\",\"y\":2,\"z\":3}]", true);
            Assert.IsFalse(result.Ok);
            CollectionAssert.AreEqual(before, mesh.positions);
        }

        [Test]
        public void MoveChangesEditableAndCompiledVerticesWithUndoRedo()
        {
            var before = mesh.positions[0];
            var compiledBefore = mesh.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices[0];
            var result = ProBuilderCommands.MoveVertices(Id(mesh), "0", 2, 0, 0, true);
            Assert.IsTrue(result.Ok, result.Error?.Message);
            Assert.AreEqual(before.x + 2, mesh.positions[0].x, 0.0001f);
            Assert.AreEqual(compiledBefore.x + 2, mesh.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices[0].x, 0.0001f);
            Undo.PerformUndo();
            Assert.AreEqual(before.x, mesh.positions[0].x, 0.0001f);
            Assert.AreEqual(compiledBefore.x, mesh.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices[0].x, 0.0001f);
            Undo.PerformRedo();
            Assert.AreEqual(before.x + 2, mesh.positions[0].x, 0.0001f);
        }

        [Test]
        public void DryRunAndMissingConfirmationLeaveGeometryIntact()
        {
            var before = mesh.positions[0];
            var preview = ProBuilderCommands.MoveVertices(Id(mesh), "0", 1, dryRun: true);
            var rejected = ProBuilderCommands.MoveVertices(Id(mesh), "0", 1);
            Assert.IsTrue(preview.Ok, preview.Error?.Message);
            Assert.IsFalse(preview.Result.Applied);
            Assert.IsFalse(rejected.Ok);
            Assert.AreEqual(before, mesh.positions[0]);
        }

        [Test]
        public void FaceExtrusionReturnsTopologyAndUndoRestoresIt()
        {
            var faces = mesh.faceCount;
            var result = ProBuilderCommands.ExtrudeFaces(Id(mesh), "0", -0.25f, confirm: true);
            Assert.IsTrue(result.Ok, result.Error?.Message);
            Assert.Greater(mesh.faceCount, faces);
            Assert.AreEqual(mesh.faceCount, result.Result.TotalFaces);
            Undo.PerformUndo();
            Assert.AreEqual(faces, mesh.faceCount);
        }

        [Test]
        public void MalformedEdgeListRefusesWithoutChangingTopology()
        {
            var faces = mesh.faceCount;
            var result = ProBuilderCommands.ExtrudeEdges(Id(mesh), "0-1,garbage", confirm: true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual(faces, mesh.faceCount);
        }

        [Test]
        public void PivotPartialCoordinatesPreserveOmittedAxesAndWorldVertex()
        {
            mesh.transform.position = new Vector3(2, 3, 4);
            var world = mesh.transform.TransformPoint(mesh.positions[0]);
            var result = ProBuilderCommands.Pivot(Id(mesh), worldX: 8, confirm: true);
            Assert.IsTrue(result.Ok, result.Error?.Message);
            Assert.AreEqual(new Vector3(8, 3, 4), mesh.transform.position);
            Assert.Less(Vector3.Distance(world, mesh.transform.TransformPoint(mesh.positions[0])), 0.0001f);
        }

        [Test]
        public void InvalidMaterialSlotRefusesBeforeFaceChanges()
        {
            var slot = mesh.faces[0].submeshIndex;
            var result = ProBuilderCommands.SetFaceMaterial(Id(mesh), "0", submeshIndex: 999, confirm: true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual(slot, mesh.faces[0].submeshIndex);
        }

        [Test]
        public void UvProjectionUpdatesEveryChannelAndUndoRestoresCoordinates()
        {
            for (var channel = 0; channel < 4; channel++)
            {
                var before = new List<Vector4>();
                mesh.GetUVs(channel, before);
                var indexes = mesh.faces[0].distinctIndexes.ToArray();
                var expected = Projection.PlanarProject(mesh.positions, indexes,
                    UnityEngine.ProBuilder.Math.Normal(mesh, mesh.faces[0]));
                var result = ProBuilderCommands.ProjectBoxUv(Id(mesh), "0", channel, true);
                Assert.IsTrue(result.Ok, result.Error?.Message);
                var after = new List<Vector4>();
                mesh.GetUVs(channel, after);
                Assert.AreEqual(mesh.vertexCount, after.Count);
                Assert.IsTrue(mesh.faces[0].manualUV);
                Assert.Greater(result.Result.AssignedVertices, 0);
                for (var i = 0; i < indexes.Length; i++)
                    Assert.Less(Vector2.Distance(expected[i], (Vector2)after[indexes[i]]), 0.0001f);
                if (before.Count == mesh.vertexCount)
                    Assert.AreEqual(before[mesh.faces[1].distinctIndexes[0]], after[mesh.faces[1].distinctIndexes[0]]);
                Undo.PerformUndo();
                var restored = new List<Vector4>();
                mesh.GetUVs(channel, restored);
                CollectionAssert.AreEqual(before, restored);
            }
        }

        [Test]
        public void UvProjectionPreservesOtherChannelsInEditableAndCompiledMeshes()
        {
            var compiled = mesh.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
            for (var channel = 0; channel < 4; channel++)
            {
                var seed = Enumerable.Range(0, mesh.vertexCount)
                    .Select(index => new Vector4(channel + index * 0.01f, channel + index * 0.02f,
                        channel + index * 0.03f, channel + index * 0.04f)).ToList();
                mesh.SetUVs(channel, seed);
                if (channel != 1)
                {
                    var stored = new List<Vector4>();
                    mesh.GetUVs(channel, stored);
                    compiled.SetUVs(channel, stored);
                }
            }
            Undo.ClearAll();

            List<Vector4>[][] beforeLast = null;
            for (var channel = 0; channel < 4; channel++)
            {
                var before = Enumerable.Range(0, 4).Select(index =>
                {
                    var editable = new List<Vector4>();
                    var output = new List<Vector4>();
                    mesh.GetUVs(index, editable);
                    compiled.GetUVs(index, output);
                    return new[] { editable, output };
                }).ToArray();
                if (channel == 3) beforeLast = before;
                var result = ProBuilderCommands.ProjectBoxUv(Id(mesh), "0", channel, true);
                Assert.IsTrue(result.Ok, result.Error?.Message);
                for (var other = 0; other < 4; other++)
                {
                    var editable = new List<Vector4>();
                    var output = new List<Vector4>();
                    mesh.GetUVs(other, editable);
                    compiled.GetUVs(other, output);
                    if (other != channel)
                    {
                        CollectionAssert.AreEqual(before[other][0], editable, "editable channel " + other);
                        CollectionAssert.AreEqual(before[other][1], output, "compiled channel " + other);
                    }
                    else if (channel >= 2)
                    {
                        foreach (var index in mesh.faces[0].distinctIndexes)
                        {
                            Assert.AreEqual(before[other][0][index].z, editable[index].z);
                            Assert.AreEqual(before[other][0][index].w, editable[index].w);
                            Assert.AreEqual(editable[index], output[index]);
                        }
                    }
                }
            }

            Undo.PerformUndo();
            for (var channel = 0; channel < 4; channel++)
            {
                var editable = new List<Vector4>();
                var output = new List<Vector4>();
                mesh.GetUVs(channel, editable);
                compiled.GetUVs(channel, output);
                CollectionAssert.AreEqual(beforeLast[channel][0], editable, "editable undo channel " + channel);
                CollectionAssert.AreEqual(beforeLast[channel][1], output, "compiled undo channel " + channel);
            }
        }

        [Test]
        public void OrdinaryCombineKeepsTargetAndRestoresSourceWithUndo()
        {
            var source = ShapeFactory.Instantiate(typeof(Cube));
            source.transform.position = Vector3.right * 4;
            var sourceWorld = source.transform.TransformPoint(source.positions[0]);
            sourceMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            source.GetComponent<MeshRenderer>().sharedMaterial = sourceMaterial;
            var targetFaces = mesh.faceCount;
            var sourceFaces = source.faceCount;
            var result = ProBuilderCommands.Combine(Id(mesh) + "," + Id(source), true);
            Assert.IsTrue(result.Ok, result.Error?.Message);
            Assert.AreEqual(1, result.Result.Outputs.Length);
            Assert.AreEqual(targetFaces + sourceFaces, mesh.faceCount);
            Assert.IsTrue(mesh.GetComponent<MeshRenderer>().sharedMaterials.Contains(sourceMaterial));
            Assert.IsTrue(mesh.positions.Any(position => Vector3.Distance(mesh.transform.TransformPoint(position), sourceWorld) < 0.0001f));
            Assert.IsTrue(source == null);
            Undo.PerformUndo();
            Assert.AreEqual(targetFaces, mesh.faceCount);
            Assert.IsNotNull(source);
        }

        [Test]
        public void CreateShapeRejectsInvalidParentWithoutCreatingObject()
        {
            var count = UnityEngine.Object.FindObjectsByType<ProBuilderMesh>(FindObjectsSortMode.None).Length;
            var result = ProBuilderCommands.CreateShape(parent: "0", confirm: true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual(count, UnityEngine.Object.FindObjectsByType<ProBuilderMesh>(FindObjectsSortMode.None).Length);
        }

        [Test]
        public void CreateShapeRejectsOpenPrefabStageBeforeCreatingObject()
        {
            var scenePath = AssetDatabase.GenerateUniqueAssetPath("Assets/ProBuilderStageGuard.unity");
            var prefabPath = AssetDatabase.GenerateUniqueAssetPath("Assets/ProBuilderStageGuard.prefab");
            var prefabSource = new GameObject("ProBuilderStageGuard");
            try
            {
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, scenePath));
                Assert.IsNotNull(PrefabUtility.SaveAsPrefabAsset(prefabSource, prefabPath));
                UnityEngine.Object.DestroyImmediate(prefabSource);
                prefabSource = null;
                Assert.IsNotNull(PrefabStageUtility.OpenPrefab(prefabPath));
                Assert.IsNotNull(PrefabStageUtility.GetCurrentPrefabStage());
                var count = UnityEngine.Object.FindObjectsByType<ProBuilderMesh>(FindObjectsSortMode.None).Length;
                var result = ProBuilderCommands.CreateShape(confirm: true);
                Assert.IsFalse(result.Ok);
                Assert.AreEqual(count, UnityEngine.Object.FindObjectsByType<ProBuilderMesh>(FindObjectsSortMode.None).Length);
            }
            finally
            {
                if (PrefabStageUtility.GetCurrentPrefabStage() != null) StageUtility.GoBackToPreviousStage();
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (prefabSource != null) UnityEngine.Object.DestroyImmediate(prefabSource);
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.DeleteAsset(scenePath);
            }
        }

        [Test]
        public void ConformReportsOnlyTheFaceWhoseWindingChanged()
        {
            var original = mesh.faces.Select(face => face.indexes.ToArray()).ToArray();
            var flipped = ProBuilderCommands.FlipFaces(Id(mesh), "0", confirm: true);
            Assert.IsTrue(flipped.Ok, flipped.Error?.Message);
            Assert.IsFalse(original[0].SequenceEqual(mesh.faces[0].indexes));

            var result = ProBuilderCommands.ConformFaces(Id(mesh), confirm: true);
            Assert.IsTrue(result.Ok, result.Error?.Message);
            Assert.AreEqual(mesh.faceCount, result.Result.SelectedFaces);
            Assert.AreEqual(1, result.Result.OutputFaces);
            for (var index = 0; index < original.Length; index++)
                CollectionAssert.AreEqual(original[index], mesh.faces[index].indexes);
        }
    }
}
