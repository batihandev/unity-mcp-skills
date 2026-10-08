using BatihanDev.UnityCliCommands.Foundation;
using System.Collections.Generic;
using System.Linq;
using Unity.Pipeline.Commands;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using H=BatihanDev.UnityCliCommands.XR.XRSceneAuthoring;
namespace BatihanDev.UnityCliCommands.XR
{
    public static class XRLocomotionCommands
    {
        private static XROrigin Origin(string target)
        {
            if(target==null)return H.Singleton<XROrigin>();
            var go=H.Object(target);var origins=go.GetComponents<XROrigin>();H.Require(origins.Length==1,"Target must contain exactly one XROrigin.");return origins[0];
        }
        private static void ValidateDependencies(XROrigin origin)
        {
            H.Require(origin.GetComponents<LocomotionMediator>().Length<=1&&origin.GetComponents<XRBodyTransformer>().Length<=1,"Ambiguous locomotion dependency components.");
            var body=origin.GetComponent<XRBodyTransformer>();H.Require(body==null||body.xrOrigin==null||body.xrOrigin==origin,"Existing body transformer belongs to another origin.");
        }
        private static T Wire<T>(XROrigin origin) where T:LocomotionProvider
        {
            var body=H.Ensure<XRBodyTransformer>(origin.gameObject);body.xrOrigin=origin;
            var mediator=H.Ensure<LocomotionMediator>(origin.gameObject);var provider=H.Ensure<T>(origin.gameObject);provider.mediator=mediator;return provider;
        }
        private static XRInputValueReader<Vector2> Stick(string side,string name)
        {
            var reader=new XRInputValueReader<Vector2>(name,XRInputValueReader.InputSourceMode.InputAction);
            reader.inputAction=new InputAction(name,InputActionType.Value,"<XRController>{"+side+"}/primary2DAxis",expectedControlType:"Vector2");return reader;
        }
        [CliCommand("xr.locomotion-teleport","Wire a teleportation provider to one exact XR Origin.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Teleport(string target=null,bool confirm=false,bool dryRun=false)
        {
            XROrigin origin=null;
            return H.Run("xr.locomotion-teleport",confirm,dryRun,()=>{origin=Origin(target);ValidateDependencies(origin);return origin.gameObject;},()=>Wire<TeleportationProvider>(origin),()=>H.Describe(origin.GetComponent<TeleportationProvider>()));
        }
        [CliCommand("xr.locomotion-move","Wire continuous locomotion and preserve configured controller inputs.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Move(string target=null,float moveSpeed=2,bool enableStrafe=true,bool enableFly=false,bool confirm=false,bool dryRun=false)
        {
            XROrigin origin=null;
            return H.Run("xr.locomotion-move",confirm,dryRun,()=>{H.Nonnegative(moveSpeed);origin=Origin(target);ValidateDependencies(origin);return origin.gameObject;},()=>{var added=origin.GetComponent<ContinuousMoveProvider>()==null;var move=Wire<ContinuousMoveProvider>(origin);move.moveSpeed=moveSpeed;move.enableStrafe=enableStrafe;move.enableFly=enableFly;if(added){move.leftHandMoveInput=Stick("LeftHand","Left Move");move.rightHandMoveInput=Stick("RightHand","Right Move");}},()=>H.Describe(origin.GetComponent<ContinuousMoveProvider>()));
        }
        [CliCommand("xr.locomotion-turn","Wire a named snap or continuous turn provider without removing other providers.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Turn(string target=null,string turnType="Snap",float turnAmount=45,float turnSpeed=90,bool confirm=false,bool dryRun=false)
        {
            XROrigin origin=null;LocomotionProvider provider=null;bool snap=false;
            return H.Run("xr.locomotion-turn",confirm,dryRun,()=>{H.Require(string.Equals(turnType,"Snap",System.StringComparison.OrdinalIgnoreCase)||string.Equals(turnType,"Continuous",System.StringComparison.OrdinalIgnoreCase),"Use Snap or Continuous.");snap=string.Equals(turnType,"Snap",System.StringComparison.OrdinalIgnoreCase);H.Nonnegative(turnAmount,turnSpeed);origin=Origin(target);ValidateDependencies(origin);return origin.gameObject;},()=>
            {
                if(snap){var added=origin.GetComponent<SnapTurnProvider>()==null;var p=Wire<SnapTurnProvider>(origin);p.turnAmount=turnAmount;if(added){p.leftHandTurnInput=Stick("LeftHand","Left Turn");p.rightHandTurnInput=Stick("RightHand","Right Turn");}provider=p;}
                else{var added=origin.GetComponent<ContinuousTurnProvider>()==null;var p=Wire<ContinuousTurnProvider>(origin);p.turnSpeed=turnSpeed;if(added){p.leftHandTurnInput=Stick("LeftHand","Left Turn");p.rightHandTurnInput=Stick("RightHand","Right Turn");}provider=p;}
            },()=>H.Describe(provider??(snap?(LocomotionProvider)origin.GetComponent<SnapTurnProvider>():origin.GetComponent<ContinuousTurnProvider>())));
        }
        [CliCommand("xr.teleport-area","Configure a teleport surface with an explicit or unique provider.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Area(string target,string matchOrientation="WorldSpaceUp",string provider=null,string manager=null,bool confirm=false,bool dryRun=false)
        {
            GameObject go=null;TeleportationProvider teleport=null;XRInteractionManager owner=null;MatchOrientation orientation=default;
            return H.Run("xr.teleport-area",confirm,dryRun,()=>{go=H.Object(target);orientation=H.EnumName<MatchOrientation>(matchOrientation);teleport=H.Singleton<TeleportationProvider>(provider,true);owner=H.Singleton<XRInteractionManager>(manager);H.Require(go.GetComponents<XRBaseInteractable>().All(c=>c is TeleportationArea),"Another interactable owns the target.");return go;},()=>{H.Collider(go);var area=H.Ensure<TeleportationArea>(go);area.interactionManager=owner;area.matchOrientation=orientation;area.teleportationProvider=teleport;},()=>H.Describe(go.GetComponent<TeleportationArea>()),()=>DestinationIssues(go,teleport));
        }
        [CliCommand("xr.teleport-anchor-create","Create a world-positioned teleport destination and visible indicator.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Anchor(string name="Teleport Anchor",float x=0,float y=0,float z=0,float rotY=0,string matchOrientation="TargetUpAndForward",string parent=null,string provider=null,string manager=null,bool confirm=false,bool dryRun=false)
        {
            Shader indicatorShader=null;GameObject go=null,parentGo=null;TeleportationProvider teleport=null;XRInteractionManager owner=null;MatchOrientation orientation=default;
            return H.Run("xr.teleport-anchor-create",confirm,dryRun,()=>{H.Require(!string.IsNullOrWhiteSpace(name),"Name is required.");H.Finite(x,y,z,rotY);orientation=H.EnumName<MatchOrientation>(matchOrientation);indicatorShader=Shader.Find("Sprites/Default");H.Require(indicatorShader!=null,"Sprites/Default shader is unavailable for the anchor indicator.");if(parent!=null)parentGo=H.Object(parent);teleport=H.Singleton<TeleportationProvider>(provider,true);owner=H.Singleton<XRInteractionManager>(manager);return parentGo;},()=>
            {
                go=H.Create(name,parentGo?.transform);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,rotY,0);
                var collider=H.Ensure<BoxCollider>(go);collider.size=new Vector3(1,.01f,1);
                var anchor=H.Ensure<TeleportationAnchor>(go);anchor.interactionManager=owner;anchor.matchOrientation=orientation;anchor.teleportationProvider=teleport;
                var destination=H.Create("Teleport Destination",go.transform);destination.transform.localPosition=Vector3.zero;destination.transform.localRotation=Quaternion.identity;anchor.teleportAnchorTransform=destination.transform;
                var visual=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Undo.RegisterCreatedObjectUndo(visual,"Create Anchor Indicator");visual.name="Anchor Visual";Undo.SetTransformParent(visual.transform,go.transform,"Parent Anchor Indicator");visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=new Vector3(1,.02f,1);Undo.DestroyObjectImmediate(visual.GetComponent<Collider>());
                var material=new Material(indicatorShader){name="XR Anchor Indicator",color=new Color(0,.8f,1,.5f)};Undo.RegisterCreatedObjectUndo(material,"Create Anchor Material");visual.GetComponent<MeshRenderer>().sharedMaterial=material;
                foreach(var c in go.GetComponentsInChildren<UnityEngine.Component>(true))EditorUtility.SetDirty(c);
            },()=>H.Describe(go?.GetComponent<TeleportationAnchor>()),()=>DestinationIssues(go,teleport));
        }
        private static string[] DestinationIssues(GameObject go,TeleportationProvider provider)=>H.Issues(go).Concat(provider==null?new[]{"No assigned teleportation provider; destination authoring is incomplete for runtime locomotion."}:System.Array.Empty<string>()).ToArray();
    }
}
