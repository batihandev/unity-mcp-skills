using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Assets;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Tests.Assets
{
    public sealed class AtlasCommandContractTests
    {
        private const string Root = "Assets/Task10AtlasCommandContracts";
        private const string AtlasPath = Root + "/Owned.spriteatlasv2";
        private bool ownsRoot;

        [SetUp]
        public void SetUp()
        {
            Assert.That(AssetDatabase.IsValidFolder(Root), Is.False, "Fixture path must be vacant.");
            Assert.That(System.IO.Directory.Exists(Root) || System.IO.File.Exists(Root) || System.IO.File.Exists(Root + ".meta"), Is.False);
            AssetDatabase.CreateFolder("Assets", "Task10AtlasCommandContracts");
            ownsRoot = true;
            var created = AtlasCommands.Create(AtlasPath, confirm: true);
            Assert.That(created.Ok, Is.True, created.Error?.Message);
            Assert.That(created.Result.Applied, Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (ownsRoot) AssetDatabase.DeleteAsset(Root);
            ownsRoot = false;
            AssetDatabase.Refresh();
        }

        [Test]
        public void CreationPreflightAndCollisionPreserveExistingAssets()
        {
            var path = Root + "/Another.spriteatlasv2";
            var dry = AtlasCommands.Create(path, dryRun: true);
            Assert.That(dry.Ok, Is.True, dry.Error?.Message);
            Assert.That(System.IO.File.Exists(path), Is.False);
            Assert.That(AtlasCommands.Create(path).Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            Assert.That(System.IO.File.Exists(path), Is.False);
            var before = System.IO.File.ReadAllBytes(AtlasPath);
            Assert.That(AtlasCommands.Create(AtlasPath, confirm: true).Error.Code, Is.EqualTo("ASSET_EXISTS"));
            Assert.That(System.IO.File.ReadAllBytes(AtlasPath), Is.EqualTo(before));
        }

        [Test]
        public void DryRunCapturesCompleteMembershipWithoutPersistingChanges()
        {
            var before = AssetDatabase.LoadAssetAtPath<UnityObject>(AtlasPath);

            var result = AtlasCommands.SetPackables(AtlasPath, packables: Array.Empty<string>(), dryRun: true);

            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.Applied, Is.False);
            Assert.That(result.Result.DryRun, Is.True);
            Assert.That(result.Result.Undoable, Is.False);
            Assert.That(result.Result.Before, Is.Empty);
            Assert.That(result.Result.After, Is.Empty);
            Assert.That(AssetDatabase.LoadAssetAtPath<UnityObject>(AtlasPath), Is.SameAs(before));
        }

        [Test]
        public void MembershipRequiresOneCompleteInputAndExplicitConfirmationBeforeWriting()
        {
            var ambiguous = AtlasCommands.SetPackables(AtlasPath);
            Assert.That(ambiguous.Ok, Is.False);
            Assert.That(ambiguous.Error.Code, Is.EqualTo("MEMBERSHIP_INPUT_REQUIRED"));

            var unconfirmed = AtlasCommands.SetPackables(AtlasPath, packables: Array.Empty<string>());
            Assert.That(unconfirmed.Ok, Is.False);
            Assert.That(unconfirmed.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            Assert.That(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath), Is.Not.Null);
        }
    }
}
