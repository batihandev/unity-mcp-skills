using BatihanDev.UnityCliCommands.Navigation;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Tests.Navigation
{
    public sealed class NavigationSurfaceContractTests
    {
        private GameObject _root;
        private NavMeshSurface _surface;
        private string _target;
        private readonly System.Collections.Generic.List<NavMeshData> _createdData = new System.Collections.Generic.List<NavMeshData>();

        [SetUp]
        public void CreateSurface()
        {
            _createdData.Clear();
            _root = new GameObject("NavMeshSurfaceContract");
            _surface = _root.AddComponent<NavMeshSurface>();
            _surface.collectObjects = CollectObjects.Children;
            _surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            _target = EntityId.ToULong(_surface.GetEntityId()).ToString();
        }

        [TearDown]
        public void DestroySurface()
        {
            if (_surface != null) _surface.RemoveData();
            if (_root != null) UnityObject.DestroyImmediate(_root);
            foreach (var data in _createdData)
                if (data != null) UnityObject.DestroyImmediate(data);
            _createdData.Clear();
        }

        [Test]
        public void StaleAndInactiveTargetsRefuseBuild()
        {
            Assert.That(NavigationSurfaceCommands.Build("0", true).Error.Code, Is.EqualTo("SURFACE_NOT_FOUND"));
            _root.SetActive(false);
            Assert.That(NavigationSurfaceCommands.Build(_target, true).Error.Code, Is.EqualTo("SURFACE_INACTIVE"));
        }

        [Test]
        public void ConfirmationAndDryRunDoNotBuild()
        {
            Assert.That(NavigationSurfaceCommands.Build(_target).Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            var preview = NavigationSurfaceCommands.Build(_target, true, true);
            Assert.That(preview.Ok, Is.True);
            Assert.That(preview.Result.Applied, Is.False);
            Assert.That(preview.Result.Dispatched, Is.False);
            Assert.That(_surface.navMeshData, Is.Null);
        }

        [Test]
        public void BuildReplacesDataWithSessionReference()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(_root.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(10f, 1f, 10f);
            var first = NavigationSurfaceCommands.Build(_target, true);
            if (_surface.navMeshData != null) _createdData.Add(_surface.navMeshData);
            Assert.That(first.Ok, Is.True);
            Assert.That(first.Result.DataReferenceChanged, Is.True);
            Assert.That(first.Result.SavedAsset, Is.False);
            var prior = _surface.navMeshData;
            var second = NavigationSurfaceCommands.Build(_target, true);
            if (_surface.navMeshData != null) _createdData.Add(_surface.navMeshData);
            Assert.That(second.Ok, Is.True);
            Assert.That(second.Result.DataBefore, Is.EqualTo(first.Result.DataAfter));
            Assert.That(_surface.navMeshData, Is.Not.SameAs(prior));
        }

        [Test]
        public void RemoveDataRetainsReferenceAndIsNotUndoable()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(_root.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(10f, 1f, 10f);
            var built = NavigationSurfaceCommands.Build(_target, true);
            if (_surface.navMeshData != null) _createdData.Add(_surface.navMeshData);
            Assert.That(built.Ok, Is.True);
            var prior = _surface.navMeshData;
            Assert.That(NavigationSurfaceCommands.RemoveData(_target).Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            var preview = NavigationSurfaceCommands.RemoveData(_target, true, true);
            Assert.That(preview.Ok, Is.True);
            Assert.That(preview.Result.Dispatched, Is.False);
            var removed = NavigationSurfaceCommands.RemoveData(_target, true);
            Assert.That(removed.Ok, Is.True);
            Assert.That(removed.Result.DataRetained, Is.True);
            Assert.That(removed.Result.Undoable, Is.False);
            Assert.That(_surface.navMeshData, Is.SameAs(prior));
        }
    }
}
