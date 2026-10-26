using BatihanDev.UnityCliCommands.Foundation;
using System.Collections.Generic;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Exact=BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
using H=BatihanDev.UnityCliCommands.XR.XRSceneAuthoring;
namespace BatihanDev.UnityCliCommands.XR
{
    public static class XRUICommands
    {
        [CliCommand("xr.event-system-ensure","Ensure XR UI input, removing only obsolete StandaloneInputModule components.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> EventSystem(string target=null,bool confirm=false,bool dryRun=false)
        {
            UnityEngine.EventSystems.EventSystem selected=null;
            return H.Run("xr.event-system-ensure",confirm,dryRun,()=>{selected=H.Singleton<UnityEngine.EventSystems.EventSystem>(target,true);return selected?.gameObject;},()=>{if(selected==null)selected=Undo.AddComponent<UnityEngine.EventSystems.EventSystem>(H.Create("EventSystem"));foreach(var module in selected.GetComponents<StandaloneInputModule>())Undo.DestroyObjectImmediate(module);H.Ensure<XRUIInputModule>(selected.gameObject);},()=>new Dictionary<string,object>{{"eventSystem",Exact.ExactId(selected)},{"xrInputModule",Exact.ExactId(selected?.GetComponent<XRUIInputModule>())},{"standaloneModules",selected==null?0:selected.GetComponents<StandaloneInputModule>().Length}});
        }
        [CliCommand("xr.canvas-convert","Convert one exact Canvas to tracked-device world-space UI.",Tags=new[]{"unity-cli-commands","xr"})]
        public static CommandResult<XRResult> Canvas(string target,bool confirm=false,bool dryRun=false)
        {
            UnityEngine.Canvas selected=null;
            return H.Run("xr.canvas-convert",confirm,dryRun,()=>{selected=H.Component<UnityEngine.Canvas>(target);return selected.gameObject;},()=>
            {
                Undo.RecordObject(selected,"Convert XR Canvas");var rect=selected.GetComponent<RectTransform>();Undo.RecordObject(rect,"Convert XR Canvas Layout");
                if(selected.renderMode!=RenderMode.WorldSpace){selected.renderMode=RenderMode.WorldSpace;rect.sizeDelta=new Vector2(400,300);rect.localScale=Vector3.one*.001f;}
                foreach(var raycaster in selected.GetComponents<GraphicRaycaster>())Undo.DestroyObjectImmediate(raycaster);
                H.Ensure<TrackedDeviceGraphicRaycaster>(selected.gameObject);
            },()=>{var rect=selected.GetComponent<RectTransform>();return new Dictionary<string,object>{{"canvas",Exact.ExactId(selected)},{"renderMode",selected.renderMode.ToString()},{"size",new[]{rect.sizeDelta.x,rect.sizeDelta.y}},{"scale",new[]{rect.localScale.x,rect.localScale.y,rect.localScale.z}},{"trackedRaycaster",Exact.ExactId(selected.GetComponent<TrackedDeviceGraphicRaycaster>())},{"standardRaycasters",selected.GetComponents<GraphicRaycaster>().Length}};});
        }
    }
}
