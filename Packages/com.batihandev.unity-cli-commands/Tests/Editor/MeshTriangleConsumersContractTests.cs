using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEngine;
using UnityObject = UnityEngine.Object;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class MeshTriangleConsumersContractTests : SmartFixtureSupport
    {
        private Mesh mesh;
        [TearDown] public void DestroyMesh() { if (mesh != null) UnityObject.DestroyImmediate(mesh); }
        private GameObject Consumer(Mesh value)
        { var go = Go("Geometry"); go.AddComponent<MeshFilter>().sharedMesh = value; go.AddComponent<MeshRenderer>(); return go; }
        private Mesh Triangles(int count)
        { mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up } }; mesh.SetIndices(Enumerable.Range(0, count * 3).Select(index => index % 3).ToArray(), MeshTopology.Triangles, 0); return mesh; }
        [TestCase(false)] [TestCase(true)]
        public void RenderingAndPerformanceReadNonReadableMeshIndexCounts(bool nonReadable)
        {
            Consumer(Triangles(10001)); if (nonReadable) mesh.UploadMeshData(true);
            Assert.That(mesh.isReadable, Is.EqualTo(!nonReadable), "Mesh readability fixture prerequisite"); Assert.That(mesh.GetIndexCount(0), Is.EqualTo(30003));
            {
                var rendering = SceneAnalysisCommands.Rendering(); Assert.That(rendering.Ok, Is.True); Assert.That(rendering.Result.TotalTriangles, Is.EqualTo(10001L)); Assert.That(rendering.Result.Issues.Single(value => value.Kind == "HighPoly").Triangles, Is.EqualTo(10001L));
                var performance = ScenePerceptionCommands.Performance(); Assert.That(performance.Ok, Is.True); Assert.That(performance.Result.Hints.Single(value => value.Category == "Geometry").Issue, Does.StartWith("1 "));
            }
        }
        [Test]
        public void MixedTopologyAggregatesAllSubmeshIndicesBeforeDividing()
        {
            mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, subMeshCount = 2 };
            mesh.SetIndices(new[] { 0, 1 }, MeshTopology.Lines, 0); mesh.SetIndices(new[] { 0, 1, 2, 0 }, MeshTopology.Points, 1); Consumer(mesh);
            Assert.That(mesh.GetIndexCount(0) + mesh.GetIndexCount(1), Is.EqualTo(6), "Mixed topology native fixture prerequisite");
            { var result = SceneAnalysisCommands.Rendering(1); Assert.That(result.Ok, Is.True); Assert.That(result.Result.TotalTriangles, Is.EqualTo(2L)); Assert.That(result.Result.Issues.Single(value => value.Kind == "HighPoly").Triangles, Is.EqualTo(2L)); }
        }
        [Test]
        public void MultipleTriangleSubmeshesContributeToBothConsumers()
        {
            Triangles(5001); mesh.subMeshCount = 2; mesh.SetIndices(Enumerable.Range(0, 15000).Select(index => index % 3).ToArray(), MeshTopology.Triangles, 1); Consumer(mesh);
            var rendering = SceneAnalysisCommands.Rendering(); Assert.That(rendering.Ok, Is.True); Assert.That(rendering.Result.TotalTriangles, Is.EqualTo(10001L));
            var performance = ScenePerceptionCommands.Performance(); Assert.That(performance.Ok, Is.True); Assert.That(performance.Result.Hints.Any(value => value.Category == "Geometry"), Is.True);
        }
        [TestCase(false)] [TestCase(true)]
        public void NullAndEmptyMeshesContributeZero(bool empty)
        {
            if (empty) mesh = new Mesh(); Consumer(mesh); var rendering = SceneAnalysisCommands.Rendering(0); Assert.That(rendering.Ok, Is.True); Assert.That(rendering.Result.TotalTriangles, Is.Zero); Assert.That(rendering.Result.Issues.Any(value => value.Kind == "HighPoly"), Is.False);
            var performance = ScenePerceptionCommands.Performance(); Assert.That(performance.Ok, Is.True); Assert.That(performance.Result.Hints.Any(value => value.Category == "Geometry"), Is.False);
        }
        [TestCase(10000, false)] [TestCase(10001, true)]
        public void StrictThresholdAndSameObjectLodArePreserved(int triangles, bool high)
        {
            var go = Consumer(Triangles(triangles)); var rendering = SceneAnalysisCommands.Rendering(); Assert.That(rendering.Ok, Is.True); Assert.That(rendering.Result.Issues.Any(value => value.Kind == "HighPoly"), Is.EqualTo(high));
            var first = ScenePerceptionCommands.Performance(); Assert.That(first.Ok, Is.True); Assert.That(first.Result.Hints.Any(value => value.Category == "Geometry"), Is.EqualTo(high));
            go.AddComponent<LODGroup>(); var withLod = ScenePerceptionCommands.Performance(); Assert.That(withLod.Ok, Is.True); Assert.That(withLod.Result.Hints.Any(value => value.Category == "Geometry"), Is.False);
        }
        [Test]
        public void TriangleIssueAndAggregateRetainLongValues()
        {
            var value = (long)int.MaxValue + 17; var issue = new SceneIssue(); var property = typeof(SceneIssue).GetProperty(nameof(SceneIssue.Triangles));
            Assert.That(property.PropertyType, Is.EqualTo(typeof(long))); property.SetValue(issue, value);
            var report = new SceneRenderingReport { TotalTriangles = value, Issues = { issue } }; Assert.That(report.TotalTriangles, Is.EqualTo(value)); Assert.That(property.GetValue(report.Issues.Single()), Is.EqualTo(value));
        }
    }
}
