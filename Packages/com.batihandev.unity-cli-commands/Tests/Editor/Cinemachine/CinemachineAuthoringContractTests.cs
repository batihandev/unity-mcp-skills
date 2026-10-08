using System;
using System.Linq;
using BatihanDev.UnityCliCommands.CinemachineAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BatihanDev.UnityCliCommands.Tests.Cinemachine
{
    [RequireComponent(typeof(CinemachineFollow))]
    public sealed class CinemachineFollowDependent : MonoBehaviour { }

    public sealed class CinemachineAuthoringContractTests
    {
        private GameObject cameraObject;
        private GameObject targetObject;
        private CinemachineCamera camera;
        private CinemachineTargetGroup group;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Cinemachine contract camera");
            camera = cameraObject.AddComponent<CinemachineCamera>();
            group = cameraObject.AddComponent<CinemachineTargetGroup>();
            targetObject = new GameObject("Cinemachine contract target");
        }

        [TearDown]
        public void TearDown()
        {
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
            if (targetObject != null) UnityEngine.Object.DestroyImmediate(targetObject);
        }

        private static string Id(UnityEngine.Object value) => EntityId.ToULong(value.GetEntityId()).ToString();

        [Test]
        public void StageSetRejectsWrongStageBeforeRemovingExistingComponent()
        {
            var follow = cameraObject.AddComponent<CinemachineFollow>();
            var result = CinemachineAuthoringCommands.SetStage(Id(camera), "Body", typeof(CinemachinePanTilt).FullName, true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual("STAGE_TYPE_MISMATCH", result.Error.Code);
            Assert.AreSame(follow, cameraObject.GetComponent<CinemachineFollow>());
        }

        [Test]
        public void StageSetReplacesComponentAndUndoRestoresOriginal()
        {
            var follow = cameraObject.AddComponent<CinemachineFollow>();
            var result = CinemachineAuthoringCommands.SetStage(Id(camera), "Body", typeof(CinemachineOrbitalFollow).FullName, true);
            Assert.IsTrue(result.Ok, result.Error?.Message);
            Assert.IsNull(cameraObject.GetComponent<CinemachineFollow>());
            Assert.IsNotNull(cameraObject.GetComponent<CinemachineOrbitalFollow>());
            Undo.PerformUndo();
            Assert.IsNotNull(cameraObject.GetComponent<CinemachineFollow>());
            Assert.IsNull(cameraObject.GetComponent<CinemachineOrbitalFollow>());
        }

        [Test]
        public void StageSetDryRunLeavesComponentUntouched()
        {
            var follow = cameraObject.AddComponent<CinemachineFollow>();
            var result = CinemachineAuthoringCommands.SetStage(Id(camera), "Body", "None", dryRun: true);
            Assert.IsTrue(result.Ok, result.Error?.Message);
            Assert.IsFalse(result.Result.Applied);
            Assert.AreSame(follow, cameraObject.GetComponent<CinemachineFollow>());
        }

        [Test]
        public void StageSetRefusesRequiredComponentRemoval()
        {
            var follow = cameraObject.AddComponent<CinemachineFollow>();
            cameraObject.AddComponent<CinemachineFollowDependent>();
            var result = CinemachineAuthoringCommands.SetStage(Id(camera), "Body", "None", true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual("REQUIRED_COMPONENT", result.Error.Code);
            Assert.AreSame(follow, cameraObject.GetComponent<CinemachineFollow>());
        }

        [Test]
        public void StageSetRefusesStaleCameraBeforeMutation()
        {
            var follow = cameraObject.AddComponent<CinemachineFollow>();
            var result = CinemachineAuthoringCommands.SetStage("0", "Body", "None", true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual("CAMERA_SCENE_REQUIRED", result.Error.Code);
            Assert.AreSame(follow, cameraObject.GetComponent<CinemachineFollow>());
        }

        [Test]
        public void StageSetRefusesAmbiguousBodyBeforeMutation()
        {
            var follow = cameraObject.AddComponent<CinemachineFollow>();
            var orbital = cameraObject.AddComponent<CinemachineOrbitalFollow>();
            var result = CinemachineAuthoringCommands.SetStage(Id(camera), "Body", "None", true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual("STAGE_AMBIGUOUS", result.Error.Code);
            Assert.AreSame(follow, cameraObject.GetComponent<CinemachineFollow>());
            Assert.AreSame(orbital, cameraObject.GetComponent<CinemachineOrbitalFollow>());
        }

        [Test]
        public void GroupUpsertMovesMemberToEndAndRejectsDuplicates()
        {
            group.AddMember(targetObject.transform, 2, 3);
            var other = new GameObject("other");
            try
            {
                group.AddMember(other.transform, 1, 1);
                var result = CinemachineAuthoringCommands.GroupMember(Id(group), Id(targetObject), "upsert", 4, 5, true);
                Assert.IsTrue(result.Ok, result.Error?.Message);
                Assert.AreSame(targetObject.transform, group.Targets.Last().Object);
                Assert.AreEqual(4, group.Targets.Last().Weight);
                Assert.AreEqual(5, group.Targets.Last().Radius);
                group.AddMember(targetObject.transform, 1, 1);
                var count = group.Targets.Count;
                var refused = CinemachineAuthoringCommands.GroupMember(Id(group), Id(targetObject), "remove", confirm: true);
                Assert.IsFalse(refused.Ok);
                Assert.AreEqual("MEMBER_AMBIGUOUS", refused.Error.Code);
                Assert.AreEqual(count, group.Targets.Count);
            }
            finally { UnityEngine.Object.DestroyImmediate(other); }
        }

        [Test]
        public void GroupMemberRejectsInvalidWeightBeforeMutation()
        {
            var result = CinemachineAuthoringCommands.GroupMember(Id(group), Id(targetObject), "upsert", float.NaN, 1, true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual("MEMBER_VALUE_INVALID", result.Error.Code);
            Assert.IsEmpty(group.Targets);
        }

        [Test]
        public void GroupMemberUndoRestoresPreviousOrder()
        {
            var first = new GameObject("first");
            try
            {
                group.AddMember(targetObject.transform, 2, 3);
                group.AddMember(first.transform, 1, 1);
                var result = CinemachineAuthoringCommands.GroupMember(Id(group), Id(targetObject), "upsert", 4, 5, true);
                Assert.IsTrue(result.Ok, result.Error?.Message);
                Undo.PerformUndo();
                Assert.AreSame(targetObject.transform, group.Targets[0].Object);
                Assert.AreEqual(2, group.Targets[0].Weight);
                Assert.AreSame(first.transform, group.Targets[1].Object);
            }
            finally { UnityEngine.Object.DestroyImmediate(first); }
        }

        [Test]
        public void GroupMemberRefusesCrossSceneTarget()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/CinemachineCrossSceneTest.unity");
            var other = default(UnityEngine.SceneManagement.Scene);
            try
            {
                var primary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Assert.IsTrue(EditorSceneManager.SaveScene(primary, path));
                cameraObject = new GameObject("Cinemachine contract group");
                group = cameraObject.AddComponent<CinemachineTargetGroup>();
                other = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                targetObject = new GameObject("Cinemachine contract target");
                SceneManager.MoveGameObjectToScene(targetObject, other);
                var result = CinemachineAuthoringCommands.GroupMember(Id(group), Id(targetObject), "upsert", confirm: true);
                Assert.IsFalse(result.Ok);
                Assert.AreEqual("TARGET_SCENE_REQUIRED", result.Error.Code);
                Assert.IsEmpty(group.Targets);
            }
            finally
            {
                if (other.IsValid() && other.isLoaded) EditorSceneManager.CloseScene(other, true);
                if (setup.Length > 0 && setup.All(scene => !string.IsNullOrEmpty(scene.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void ImpulseNullEventIsFailureAndPreviewDoesNotGenerate()
        {
            var source = cameraObject.AddComponent<CinemachineImpulseSource>();
            source.ImpulseDefinition.DissipationDistance = 0;
            var preview = CinemachineAuthoringCommands.GenerateImpulse(Id(source), 0, -1, 0, dryRun: true);
            Assert.IsTrue(preview.Ok, preview.Error?.Message);
            Assert.IsFalse(preview.Result.Generated);
            var result = CinemachineAuthoringCommands.GenerateImpulse(Id(source), 0, -1, 0, confirm: true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual("IMPULSE_EVENT_NOT_CREATED", result.Error.Code);
        }
    }
}
