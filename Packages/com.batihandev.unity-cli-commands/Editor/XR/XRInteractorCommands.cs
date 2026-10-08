using BatihanDev.UnityCliCommands.Foundation;
using System.Collections.Generic;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using H=BatihanDev.UnityCliCommands.XR.XRSceneAuthoring;
namespace BatihanDev.UnityCliCommands.XR
{
    public static class XRInteractorCommands
    {
        [CliCommand("xr.interactor-ray","Configure a ray interactor and optional line visual on one exact scene object.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Ray(string target,float maxDistance=30,string lineType="StraightLine",bool addLineVisual=true,string manager=null,bool confirm=false,bool dryRun=false)
        {
            Shader lineShader=null;GameObject go=null;XRInteractionManager owner=null;XRRayInteractor ray=null;XRRayInteractor.LineType parsed=default;
            return H.Run("xr.interactor-ray",confirm,dryRun,()=>{go=H.Object(target);H.Nonnegative(maxDistance);parsed=H.EnumName<XRRayInteractor.LineType>(lineType);if(go.GetComponent<LineRenderer>()==null){lineShader=Shader.Find("Sprites/Default");H.Require(lineShader!=null,"Sprites/Default shader unavailable for ray line.");}owner=H.Singleton<XRInteractionManager>(manager);return go;},()=>
            {
                ray=H.Ensure<XRRayInteractor>(go);ray.interactionManager=owner;ray.maxRaycastDistance=maxDistance;ray.lineType=parsed;
                if(go.GetComponent<LineRenderer>()==null){var line=H.Ensure<LineRenderer>(go);line.startWidth=line.endWidth=.01f;line.startColor=Color.white;line.endColor=new Color(1,1,1,.5f);var material=new Material(lineShader){name="XR Ray Line"};UnityEditor.Undo.RegisterCreatedObjectUndo(material,"Create XR Line Material");line.sharedMaterial=material;}
                if(addLineVisual)H.Ensure<XRInteractorLineVisual>(go).lineWidth=.01f;
            },()=>H.Describe(ray??go.GetComponent<XRRayInteractor>()));
        }
        [CliCommand("xr.interactor-direct","Configure a direct interactor, preserving existing colliders.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Direct(string target,float radius=.1f,string manager=null,bool confirm=false,bool dryRun=false)
        {
            GameObject go=null;XRInteractionManager owner=null;
            return H.Run("xr.interactor-direct",confirm,dryRun,()=>{go=H.Object(target);H.Nonnegative(radius);H.Require(radius>0,"Radius must be positive.");owner=H.Singleton<XRInteractionManager>(manager);return go;},()=>{H.Collider(go,true,radius);H.Ensure<XRDirectInteractor>(go).interactionManager=owner;},()=>H.Describe(go.GetComponent<XRDirectInteractor>()));
        }
        [CliCommand("xr.interactor-socket","Configure supported socket hover-mesh and recycle-delay properties.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Socket(string target,bool showHoverMesh=true,float recycleDelay=1,string manager=null,bool confirm=false,bool dryRun=false)
        {
            GameObject go=null;XRInteractionManager owner=null;
            return H.Run("xr.interactor-socket",confirm,dryRun,()=>{go=H.Object(target);H.Nonnegative(recycleDelay);owner=H.Singleton<XRInteractionManager>(manager);return go;},()=>{H.Collider(go,true,.15f);var socket=H.Ensure<XRSocketInteractor>(go);socket.interactionManager=owner;socket.showInteractableHoverMeshes=showHoverMesh;socket.recycleDelayTime=recycleDelay;},()=>H.Describe(go.GetComponent<XRSocketInteractor>()));
        }
    }
}
