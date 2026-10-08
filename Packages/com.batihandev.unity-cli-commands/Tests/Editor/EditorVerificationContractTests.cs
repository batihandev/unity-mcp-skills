using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Console;
using BatihanDev.UnityCliCommands.Editor;
using BatihanDev.UnityCliCommands.Project;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class EditorVerificationContractTests
    {
        [Test]
        public void ConsoleSettingsReadAndMutationGatesExposeEffectiveSnapshots()
        {
            var read = ConsoleCommands.Settings();
            Assert.That(read.Ok, Is.True, read.Error == null ? null :
                read.Error.Code + ":" + read.Error.Details?["exceptionType"]);
            Assert.That(read.Result.After.Collapse, Is.EqualTo(read.Result.Before.Collapse));
            Assert.That(read.Result.Applied, Is.False);

            var refused = ConsoleCommands.Settings(collapse: !read.Result.Before.Collapse);
            Assert.That(refused.Ok, Is.False);
            Assert.That(refused.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));

            var dryRun = ConsoleCommands.Settings(collapse: !read.Result.Before.Collapse, dryRun: true);
            Assert.That(dryRun.Ok, Is.True);
            Assert.That(dryRun.Result.Proposed.Collapse, Is.Not.EqualTo(read.Result.Before.Collapse));
            Assert.That(dryRun.Result.After.Collapse, Is.EqualTo(read.Result.Before.Collapse));
            Assert.That(dryRun.Result.DryRun, Is.True);
            Assert.That(dryRun.Result.Applied, Is.False);
            Assert.That(dryRun.Result.Undoable, Is.False);
        }

        [Test]
        public void ConsoleEntriesValidatesLimitAndRetainsOrderedLiveMessages()
        {
            Assert.That(ConsoleCommands.Entries(-1).Error.Code, Is.EqualTo("LIMIT_OUT_OF_RANGE"));
            Assert.That(ConsoleCommands.Entries(1001).Error.Code, Is.EqualTo("LIMIT_OUT_OF_RANGE"));
            var empty = ConsoleCommands.Entries(0);
            Assert.That(empty.Ok, Is.True, empty.Error == null ? null :
                empty.Error.Code + ":" + empty.Error.Details?["exceptionType"]);
            Assert.That(empty.Result.Source, Is.EqualTo("editor-console"));
            Assert.That(empty.Result.Collapse, Is.EqualTo(ConsoleCommands.Settings().Result.After.Collapse));
            Assert.That(empty.Result.Count, Is.Zero);
            Assert.That(empty.Result.Entries, Is.Empty);

            var token = Guid.NewGuid().ToString("N");
            LogAssert.Expect(LogType.Warning, token + " warning");
            LogAssert.Expect(LogType.Error, token + " error");
            UnityEngine.Debug.Log(token + " log");
            UnityEngine.Debug.LogWarning(token + " warning");
            UnityEngine.Debug.LogError(token + " error");
            var entries = ConsoleCommands.Entries();
            Assert.That(entries.Ok, Is.True);
            Assert.That(entries.Result.Collapse, Is.EqualTo(ConsoleCommands.Settings().Result.After.Collapse));
            Assert.That(entries.Result.Entries.Single(item => item.Message.StartsWith(token + " log")).Severity,
                Is.EqualTo("Log"));
            Assert.That(entries.Result.Entries.Single(item => item.Message.StartsWith(token + " warning")).Severity,
                Is.EqualTo("Warning"));
            Assert.That(entries.Result.Entries.Single(item => item.Message.StartsWith(token + " error")).Severity,
                Is.EqualTo("Error"));
        }

        [Test]
        public void ProjectDefinesReadsSelectedGroupAndMutationRequiresExplicitGroupAndConfirmation()
        {
            var read = ProjectCommands.Defines();
            Assert.That(read.Ok, Is.True);
            Assert.That(read.Result.Group, Is.EqualTo(EditorUserBuildSettings.selectedBuildTargetGroup.ToString()));
            Assert.That(read.Result.Applied, Is.False);

            var missingGroup = ProjectCommands.Defines(defines: "TASK10_TEST");
            Assert.That(missingGroup.Error.Code, Is.EqualTo("BUILD_TARGET_GROUP_REQUIRED"));

            var numericGroup = ProjectCommands.Defines(group: "1");
            Assert.That(numericGroup.Error.Code, Is.EqualTo("BUILD_TARGET_GROUP_INVALID"));

            var refused = ProjectCommands.Defines(
                group: read.Result.Group, defines: read.Result.Before);
            Assert.That(refused.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));

            var dryRun = ProjectCommands.Defines(
                group: read.Result.Group, defines: read.Result.Before, dryRun: true);
            Assert.That(dryRun.Ok, Is.True);
            Assert.That(dryRun.Result.DryRun, Is.True);
            Assert.That(dryRun.Result.Applied, Is.False);
            Assert.That(dryRun.Result.After, Is.EqualTo(read.Result.Before));
            Assert.That(dryRun.Result.Undoable, Is.False);
        }

        [Test]
        public void EditorContextDetailsUsesExactStringIdentitiesForSceneAndAssetSelections()
        {
            var prior = Selection.objects;
            var gameObject = new GameObject("Task10ContextTarget");
            try
            {
                Selection.objects = new UnityObject[] { gameObject };
                var sceneResult = EditorCommands.ContextDetails();
                Assert.That(sceneResult.Ok, Is.True);
                Assert.That(sceneResult.Result.SelectedGameObjects.Single().InstanceId,
                    Is.EqualTo(EntityId.ToULong(gameObject.GetEntityId()).ToString()));
                Assert.That(sceneResult.Result.SelectedGameObjects.Single().Name, Is.EqualTo(gameObject.name));

                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath("Assets");
                var assetResult = EditorCommands.ContextDetails();
                Assert.That(assetResult.Ok, Is.True);
                Assert.That(assetResult.Result.SelectedAssets.Any(item => item.Path == "Assets" && item.IsFolder),
                    Is.True);
            }
            finally
            {
                Selection.objects = prior;
                UnityObject.DestroyImmediate(gameObject);
            }
        }
    }
}
