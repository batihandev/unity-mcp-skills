using System;
using System.Linq;
using BatihanDev.UnityCliCommands.TimelineAuthoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace BatihanDev.UnityCliCommands.Tests.Timeline
{
    public sealed class TimelineAuthoringContractTests
    {
        private const string Path = "Assets/__TimelineAuthoringContractTests.playable";
        private TimelineAsset asset;
        private GameObject directorObject;
        private GameObject targetObject;
        private PlayableDirector director;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(Path), "The test target must be absent.");
            asset = UnityEngine.ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(asset, Path);
            directorObject = new GameObject("Timeline contract director");
            director = directorObject.AddComponent<PlayableDirector>();
            director.playableAsset = asset;
            targetObject = new GameObject("Timeline contract target");
        }

        [TearDown]
        public void TearDown()
        {
            if (directorObject != null) UnityEngine.Object.DestroyImmediate(directorObject);
            if (targetObject != null) UnityEngine.Object.DestroyImmediate(targetObject);
            AssetDatabase.DeleteAsset(Path);
        }

        [Test]
        public void DefaultClipPreflightRejectsSignalWithoutMutation()
        {
            var signal = asset.CreateTrack<SignalTrack>("Signal Track");
            var before = signal.GetClips().Count();
            var result = TimelineAuthoringCommands.CreateDefaultClip(Path, signal.name, confirm: true);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual("DEFAULT_CLIP_UNSUPPORTED", result.Error.Code);
            Assert.AreEqual(before, signal.GetClips().Count());
        }

        [Test]
        public void DefaultClipCreatesActivationAndRemovalDeletesIt()
        {
            var track = asset.CreateTrack<ActivationTrack>("Activation Track");
            var preview = TimelineAuthoringCommands.CreateDefaultClip(Path, track.name, dryRun: true);
            Assert.IsTrue(preview.Ok);
            Assert.AreEqual(0, track.GetClips().Count());
            var created = TimelineAuthoringCommands.CreateDefaultClip(Path, track.name, 0.5, 2, confirm: true);
            Assert.IsTrue(created.Ok, created.Error?.Message);
            Assert.AreEqual(1, track.GetClips().Count());
            Assert.AreEqual(0.5, track.GetClips().Single().start);
            var removed = TimelineAuthoringCommands.RemoveTrack(Path, track.name, confirm: true);
            Assert.IsTrue(removed.Ok, removed.Error?.Message);
            Assert.AreEqual(1, removed.Result.ClipCount, "Removal reports the deleted track's clip count.");
            Assert.IsEmpty(asset.GetOutputTracks().Where(t => t.name == "Activation Track"));
        }

        [Test]
        public void BindingPreflightLeavesObjectUnchangedAndAutoAddsAnimator()
        {
            var track = asset.CreateTrack<AnimationTrack>("Animation Track");
            var directorId = UnityEngine.EntityId.ToULong(director.GetEntityId()).ToString();
            var targetId = UnityEngine.EntityId.ToULong(targetObject.GetEntityId()).ToString();
            var failed = TimelineAuthoringCommands.SetBinding(directorId, track.name, targetId);
            Assert.IsFalse(failed.Ok);
            Assert.AreEqual("TARGET_COMPONENT_MISSING", failed.Error.Code);
            Assert.IsNull(targetObject.GetComponent<Animator>());
            var applied = TimelineAuthoringCommands.SetBinding(directorId, track.name, targetId, true);
            Assert.IsTrue(applied.Ok, applied.Error?.Message);
            Assert.AreSame(targetObject.GetComponent<Animator>(), director.GetGenericBinding(track));
        }

        [Test]
        public void DurationRejectsInvalidWrapBeforeMutationAndReportsActualDuration()
        {
            var directorId = UnityEngine.EntityId.ToULong(director.GetEntityId()).ToString();
            var before = asset.fixedDuration;
            var failed = TimelineAuthoringCommands.SetDuration(directorId, 2, "1", confirm: true);
            Assert.IsFalse(failed.Ok);
            Assert.AreEqual("WRAP_MODE_INVALID", failed.Error.Code);
            Assert.AreEqual(before, asset.fixedDuration);
            var applied = TimelineAuthoringCommands.SetDuration(directorId, 2, "loop", confirm: true);
            Assert.IsTrue(applied.Ok, applied.Error?.Message);
            Assert.AreEqual(TimelineAsset.DurationMode.FixedLength, asset.durationMode);
            Assert.AreEqual(DirectorWrapMode.Loop, director.extrapolationMode);
            Assert.AreEqual(asset.fixedDuration, applied.Result.FixedDuration);
        }
    }
}
