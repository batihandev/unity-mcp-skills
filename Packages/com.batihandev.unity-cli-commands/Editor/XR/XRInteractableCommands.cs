using BatihanDev.UnityCliCommands.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using H=BatihanDev.UnityCliCommands.XR.XRSceneAuthoring;
namespace BatihanDev.UnityCliCommands.XR
{
    public static class XRInteractableCommands
    {
        [CliCommand("xr.interactable-grab","Configure a grab interactable and missing physics dependencies.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Grab(string target,string movementType="VelocityTracking",bool throwOnDetach=true,bool smoothPosition=true,bool smoothRotation=true,float smoothPositionAmount=5,float smoothRotationAmount=5,bool useGravity=true,bool isKinematic=false,string attachTransformOffset=null,string manager=null,bool confirm=false,bool dryRun=false)
        {
            GameObject go=null;XRInteractionManager owner=null;XRBaseInteractable.MovementType movement=default;Vector3 offset=default;
            return H.Run("xr.interactable-grab",confirm,dryRun,()=>{go=H.Object(target);movement=H.EnumName<XRBaseInteractable.MovementType>(movementType);H.Nonnegative(smoothPositionAmount,smoothRotationAmount);if(attachTransformOffset!=null)offset=H.Vector(attachTransformOffset);owner=H.Singleton<XRInteractionManager>(manager);H.Require(go.GetComponents<XRBaseInteractable>().All(c=>c is XRGrabInteractable),"A different interactable already owns this object.");return go;},()=>
            {
                if(go.GetComponent<Rigidbody>()==null){var body=H.Ensure<Rigidbody>(go);body.useGravity=useGravity;body.isKinematic=isKinematic;}
                H.Collider(go,convex:true);var grab=H.Ensure<XRGrabInteractable>(go);grab.interactionManager=owner;grab.movementType=movement;grab.throwOnDetach=throwOnDetach;grab.smoothPosition=smoothPosition;grab.smoothRotation=smoothRotation;grab.smoothPositionAmount=smoothPositionAmount;grab.smoothRotationAmount=smoothRotationAmount;
                if(attachTransformOffset!=null){var attach=H.Create("Attach Point",go.transform);attach.transform.localPosition=offset;attach.transform.localRotation=Quaternion.identity;grab.attachTransform=attach.transform;}
            },()=>H.Describe(go.GetComponent<XRGrabInteractable>()));
        }
        [CliCommand("xr.interactable-simple","Configure a simple interactable with a missing detection collider.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Simple(string target,string manager=null,bool confirm=false,bool dryRun=false)
        {
            GameObject go=null;XRInteractionManager owner=null;
            return H.Run("xr.interactable-simple",confirm,dryRun,()=>{go=H.Object(target);owner=H.Singleton<XRInteractionManager>(manager);H.Require(go.GetComponents<XRBaseInteractable>().All(c=>c is XRSimpleInteractable),"A different interactable already owns this object.");return go;},()=>{H.Collider(go,mesh:false);H.Ensure<XRSimpleInteractable>(go).interactionManager=owner;},()=>H.Describe(go.GetComponent<XRSimpleInteractable>()));
        }
        [CliCommand("xr.interactable-configure","Apply only supplied supported JSON fields to one exact interactable.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Configure(string target,string properties,bool confirm=false,bool dryRun=false)
        {
            XRBaseInteractable component=null;XRGrabInteractable grab=null;JObject fields=null;var actions=new List<Action>();
            return H.Run("xr.interactable-configure",confirm,dryRun,()=>
            {
                component=H.Component<XRBaseInteractable>(target);H.Require(component is XRGrabInteractable||component is XRSimpleInteractable,"Select a grab or simple interactable.");grab=component as XRGrabInteractable;
                fields=JObject.Parse(properties??"",new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                foreach(var field in fields.Properties())
                {
                    var n=field.Name;var value=field.Value;H.Require(n=="selectMode"||grab!=null,"Grab-only options are not supported on simple interactables.");
                    if(n=="selectMode"){H.Require(value.Type==JTokenType.String,"selectMode must be a string.");var mode=H.EnumName<InteractableSelectMode>((string)value);actions.Add(()=>component.selectMode=mode);}
                    else if(n=="movementType"){H.Require(value.Type==JTokenType.String,"movementType must be a string.");var movement=H.EnumName<XRBaseInteractable.MovementType>((string)value);actions.Add(()=>grab.movementType=movement);}
                    else if(n=="smoothPositionAmount"||n=="smoothRotationAmount")
                    {
                        H.Require(value.Type==JTokenType.Float||value.Type==JTokenType.Integer,"Smoothing amounts must be numbers.");var amount=(float)value;H.Nonnegative(amount);actions.Add(()=>{if(n=="smoothPositionAmount")grab.smoothPositionAmount=amount;else grab.smoothRotationAmount=amount;});
                    }
                    else
                    {
                        H.Require(new[]{"throwOnDetach","smoothPosition","smoothRotation","trackPosition","trackRotation"}.Contains(n),"Unsupported property: "+n);H.Require(value.Type==JTokenType.Boolean,"Flags must be JSON booleans.");var flag=(bool)value;
                        actions.Add(()=>{switch(n){case "throwOnDetach":grab.throwOnDetach=flag;break;case "smoothPosition":grab.smoothPosition=flag;break;case "smoothRotation":grab.smoothRotation=flag;break;case "trackPosition":grab.trackPosition=flag;break;case "trackRotation":grab.trackRotation=flag;break;}});
                    }
                }
                return component.gameObject;
            },()=>{UnityEditor.Undo.RecordObject(component,"Configure XR Interactable");foreach(var action in actions)action();},()=>{var state=H.Describe(component);state["suppliedProperties"]=fields.Properties().Select(p=>p.Name).ToArray();return state;});
        }
    }
}
