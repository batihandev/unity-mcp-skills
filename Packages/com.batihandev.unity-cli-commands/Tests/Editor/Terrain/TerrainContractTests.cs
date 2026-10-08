using System;
using System.Globalization;
using BatihanDev.UnityCliCommands.TerrainAuthoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityTerrain = UnityEngine.Terrain;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Tests.TerrainAuthoring
{
    public sealed class TerrainContractTests
    {
        private TerrainData _data;
        private GameObject _object;
        private UnityTerrain _terrain;
        private string _target;

        [SetUp]
        public void SetUp()
        {
            _data = new TerrainData { heightmapResolution = 33, alphamapResolution = 16, size = new Vector3(32, 10, 32) };
            _object = UnityTerrain.CreateTerrainGameObject(_data);
            _terrain = _object.GetComponent<UnityTerrain>();
            _target = EntityId.ToULong(_terrain.GetEntityId()).ToString(CultureInfo.InvariantCulture);
        }

        [TearDown]
        public void TearDown()
        {
            if (_object != null) UnityObject.DestroyImmediate(_object);
            if (_data != null) UnityObject.DestroyImmediate(_data);
        }

        [Test]
        public void ExactTargetAndMalformedRectangleRefuseWithoutMutation()
        {
            var before = _data.GetHeight(16, 16);
            var stale = TerrainHeightCommands.SetHeight("missing", confirm: true);
            Assert.That(stale.Ok, Is.False);
            var ragged = TerrainHeightCommands.SetHeights(_target, "[[0.2,0.3],[0.4]]", confirm: true);
            Assert.That(ragged.Ok, Is.False);
            var nonnumeric = TerrainHeightCommands.SetHeights(_target, "[[null]]", confirm: true);
            Assert.That(nonnumeric.Ok, Is.False);
            var missingConfirm = TerrainHeightCommands.SetHeight(_target, height: 0.8f);
            Assert.That(missingConfirm.Ok, Is.False);
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(before));
        }

        [Test]
        public void HeightSetPreviewAndUndoPreserveData()
        {
            var before = _data.GetHeight(16, 16);
            var preview = TerrainHeightCommands.SetHeight(_target, height: 0.8f, dryRun: true);
            Assert.That(preview.Ok, Is.True);
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(before));
            var changed = TerrainHeightCommands.SetHeight(_target, height: 0.8f, confirm: true);
            Assert.That(changed.Ok, Is.True);
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(8f).Within(0.01f));
            Undo.PerformUndo();
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(before).Within(0.01f));
        }

        [Test]
        public void ConsecutiveHeightEditsUndoOneAtATime()
        {
            var before = _data.GetHeight(16, 16);
            Assert.That(TerrainHeightCommands.SetHeight(_target, height: 0.3f, confirm: true).Ok, Is.True);
            Assert.That(TerrainHeightCommands.SetHeight(_target, height: 0.7f, confirm: true).Ok, Is.True);
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(7f).Within(0.01f));

            Undo.PerformUndo();
            Assert.That(_terrain, Is.Not.Null);
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(3f).Within(0.01f));

            Undo.PerformUndo();
            Assert.That(_terrain, Is.Not.Null);
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(before).Within(0.01f));
        }

        [Test]
        public void LocalFlattenAndHillLeaveFarSamplesUntouched()
        {
            Assert.That(TerrainHeightCommands.Hill(_target, radius: 0f, height: 0.6f, confirm: true).Ok, Is.True);
            Assert.That(_data.GetHeight(16, 16), Is.GreaterThan(5f));
            Assert.That(_data.GetHeight(0, 0), Is.Zero.Within(0.001f));
            Assert.That(TerrainHeightCommands.Flatten(_target, radius: 0f, targetHeight: 0.2f, confirm: true).Ok, Is.True);
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(2f).Within(0.01f));
            Assert.That(_data.GetHeight(0, 0), Is.Zero.Within(0.001f));
        }

        [Test]
        public void SmoothPreservesOutermostBorderAndZeroIterations()
        {
            _data.SetHeights(16, 16, new[,] { { 1f } });
            var zero = TerrainHeightCommands.Smooth(_target, radius: 0f, iterations: 0, confirm: true);
            Assert.That(zero.Ok, Is.True);
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(10f).Within(0.01f));
            Assert.That(TerrainHeightCommands.Smooth(_target, radius: 0f, iterations: 1, confirm: true).Ok, Is.True);
            Assert.That(_data.GetHeight(16, 16), Is.EqualTo(10f / 9f).Within(0.02f));
            Assert.That(_data.GetHeight(0, 0), Is.Zero.Within(0.001f));
        }

        [Test]
        public void PerlinSeedRepeatsAndBatchClipsAtEdge()
        {
            Assert.That(TerrainHeightCommands.Perlin(_target, seed: 99, confirm: true).Ok, Is.True);
            var first = _data.GetHeights(0, 0, 33, 33);
            Assert.That(TerrainHeightCommands.Perlin(_target, seed: 99, confirm: true).Ok, Is.True);
            var second = _data.GetHeights(0, 0, 33, 33);
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++)
                Assert.That(second[z, x], Is.EqualTo(first[z, x]));
            Assert.That(TerrainHeightCommands.SetHeights(_target, "[[2,2],[2,2]]", 32, 32, confirm: true).Ok, Is.True);
            Assert.That(_data.GetHeight(32, 32), Is.EqualTo(10f).Within(0.01f));
        }

        [Test]
        public void SampleHeightAddsNonzeroTerrainElevation()
        {
            _object.transform.position = new Vector3(0, 7, 0);
            _data.SetHeights(0, 0, new[,] { { 0.5f } });
            var result = TerrainCreationCommands.SampleHeight(_target, 0, 0);
            Assert.That(result.Ok, Is.True);
            Assert.That(Convert.ToSingle(result.Result.GetType().GetProperty("worldY").GetValue(result.Result)), Is.EqualTo(12f).Within(0.01f));
        }

        [Test]
        public void PaintNormalizesWeightsAndUndoRestoresCenter()
        {
            var first = new TerrainLayer();
            var second = new TerrainLayer();
            try
            {
                _data.terrainLayers = new[] { first, second };
                var original = new float[1, 1, 2];
                original[0, 0, 0] = 1f;
                _data.SetAlphamaps(8, 8, original);
                var heightBefore = _data.GetHeight(16, 16);
                Assert.That(TerrainHeightCommands.SetHeight(_target, height: 0.6f, confirm: true).Ok, Is.True);
                var preview = TerrainTextureCommands.Paint(_target, centerX: 8f / 15f, centerZ: 8f / 15f,
                    layer: 1, brushSize: 1, dryRun: true);
                Assert.That(preview.Ok, Is.True);
                Assert.That(_data.GetAlphamaps(8, 8, 1, 1)[0, 0, 1], Is.Zero);
                var changed = TerrainTextureCommands.Paint(_target, centerX: 8f / 15f, centerZ: 8f / 15f,
                    layer: 1, brushSize: 1, confirm: true);
                Assert.That(changed.Ok, Is.True);
                var painted = _data.GetAlphamaps(8, 8, 1, 1);
                Assert.That(painted[0, 0, 1], Is.EqualTo(1f).Within(0.001f));
                Assert.That(painted[0, 0, 0] + painted[0, 0, 1], Is.EqualTo(1f).Within(0.001f));
                Undo.PerformUndo();
                Assert.That(_data.GetAlphamaps(8, 8, 1, 1)[0, 0, 0], Is.EqualTo(1f).Within(0.001f));
                Assert.That(_data.GetHeight(16, 16), Is.EqualTo(6f).Within(0.01f));
                Undo.PerformUndo();
                Assert.That(_data.GetHeight(16, 16), Is.EqualTo(heightBefore).Within(0.01f));
            }
            finally { UnityObject.DestroyImmediate(first); UnityObject.DestroyImmediate(second); }
        }

        [Test]
        public void SinglePixelBrushAtDefaultCenterFullyPaintsNearestPixel()
        {
            var first = new TerrainLayer();
            var second = new TerrainLayer();
            try
            {
                _data.terrainLayers = new[] { first, second };
                var baseline = new float[16, 16, 2];
                for (int z = 0; z < 16; z++) for (int x = 0; x < 16; x++) baseline[z, x, 0] = 1f;
                _data.SetAlphamaps(0, 0, baseline);

                var result = TerrainTextureCommands.Paint(_target, layer: 1, brushSize: 1, confirm: true);

                Assert.That(result.Ok, Is.True, result.Error?.Message);
                var painted = _data.GetAlphamaps(8, 8, 1, 1);
                Assert.That(painted[0, 0, 1], Is.EqualTo(1f).Within(0.001f));
                Assert.That(painted[0, 0, 0] + painted[0, 0, 1], Is.EqualTo(1f).Within(0.001f));
                Assert.That(_data.GetAlphamaps(7, 7, 1, 1)[0, 0, 1], Is.Zero.Within(0.001f));
            }
            finally { UnityObject.DestroyImmediate(first); UnityObject.DestroyImmediate(second); }
        }

        [Test]
        public void HillMidpointUsesCosineFalloff()
        {
            var result = TerrainHeightCommands.Hill(_target, radius: 2f / 33f, height: 1f, confirm: true);
            Assert.That(result.Ok, Is.True);
            var expected = Mathf.Cos(Mathf.PI * 0.25f);
            Assert.That(_data.GetHeights(17, 16, 1, 1)[0, 0], Is.EqualTo(expected).Within(0.001f));
            Assert.That(TerrainHeightCommands.Hill(_target, radius: 2f / 33f, height: 0.2f,
                smoothness: 0f, confirm: true).Ok, Is.True);
            Assert.That(_data.GetHeights(18, 16, 1, 1)[0, 0], Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(_data.GetHeights(19, 16, 1, 1)[0, 0], Is.Zero.Within(0.001f));
        }

        [Test]
        public void FlattenMidpointUsesCosineBlend()
        {
            _data.SetHeights(17, 16, new[,] { { 1f } });
            var result = TerrainHeightCommands.Flatten(_target, radius: 2f / 33f, targetHeight: 0f, confirm: true);
            Assert.That(result.Ok, Is.True);
            var expected = 1f - Mathf.Cos(Mathf.PI * 0.25f);
            Assert.That(_data.GetHeights(17, 16, 1, 1)[0, 0], Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void PerlinMatchesOriginalSeededSamplingFormula()
        {
            var result = TerrainHeightCommands.Perlin(_target, seed: 3050, confirm: true);
            Assert.That(result.Ok, Is.True);
            var random = new System.Random(3050);
            float offsetX = random.Next(-10000, 10000);
            float offsetZ = random.Next(-10000, 10000);
            float amplitude = 1f, frequency = 1f, noiseHeight = 0f;
            for (int octave = 0; octave < 4; octave++)
            {
                float x = (9 / 33f * 20f + offsetX) * frequency;
                float z = (13 / 33f * 20f + offsetZ) * frequency;
                noiseHeight += (Mathf.PerlinNoise(x, z) * 2f - 1f) * amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }
            var expected = Mathf.Clamp01(noiseHeight * 0.3f + 0.5f);
            var nativeData = new TerrainData { heightmapResolution = 33 };
            try
            {
                nativeData.SetHeights(9, 13, new[,] { { expected } });
                var nativeExpected = nativeData.GetHeights(9, 13, 1, 1)[0, 0];
                Assert.That(_data.GetHeights(9, 13, 1, 1)[0, 0], Is.EqualTo(nativeExpected));
            }
            finally
            {
                UnityObject.DestroyImmediate(nativeData);
            }
        }

        [Test]
        public void InspectIncludesLayerDiffuseTextureAndTileSize()
        {
            var layer = new TerrainLayer();
            var texture = new Texture2D(2, 2) { name = "OwnedDiffuse" };
            try
            {
                layer.diffuseTexture = texture;
                layer.tileSize = new Vector2(4f, 7f);
                _data.terrainLayers = new[] { layer };
                var info = TerrainCreationCommands.Inspect(_target);
                Assert.That(info.Ok, Is.True);
                var layers = (Array)info.Result.GetType().GetProperty("layers").GetValue(info.Result);
                var first = layers.GetValue(0);
                Assert.That(first.GetType().GetProperty("diffuseTexture").GetValue(first), Is.EqualTo("OwnedDiffuse"));
                var size = first.GetType().GetProperty("tileSize").GetValue(first);
                Assert.That(size.GetType().GetProperty("x").GetValue(size), Is.EqualTo(4f));
                Assert.That(size.GetType().GetProperty("y").GetValue(size), Is.EqualTo(7f));
            }
            finally { UnityObject.DestroyImmediate(layer); UnityObject.DestroyImmediate(texture); }
        }

        [Test]
        public void EvenBrushAtHalfPixelPaintsFourNearestPixels()
        {
            var first = new TerrainLayer();
            var second = new TerrainLayer();
            try
            {
                _data.terrainLayers = new[] { first, second };
                var baseline = new float[16, 16, 2];
                for (int z = 0; z < 16; z++) for (int x = 0; x < 16; x++) baseline[z, x, 0] = 1f;
                _data.SetAlphamaps(0, 0, baseline);
                var result = TerrainTextureCommands.Paint(_target, centerX: 0.5f, centerZ: 0.5f,
                    layer: 1, brushSize: 2, confirm: true);
                Assert.That(result.Ok, Is.True);
                var map = _data.GetAlphamaps(6, 6, 4, 4);
                foreach (var z in new[] { 1, 2 }) foreach (var x in new[] { 1, 2 })
                    Assert.That(map[z, x, 1], Is.GreaterThan(0.2f));
                Assert.That(map[0, 0, 1], Is.Zero.Within(0.001f));
            }
            finally { UnityObject.DestroyImmediate(first); UnityObject.DestroyImmediate(second); }
        }

        [Test]
        public void CreationOwnsSceneUndoButLeavesAssetForExplicitCleanup()
        {
            var path = "Assets/__TerrainContract_" + Guid.NewGuid().ToString("N") + ".asset";
            try
            {
                var preview = TerrainCreationCommands.Create(assetPath: path, resolution: 33, dryRun: true);
                Assert.That(preview.Ok, Is.True);
                Assert.That(AssetDatabase.LoadMainAssetAtPath(path), Is.Null);
                var created = TerrainCreationCommands.Create(assetPath: path, resolution: 33, confirm: true);
                Assert.That(created.Ok, Is.True, created.Error?.Message);
                var asset = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                Assert.That(asset, Is.Not.Null);
                var result = created.Result;
                var identity = result.GetType().GetProperty("identity").GetValue(result);
                var terrainId = (string)identity.GetType().GetProperty("target").GetValue(identity);
                var terrain = EditorUtility.EntityIdToObject(EntityId.FromULong(ulong.Parse(terrainId, CultureInfo.InvariantCulture))) as UnityTerrain;
                Assert.That(terrain, Is.Not.Null);
                Assert.That(terrain.GetComponent<TerrainCollider>().terrainData, Is.SameAs(asset));
                Assert.That(TerrainCreationCommands.Create(assetPath: path, resolution: 33, confirm: true).Ok, Is.False);
                var createdHeight = asset.GetHeight(16, 16);
                Assert.That(TerrainHeightCommands.SetHeight(terrainId, height: 0.4f, confirm: true).Ok, Is.True);
                Assert.That(asset.GetHeight(16, 16), Is.EqualTo(40f).Within(0.01f));
                Undo.PerformUndo();
                Assert.That(terrain, Is.Not.Null);
                Assert.That(asset.GetHeight(16, 16), Is.EqualTo(createdHeight).Within(0.01f));
                Undo.PerformUndo();
                Assert.That(terrain == null, Is.True);
                Assert.That(AssetDatabase.LoadAssetAtPath<TerrainData>(path), Is.Not.Null);
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }
    }
}
