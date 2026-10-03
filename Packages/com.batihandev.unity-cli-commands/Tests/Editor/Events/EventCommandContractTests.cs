using System;
using BatihanDev.UnityCliCommands.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Tests.Events
{
    public sealed class EventCommandContractTests
    {
        private GameObject _gameObject;
        private EventProbe _probe;
        private string _target;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _gameObject = new GameObject("Event Probe");
            _probe = _gameObject.AddComponent<EventProbe>();
            _target = BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(_probe);
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null) UnityObject.DestroyImmediate(_gameObject);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void InspectListsInheritedFieldPropertyAndGenericEvent()
        {
            var result = EventCommands.Inspect(_target, includeListeners: true);
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.Events, Has.Some.Property("Name").EqualTo("Inherited"));
            Assert.That(result.Result.Events, Has.Some.Property("Name").EqualTo("PropertyEvent"));
            Assert.That(result.Result.Events, Has.Some.Property("Name").EqualTo("Generic"));
            Assert.That(result.Result.Events, Has.None.Property("Name").EqualTo("propertyEvent"));
            Assert.That(EventCommands.Inspect(_target, "Generic").Ok, Is.True);
            Assert.That(EventCommands.Inspect(_target, "Missing").Ok, Is.False);
        }

        [Test]
        public void InspectListsNullPublicFieldAndPropertyButRefusesSelection()
        {
            _probe.NullField = null;
            var listed = EventCommands.Inspect(_target, includeListeners: true);
            Assert.That(listed.Ok, Is.True);
            foreach (var name in new[] { "NullField", "NullProperty" })
            {
                var info = listed.Result.Events.Find(item => item.Name == name);
                Assert.That(info, Is.Not.Null, name);
                Assert.That(info.IsNull, Is.True, name);
                Assert.That(info.PersistentCount, Is.Zero, name);
                Assert.That(info.Listeners, Is.Empty, name);
                Assert.That(EventCommands.Inspect(_target, name).Ok, Is.False, name);
                Assert.That(EventCommands.ListenerAdd(_target, name, _target, "OnVoid").Ok, Is.False, name);
                Assert.That(EventCommands.Invoke(_target, name, confirm: true).Ok, Is.False, name);
            }
        }

        [Test]
        public void AddSupportsTypedArgumentsSetterAndStrictPreflight()
        {
            Assert.That(EventCommands.ListenerAdd(_target, "PropertyEvent", _target, "OnVoid").Ok, Is.True);
            Assert.That(EventCommands.ListenerAdd(_target, "PropertyEvent", _target, "OnInt", argType: "int", intArg: 7).Ok, Is.True);
            Assert.That(EventCommands.ListenerAdd(_target, "PropertyEvent", _target, "OnFloat", argType: "float", floatArg: 1.25f).Ok, Is.True);
            Assert.That(EventCommands.ListenerAdd(_target, "PropertyEvent", _target, "OnString", argType: "string", stringArg: "hello").Ok, Is.True);
            Assert.That(EventCommands.ListenerAdd(_target, "PropertyEvent", _target, "OnBool", argType: "bool", boolArg: true).Ok, Is.True);
            Assert.That(EventCommands.ListenerAdd(_target, "PropertyEvent", _target, "set_SetValue", argType: "int", intArg: 9, mode: "Off").Ok, Is.True);
            Assert.That(_probe.PropertyEvent.GetPersistentEventCount(), Is.EqualTo(6));
            Assert.That(_probe.PropertyEvent.GetPersistentListenerState(5), Is.EqualTo(UnityEventCallState.Off));
            Assert.That(EventCommands.ListenerAdd(_target, "PropertyEvent", _target, "WrongReturn").Ok, Is.False);
            Assert.That(EventCommands.ListenerAdd(_target, "Computed", _target, "OnVoid").Ok, Is.False);
            Assert.That(EventCommands.ListenerAdd(_target, "PropertyEvent", _target, "OnFloat", argType: "float", floatArg: float.NaN).Ok, Is.False);
            Assert.That(EventCommands.ListenerAdd(_target, "PropertyEvent", _target, "OnVoid", mode: "9").Ok, Is.False);
            Assert.That(_probe.PropertyEvent.GetPersistentEventCount(), Is.EqualTo(6));
        }

        [Test]
        public void EachMutationCanBeUndone()
        {
            var add = EventCommands.ListenerAdd(_target, "Event", _target, "OnVoid");
            Assert.That(add.Ok, Is.True);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(_probe.Event.GetPersistentEventCount(), Is.Zero);
            EventCommands.ListenerAdd(_target, "Event", _target, "OnVoid");
            var state = EventCommands.ListenerState(_target, "Event", "Off");
            Assert.That(state.Ok, Is.True);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(_probe.Event.GetPersistentListenerState(0), Is.EqualTo(UnityEventCallState.RuntimeOnly));
            var remove = EventCommands.ListenerRemove(_target, "Event");
            Assert.That(remove.Ok, Is.True);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(_probe.Event.GetPersistentEventCount(), Is.EqualTo(1));
            var clear = EventCommands.ListenersClear(_target, "Event");
            Assert.That(clear.Ok, Is.True);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(_probe.Event.GetPersistentEventCount(), Is.EqualTo(1));
        }

        [Test]
        public void BatchRetainsSuccessesAndResultOnFailure()
        {
            var items = "[{\"target\":\"" + _target + "\",\"methodName\":\"OnVoid\"},{\"target\":\"" + _target + "\",\"methodName\":\"Missing\"},{\"target\":\"" + _target + "\",\"methodName\":\"OnVoid\"}]";
            var preview = EventCommands.ListenersAddBatch(_target, "Event", items, true);
            Assert.That(preview.Ok, Is.False);
            Assert.That(preview.Result.WouldAdd, Is.EqualTo(2));
            Assert.That(_probe.Event.GetPersistentEventCount(), Is.Zero);
            var result = EventCommands.ListenersAddBatch(_target, "Event", items);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result.Added, Is.EqualTo(2));
            Assert.That(result.Result.Failed, Is.EqualTo(1));
            Assert.That(_probe.Event.GetPersistentEventCount(), Is.EqualTo(2));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(_probe.Event.GetPersistentEventCount(), Is.Zero);
        }

        [Test]
        public void CopySkipsTypedOverloadAndPreservesOffAndAppend()
        {
            UnityEventTools.AddIntPersistentListener(_probe.Inherited, _probe.OnOverload, 3);
            UnityEventTools.AddPersistentListener(_probe.Inherited, _probe.OnVoid);
            _probe.Inherited.SetPersistentListenerState(1, UnityEventCallState.Off);
            UnityEventTools.AddPersistentListener(_probe.Event, _probe.OnVoid);
            var result = EventCommands.ListenersCopy(_target, "Inherited", _target, "Event");
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.Copied, Is.EqualTo(1));
            Assert.That(result.Result.Skipped, Has.Count.EqualTo(1));
            Assert.That(_probe.Event.GetPersistentEventCount(), Is.EqualTo(2));
            Assert.That(_probe.Event.GetPersistentListenerState(1), Is.EqualTo(UnityEventCallState.Off));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(_probe.Event.GetPersistentEventCount(), Is.EqualTo(1));
        }

        [Test]
        public void CopyDoesNotRebindGenericDynamicCallbackToVoidOverload()
        {
            UnityEventTools.AddPersistentListener(_probe.Generic, (UnityAction<int>)_probe.OnOverload);
            var result = EventCommands.ListenersCopy(_target, "Generic", _target, "Event");
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.Copied, Is.Zero);
            Assert.That(result.Result.Skipped, Has.Count.EqualTo(1));
            Assert.That(_probe.Event.GetPersistentEventCount(), Is.Zero);
        }

        [Test]
        public void InvokeRequiresConfirmationAndReportsThrow()
        {
            UnityEventTools.AddPersistentListener(_probe.Event, _probe.OnVoid);
            _probe.Event.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            Assert.That(EventCommands.Invoke(_target, "Event").Ok, Is.False);
            Assert.That(EventCommands.Invoke(_target, "Event", dryRun: true).Ok, Is.True);
            Assert.That(_probe.Calls, Is.Zero);
            Assert.That(EventCommands.Invoke(_target, "Event", confirm: true).Ok, Is.True);
            Assert.That(_probe.Calls, Is.EqualTo(1));
            UnityEventTools.AddPersistentListener(_probe.Event, _probe.Throw);
            _probe.Event.SetPersistentListenerState(1, UnityEventCallState.EditorAndRuntime);
            var thrown = EventCommands.Invoke(_target, "Event", confirm: true);
            Assert.That(thrown.Ok, Is.False);
            Assert.That(thrown.Result.Dispatched, Is.True);
        }

        [Test]
        public void GenericStaticVoidAddInvokesAndRemoveUndoRestores()
        {
            var added=EventCommands.ListenerAdd(_target,"Generic",_target,"OnVoid",mode:"EditorAndRuntime");
            Assert.That(added.Ok,Is.True,added.Error?.Message);
            Assert.That(_probe.Generic.GetPersistentEventCount(),Is.EqualTo(1));
            _probe.Generic.Invoke(7);Assert.That(_probe.Calls,Is.EqualTo(1));
            Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert.That(_probe.Generic.GetPersistentEventCount(),Is.Zero);
            Undo.PerformRedo();Assert.That(_probe.Generic.GetPersistentEventCount(),Is.EqualTo(1));
            Assert.That(EventCommands.ListenerRemove(_target,"Generic").Ok,Is.True);
            Assert.That(_probe.Generic.GetPersistentEventCount(),Is.Zero);
            Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert.That(_probe.Generic.GetPersistentEventCount(),Is.EqualTo(1));
        }

        [Test]
        public void GenericStaticVoidPreflightAndPreviewLeaveListenersUnchanged()
        {
            Assert.That(EventCommands.ListenerAdd(_target,"Generic",_target,"OnVoid",dryRun:true).Ok,Is.True);
            Assert.That(EventCommands.ListenerAdd(_target,"Generic",_target,"Missing").Ok,Is.False);
            Assert.That(EventCommands.ListenerAdd(_target,"Generic",_target,"OnInt",argType:"int",intArg:7).Ok,Is.False);
            Assert.That(EventCommands.ListenerAdd(_target,"Generic",_target,"OnVoid",mode:"8").Ok,Is.False);
            Assert.That(EventCommands.ListenersAddBatch(_target,"Generic","[]").Ok,Is.False);
            Assert.That(EventCommands.Invoke(_target,"Generic",confirm:true).Ok,Is.False);
            Assert.That(_probe.Generic.GetPersistentEventCount(),Is.Zero);
            Assert.That(_probe.Calls,Is.Zero);
        }

        [Test]
        public void GenericStaticVoidPersistsAcrossSavedSceneReopen()
        {
            var path=AssetDatabase.GenerateUniqueAssetPath("Assets/EventGenericContract.unity");
            try
            {
                Assert.That(EventCommands.ListenerAdd(_target,"Generic",_target,"OnVoid",mode:"EditorAndRuntime").Ok,Is.True);
                Assert.That(EditorSceneManager.SaveScene(_gameObject.scene,path),Is.True);
                EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                _probe=UnityObject.FindFirstObjectByType<EventProbe>();_gameObject=_probe.gameObject;
                Assert.That(_probe.Generic.GetPersistentEventCount(),Is.EqualTo(1));
                Assert.That(_probe.Generic.GetPersistentTarget(0),Is.SameAs(_probe));
                _probe.Generic.Invoke(12);Assert.That(_probe.Calls,Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
