using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.XR;
using BatihanDev.UnityCliCommands.Events;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BatihanDev.UnityCliCommands.Tests.XR
{
    public abstract class XRContractSceneFixture
    {
        [Serializable] private sealed class SavedScene
        {
            public string path;
            public bool isLoaded;
            public bool isActive;
        }
        [Serializable] private sealed class SceneCheckpoint
        {
            public string path;
            public SavedScene[] setup;
        }
        protected string path;
        protected GameObject target;
        protected XRInteractionManager manager;
        protected static string Id(UnityEngine.Object value) => EntityId.ToULong(value.GetEntityId()).ToString();
        private string CheckpointKey => GetType().FullName + ".SceneCheckpoint";

        [SetUp] public void SetUp()
        {
            // NUnit repeats SetUp after EnterPlayMode; the saved checkpoint owns this scene across reload.
            var saved=SessionState.GetString(CheckpointKey, "");
            if(!string.IsNullOrEmpty(saved))
            {
                path=JsonUtility.FromJson<SceneCheckpoint>(saved).path;
                var restoredScene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
                if(restoredScene.IsValid() && restoredScene.isLoaded)
                {
                    var roots=restoredScene.GetRootGameObjects();
                    target=roots.SingleOrDefault(go=>go.name=="Target");
                    manager=roots.Select(go=>go.GetComponent<XRInteractionManager>()).SingleOrDefault(c=>c!=null);
                }
                return;
            }
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var checkpoint=new SceneCheckpoint
            {
                path=AssetDatabase.GenerateUniqueAssetPath("Assets/XRContract.unity"),
                setup=EditorSceneManager.GetSceneManagerSetup().Select(s=>new SavedScene
                {path=s.path,isLoaded=s.isLoaded,isActive=s.isActive}).ToArray()
            };
            path=checkpoint.path;
            SessionState.SetString(CheckpointKey,JsonUtility.ToJson(checkpoint));
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Assert.IsTrue(EditorSceneManager.SaveScene(scene,path));
            manager=new GameObject("Manager").AddComponent<XRInteractionManager>();
            target=new GameObject("Target");
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            // This also runs when an assertion interrupts the test before its normal ExitPlayMode.
            if(EditorApplication.isPlayingOrWillChangePlaymode) yield return new ExitPlayMode();
            var saved=SessionState.GetString(CheckpointKey, "");
            if(string.IsNullOrEmpty(saved)) yield break;
            var checkpoint=JsonUtility.FromJson<SceneCheckpoint>(saved);
            Undo.ClearAll();
            try
            {
                if(checkpoint.setup.Length>0 && checkpoint.setup.All(s=>!string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(checkpoint.setup.Select(s=>new SceneSetup
                    {path=s.path,isLoaded=s.isLoaded,isActive=s.isActive}).ToArray());
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
            finally
            {
                try { AssetDatabase.DeleteAsset(checkpoint.path); }
                finally { SessionState.EraseString(CheckpointKey); }
            }
        }
    }

    public sealed class XRAuthoringContractTests : XRContractSceneFixture
    {
        [Test] public void RigCreatesSharedOffsetHierarchyAndRealTrackingBindings()
        {
            var r = XRSetupCommands.CreateRig("Rig", 2, 3, 4, confirm: true); Assert.IsTrue(r.Ok, r.Error?.Message);
            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            Assert.AreEqual(new Vector3(2,3,4), origin.transform.position);
            Assert.AreEqual(1.36144f, origin.CameraYOffset);
            Assert.AreSame(origin.CameraFloorOffsetObject.transform, origin.Camera.transform.parent);
            foreach(var driver in origin.GetComponentsInChildren<TrackedPoseDriver>())
            {
                Assert.AreSame(origin.CameraFloorOffsetObject.transform, driver.transform.parent);
                Assert.IsNotEmpty(driver.positionInput.action.bindings);
                Assert.IsNotEmpty(driver.rotationInput.action.bindings);
                Assert.IsNotEmpty(driver.trackingStateInput.action.bindings);
            }
            Assert.AreEqual(3, origin.GetComponentsInChildren<TrackedPoseDriver>().Length);
        }
        [Test] public void RigInvalidAndDryRunLeaveSceneUnchanged()
        {
            Assert.IsFalse(XRSetupCommands.CreateRig(cameraYOffset: float.NaN, confirm:true).Ok);
            Assert.IsTrue(XRSetupCommands.CreateRig(dryRun:true).Ok);
            Assert.IsNull(UnityEngine.Object.FindAnyObjectByType<XROrigin>());
        }
        [Test] public void ManagerAmbiguityRefusesRigBeforeCreation()
        {
            new GameObject("Second Manager").AddComponent<XRInteractionManager>();
            Assert.IsFalse(XRSetupCommands.CreateRig(confirm:true).Ok);
            Assert.IsNull(UnityEngine.Object.FindAnyObjectByType<XROrigin>());
        }
        [Test] public void RayRejectsNumericEnumBeforeAddingAnything()
        {
            Assert.IsFalse(XRInteractorCommands.Ray(Id(target), lineType:"0", confirm:true).Ok);
            Assert.IsNull(target.GetComponent<XRRayInteractor>()); Assert.IsNull(target.GetComponent<LineRenderer>());
        }
        [Test] public void RayUsesActualLineVisualAndNoCollider()
        {
            Assert.IsTrue(XRInteractorCommands.Ray(Id(target), confirm:true).Ok);
            Assert.AreEqual(30, target.GetComponent<XRRayInteractor>().maxRaycastDistance);
            Assert.AreEqual(.01f, target.GetComponent<LineRenderer>().startWidth);
            Assert.IsNull(target.GetComponent<Collider>());
            Assert.AreSame(manager,target.GetComponent<XRRayInteractor>().interactionManager);
        }
        [Test] public void DirectPreservesExistingColliderAndReportsActualShape()
        {
            var box=target.AddComponent<BoxCollider>(); box.isTrigger=false;
            var r=XRInteractorCommands.Direct(Id(target), radius:.7f, confirm:true);
            Assert.IsTrue(r.Ok,r.Error?.Message); Assert.AreSame(box,target.GetComponent<Collider>());
            Assert.IsFalse(box.isTrigger); Assert.IsNotEmpty(r.Result.Issues);
        }
        [Test] public void SocketSetsSupportedFieldsAndUndoRemovesComposite()
        {
            Assert.IsTrue(XRInteractorCommands.Socket(Id(target),false,2,confirm:true).Ok);
            Assert.IsFalse(target.GetComponent<XRSocketInteractor>().showInteractableHoverMeshes);
            Assert.AreEqual(2,target.GetComponent<XRSocketInteractor>().recycleDelayTime);
            Assert.AreEqual(.15f,target.GetComponent<SphereCollider>().radius);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.IsNull(target.GetComponent<XRSocketInteractor>()); Assert.IsNull(target.GetComponent<Collider>());
            Undo.PerformRedo(); Assert.IsNotNull(target.GetComponent<XRSocketInteractor>());
        }
        [Test] public void GrabPreservesExistingBodyAndCreatesAttachGeometry()
        {
            var rb=target.AddComponent<Rigidbody>(); rb.useGravity=false;rb.isKinematic=true;
            Assert.IsNull(target.GetComponent<MeshFilter>());
            var r=XRInteractableCommands.Grab(Id(target),attachTransformOffset:"1,2,3",confirm:true);
            Assert.IsTrue(r.Ok,r.Error?.Message);Assert.IsNotNull(target.GetComponent<BoxCollider>());
            var grab=target.GetComponent<XRGrabInteractable>(); Assert.IsFalse(rb.useGravity);Assert.IsTrue(rb.isKinematic);
            Assert.AreEqual(new Vector3(1,2,3),grab.attachTransform.localPosition);Assert.AreSame(target.transform,grab.attachTransform.parent);
            Assert.AreEqual(5,grab.smoothPositionAmount);Assert.IsTrue(grab.throwOnDetach);
        }
        [Test] public void GrabRejectsMalformedOffsetBeforeBodyOrColliderWrites()
        {
            Assert.IsFalse(XRInteractableCommands.Grab(Id(target),attachTransformOffset:"NaN,2,3",confirm:true).Ok);
            Assert.IsNull(target.GetComponent<Rigidbody>());Assert.IsNull(target.GetComponent<Collider>());
        }
        [Test] public void SimpleRejectsGrabOptionsAndPreservesSelectMode()
        {
            Assert.IsNull(target.GetComponent<MeshFilter>());
            var r=XRInteractableCommands.Simple(Id(target),confirm:true);
            Assert.IsTrue(r.Ok,r.Error?.Message);Assert.IsNotNull(target.GetComponent<BoxCollider>());
            var simple=target.GetComponent<XRSimpleInteractable>();var old=simple.selectMode;
            Assert.IsFalse(XRInteractableCommands.Configure(Id(simple),"{\"selectMode\":\"Multiple\",\"throwOnDetach\":false}",confirm:true).Ok);
            Assert.AreEqual(old,simple.selectMode);Assert.IsNull(target.GetComponent<Rigidbody>());
        }
        [Test] public void ConfigurePreservesOmittedFieldsAndRejectsUnknownFields()
        {
            Assert.IsTrue(XRInteractableCommands.Grab(Id(target),confirm:true).Ok);var grab=target.GetComponent<XRGrabInteractable>();
            Assert.IsTrue(XRInteractableCommands.Configure(Id(grab),"{\"smoothPosition\":false}",confirm:true).Ok);
            Assert.IsFalse(grab.smoothPosition);Assert.IsTrue(grab.smoothRotation);
            Assert.IsFalse(XRInteractableCommands.Configure(Id(grab),"{\"smoothPosition\":true,\"unknown\":3}",confirm:true).Ok);Assert.IsFalse(grab.smoothPosition);
        }
        [Test] public void LocomotionWiresExactOriginAndPreservesOtherProviders()
        {
            var origin=target.AddComponent<XROrigin>();
            Assert.IsTrue(XRLocomotionCommands.Teleport(Id(origin),confirm:true).Ok);
            Assert.IsTrue(XRLocomotionCommands.Move(Id(origin),confirm:true).Ok);
            Assert.AreSame(origin,target.GetComponent<XRBodyTransformer>().xrOrigin);
            Assert.AreSame(target.GetComponent<LocomotionMediator>(),target.GetComponent<ContinuousMoveProvider>().mediator);
            Assert.IsNotNull(target.GetComponent<TeleportationProvider>());
            Assert.IsNotEmpty(target.GetComponent<ContinuousMoveProvider>().leftHandMoveInput.inputAction.bindings);
        }
        [Test] public void LocomotionAmbiguityAndInvalidTurnLeaveNoDependencies()
        {
            target.AddComponent<XROrigin>();new GameObject("Other").AddComponent<XROrigin>();
            Assert.IsFalse(XRLocomotionCommands.Move(confirm:true).Ok);
            Assert.IsFalse(XRLocomotionCommands.Turn(Id(target),"typo",confirm:true).Ok);
            Assert.IsNull(target.GetComponent<XRBodyTransformer>());
        }
        [TestCase("Move", 0)] [TestCase("Move", 1)] [TestCase("Move", 2)] [TestCase("Move", 3)] [TestCase("Move", 4)]
        [TestCase("Snap", 0)] [TestCase("Snap", 1)] [TestCase("Snap", 2)] [TestCase("Snap", 3)] [TestCase("Snap", 4)]
        [TestCase("Continuous", 0)] [TestCase("Continuous", 1)] [TestCase("Continuous", 2)] [TestCase("Continuous", 3)] [TestCase("Continuous", 4)]
        public void ExistingLocomotionPreservesReadersAndReportsConfiguredSources(string kind, int scenario)
        {
            var origin=target.AddComponent<XROrigin>();
            var asset=UnityEngine.ScriptableObject.CreateInstance<InputActionAsset>();
            var map=new InputActionMap("Custom Motion");asset.AddActionMap(map);
            var action=map.AddAction("Custom Stick",InputActionType.Value,"<Gamepad>/rightStick");
            var reference=InputActionReference.Create(action);
            var modes=new[]{XRInputValueReader.InputSourceMode.Unused,XRInputValueReader.InputSourceMode.ManualValue,
                XRInputValueReader.InputSourceMode.InputAction,XRInputValueReader.InputSourceMode.InputActionReference,
                XRInputValueReader.InputSourceMode.InputActionReference};
            var left=new XRInputValueReader<Vector2>("Left Custom",modes[scenario]);
            var right=new XRInputValueReader<Vector2>("Right Custom",modes[(scenario+1)%modes.Length]);
            left.inputAction=scenario==2?new InputAction("Inline Left",InputActionType.Value,"<Gamepad>/leftStick"):null;
            right.inputAction=scenario==1?new InputAction("Inline Right",InputActionType.Value,"<Joystick>/stick"):null;
            left.inputActionReference=scenario==3?reference:null;right.inputActionReference=scenario==2?reference:null;
            left.manualValue=new Vector2(.25f,-.75f);right.manualValue=new Vector2(-.5f,.125f);
            LocomotionProvider provider;
            if(kind=="Move"){var p=target.AddComponent<ContinuousMoveProvider>();p.leftHandMoveInput=left;p.rightHandMoveInput=right;provider=p;}
            else if(kind=="Snap"){var p=target.AddComponent<SnapTurnProvider>();p.leftHandTurnInput=left;p.rightHandTurnInput=right;provider=p;}
            else {var p=target.AddComponent<ContinuousTurnProvider>();p.leftHandTurnInput=left;p.rightHandTurnInput=right;provider=p;}
            try
            {
                var result=kind=="Move"?XRLocomotionCommands.Move(Id(origin),moveSpeed:7,confirm:true):
                    XRLocomotionCommands.Turn(Id(origin),kind,turnAmount:30,turnSpeed:123,confirm:true);
                Assert.IsTrue(result.Ok,result.Error?.Message);
                Assert.AreSame(left,LeftReader(provider));Assert.AreSame(right,RightReader(provider));
                Assert.AreEqual(modes[scenario],left.inputSourceMode);Assert.AreEqual(modes[(scenario+1)%modes.Length],right.inputSourceMode);
                Assert.AreEqual(new Vector2(.25f,-.75f),left.manualValue);Assert.AreEqual(new Vector2(-.5f,.125f),right.manualValue);
                Assert.AreSame(scenario==3?reference:null,left.inputActionReference);
                Assert.AreSame(scenario==2?reference:null,right.inputActionReference);
                Assert.AreEqual(scenario==2?"Inline Left":null,left.inputAction?.name);
                Assert.AreEqual(scenario==1?"Inline Right":null,right.inputAction?.name);
                var state=result.Result.State;
                AssertReaderSnapshot(state,"left",modes[scenario].ToString(),scenario==2?"Inline Left":null,
                    scenario==2?new[]{"<Gamepad>/leftStick"}:Array.Empty<string>(),scenario==3?"Custom Stick":null,
                    scenario==3?new[]{"<Gamepad>/rightStick"}:Array.Empty<string>(),left.manualValue);
                AssertReaderSnapshot(state,"right",modes[(scenario+1)%modes.Length].ToString(),scenario==1?"Inline Right":null,
                    scenario==1?new[]{"<Joystick>/stick"}:Array.Empty<string>(),scenario==2?"Custom Stick":null,
                    scenario==2?new[]{"<Gamepad>/rightStick"}:Array.Empty<string>(),right.manualValue);
                if(kind=="Move")Assert.AreEqual(7,((ContinuousMoveProvider)provider).moveSpeed);
                else if(kind=="Snap")Assert.AreEqual(30,((SnapTurnProvider)provider).turnAmount);
                else Assert.AreEqual(123,((ContinuousTurnProvider)provider).turnSpeed);
            }
            finally {UnityEngine.Object.DestroyImmediate(reference);UnityEngine.Object.DestroyImmediate(asset);left.inputAction?.Dispose();right.inputAction?.Dispose();}
        }
        private static XRInputValueReader<Vector2> LeftReader(LocomotionProvider p)=>p is ContinuousMoveProvider m?m.leftHandMoveInput:p is SnapTurnProvider s?s.leftHandTurnInput:((ContinuousTurnProvider)p).leftHandTurnInput;
        private static XRInputValueReader<Vector2> RightReader(LocomotionProvider p)=>p is ContinuousMoveProvider m?m.rightHandMoveInput:p is SnapTurnProvider s?s.rightHandTurnInput:((ContinuousTurnProvider)p).rightHandTurnInput;
        private static void AssertReaderSnapshot(Dictionary<string,object> state,string side,string mode,string inlineName,string[] inlineBindings,string referenceName,string[] referenceBindings,Vector2 manual)
        {
            var input=(Dictionary<string,object>)state[side+"Input"];
            Assert.AreEqual(mode,input["mode"]);Assert.AreEqual(inlineName,input["inlineActionName"]);
            CollectionAssert.AreEqual(inlineBindings,(string[])input["inlineBindings"]);
            Assert.AreEqual(referenceName,input["referencedActionName"]);
            CollectionAssert.AreEqual(referenceBindings,(string[])input["referenceBindings"]);
            if(referenceName!=null)Assert.IsNotNull(input["reference"]);else Assert.IsNull(input["reference"]);
            CollectionAssert.AreEqual(new[]{manual.x,manual.y},(float[])input["manualValue"]);
            CollectionAssert.AreEqual(mode=="InputAction"?inlineBindings:mode=="InputActionReference"?referenceBindings:Array.Empty<string>(),(string[])state[side+"Bindings"]);
        }
        [TestCase("Move")] [TestCase("Snap")] [TestCase("Continuous")]
        public void NewlyAddedLocomotionHasBothDefaultInlineStickBindings(string kind)
        {
            var origin=target.AddComponent<XROrigin>();
            var result=kind=="Move"?XRLocomotionCommands.Move(Id(origin),confirm:true):XRLocomotionCommands.Turn(Id(origin),kind,confirm:true);
            Assert.IsTrue(result.Ok,result.Error?.Message);
            var p=target.GetComponents<LocomotionProvider>().Single();
            Assert.AreEqual(XRInputValueReader.InputSourceMode.InputAction,LeftReader(p).inputSourceMode);
            Assert.AreEqual(XRInputValueReader.InputSourceMode.InputAction,RightReader(p).inputSourceMode);
            CollectionAssert.AreEqual(new[]{"<XRController>{LeftHand}/primary2DAxis"},LeftReader(p).inputAction.bindings.Select(b=>b.path));
            CollectionAssert.AreEqual(new[]{"<XRController>{RightHand}/primary2DAxis"},RightReader(p).inputAction.bindings.Select(b=>b.path));
        }
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void ReportInventoriesStandaloneDisabledAndInactiveLineVisuals(bool verbose,bool includeInactive)
        {
            var enabled=target.AddComponent<XRInteractorLineVisual>();
            var disabled=new GameObject("Disabled Visual").AddComponent<XRInteractorLineVisual>();disabled.enabled=false;
            var inactive=new GameObject("Inactive Visual").AddComponent<XRInteractorLineVisual>();inactive.gameObject.SetActive(false);
            var r=XRSetupCommands.Report(verbose,includeInactive);Assert.IsTrue(r.Ok,r.Error?.Message);
            var entries=ReportEntries(r.Result);Assert.AreEqual(includeInactive?4:3,r.Result.State["total"]);
            var visuals=entries.Where(d=>(string)d["type"]==typeof(XRInteractorLineVisual).FullName).ToArray();
            Assert.AreEqual(includeInactive?3:2,visuals.Length);
            Assert.IsTrue((bool)Entry(entries,enabled)["active"]);Assert.IsTrue((bool)Entry(entries,enabled)["enabled"]);
            Assert.IsTrue((bool)Entry(entries,disabled)["active"]);Assert.IsFalse((bool)Entry(entries,disabled)["enabled"]);
            if(includeInactive)Assert.IsFalse((bool)Entry(entries,inactive)["active"]);
            else Assert.IsFalse(visuals.Any(d=>(string)d["component"]==Id(inactive)));
        }
        private static Dictionary<string,object>[] ReportEntries(XRResult result)=>(Dictionary<string,object>[])result.State["components"];
        private static Dictionary<string,object> Entry(Dictionary<string,object>[] entries,UnityEngine.Component component)=>entries.Single(d=>(string)d["component"]==Id(component));
        [Test] public void VerboseReportIncludesActualSocketAndSnapDirectionFlags()
        {
            var socket=target.AddComponent<XRSocketInteractor>();socket.socketActive=false;
            var snap=new GameObject("Snap").AddComponent<SnapTurnProvider>();snap.enableTurnLeftRight=false;snap.enableTurnAround=true;
            var r=XRSetupCommands.Report(verbose:true);Assert.IsTrue(r.Ok,r.Error?.Message);
            var entries=ReportEntries(r.Result);Assert.AreEqual(false,Entry(entries,socket)["socketActive"]);
            Assert.AreEqual(false,Entry(entries,snap)["enableTurnLeftRight"]);Assert.AreEqual(true,Entry(entries,snap)["enableTurnAround"]);
        }
        [Test] public void ReportWarnsWhenManagerSelectionIsAmbiguous()
        {
            new GameObject("Second Manager").AddComponent<XRInteractionManager>();
            var r=XRSetupCommands.Report();Assert.IsTrue(r.Ok,r.Error?.Message);
            Assert.IsTrue(r.Result.Issues.Any(i=>i.IndexOf("multiple",StringComparison.OrdinalIgnoreCase)>=0&&i.IndexOf("manager",StringComparison.OrdinalIgnoreCase)>=0),string.Join("; ",r.Result.Issues));
        }
        private static XROrigin ReportRig()
        {
            var r=XRSetupCommands.CreateRig("Diagnostic Rig",confirm:true);Assert.IsTrue(r.Ok,r.Error?.Message);
            return UnityEngine.Object.FindAnyObjectByType<XROrigin>();
        }
        private static void AssertPoseIssue(string part,string deficiency)
        {
            var r=XRSetupCommands.Report();Assert.IsTrue(r.Ok,r.Error?.Message);
            Assert.IsTrue(r.Result.Issues.Any(i=>i.StartsWith("Diagnostic Rig:",StringComparison.Ordinal)&&i.IndexOf(part,StringComparison.OrdinalIgnoreCase)>=0&&i.IndexOf(deficiency,StringComparison.OrdinalIgnoreCase)>=0),string.Join("; ",r.Result.Issues));
            StringAssert.Contains("Authoring only",r.Result.HardwareStatus);
            Assert.IsFalse(r.Result.Issues.Any(i=>i.Contains("configured tracked poses")));
        }
        [Test] public void ReportDiagnosesMissingAssignedCameraDriverDespiteThreeOtherDescendants()
        {
            var origin=ReportRig();UnityEngine.Object.DestroyImmediate(origin.Camera.GetComponent<TrackedPoseDriver>());
            new GameObject("Unrelated Pose").AddComponent<TrackedPoseDriver>().transform.SetParent(origin.transform);
            AssertPoseIssue("camera","missing");
        }
        [Test] public void ReportDiagnosesUnboundCameraAndControllerActions()
        {
            var origin=ReportRig();var camera=origin.Camera.GetComponent<TrackedPoseDriver>();
            camera.positionInput=new InputActionProperty(new InputAction("Empty Position",InputActionType.Value));
            var controller=origin.GetComponentsInChildren<TrackedPoseDriver>().First(p=>p.gameObject!=origin.Camera.gameObject);
            controller.trackingStateInput=default;
            AssertPoseIssue("camera","unbound");AssertPoseIssue("controller","unbound");
        }
        [Test] public void ReportDiagnosesControllersOutsideSharedOffset()
        {
            var origin=ReportRig();
            foreach(var driver in origin.GetComponentsInChildren<TrackedPoseDriver>().Where(p=>p.gameObject!=origin.Camera.gameObject))driver.transform.SetParent(origin.transform);
            AssertPoseIssue("controller","misplaced");
        }
        [Test] public void ReportDiagnosesAssignedCameraOutsideSharedOffset()
        {
            var origin=ReportRig();origin.Camera.transform.SetParent(origin.transform);
            AssertPoseIssue("camera","misplaced");
        }
        [Test] public void ReportDiagnosesOffsetOutsideOrigin()
        {
            var origin=ReportRig();origin.CameraFloorOffsetObject.transform.SetParent(null);
            AssertPoseIssue("offset","misplaced");
        }
        [Test] public void ReportDiagnosesMissingControllerDrivers()
        {
            var origin=ReportRig();
            foreach(var driver in origin.GetComponentsInChildren<TrackedPoseDriver>().Where(p=>p.gameObject!=origin.Camera.gameObject))UnityEngine.Object.DestroyImmediate(driver);
            AssertPoseIssue("controller","missing");
        }
        [Test] public void ReportAcceptsCustomReferencedPoseBindingsAndUnnamedNestedControllers()
        {
            var origin=ReportRig();var asset=UnityEngine.ScriptableObject.CreateInstance<InputActionAsset>();
            var map=new InputActionMap("Custom Tracking");asset.AddActionMap(map);
            var references=new List<InputActionReference>();
            try
            {
                var drivers=origin.GetComponentsInChildren<TrackedPoseDriver>();
                for(var index=0;index<drivers.Length;index++)
                {
                    var driver=drivers[index];driver.gameObject.name="Tracked Object "+index;
                    var nested=new GameObject("Custom Branch "+index);nested.transform.SetParent(origin.CameraFloorOffsetObject.transform);driver.transform.SetParent(nested.transform);
                    var position=InputActionReference.Create(map.AddAction("Position "+index,InputActionType.Value,"<Gamepad>/leftStick"));
                    var rotation=InputActionReference.Create(map.AddAction("Rotation "+index,InputActionType.Value,"<Gamepad>/rightStick"));
                    var tracking=InputActionReference.Create(map.AddAction("Tracking "+index,InputActionType.Value,"<Gamepad>/buttonSouth"));
                    references.AddRange(new[]{position,rotation,tracking});
                    driver.positionInput=new InputActionProperty(position);driver.rotationInput=new InputActionProperty(rotation);driver.trackingStateInput=new InputActionProperty(tracking);
                }
                var r=XRSetupCommands.Report();Assert.IsTrue(r.Ok,r.Error?.Message);
                Assert.IsFalse(r.Result.Issues.Any(i=>i.StartsWith("Diagnostic Rig:",StringComparison.Ordinal)),string.Join("; ",r.Result.Issues));
                StringAssert.Contains("Authoring only",r.Result.HardwareStatus);
            }
            finally {foreach(var reference in references)UnityEngine.Object.DestroyImmediate(reference);UnityEngine.Object.DestroyImmediate(asset);}
        }
        [Test] public void AnchorKeepsWorldPoseParentAndDestinationAndIndicator()
        {
            target.transform.position=new Vector3(10,0,0);target.transform.rotation=Quaternion.Euler(0,90,0);
            var r=XRLocomotionCommands.Anchor("Anchor",1,2,3,45,parent:Id(target),confirm:true);Assert.IsTrue(r.Ok,r.Error?.Message);
            var a=UnityEngine.Object.FindAnyObjectByType<TeleportationAnchor>();Assert.Less(Vector3.Distance(new Vector3(1,2,3),a.transform.position),.0001f);
            Assert.AreSame(target.transform,a.transform.parent);Assert.IsNotNull(a.teleportAnchorTransform);
            Assert.AreEqual(new Vector3(1,.01f,1),a.GetComponent<BoxCollider>().size);
            var visual=a.transform.Find("Anchor Visual");Assert.AreEqual(new Vector3(1,.02f,1),visual.localScale);Assert.IsNull(visual.GetComponent<Collider>());
            Assert.AreEqual(new Color(0,.8f,1,.5f),visual.GetComponent<MeshRenderer>().sharedMaterial.color);
            Assert.IsTrue(EditorSceneManager.SaveScene(a.gameObject.scene,path));EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            a=UnityEngine.Object.FindAnyObjectByType<TeleportationAnchor>();visual=a.transform.Find("Anchor Visual");
            Assert.AreEqual("Sprites/Default",visual.GetComponent<MeshRenderer>().sharedMaterial.shader.name);
            Assert.AreEqual(new Color(0,.8f,1,.5f),visual.GetComponent<MeshRenderer>().sharedMaterial.color);
        }
        [Test] public void AreaPreservesColliderAndRejectsInvalidOrientation()
        {
            var box=target.AddComponent<BoxCollider>();box.isTrigger=true;
            Assert.IsFalse(XRLocomotionCommands.Area(Id(target),"100",confirm:true).Ok);Assert.IsNull(target.GetComponent<TeleportationArea>());
            var r=XRLocomotionCommands.Area(Id(target),confirm:true);Assert.IsTrue(r.Ok,r.Error?.Message);Assert.IsTrue(box.isTrigger);Assert.IsNotEmpty(r.Result.Issues);
        }
        [UnityTest] public IEnumerator CanvasConversionAppliesDefaultsOnceAndUndoRestores()
        {
            var canvas=target.AddComponent<Canvas>();target.AddComponent<GraphicRaycaster>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var before=target.GetComponent<RectTransform>();var originalSize=before.sizeDelta;var originalScale=before.localScale;
            Assert.IsTrue(XRUICommands.Canvas(Id(canvas),confirm:true).Ok);
            var rect=target.GetComponent<RectTransform>();Assert.AreEqual(new Vector2(400,300),rect.sizeDelta);Assert.AreEqual(Vector3.one*.001f,rect.localScale);
            Undo.FlushUndoRecordObjects();Undo.PerformUndo();
            yield return null;
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay,canvas.renderMode);Assert.AreEqual(originalSize,rect.sizeDelta);Assert.AreEqual(originalScale,rect.localScale);
            Assert.IsNotNull(target.GetComponent<GraphicRaycaster>());Assert.IsNull(target.GetComponent<TrackedDeviceGraphicRaycaster>());
            Undo.PerformRedo();yield return null;Assert.AreEqual(RenderMode.WorldSpace,canvas.renderMode);Assert.AreEqual(new Vector2(400,300),rect.sizeDelta);
            rect.sizeDelta=new Vector2(800,600);Assert.IsTrue(XRUICommands.Canvas(Id(canvas),confirm:true).Ok);Assert.AreEqual(new Vector2(800,600),rect.sizeDelta);
            Assert.IsNull(target.GetComponent<GraphicRaycaster>());Assert.IsNotNull(target.GetComponent<TrackedDeviceGraphicRaycaster>());
        }
        [Test] public void EventSystemRemovesOnlyStandaloneModule()
        {
            var es=target.AddComponent<EventSystem>();target.AddComponent<StandaloneInputModule>();var marker=target.AddComponent<BoxCollider>();
            Assert.IsTrue(XRUICommands.EventSystem(Id(es),confirm:true).Ok);Assert.IsNull(target.GetComponent<StandaloneInputModule>());
            Assert.IsNotNull(target.GetComponent<XRUIInputModule>());Assert.AreSame(marker,target.GetComponent<BoxCollider>());
            Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert.IsNotNull(target.GetComponent<StandaloneInputModule>());
        }
        [Test] public void HapticValidationRejectsBeforeFeedbackOrPlayerCreation()
        {
            var ray=target.AddComponent<XRRayInteractor>();
            Assert.IsFalse(XRFeedbackCommands.Haptics(Id(ray),selectIntensity:2,confirm:true).Ok);
            var preview=XRFeedbackCommands.Haptics(Id(ray),dryRun:true);Assert.IsTrue(preview.Ok,preview.Error?.Message);
            Assert.IsNull(target.GetComponent<SimpleHapticFeedback>());Assert.IsNull(target.GetComponent<HapticImpulsePlayer>());
        }
        [Test] public void SocketHapticsDisclosesMissingOutput()
        {
            var socket=target.AddComponent<XRSocketInteractor>();var r=XRFeedbackCommands.Haptics(Id(socket),confirm:true);
            Assert.IsTrue(r.Ok,r.Error?.Message);Assert.IsNotEmpty(r.Result.Issues);
        }
        [Test] public void LayersRejectUnknownNameBeforeChangingMask()
        {
            var ray=target.AddComponent<XRRayInteractor>();var old=ray.interactionLayers.value;
            Assert.IsFalse(XRFeedbackCommands.Layers(Id(ray),"Default,DoesNotExist",confirm:true).Ok);Assert.AreEqual(old,ray.interactionLayers.value);
            Assert.IsTrue(XRFeedbackCommands.Layers(Id(ray),"Default",confirm:true).Ok);Assert.AreEqual(1,ray.interactionLayers.value);
        }
        [Test] public void XRGenericPersistentCallbackInvokesAndUndoRemoves()
        {
            var simple=target.AddComponent<XRSimpleInteractable>();var provider=target.AddComponent<XRTestHapticProvider>();
            var r=EventCommands.ListenerAdd(Id(simple),"selectEntered",Id(provider),"Callback",mode:"EditorAndRuntime");Assert.IsTrue(r.Ok,r.Error?.Message);
            simple.selectEntered.Invoke(new SelectEnterEventArgs());Assert.AreEqual(1,provider.Calls);
            Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert.AreEqual(0,simple.selectEntered.GetPersistentEventCount());
        }
        [Test] public void SceneReportIncludesInactiveAndActualPackageMetadata()
        {
            target.AddComponent<XRSimpleInteractable>();target.SetActive(false);
            var r=XRSetupCommands.Report(verbose:true);Assert.IsTrue(r.Ok,r.Error?.Message);
            Assert.IsTrue(r.Result.State.ContainsKey("packages"));Assert.IsTrue(r.Result.State.ContainsKey("components"));
        }
        [Test] public void SavedScenePreservesRigSocketAndTrackingConfiguration()
        {
            Assert.IsTrue(XRSetupCommands.CreateRig(confirm:true).Ok);Assert.IsTrue(XRInteractorCommands.Socket(Id(target),confirm:true).Ok);
            Assert.IsTrue(EditorSceneManager.SaveScene(target.scene,path));EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var origin=UnityEngine.Object.FindAnyObjectByType<XROrigin>();Assert.AreEqual(1.36144f,origin.CameraYOffset);
            Assert.IsNotEmpty(origin.Camera.GetComponent<TrackedPoseDriver>().positionInput.action.bindings);
            Assert.AreEqual(.15f,UnityEngine.Object.FindAnyObjectByType<XRSocketInteractor>().GetComponent<SphereCollider>().radius);
        }

    }
    public sealed class XRHapticsLifecycleContractTests : XRContractSceneFixture
    {
        [UnityTest] public IEnumerator HapticsRoutesActualSelectAndHoverEventsThroughPublicProvider()
        {
            var ray=target.AddComponent<XRRayInteractor>();var provider=target.AddComponent<XRTestHapticProvider>();
            var interactable=new GameObject("Haptic Target").AddComponent<XRSimpleInteractable>();
            var r=XRFeedbackCommands.Haptics(Id(ray),output:Id(provider),confirm:true);Assert.IsTrue(r.Ok,r.Error?.Message);
            yield return new EnterPlayMode();
            ray=UnityEngine.Object.FindAnyObjectByType<XRRayInteractor>();provider=ray.GetComponent<XRTestHapticProvider>();
            interactable=UnityEngine.Object.FindAnyObjectByType<XRSimpleInteractable>();
            var before=provider.Calls;
            ray.selectEntered.Invoke(new SelectEnterEventArgs{interactorObject=ray,interactableObject=interactable});
            Assert.AreEqual(before+1,provider.Calls);Assert.AreEqual(.5f,provider.Amplitude);Assert.AreEqual(.1f,provider.Duration);
            ray.hoverEntered.Invoke(new HoverEnterEventArgs{interactorObject=ray,interactableObject=interactable});
            Assert.AreEqual(before+2,provider.Calls);Assert.AreEqual(.1f,provider.Amplitude);Assert.AreEqual(.05f,provider.Duration);
            yield return new ExitPlayMode();
        }
    }

    public sealed class XROriginLifecycleContractTests : XRContractSceneFixture
    {
        [UnityTest] public IEnumerator NormalLifecycleResolvesMediatorTransformerOriginAndFloorOffset()
        {
            Assert.IsTrue(XRSetupCommands.CreateRig(confirm:true).Ok);var origin=UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            Assert.IsTrue(XRLocomotionCommands.Move(Id(origin),confirm:true).Ok);
            yield return new EnterPlayMode();
            origin=UnityEngine.Object.FindAnyObjectByType<XROrigin>();var move=origin.GetComponent<ContinuousMoveProvider>();
            Assert.AreSame(origin,move.mediator.bodyTransformer.xrOrigin);
            Assert.AreEqual(1.36144f,origin.CameraFloorOffsetObject.transform.localPosition.y,.0001f);
            yield return new ExitPlayMode();
        }
    }
}
