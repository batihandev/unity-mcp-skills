using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Exact=BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
using H=BatihanDev.UnityCliCommands.XR.XRSceneAuthoring;
namespace BatihanDev.UnityCliCommands.XR
{
    public static class XRSetupCommands
    {
        [CliCommand("xr.manager-ensure","Ensure a unique scene XR interaction manager.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Manager(string manager=null,string name="XR Interaction Manager",bool confirm=false,bool dryRun=false)
        {
            XRInteractionManager selected=null;
            return H.Run("xr.manager-ensure",confirm,dryRun,()=>{H.Require(!string.IsNullOrWhiteSpace(name),"Name is required.");selected=H.Singleton<XRInteractionManager>(manager,true);return selected?.gameObject;},()=>{if(selected==null)selected=Undo.AddComponent<XRInteractionManager>(H.Create(name));},()=>H.Describe(selected));
        }
        [CliCommand("xr.rig-create","Create an XR Origin hierarchy with inline tracked-pose bindings.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> CreateRig(string name="XR Origin",float x=0,float y=0,float z=0,float cameraYOffset=1.36144f,bool confirm=false,bool dryRun=false)
        {
            XROrigin origin=null;XRInteractionManager manager=null;
            return H.Run("xr.rig-create",confirm,dryRun,()=>{H.Require(!string.IsNullOrWhiteSpace(name),"Name is required.");H.Finite(x,y,z,cameraYOffset);manager=H.Singleton<XRInteractionManager>(allowMissing:true);return null;},()=>
            {
                var root=H.Create(name);root.transform.position=new Vector3(x,y,z);origin=Undo.AddComponent<XROrigin>(root);
                origin.Origin=root;origin.CameraYOffset=cameraYOffset;origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Device;
                var offset=H.Create("Camera Offset",root.transform);offset.transform.localPosition=Vector3.up*cameraYOffset;origin.CameraFloorOffsetObject=offset;
                var cam=H.Create("Main Camera",offset.transform);cam.tag="MainCamera";cam.transform.localPosition=Vector3.zero;
                var camera=Undo.AddComponent<Camera>(cam);camera.nearClipPlane=.01f;Undo.AddComponent<AudioListener>(cam);origin.Camera=camera;
                Pose(cam,"<XRHMD>","centerEyePosition","centerEyeRotation");
                Pose(H.Create("Left Controller",offset.transform),"<XRController>{LeftHand}","devicePosition","deviceRotation");
                Pose(H.Create("Right Controller",offset.transform),"<XRController>{RightHand}","devicePosition","deviceRotation");
                if(manager==null)manager=Undo.AddComponent<XRInteractionManager>(root);
                foreach(var component in root.GetComponentsInChildren<UnityEngine.Component>(true))EditorUtility.SetDirty(component);
            },()=>origin==null?new Dictionary<string,object>{{"name",name},{"cameraYOffset",cameraYOffset}}:new Dictionary<string,object>{{"origin",Exact.ExactId(origin)},{"object",Exact.ExactId(origin.gameObject)},{"camera",Exact.ExactId(origin.Camera)},{"offset",Exact.ExactId(origin.CameraFloorOffsetObject)},{"cameraYOffset",origin.CameraYOffset},{"actualOffsetY",origin.CameraFloorOffsetObject.transform.localPosition.y},{"position",new[]{origin.transform.position.x,origin.transform.position.y,origin.transform.position.z}},{"manager",Exact.ExactId(manager)},{"trackedPoses",origin.GetComponentsInChildren<TrackedPoseDriver>().Select(p=>new{component=Exact.ExactId(p),parent=Exact.ExactId(p.transform.parent),positionBindings=p.positionInput.action.bindings.Select(b=>b.path).ToArray(),rotationBindings=p.rotationInput.action.bindings.Select(b=>b.path).ToArray(),trackingBindings=p.trackingStateInput.action.bindings.Select(b=>b.path).ToArray()}).ToArray()}});
        }
        private static void Pose(GameObject go,string device,string position,string rotation)
        {
            var driver=Undo.AddComponent<TrackedPoseDriver>(go);
            driver.positionInput=new InputActionProperty(new InputAction("Position",InputActionType.Value,device+"/"+position,expectedControlType:"Vector3"));
            driver.rotationInput=new InputActionProperty(new InputAction("Rotation",InputActionType.Value,device+"/"+rotation,expectedControlType:"Quaternion"));
            driver.trackingStateInput=new InputActionProperty(new InputAction("Tracking State",InputActionType.Value,device+"/trackingState",expectedControlType:"Integer"));
        }
        private static IEnumerable<string> PoseIssues(XROrigin origin)
        {
            var prefix=origin.name+": ";var offset=origin.CameraFloorOffsetObject;var camera=origin.Camera;
            if(offset==null)yield return prefix+"missing assigned camera floor offset.";
            else if(offset.transform==origin.transform||!offset.transform.IsChildOf(origin.transform))
                yield return prefix+"misplaced camera floor offset; assign an offset below the XR Origin.";
            if(camera==null)yield return prefix+"missing assigned camera.";
            else
            {
                if(offset!=null&&!camera.transform.IsChildOf(offset.transform))
                    yield return prefix+"misplaced assigned camera; place it in the assigned floor offset subtree.";
                var driver=camera.GetComponent<TrackedPoseDriver>();
                if(driver==null)yield return prefix+"missing tracked pose driver on the assigned camera.";
                else foreach(var issue in UnboundPoseInputs(driver))yield return prefix+"camera pose has unbound "+issue+" input.";
            }
            var controllers=origin.GetComponentsInChildren<TrackedPoseDriver>(true)
                .Where(driver=>driver.GetComponent<Camera>()==null).GroupBy(driver=>driver.gameObject).Select(group=>group.First()).ToArray();
            if(controllers.Length<2)yield return prefix+"missing controller tracked pose drivers; at least two distinct non-camera drivers are required.";
            foreach(var driver in controllers)
            {
                if(offset!=null&&!driver.transform.IsChildOf(offset.transform))
                    yield return prefix+"misplaced controller pose "+H.Path(driver.transform)+"; place it in the assigned floor offset subtree.";
                foreach(var issue in UnboundPoseInputs(driver))yield return prefix+"controller pose "+H.Path(driver.transform)+" has unbound "+issue+" input.";
            }
        }
        private static IEnumerable<string> UnboundPoseInputs(TrackedPoseDriver driver)
        {
            if(!HasBinding(driver.positionInput))yield return "position";
            if(!HasBinding(driver.rotationInput))yield return "rotation";
            if(!HasBinding(driver.trackingStateInput))yield return "tracking state";
        }
        private static bool HasBinding(InputActionProperty property)=>property.action!=null&&property.action.bindings.Any(binding=>!string.IsNullOrWhiteSpace(binding.effectivePath));
        [CliCommand("xr.scene-report","Read actual XR package metadata, scene counts, and configuration issues.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Report(bool verbose=false,bool includeInactive=true)
        {
            var compatibility=CompatibilityPolicy.CheckInstalled();if(!compatibility.Ok)return CommandResult<XRResult>.Failure(H.Schema("xr.scene-report"),compatibility.Error);
            var components=H.All<UnityEngine.Component>(includeInactive).Where(c=>c is XRInteractionManager||c is XROrigin||c is XRBaseInteractor||c is XRBaseInteractable||c is LocomotionProvider||c is LocomotionMediator||c is XRBodyTransformer||c is EventSystem||c is Canvas||c is Camera||c is XRUIInputModule||c is TrackedDeviceGraphicRaycaster||c is TrackedPoseDriver||c is XRInteractorLineVisual).ToArray();
            var packages=UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Where(p=>p.name=="com.unity.xr.interaction.toolkit"||p.name=="com.unity.xr.core-utils"||p.name=="com.unity.inputsystem"||p.name=="com.unity.ugui").Select(p=>new{name=p.name,version=p.version,source=p.source.ToString()}).ToArray();
            var issues=components.SelectMany(c=>H.Issues(c.gameObject)).Distinct().ToList();
            var managerCount=components.Count(c=>c is XRInteractionManager);
            if(managerCount==0)issues.Add("No XR interaction manager.");
            if(managerCount>1)issues.Add("Multiple XR interaction managers ("+managerCount+"); supply an exact manager Component when authoring.");
            if(!components.Any(c=>c is XROrigin))issues.Add("No XR Origin.");
            if(!components.Any(c=>c is Camera camera&&camera.CompareTag("MainCamera")))issues.Add("No tagged Main Camera.");
            if(!components.Any(c=>c is XRUIInputModule))issues.Add("No XRUIInputModule.");
            foreach(var origin in components.OfType<XROrigin>())issues.AddRange(PoseIssues(origin));
            foreach(var provider in components.OfType<LocomotionProvider>())
            {
                var mediator=provider.mediator;var transformer=mediator==null?null:mediator.GetComponent<XRBodyTransformer>();
                if(transformer==null||transformer.xrOrigin==null)issues.Add(provider.name+": unresolved locomotion origin chain.");
            }
            return CommandResult<XRResult>.Success(H.Schema("xr.scene-report"),new XRResult{Issues=issues.ToArray(),State=new Dictionary<string,object>{{"packages",packages},{"includeInactive",includeInactive},{"total",components.Length},{"counts",new{managers=components.Count(c=>c is XRInteractionManager),origins=components.Count(c=>c is XROrigin),cameras=components.Count(c=>c is Camera),interactors=components.Count(c=>c is XRBaseInteractor),interactables=components.Count(c=>c is XRBaseInteractable),locomotion=components.Count(c=>c is LocomotionProvider),eventSystems=components.Count(c=>c is EventSystem),canvases=components.Count(c=>c is Canvas)}},{"components",components.Select(c=>verbose?H.Describe(c):new Dictionary<string,object>{{"component",Exact.ExactId(c)},{"type",c.GetType().FullName},{"path",H.Path(c.transform)},{"enabled",!(c is Behaviour b)||b.enabled},{"active",c.gameObject.activeInHierarchy}}).ToArray()}}});
        }
    }
}
