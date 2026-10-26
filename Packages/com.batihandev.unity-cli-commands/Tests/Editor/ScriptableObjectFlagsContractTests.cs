using System.IO;
using BatihanDev.UnityCliCommands.ScriptableObject;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class ScriptableObjectFlagsContractTests
    {
        private const string Root = "Assets/Task10FlagsSnapshotTests";
        private const string Target = Root + "/Target.asset";
        private ScriptableObjectFlagsFixtureAsset asset;

        [SetUp]
        public void SetUp()
        {
            Assert.That(AssetDatabase.IsValidFolder(Root), Is.False, "Test refuses an existing fixture folder.");
            AssetDatabase.CreateFolder("Assets", "Task10FlagsSnapshotTests");
            asset = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectFlagsFixtureAsset>();
            AssetDatabase.CreateAsset(asset, Target);
            AssetDatabase.SaveAssetIfDirty(asset);
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            AssetDatabase.DeleteAsset(Root);
        }

        private static string Payload(int flags) => "{\"MonoBehaviour\":{\"Flags\":" + flags + "}}";
        private static byte[] Bytes(string path) => File.ReadAllBytes(Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, path));
        private void Reimport()
        {
            AssetDatabase.ImportAsset(Target, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            asset = AssetDatabase.LoadAssetAtPath<ScriptableObjectFlagsFixtureAsset>(Target);
        }
        private void Apply(int flags)
        {
            var result = ScriptableObjectAuthoringCommands.ImportJson(Target, Payload(flags), confirm: true);
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.ChangedMembers, Does.Contain("Flags"));
            Assert.That((int)asset.Flags, Is.EqualTo(flags));
            Assert.That(asset.Omitted, Is.EqualTo(42));
        }

        [TestCase(5, 9)]
        [TestCase(9, 5)]
        public void CombinedFlagsTransitionPersistsBothMasks(int first, int second)
        {
            Apply(first);
            Reimport();
            Assert.That((int)asset.Flags, Is.EqualTo(first));
            Apply(second);
            Reimport();
            Assert.That((int)asset.Flags, Is.EqualTo(second));
            Assert.That(asset.Omitted, Is.EqualTo(42));
        }

        [Test]
        public void FlagsDryRunAndNoConfirmationPreserveDiskAndMemory()
        {
            var before = Bytes(Target);
            var preview = ScriptableObjectAuthoringCommands.ImportJson(Target, Payload(5), dryRun: true);
            Assert.That(preview.Ok, Is.True, preview.Error?.Code);
            Assert.That(preview.Result.ChangedMembers, Does.Contain("Flags"));
            Assert.That(preview.Result.Applied, Is.False);
            var refused = ScriptableObjectAuthoringCommands.ImportJson(Target, Payload(9));
            Assert.That(refused.Ok, Is.False);
            Assert.That(refused.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            Assert.That(asset.Flags, Is.EqualTo(SnapshotFlags.One));
            Assert.That(Bytes(Target), Is.EqualTo(before));
            Assert.That(EditorUtility.IsDirty(asset), Is.False);
        }

        [Test]
        public void FlagsNoOpReportsNoChangedMembers()
        {
            Apply(5);
            Reimport();
            var before = Bytes(Target);
            var result = ScriptableObjectAuthoringCommands.ImportJson(Target, Payload(5), confirm: true);
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.ChangedMembers, Is.Empty);
            Assert.That((int)asset.Flags, Is.EqualTo(5));
            Assert.That(Bytes(Target), Is.EqualTo(before));
        }

        [Test]
        public void SparseNamedEnumPreservesUnderlyingValueAfterReimport()
        {
            var result = ScriptableObjectAuthoringCommands.ImportJson(Target,
                "{\"MonoBehaviour\":{\"Sparse\":91}}", confirm: true);
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.ChangedMembers, Does.Contain("Sparse"));
            Reimport();
            Assert.That(asset.Sparse, Is.EqualTo(SnapshotSparse.Third));
            Assert.That(asset.Flags, Is.EqualTo(SnapshotFlags.One));
            Assert.That(asset.Omitted, Is.EqualTo(42));
        }

        [Test]
        public void FlagsUndoRedoRestoresMemoryBeforeExplicitPersistence()
        {
            Apply(5);
            Undo.FlushUndoRecordObjects();
            var saved = Bytes(Target);
            Undo.PerformUndo();
            Assert.That(asset.Flags, Is.EqualTo(SnapshotFlags.One));
            Assert.That(Bytes(Target), Is.EqualTo(saved), "Undo alone does not restore persistent bytes.");
            Undo.PerformRedo();
            Assert.That((int)asset.Flags, Is.EqualTo(5));
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            Reimport();
            Assert.That((int)asset.Flags, Is.EqualTo(5));
        }

        [TestCase("json")]
        [TestCase("member")]
        public void AuthoredStateCommandDoesNotSaveOrClearUnrelatedDirtyAsset(string command)
        {
            const string otherPath = Root + "/Other.asset";
            var other = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectFlagsFixtureAsset>();
            AssetDatabase.CreateAsset(other, otherPath);
            AssetDatabase.SaveAssetIfDirty(other);
            AssetDatabase.ImportAsset(otherPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            other = AssetDatabase.LoadAssetAtPath<ScriptableObjectFlagsFixtureAsset>(otherPath);
            var before = Bytes(otherPath);
            other.Omitted = 777;
            EditorUtility.SetDirty(other);
            Assert.That(other.Omitted, Is.EqualTo(777));
            Assert.That(EditorUtility.IsDirty(other), Is.True);
            if (command == "json")
            {
                var result = ScriptableObjectAuthoringCommands.ImportJson(Target,
                    "{\"MonoBehaviour\":{\"Omitted\":43}}", confirm: true);
                Assert.That(result.Ok, Is.True, result.Error?.Code);
            }
            else
            {
                var result = ScriptableObjectAuthoringCommands.MemberSet(Target, "Omitted", "43", confirm: true);
                Assert.That(result.Ok, Is.True, result.Error?.Code);
            }
            Reimport();
            Assert.That(asset.Omitted, Is.EqualTo(43));
            Assert.That(other.Omitted, Is.EqualTo(777));
            Assert.That(Bytes(otherPath), Is.EqualTo(before), "Command saved an unrelated dirty asset.");
            Assert.That(EditorUtility.IsDirty(other), Is.True, "Command cleared unrelated dirty state.");
        }
    }
}
