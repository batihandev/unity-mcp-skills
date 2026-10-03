using BatihanDev.UnityCliCommands.Foundation;
using System.Collections.Generic;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using Exact=BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
using H=BatihanDev.UnityCliCommands.XR.XRSceneAuthoring;
namespace BatihanDev.UnityCliCommands.XR
{
    public static class XRFeedbackCommands
    {
        [CliCommand("xr.haptics-configure","Configure event-driven haptic feedback and report actual output availability.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Haptics(string target,float selectIntensity=.5f,float selectDuration=.1f,float hoverIntensity=.1f,float hoverDuration=.05f,string output=null,bool confirm=false,bool dryRun=false)
        {
            XRBaseInteractor interactor=null;UnityEngine.Component provider=null;SimpleHapticFeedback feedback=null;HapticImpulsePlayer player=null;
            return H.Run("xr.haptics-configure",confirm,dryRun,()=>
            {
                H.Nonnegative(selectIntensity,selectDuration,hoverIntensity,hoverDuration);H.Require(selectIntensity<=1&&hoverIntensity<=1,"Haptic amplitude must be in [0,1].");interactor=H.Component<XRBaseInteractor>(target);
                if(output!=null){provider=H.Component<UnityEngine.Component>(output);H.Require(provider is IXRHapticImpulseProvider,"Output Component must implement public IXRHapticImpulseProvider.");H.Require(provider.gameObject.scene==interactor.gameObject.scene,"Output must belong to the same scene.");}
                else provider=interactor.GetComponentInParent<IXRHapticImpulseProvider>(true) as UnityEngine.Component;
                H.Require(interactor.GetComponents<SimpleHapticFeedback>().Length<=1&&interactor.GetComponents<HapticImpulsePlayer>().Length<=1,"Multiple feedback/player components on this interactor.");return interactor.gameObject;
            },()=>
            {
                player=H.Ensure<HapticImpulsePlayer>(interactor.gameObject);
                if(provider!=null){var hapticOutput=new XRInputHapticImpulseProvider("Haptic",inputSourceMode:XRInputHapticImpulseProvider.InputSourceMode.ObjectReference);hapticOutput.SetObjectReference((IXRHapticImpulseProvider)provider);player.hapticOutput=hapticOutput;}
                feedback=H.Ensure<SimpleHapticFeedback>(interactor.gameObject);feedback.hapticImpulsePlayer=player;feedback.SetInteractorSource(interactor);
                feedback.playSelectEntered=true;feedback.selectEnteredData.amplitude=selectIntensity;feedback.selectEnteredData.duration=selectDuration;
                feedback.playHoverEntered=hoverIntensity>0;feedback.hoverEnteredData.amplitude=hoverIntensity;feedback.hoverEnteredData.duration=hoverDuration;
            },()=>
            {
                if(feedback==null)feedback=interactor.GetComponent<SimpleHapticFeedback>();if(player==null)player=interactor.GetComponent<HapticImpulsePlayer>();
                return new Dictionary<string,object>{{"interactor",Exact.ExactId(interactor)},{"feedback",Exact.ExactId(feedback)},{"source",Exact.ExactId(feedback==null?null:feedback.GetInteractorSource() as UnityEngine.Object)},{"player",Exact.ExactId(player)},{"outputMode",player==null?null:player.hapticOutput.inputSourceMode.ToString()},{"outputComponent",Exact.ExactId(player==null?null:player.hapticOutput.GetObjectReference() as UnityEngine.Object)},{"selectAmplitude",feedback==null?(float?)null:feedback.selectEnteredData.amplitude},{"selectDuration",feedback==null?(float?)null:feedback.selectEnteredData.duration},{"hoverAmplitude",feedback==null?(float?)null:feedback.hoverEnteredData.amplitude},{"hoverDuration",feedback==null?(float?)null:feedback.hoverEnteredData.duration},{"playSelectEntered",feedback==null?(bool?)null:feedback.playSelectEntered},{"playHoverEntered",feedback==null?(bool?)null:feedback.playHoverEntered}};
            },()=>provider==null?new[]{"No explicit or parent haptic output provider resolved. Existing/default output configuration is retained; hardware readiness is unverified."}:System.Array.Empty<string>());
        }
        [CliCommand("xr.layers-set","Set a validated XRI InteractionLayerMask on one exact interactor or interactable.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Layers(string target,string layers="Default",bool confirm=false,bool dryRun=false)
        {
            UnityEngine.Component component=null;int mask=0;string[] names=null;
            return H.Run("xr.layers-set",confirm,dryRun,()=>
            {
                component=H.Component<UnityEngine.Component>(target);H.Require(component is XRBaseInteractor||component is XRBaseInteractable,"Select an exact XR interactor or interactable Component.");
                names=(layers??"").Split(',').Select(n=>n.Trim()).ToArray();H.Require(names.Length>0&&names.All(n=>n.Length>0),"Supply nonempty XRI interaction layer names.");
                foreach(var name in names){var index=InteractionLayerMask.NameToLayer(name);H.Require(index>=0,"Unknown XRI interaction layer: "+name);mask|=1<<index;}return component.gameObject;
            },()=>{Undo.RecordObject(component,"Set XR Interaction Layers");if(component is XRBaseInteractor i)i.interactionLayers=mask;else ((XRBaseInteractable)component).interactionLayers=mask;},()=>
            {
                var actual=component is XRBaseInteractor i?i.interactionLayers.value:((XRBaseInteractable)component).interactionLayers.value;
                return new Dictionary<string,object>{{"component",Exact.ExactId(component)},{"mask",actual},{"names",Enumerable.Range(0,32).Where(index=>(actual&(1<<index))!=0).Select(InteractionLayerMask.LayerToName).ToArray()},{"requestedNames",names}};
            });
        }
    }
}
