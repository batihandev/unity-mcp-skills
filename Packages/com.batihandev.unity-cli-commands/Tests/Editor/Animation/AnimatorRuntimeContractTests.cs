using BatihanDev.UnityCliCommands.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests.Animation
{
    public sealed class AnimatorRuntimeContractTests
    {
        private GameObject _gameObject;
        private Animator _animator;
        private AnimatorController _controller;
        private AnimatorStateMachine _rootMachine;
        private AnimatorStateMachine _firstMachine;
        private AnimatorStateMachine _secondMachine;

        [SetUp]
        public void SetUp()
        {
            if (EditorApplication.isPlaying)
                Assert.Ignore("These boundary tests exercise the stopped-editor refusal.");
            _gameObject = new GameObject("AnimatorRuntimeContractTarget");
            _animator = _gameObject.AddComponent<Animator>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null) Object.DestroyImmediate(_gameObject);
            if (_controller != null) Object.DestroyImmediate(_controller);
            if (_rootMachine != null) Object.DestroyImmediate(_rootMachine);
            if (_firstMachine != null) Object.DestroyImmediate(_firstMachine);
            if (_secondMachine != null) Object.DestroyImmediate(_secondMachine);
        }

        [Test]
        public void SetParameterRefusesOutsidePlayModeBeforeControllerMutation()
        {
            var result = AnimatorRuntimeCommands.SetParameter(
                BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(_animator),
                "Speed", "float", 3.5f);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("PLAY_MODE_REQUIRED"));
            Assert.That(_animator.runtimeAnimatorController, Is.Null);
        }

        [Test]
        public void PlayRefusesOutsidePlayModeBeforeControllerMutation()
        {
            var result = AnimatorRuntimeCommands.Play(
                BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(_animator),
                "Walk", 0, 0f);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("PLAY_MODE_REQUIRED"));
            Assert.That(_animator.runtimeAnimatorController, Is.Null);
        }

        [Test]
        public void QualifiedRootAndNestedStatePathsResolveExactly()
        {
            BuildController();
            _rootMachine.AddState("Idle");
            _firstMachine.AddState("Walk");

            Assert.That(AnimatorRuntimeCommands.TryResolveStatePath(_controller, 0, "Base Layer.Idle", out var rootPath, out var rootAmbiguous), Is.True);
            Assert.That(rootPath, Is.EqualTo("Base Layer.Idle"));
            Assert.That(rootAmbiguous, Is.False);
            Assert.That(AnimatorRuntimeCommands.TryResolveStatePath(_controller, 0, "Base Layer.First.Walk", out var nestedPath, out var nestedAmbiguous), Is.True);
            Assert.That(nestedPath, Is.EqualTo("Base Layer.First.Walk"));
            Assert.That(nestedAmbiguous, Is.False);
        }

        [Test]
        public void UniqueBareStateNameResolvesToCanonicalNestedPath()
        {
            BuildController();
            _firstMachine.AddState("Unique");

            Assert.That(AnimatorRuntimeCommands.TryResolveStatePath(_controller, 0, "Unique", out var path, out var ambiguous), Is.True);
            Assert.That(path, Is.EqualTo("Base Layer.First.Unique"));
            Assert.That(ambiguous, Is.False);
        }

        [Test]
        public void DuplicateBareStateNameRefusesEvenAcrossNestedMachines()
        {
            BuildController();
            _firstMachine.AddState("Walk");
            _secondMachine.AddState("Walk");

            Assert.That(AnimatorRuntimeCommands.TryResolveStatePath(_controller, 0, "Walk", out var path, out var ambiguous), Is.False);
            Assert.That(path, Is.Null);
            Assert.That(ambiguous, Is.True);
        }

        [Test]
        public void MissingStatePathRefuses()
        {
            BuildController();
            _firstMachine.AddState("Walk");

            Assert.That(AnimatorRuntimeCommands.TryResolveStatePath(_controller, 0, "Base Layer.Second.Walk", out var path, out var ambiguous), Is.False);
            Assert.That(path, Is.Null);
            Assert.That(ambiguous, Is.False);
        }

        private void BuildController()
        {
            _controller = new AnimatorController();
            _rootMachine = new AnimatorStateMachine();
            _firstMachine = new AnimatorStateMachine();
            _secondMachine = new AnimatorStateMachine();
            _firstMachine.name = "First";
            _secondMachine.name = "Second";
            _rootMachine.AddStateMachine(_firstMachine, Vector3.zero);
            _rootMachine.AddStateMachine(_secondMachine, Vector3.right);
            _controller.layers = new[] { new AnimatorControllerLayer { name = "Base Layer", stateMachine = _rootMachine } };
        }
    }
}
