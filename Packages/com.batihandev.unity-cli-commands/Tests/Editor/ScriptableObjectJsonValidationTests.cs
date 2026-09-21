using System.IO;
using BatihanDev.UnityCliCommands.ScriptableObject;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class ScriptableObjectJsonValidationTests
    {
        private const string Root = "Assets/ScriptableObjectJsonValidationTests";
        private const string AssetPath = Root + "/Target.asset";
        private const string EngineAssetPath = Root + "/EngineTarget.asset";
        private const string MaterialPath = Root + "/Reference.mat";
        private const string TexturePath = Root + "/WrongTexture.asset";
        private ScriptableObjectJsonValidationFixtureAsset asset;
        private ScriptableObjectJsonEngineShapesFixtureAsset engineAsset;
        private Material material;
        private Texture2D texture;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjectJsonValidationTests");
            material = new Material(Shader.Find("Standard"));
            texture = new Texture2D(2, 2);
            asset = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectJsonValidationFixtureAsset>();
            asset.name = "Target";
            asset.Number = 11;
            asset.SmallUnsigned = 12;
            asset.Vector = new Vector3(1f, 2f, 3f);
            asset.Mode = ScriptableObjectJsonValidationMode.Alpha;
            asset.Numbers.AddRange(new[] { 2, 4, 8 });
            asset.Nested.Count = 5;
            asset.Nested.Label = "before";
            engineAsset = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectJsonEngineShapesFixtureAsset>();
            engineAsset.Rect = new Rect(1f, 2f, 3f, 4f);
            engineAsset.Bounds = new Bounds(new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f));
            engineAsset.Curve = AnimationCurve.Constant(0f, 1f, 2f);
            engineAsset.ColorGradient.SetKeys(
                new[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            engineAsset.LayerMask = (LayerMask)(1 << 2);
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.CreateAsset(texture, TexturePath);
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.CreateAsset(engineAsset, EngineAssetPath);
            AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Root);
            AssetDatabase.Refresh();
        }

        [Test]
        public void ImportJsonRefusesMalformedNumericInDryRunAndActualWithoutChangingAsset()
        {
            var before = CaptureState();
            const string json = "{\"MonoBehaviour\":{\"Number\":\"invalid-numeric\"}}";

            var dryRun = ScriptableObjectAuthoringCommands.ImportJson(AssetPath, json, dryRun: true);
            var actual = ScriptableObjectAuthoringCommands.ImportJson(AssetPath, json, confirm: true);

            AssertRefused(dryRun.Ok, dryRun.Error?.Code);
            AssertRefused(actual.Ok, actual.Error?.Code);
            AssertStateUnchanged(before);
        }

        [Test]
        public void ImportJsonRefusesWrongObjectReferenceTypeInDryRunAndActualWithoutChangingAsset()
        {
            var before = CaptureState();
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture, out string guid, out long fileId), Is.True);
            var json = "{\"MonoBehaviour\":{\"MaterialReference\":{\"fileID\":" + fileId +
                ",\"guid\":\"" + guid + "\",\"type\":2}}}";

            var dryRun = ScriptableObjectAuthoringCommands.ImportJson(AssetPath, json, dryRun: true);
            var actual = ScriptableObjectAuthoringCommands.ImportJson(AssetPath, json, confirm: true);

            AssertRefused(dryRun.Ok, dryRun.Error?.Code);
            AssertRefused(actual.Ok, actual.Error?.Code);
            AssertStateUnchanged(before);
        }

        [Test]
        public void ImportJsonRefusesMixedValidAndInvalidFieldsWithoutPartialWrite()
        {
            var before = CaptureState();
            const string json = "{\"MonoBehaviour\":{\"Number\":99,\"SmallUnsigned\":65536}}";

            var result = ScriptableObjectAuthoringCommands.ImportJson(AssetPath, json, confirm: true);

            AssertRefused(result.Ok, result.Error?.Code);
            AssertStateUnchanged(before);
        }

        [Test]
        public void ImportJsonAcceptsPartialNestedListVectorEnumAndBoundedNumericValues()
        {
            const string json = "{\"MonoBehaviour\":{" +
                "\"SmallUnsigned\":65535,\"Vector\":{\"x\":4.0,\"y\":5.0,\"z\":6.0}," +
                "\"Mode\":9,\"Numbers\":[9,7,5],\"Nested\":{\"Count\":8,\"Label\":\"after\"}}}";

            var result = ScriptableObjectAuthoringCommands.ImportJson(AssetPath, json, confirm: true);

            Assert.That(result.Ok, Is.True, result.Error == null ? null : result.Error.Code + ":" +
                string.Join(",", result.Error.Details));
            Assert.That(asset.Number, Is.EqualTo(11));
            Assert.That(asset.SmallUnsigned, Is.EqualTo(ushort.MaxValue));
            Assert.That(asset.Vector, Is.EqualTo(new Vector3(4f, 5f, 6f)));
            Assert.That(asset.Mode, Is.EqualTo(ScriptableObjectJsonValidationMode.Beta));
            Assert.That(asset.Numbers, Is.EqualTo(new[] { 9, 7, 5 }));
            Assert.That(asset.Nested.Count, Is.EqualTo(8));
            Assert.That(asset.Nested.Label, Is.EqualTo("after"));
        }

        [Test]
        public void ImportJsonPersistsListShrinkWithRetainedValues()
        {
            AssertNumbersImportedAndReloaded("[9,7]", new[] { 9, 7 });
        }

        [Test]
        public void ImportJsonPersistsListShrinkToEmpty()
        {
            AssertNumbersImportedAndReloaded("[]", new int[0]);
        }

        [Test]
        public void ImportJsonPersistsListGrowthWithAddedValue()
        {
            AssertNumbersImportedAndReloaded("[9,7,5,3]", new[] { 9, 7, 5, 3 });
        }

        [Test]
        public void ImportJsonAcceptsExportedEngineOwnedShapesAndReloadsTheirValues()
        {
            var source = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectJsonEngineShapesFixtureAsset>();
            try
            {
                source.Rect = new Rect(-2.5f, 3.25f, 11f, 12f);
                source.Bounds = new Bounds(new Vector3(7f, 8f, 9f), new Vector3(10f, 12f, 14f));
                source.Curve = new AnimationCurve(
                    new Keyframe(-1f, 2f, 0.25f, 0.5f),
                    new Keyframe(2f, 8f, 1.5f, -0.75f))
                {
                    preWrapMode = WrapMode.Loop,
                    postWrapMode = WrapMode.PingPong
                };
                source.ColorGradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.1f, 0.2f, 0.3f, 1f), 0f),
                        new GradientColorKey(new Color(0.8f, 0.6f, 0.4f, 1f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.25f, 0f),
                        new GradientAlphaKey(0.75f, 1f)
                    });
                source.ColorGradient.mode = GradientMode.Blend;
                source.LayerMask = (LayerMask)~0;
                var json = EditorJsonUtility.ToJson(source);

                var result = ScriptableObjectAuthoringCommands.ImportJson(EngineAssetPath, json, confirm: true);

                Assert.That(result.Ok, Is.True, result.Error == null ? null : result.Error.Code + ":" +
                    string.Join(",", result.Error.Details) + ":" + json);
                AssetDatabase.ImportAsset(EngineAssetPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var reloaded = AssetDatabase.LoadAssetAtPath<ScriptableObjectJsonEngineShapesFixtureAsset>(EngineAssetPath);
                Assert.That(reloaded.Rect, Is.EqualTo(new Rect(-2.5f, 3.25f, 11f, 12f)));
                Assert.That(reloaded.Bounds.center, Is.EqualTo(new Vector3(7f, 8f, 9f)));
                Assert.That(reloaded.Bounds.size, Is.EqualTo(new Vector3(10f, 12f, 14f)));
                AssertCurve(reloaded.Curve);
                AssertGradient(reloaded.ColorGradient);
                Assert.That(reloaded.LayerMask.value, Is.EqualTo(~0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void ImportJsonAcceptsPartialEngineOwnedShapeAndPreservesOmittedMembers()
        {
            const string json = "{\"MonoBehaviour\":{\"Rect\":{\"x\":21.5}}}";

            var result = ScriptableObjectAuthoringCommands.ImportJson(EngineAssetPath, json, confirm: true);

            Assert.That(result.Ok, Is.True, result.Error == null ? null : result.Error.Code + ":" +
                string.Join(",", result.Error.Details));
            AssetDatabase.ImportAsset(EngineAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<ScriptableObjectJsonEngineShapesFixtureAsset>(EngineAssetPath);
            Assert.That(reloaded.Rect, Is.EqualTo(new Rect(21.5f, 2f, 3f, 4f)));
            Assert.That(reloaded.Bounds.center, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(reloaded.Bounds.size, Is.EqualTo(new Vector3(4f, 5f, 6f)));
        }

        [Test]
        public void ImportJsonRefusesMalformedNestedEngineOwnedShapeWithoutChangingAsset()
        {
            var before = CaptureEngineState();
            const string json = "{\"MonoBehaviour\":{\"Bounds\":{\"m_Center\":{\"x\":\"invalid\"}}}}";

            var result = ScriptableObjectAuthoringCommands.ImportJson(EngineAssetPath, json, confirm: true);

            AssertRefused(result.Ok, result.Error?.Code);
            AssertEngineStateUnchanged(before);
        }

        [Test]
        public void ImportJsonAcceptsFullEditorJsonAcrossAssetsAndMaterialNullClearWithoutChangingName()
        {
            var sourcePath = Root + "/Source.asset";
            var source = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectJsonValidationFixtureAsset>();
            source.name = "Source";
            source.Number = 23;
            source.SmallUnsigned = 42;
            source.Vector = new Vector3(7f, 8f, 9f);
            source.Mode = ScriptableObjectJsonValidationMode.Beta;
            source.Numbers.AddRange(new[] { 6, 5, 4 });
            source.Nested.Count = 17;
            source.Nested.Label = "source";
            source.MaterialReference = material;
            AssetDatabase.CreateAsset(source, sourcePath);
            AssetDatabase.SaveAssets();

            var imported = ScriptableObjectAuthoringCommands.ImportJson(
                AssetPath, EditorJsonUtility.ToJson(source), confirm: true);

            Assert.That(imported.Ok, Is.True, imported.Error?.Code);
            Assert.That(asset.name, Is.EqualTo("Target"));
            Assert.That(asset.Number, Is.EqualTo(23));
            Assert.That(asset.SmallUnsigned, Is.EqualTo(42));
            Assert.That(asset.Vector, Is.EqualTo(new Vector3(7f, 8f, 9f)));
            Assert.That(asset.Mode, Is.EqualTo(ScriptableObjectJsonValidationMode.Beta));
            Assert.That(asset.Numbers, Is.EqualTo(new[] { 6, 5, 4 }));
            Assert.That(asset.Nested.Count, Is.EqualTo(17));
            Assert.That(asset.Nested.Label, Is.EqualTo("source"));
            Assert.That(asset.MaterialReference, Is.SameAs(material));

            source.MaterialReference = null;
            var cleared = ScriptableObjectAuthoringCommands.ImportJson(
                AssetPath, EditorJsonUtility.ToJson(source), confirm: true);

            Assert.That(cleared.Ok, Is.True, cleared.Error?.Code);
            Assert.That(asset.MaterialReference, Is.Null);
            Assert.That(asset.name, Is.EqualTo("Target"));
        }

        [Test]
        public void ImportJsonAcceptsEditorInstanceReferenceEncodingWhenAssignable()
        {
            var source = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectJsonValidationFixtureAsset>();
            var transientReference = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectJsonValidationFixtureAsset>();
            try
            {
                source.Reference = transientReference;
                var json = EditorJsonUtility.ToJson(source);
                StringAssert.Contains("\"instanceID\"", json);

                var result = ScriptableObjectAuthoringCommands.ImportJson(AssetPath, json, dryRun: true);

                Assert.That(result.Ok, Is.True, result.Error == null ? null : result.Error.Code + ":" + json + ":" +
                    string.Join(",", result.Error.Details));
                Assert.That(asset.Reference, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(transientReference);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [TestCase("-1")]
        [TestCase("65536")]
        [TestCase("\"12\"")]
        public void ImportJsonRefusesUnsignedNumericBoundsAndTokenType(string value)
        {
            var before = CaptureState();

            var result = ScriptableObjectAuthoringCommands.ImportJson(
                AssetPath, "{\"MonoBehaviour\":{\"SmallUnsigned\":" + value + "}}", confirm: true);

            AssertRefused(result.Ok, result.Error?.Code);
            AssertStateUnchanged(before);
        }

        [Test]
        public void ImportJsonRefusesEnumStringToken()
        {
            var before = CaptureState();

            var result = ScriptableObjectAuthoringCommands.ImportJson(
                AssetPath, "{\"MonoBehaviour\":{\"Mode\":\"Beta\"}}", confirm: true);

            AssertRefused(result.Ok, result.Error?.Code);
            AssertStateUnchanged(before);
        }

        private void AssertNumbersImportedAndReloaded(string numbersJson, int[] expected)
        {
            var result = ScriptableObjectAuthoringCommands.ImportJson(
                AssetPath, "{\"MonoBehaviour\":{\"Numbers\":" + numbersJson + "}}", confirm: true);

            Assert.That(result.Ok, Is.True, result.Error == null ? null : result.Error.Code + ":" +
                string.Join(",", result.Error.Details));
            Assert.That(asset.Numbers, Is.EqualTo(expected));
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<ScriptableObjectJsonValidationFixtureAsset>(AssetPath);
            Assert.That(reloaded.Numbers, Is.EqualTo(expected));
            Assert.That(reloaded.Number, Is.EqualTo(11));
        }

        private static void AssertCurve(AnimationCurve curve)
        {
            Assert.That(curve, Is.Not.Null);
            Assert.That(curve.length, Is.EqualTo(2));
            Assert.That(curve.preWrapMode, Is.EqualTo(WrapMode.Loop));
            Assert.That(curve.postWrapMode, Is.EqualTo(WrapMode.PingPong));
            AssertKeyframe(curve[0], -1f, 2f, 0.25f, 0.5f);
            AssertKeyframe(curve[1], 2f, 8f, 1.5f, -0.75f);
        }

        private static void AssertKeyframe(Keyframe key, float time, float value, float inTangent, float outTangent)
        {
            Assert.That(key.time, Is.EqualTo(time).Within(0.0001f));
            Assert.That(key.value, Is.EqualTo(value).Within(0.0001f));
            Assert.That(key.inTangent, Is.EqualTo(inTangent).Within(0.0001f));
            Assert.That(key.outTangent, Is.EqualTo(outTangent).Within(0.0001f));
        }

        private static void AssertGradient(Gradient gradient)
        {
            Assert.That(gradient, Is.Not.Null);
            Assert.That(gradient.mode, Is.EqualTo(GradientMode.Blend));
            Assert.That(gradient.colorKeys, Has.Length.EqualTo(2));
            AssertColorKey(gradient.colorKeys[0], new Color(0.1f, 0.2f, 0.3f, 1f), 0f);
            AssertColorKey(gradient.colorKeys[1], new Color(0.8f, 0.6f, 0.4f, 1f), 1f);
            Assert.That(gradient.alphaKeys, Has.Length.EqualTo(2));
            AssertAlphaKey(gradient.alphaKeys[0], 0.25f, 0f);
            AssertAlphaKey(gradient.alphaKeys[1], 0.75f, 1f);
        }

        private static void AssertColorKey(GradientColorKey key, Color color, float time)
        {
            Assert.That(key.color.r, Is.EqualTo(color.r).Within(0.0001f));
            Assert.That(key.color.g, Is.EqualTo(color.g).Within(0.0001f));
            Assert.That(key.color.b, Is.EqualTo(color.b).Within(0.0001f));
            Assert.That(key.time, Is.EqualTo(time).Within(0.0001f));
        }

        private static void AssertAlphaKey(GradientAlphaKey key, float alpha, float time)
        {
            Assert.That(key.alpha, Is.EqualTo(alpha).Within(0.0001f));
            Assert.That(key.time, Is.EqualTo(time).Within(0.0001f));
        }

        private AssetState CaptureState()
        {
            return new AssetState
            {
                Bytes = File.ReadAllBytes(AbsolutePath(AssetPath)),
                Guid = AssetDatabase.AssetPathToGUID(AssetPath),
                Dirty = EditorUtility.IsDirty(asset),
                Number = asset.Number,
                SmallUnsigned = asset.SmallUnsigned,
                MaterialReference = asset.MaterialReference
            };
        }

        private EngineAssetState CaptureEngineState()
        {
            return new EngineAssetState
            {
                Bytes = File.ReadAllBytes(AbsolutePath(EngineAssetPath)),
                Guid = AssetDatabase.AssetPathToGUID(EngineAssetPath),
                Dirty = EditorUtility.IsDirty(engineAsset),
                Json = EditorJsonUtility.ToJson(engineAsset)
            };
        }

        private void AssertEngineStateUnchanged(EngineAssetState before)
        {
            Assert.That(File.ReadAllBytes(AbsolutePath(EngineAssetPath)), Is.EqualTo(before.Bytes),
                "engine asset file bytes changed");
            Assert.That(AssetDatabase.AssetPathToGUID(EngineAssetPath), Is.EqualTo(before.Guid),
                "engine asset GUID changed");
            Assert.That(EditorUtility.IsDirty(engineAsset), Is.EqualTo(before.Dirty),
                "engine asset dirty state changed");
            AssetDatabase.ImportAsset(EngineAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<ScriptableObjectJsonEngineShapesFixtureAsset>(EngineAssetPath);
            Assert.That(EditorJsonUtility.ToJson(reloaded), Is.EqualTo(before.Json));
        }

        private void AssertStateUnchanged(AssetState before)
        {
            Assert.That(File.ReadAllBytes(AbsolutePath(AssetPath)), Is.EqualTo(before.Bytes), "asset file bytes changed");
            Assert.That(AssetDatabase.AssetPathToGUID(AssetPath), Is.EqualTo(before.Guid), "asset GUID changed");
            Assert.That(EditorUtility.IsDirty(asset), Is.EqualTo(before.Dirty), "asset dirty state changed");
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<ScriptableObjectJsonValidationFixtureAsset>(AssetPath);
            Assert.That(reloaded.Number, Is.EqualTo(before.Number));
            Assert.That(reloaded.SmallUnsigned, Is.EqualTo(before.SmallUnsigned));
            if (before.MaterialReference == null)
                Assert.That(reloaded.MaterialReference, Is.Null);
            else
                Assert.That(reloaded.MaterialReference, Is.SameAs(before.MaterialReference));
        }

        private static void AssertRefused(bool ok, string errorCode)
        {
            Assert.That(ok, Is.False);
            Assert.That(errorCode, Is.EqualTo("JSON_INVALID"));
        }

        private static string AbsolutePath(string projectPath) =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, projectPath);

        private sealed class AssetState
        {
            public byte[] Bytes;
            public string Guid;
            public bool Dirty;
            public int Number;
            public ushort SmallUnsigned;
            public Material MaterialReference;
        }

        private sealed class EngineAssetState
        {
            public byte[] Bytes;
            public string Guid;
            public bool Dirty;
            public string Json;
        }
    }
}
