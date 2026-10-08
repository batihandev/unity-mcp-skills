using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BatihanDev.UnityCliCommands.Scene;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class SceneSaveContractTests
    {
        private const string Root = "Assets/Task10SceneSaveContractTests";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets", "Task10SceneSaveContractTests");
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(Root);
            AssetDatabase.Refresh();
        }

        [Test]
        public void SaveAsRejectsDisplayNameInvalidDestinationAndExtensionWithoutChangingSource()
        {
            var sourcePath = Root + "/RefusalSource.unity";
            var source = CreateSavedScene(sourcePath, "PersistedRoot");
            var sourceBytes = ReadProjectFile(sourcePath);
            new GameObject("UnsavedRoot");
            EditorSceneManager.MarkSceneDirty(source);

            var byDisplayName = SceneAuthoringCommands.SaveAs(source.name, Root + "/ByName.unity");
            Assert.That(byDisplayName.Ok, Is.False);
            Assert.That(byDisplayName.Error.Code, Is.EqualTo("SCENE_NOT_FOUND"));

            var outside = SceneAuthoringCommands.SaveAs(SceneHandle(source), "../Outside.unity");
            Assert.That(outside.Ok, Is.False);
            Assert.That(outside.Error.Code, Is.EqualTo("PATH_TRAVERSAL"));

            var wrongExtension = SceneAuthoringCommands.SaveAs(SceneHandle(source), Root + "/Wrong.txt");
            Assert.That(wrongExtension.Ok, Is.False);
            Assert.That(wrongExtension.Error.Code, Is.EqualTo("SCENE_EXTENSION_REQUIRED"));

            Assert.That(source.path, Is.EqualTo(sourcePath));
            Assert.That(source.isDirty, Is.True);
            Assert.That(ReadProjectFile(sourcePath), Is.EqualTo(sourceBytes));
            Assert.That(File.Exists(ProjectFullPath(Root + "/ByName.unity")), Is.False);
        }

        [Test]
        public void SaveAsDryRunReportsProspectiveDestinationWithoutMutation()
        {
            var sourcePath = Root + "/DryRunSource.unity";
            var destination = Root + "/DryRunDestination.unity";
            var source = CreateSavedScene(sourcePath, "PersistedRoot");
            var sourceBytes = ReadProjectFile(sourcePath);
            new GameObject("UnsavedRoot");
            EditorSceneManager.MarkSceneDirty(source);

            var result = SceneAuthoringCommands.SaveAs(
                SceneHandle(source), destination, dryRun: true);

            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.Path, Is.EqualTo(destination));
            Assert.That(result.Result.SourcePath, Is.EqualTo(sourcePath));
            Assert.That(result.Result.SceneHandle, Is.EqualTo(SceneHandle(source)));
            Assert.That(result.Result.Exists, Is.False);
            Assert.That(result.Result.Saved, Is.False);
            Assert.That(result.Result.Applied, Is.False);
            Assert.That(result.Result.DryRun, Is.True);
            Assert.That(result.Result.Undoable, Is.False);
            Assert.That(source.path, Is.EqualTo(sourcePath));
            Assert.That(source.isDirty, Is.True);
            Assert.That(ReadProjectFile(sourcePath), Is.EqualTo(sourceBytes));
            Assert.That(File.Exists(ProjectFullPath(destination)), Is.False);
        }

        [Test]
        public void SaveAsCreatesNewSceneWhilePreservingOldFileAndReloadsCleanly()
        {
            var sourcePath = Root + "/Original.unity";
            var destination = Root + "/SavedAs.unity";
            var source = CreateSavedScene(sourcePath, "OriginalRoot");
            var originalBytes = ReadProjectFile(sourcePath);
            new GameObject("SavedAsOnlyRoot");
            EditorSceneManager.MarkSceneDirty(source);

            var result = SceneAuthoringCommands.SaveAs(SceneHandle(source), destination);

            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.Saved, Is.True);
            Assert.That(result.Result.Applied, Is.True);
            Assert.That(result.Result.Path, Is.EqualTo(destination));
            Assert.That(result.Result.SourcePath, Is.EqualTo(sourcePath));
            Assert.That(result.Result.SceneHandle, Is.EqualTo(SceneHandle(source)));
            Assert.That(result.Result.Guid, Is.Not.Empty);
            Assert.That(result.Result.DryRun, Is.False);
            Assert.That(result.Result.Undoable, Is.False);
            Assert.That(source.path, Is.EqualTo(destination));
            Assert.That(source.isDirty, Is.False);
            Assert.That(ReadProjectFile(sourcePath), Is.EqualTo(originalBytes));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(destination), Is.Not.Null);

            var reloaded = EditorSceneManager.OpenScene(destination, OpenSceneMode.Single);
            Assert.That(reloaded.isDirty, Is.False);
            Assert.That(reloaded.GetRootGameObjects().Select(item => item.name),
                Does.Contain("SavedAsOnlyRoot"));
        }

        [Test]
        public void SaveAsOverwriteRequiresConfirmationAndCurrentHashAndPreservesDestinationGuid()
        {
            var sourcePath = Root + "/OverwriteSource.unity";
            var destination = Root + "/OverwriteDestination.unity";
            var source = CreateSavedScene(sourcePath, "SourceRoot");
            var destinationScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(destinationScene);
            new GameObject("DestinationRoot");
            Assert.That(EditorSceneManager.SaveScene(destinationScene, destination), Is.True);
            Assert.That(EditorSceneManager.CloseScene(destinationScene, true), Is.True);
            SceneManager.SetActiveScene(source);
            new GameObject("ReplacementRoot");
            EditorSceneManager.MarkSceneDirty(source);
            var destinationHash = Sha256(destination);
            var destinationGuid = AssetDatabase.AssetPathToGUID(destination);
            var destinationBytes = ReadProjectFile(destination);

            var noConfirmation = SceneAuthoringCommands.SaveAs(SceneHandle(source), destination);
            Assert.That(noConfirmation.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            AssertUnchanged(source, sourcePath, destination, destinationBytes);

            var missingHash = SceneAuthoringCommands.SaveAs(
                SceneHandle(source), destination, confirm: true);
            Assert.That(missingHash.Error.Code, Is.EqualTo("EXPECTED_DESTINATION_SHA256_REQUIRED"));
            AssertUnchanged(source, sourcePath, destination, destinationBytes);

            var staleHash = SceneAuthoringCommands.SaveAs(
                SceneHandle(source), destination, confirm: true,
                expectedDestinationSha256: new string('0', 64));
            Assert.That(staleHash.Error.Code, Is.EqualTo("DESTINATION_SHA256_MISMATCH"));
            AssertUnchanged(source, sourcePath, destination, destinationBytes);

            var replaced = SceneAuthoringCommands.SaveAs(
                SceneHandle(source), destination, confirm: true,
                expectedDestinationSha256: destinationHash);
            Assert.That(replaced.Ok, Is.True, replaced.Error?.Code);
            Assert.That(replaced.Result.Guid, Is.EqualTo(destinationGuid));
            Assert.That(AssetDatabase.AssetPathToGUID(destination), Is.EqualTo(destinationGuid));
            Assert.That(source.path, Is.EqualTo(destination));
            Assert.That(source.isDirty, Is.False);
            Assert.That(Sha256(destination), Is.Not.EqualTo(destinationHash));
        }

        [Test]
        public void SaveAsRefusesDestinationOwnedByAnotherLoadedSceneWithoutChangingEitherScene()
        {
            var sourcePath = Root + "/LoadedOwnerSource.unity";
            var destination = Root + "/LoadedOwnerDestination.unity";
            var source = CreateSavedScene(sourcePath, "SourceRoot");
            new GameObject("UnsavedSourceRoot");
            EditorSceneManager.MarkSceneDirty(source);
            var destinationScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(destinationScene);
            new GameObject("DestinationRoot");
            Assert.That(EditorSceneManager.SaveScene(destinationScene, destination), Is.True);
            var destinationBytes = ReadProjectFile(destination);
            var destinationHash = Sha256(destination);
            SceneManager.SetActiveScene(source);

            var refused = SceneAuthoringCommands.SaveAs(
                sourcePath, destination, confirm: true,
                expectedDestinationSha256: destinationHash);

            Assert.That(refused.Ok, Is.False);
            Assert.That(refused.Error.Code, Is.EqualTo("DESTINATION_SCENE_LOADED"));
            Assert.That(source.path, Is.EqualTo(sourcePath));
            Assert.That(source.isDirty, Is.True);
            Assert.That(destinationScene.path, Is.EqualTo(destination));
            Assert.That(destinationScene.isLoaded, Is.True);
            Assert.That(ReadProjectFile(destination), Is.EqualTo(destinationBytes));
        }

        [Test]
        public void SaveAsRefusesCaseAliasedSourcePathWithoutSavingOnWindows()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Ignore("Windows filesystem path identity is exercised on the supported Editor host.");
            var sourcePath = Root + "/CaseAliasSource.unity";
            var aliasedPath = "Assets/task10SceneSaveContractTests/CaseAliasSource.unity";
            var source = CreateSavedScene(sourcePath, "PersistedRoot");
            var sourceBytes = ReadProjectFile(sourcePath);
            new GameObject("UnsavedRoot");
            EditorSceneManager.MarkSceneDirty(source);

            var result = SceneAuthoringCommands.SaveAs(
                SceneHandle(source), aliasedPath, confirm: true,
                expectedDestinationSha256: Sha256(sourcePath));

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("DESTINATION_MATCHES_SOURCE"));
            Assert.That(source.path, Is.EqualTo(sourcePath));
            Assert.That(source.isDirty, Is.True);
            Assert.That(ReadProjectFile(sourcePath), Is.EqualTo(sourceBytes));
        }

        [Test]
        public void SaveAsRefusesCaseAliasedPathOwnedByAnotherLoadedSceneOnWindows()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Ignore("Windows filesystem path identity is exercised on the supported Editor host.");
            var sourcePath = Root + "/CaseAliasCaller.unity";
            var destination = Root + "/CaseAliasOwned.unity";
            var aliasedDestination = Root + "/casealiasowned.unity";
            var source = CreateSavedScene(sourcePath, "SourceRoot");
            var destinationScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(destinationScene);
            new GameObject("DestinationRoot");
            Assert.That(EditorSceneManager.SaveScene(destinationScene, destination), Is.True);
            var sourceBytes = ReadProjectFile(sourcePath);
            var destinationBytes = ReadProjectFile(destination);
            SceneManager.SetActiveScene(source);
            new GameObject("UnsavedSourceRoot");
            EditorSceneManager.MarkSceneDirty(source);

            var result = SceneAuthoringCommands.SaveAs(
                SceneHandle(source), aliasedDestination, confirm: true,
                expectedDestinationSha256: Sha256(destination));

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("DESTINATION_SCENE_LOADED"));
            Assert.That(source.path, Is.EqualTo(sourcePath));
            Assert.That(source.isDirty, Is.True);
            Assert.That(destinationScene.path, Is.EqualTo(destination));
            Assert.That(destinationScene.isLoaded, Is.True);
            Assert.That(ReadProjectFile(sourcePath), Is.EqualTo(sourceBytes));
            Assert.That(ReadProjectFile(destination), Is.EqualTo(destinationBytes));
        }

        [Test]
        public void SceneUnloadSaveRefusesUnsavedDirtySceneBeforeInvokingSave()
        {
            var primaryPath = Root + "/UnloadPrimary.unity";
            var primary = CreateSavedScene(primaryPath, "PrimaryRoot");
            var unsaved = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(unsaved);
            new GameObject("UnsavedRoot");
            EditorSceneManager.MarkSceneDirty(unsaved);

            var result = SceneAuthoringCommands.Unload(SceneHandle(unsaved), dirtyAction: "save");

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("UNSAVED_SCENE"));
            Assert.That(unsaved.isLoaded, Is.True);
            Assert.That(unsaved.isDirty, Is.True);
            Assert.That(unsaved.path, Is.Empty);
            Assert.That(primary.isLoaded, Is.True);
        }

        [Test]
        public void SceneUnloadReportsTheExecutedDirtyActionForSaveAndDiscard()
        {
            var primaryPath = Root + "/DirtyActionPrimary.unity";
            var savePath = Root + "/DirtyActionSave.unity";
            var discardPath = Root + "/DirtyActionDiscard.unity";
            CreateSavedScene(primaryPath, "PrimaryRoot");

            var saveScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Assert.That(EditorSceneManager.SaveScene(saveScene, savePath), Is.True);
            SceneManager.SetActiveScene(saveScene);
            new GameObject("SavedRoot");
            EditorSceneManager.MarkSceneDirty(saveScene);
            var saved = SceneAuthoringCommands.Unload(SceneHandle(saveScene), dirtyAction: "save");

            var discardScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Assert.That(EditorSceneManager.SaveScene(discardScene, discardPath), Is.True);
            SceneManager.SetActiveScene(discardScene);
            new GameObject("DiscardedRoot");
            EditorSceneManager.MarkSceneDirty(discardScene);
            var discarded = SceneAuthoringCommands.Unload(SceneHandle(discardScene), dirtyAction: "discard");

            Assert.That(saved.Ok, Is.True, saved.Error?.Code);
            Assert.That(saved.Result.DirtyAction, Is.EqualTo("save"));
            Assert.That(discarded.Ok, Is.True, discarded.Error?.Code);
            Assert.That(discarded.Result.DirtyAction, Is.EqualTo("discard"));
        }

        private static UnityScene CreateSavedScene(string path, string rootName)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(rootName);
            Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
            return scene;
        }

        private static void AssertUnchanged(
            UnityScene source, string sourcePath, string destination, byte[] destinationBytes)
        {
            Assert.That(source.path, Is.EqualTo(sourcePath));
            Assert.That(source.isDirty, Is.True);
            Assert.That(ReadProjectFile(destination), Is.EqualTo(destinationBytes));
        }

        private static string SceneHandle(UnityScene scene) =>
            scene.handle.GetRawData().ToString(CultureInfo.InvariantCulture);

        private static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(ReadProjectFile(path))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static byte[] ReadProjectFile(string path) => File.ReadAllBytes(ProjectFullPath(path));

        private static string ProjectFullPath(string path) => Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            path.Replace('/', Path.DirectorySeparatorChar));
    }
}
