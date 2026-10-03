using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BatihanDev.UnityCliCommands.Asset;
using BatihanDev.UnityCliCommands.Foundation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class SupportScriptTrashContractTests
    {
        private string root;
        private string path;
        private byte[] source;
        private byte[] meta;
        private string guid;
        [SetUp] public void SetUp()
        {
            root = "Assets/SupportScriptTrash_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(root));
            path = root + "/Exact.txt";
            source = new byte[] { 0xef, 0xbb, 0xbf, 97, 13, 10, 98, 13, 10 };
            File.WriteAllBytes(path, source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            meta = File.ReadAllBytes(path + ".meta");
            guid = AssetDatabase.AssetPathToGUID(path);
        }
        [TearDown] public void TearDown() { AssetDatabase.DeleteAsset(root); AssetDatabase.Refresh(); }
        [TestCase("")]
        [TestCase("abc")]
        [TestCase("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
        public void MalformedHashRefusesBeforeMutation(string hash)
        {
            var result = Trash(hash, true, false);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("EXPECTED_SHA256_INVALID"));
            AssertUnchanged();
        }
        [Test] public void StaleHashRefusesEvenDuringPreview()
        {
            var result = Trash(new string('0', 64), true, true);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("SOURCE_SHA256_MISMATCH"));
            AssertUnchanged();
        }
        [Test] public void CorrectHashPreviewReturnsIdentityWithoutMutation()
        {
            var result = Trash(Hash(source).ToUpperInvariant(), false, true);
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.Applied, Is.False);
            Assert.That(result.Result.DryRun, Is.True);
            Assert.That(result.Result.Guid, Is.EqualTo(guid));
            AssertUnchanged();
        }
        [Test] public void CorrectHashWithoutConfirmationPreservesFileAndMeta()
        {
            var result = Trash(Hash(source), false, false);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            AssertUnchanged();
        }
        [Test] public void ContentChangedAfterPreviewRefusesConfirmedDeletion()
        {
            var hash = Hash(source);
            Assert.That(Trash(hash, false, true).Ok, Is.True);
            var changed = new byte[] { 99, 10 };
            File.WriteAllBytes(path, changed);
            var result = Trash(hash, true, false);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("SOURCE_SHA256_MISMATCH"));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(changed));
            Assert.That(File.ReadAllBytes(path + ".meta"), Is.EqualTo(meta));
        }
        [Test] public void ConfirmedExactDeletionRestoresOriginalBytesMetaAndGuid()
        {
            var neighbor = root + "/Neighbor.txt";
            File.WriteAllText(neighbor, "retain");
            AssetDatabase.ImportAsset(neighbor, ImportAssetOptions.ForceSynchronousImport);
            var neighborMeta = File.ReadAllBytes(neighbor + ".meta");
            var result = Trash(Hash(source), true, false);
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.Guid, Is.EqualTo(guid));
            Assert.That(result.Result.Applied, Is.True);
            Assert.That(result.Result.Undoable, Is.False);
            Assert.That(File.Exists(path), Is.False);
            Assert.That(File.Exists(path + ".meta"), Is.False);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(path), Is.Null);
            Assert.That(File.ReadAllText(neighbor), Is.EqualTo("retain"));
            Assert.That(File.ReadAllBytes(neighbor + ".meta"), Is.EqualTo(neighborMeta));
            File.WriteAllBytes(path, source);
            File.WriteAllBytes(path + ".meta", meta);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssertUnchanged();
        }
        [Test] public void FolderHashRefusesButUnhashedFolderPreviewRemainsSupported()
        {
            var result = Trash(Hash(source), true, false, root);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("NOT_REGULAR_FILE"));
            AssertUnchanged();
            var preview = AssetAuthoringCommands.Trash(root, dryRun: true);
            Assert.That(preview.Ok, Is.True, preview.Error?.Code);
            Assert.That(preview.Result.Applied, Is.False);
            AssertUnchanged();
        }
        private CommandResult<AssetTrashResult> Trash(string hash, bool confirm, bool dryRun, string target = null)
        {
            var method = typeof(AssetAuthoringCommands).GetMethod(nameof(AssetAuthoringCommands.Trash));
            var parameters = method.GetParameters();
            Assert.That(parameters.Any(parameter => parameter.Name == "expectedSha256"), Is.True, "asset.trash must accept an exact regular-file hash.");
            return (CommandResult<AssetTrashResult>)method.Invoke(null, parameters.Select(parameter => parameter.Name == "asset" ? (object)(target ?? path) : parameter.Name == "expectedSha256" ? hash : parameter.Name == "confirm" ? confirm : parameter.Name == "dryRun" ? dryRun : parameter.DefaultValue).ToArray());
        }
        private void AssertUnchanged()
        {
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(source));
            Assert.That(File.ReadAllBytes(path + ".meta"), Is.EqualTo(meta));
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(path), Is.Not.Null);
        }
        private static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
