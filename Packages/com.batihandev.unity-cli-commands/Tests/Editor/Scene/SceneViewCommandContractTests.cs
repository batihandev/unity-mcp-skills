using BatihanDev.UnityCliCommands.Scene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Tests.Scene
{
    public sealed class SceneViewCommandContractTests
    {
        private UnityEditor.SceneView _view;
        private bool _ownsView;
        private Vector3 _pivot;
        private Quaternion _rotation;
        private float _size;
        private bool _orthographic;

        [SetUp]
        public void SetUp()
        {
            _view = UnityEditor.SceneView.lastActiveSceneView;
            if (_view == null)
            {
                _view = EditorWindow.GetWindow<UnityEditor.SceneView>();
                _ownsView = true;
            }
            _pivot = _view.pivot;
            _rotation = _view.rotation;
            _size = _view.size;
            _orthographic = _view.orthographic;
        }

        [TearDown]
        public void TearDown()
        {
            if (_view == null) return;
            if (_ownsView) _view.Close();
            else
            {
                _view.pivot = _pivot;
                _view.rotation = _rotation;
                _view.size = _size;
                _view.orthographic = _orthographic;
                _view.Repaint();
            }
        }

        [Test]
        public void FramePreservesOmittedRotationAndSizeAndReportsScalarState()
        {
            _view.LookAtDirect(new Vector3(1f, 2f, 3f), Quaternion.Euler(12f, 24f, 3f), 6f);
            var requested = new Vector3(4f, 5f, 6f);

            var result = SceneViewCommands.Frame(requested.x, requested.y, requested.z);

            Assert.That(result.Ok, Is.True, result.Error?.Code);
            Assert.That(result.Result.Requested.Pivot, Is.EqualTo(new[] { 4f, 5f, 6f }));
            Assert.That(result.Result.Requested.Size, Is.EqualTo(6f));
            Assert.That(result.Result.Requested.Euler[0], Is.EqualTo(12f).Within(0.01f));
            Assert.That(result.Result.Requested.Euler[1], Is.EqualTo(24f).Within(0.01f));
            Assert.That(result.Result.Requested.Euler[2], Is.EqualTo(3f).Within(0.01f));
            Assert.That(result.Result.Actual.CameraPosition, Is.Not.Null);
        }

        [Test]
        public void InvalidPartialEulerInputLeavesTheViewUnchanged()
        {
            _view.LookAtDirect(new Vector3(7f, 8f, 9f), Quaternion.Euler(10f, 20f, 30f), 5f);
            var beforePivot = _view.pivot;
            var beforeRotation = _view.rotation;
            var beforeSize = _view.size;

            var result = SceneViewCommands.Frame(20f, 30f, 40f, rotX: 90f);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("INCOMPLETE_ROTATION"));
            Assert.That(_view.pivot, Is.EqualTo(beforePivot));
            Assert.That(_view.rotation, Is.EqualTo(beforeRotation));
            Assert.That(_view.size, Is.EqualTo(beforeSize));
        }
    }
}
