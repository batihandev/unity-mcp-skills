using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
using UnityComponent = UnityEngine.Component;

namespace BatihanDev.UnityCliCommands.XR
{
    [Serializable] public sealed class XRResult
    {
        public string Target { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public bool SceneDirty { get; set; }
        public Dictionary<string,object> State { get; set; } = new Dictionary<string,object>();
        public string[] Issues { get; set; } = Array.Empty<string>();
        public string HardwareStatus { get; set; } = "Authoring only. XR provider, device support, and physical hardware operation are separate prerequisites.";
    }
    internal static class XRSceneAuthoring
    {
        internal static string Schema(string command) => "unity."+command+"@1";
        internal static CommandResult<XRResult> Refuse(string command,Exception ex) => CommandResult<XRResult>.Failure(Schema(command),"XR_REQUEST_INVALID",ex.Message);
        internal static bool Regular(UnityEngine.Object value)
        {
            var go=value as GameObject ?? (value as UnityComponent)?.gameObject;
            return go!=null && !EditorUtility.IsPersistent(go) && go.scene.IsValid() && go.scene.isLoaded &&
                !EditorSceneManager.IsPreviewSceneObject(go) && PrefabStageUtility.GetPrefabStage(go)==null;
        }
        internal static T[] All<T>(bool inactive=true) where T:UnityComponent => UnityEngine.Object.FindObjectsByType<T>(inactive?FindObjectsInactive.Include:FindObjectsInactive.Exclude).Where(c=>Regular(c)).OrderBy(c=>Exact.ExactId(c),StringComparer.Ordinal).ToArray();
        internal static GameObject Object(string reference)
        {
            var go=Exact.Resolve<GameObject>(reference) ?? Exact.Resolve<UnityComponent>(reference)?.gameObject;
            Require(Regular(go),"Supply an exact object in a loaded regular scene.");return go;
        }
        internal static T Component<T>(string reference) where T:UnityComponent
        {
            var exact=Exact.Resolve<T>(reference);Require(Regular(exact),"Supply an exact "+typeof(T).Name+" Component in a loaded regular scene.");return exact;
        }
        internal static T Singleton<T>(string reference=null,bool allowMissing=false) where T:UnityComponent
        {
            if(reference!=null)return Component<T>(reference);
            var all=All<T>();Require(all.Length<=1,"Multiple "+typeof(T).Name+" objects; supply an exact Component.");
            Require(allowMissing||all.Length==1,"No "+typeof(T).Name+" found; author its dependency first.");return all.SingleOrDefault();
        }
        internal static void Require(bool condition,string message){if(!condition)throw new ArgumentException(message);}
        internal static void Finite(params float[] values)=>Require(values.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)),"All numbers must be finite.");
        internal static void Nonnegative(params float[] values){Finite(values);Require(values.All(v=>v>=0),"Values must be nonnegative.");}
        internal static T EnumName<T>(string value) where T:struct
        {
            var name=Enum.GetNames(typeof(T)).SingleOrDefault(n=>string.Equals(n,value,StringComparison.OrdinalIgnoreCase));
            Require(name!=null,"Use one named "+typeof(T).Name+" value: "+string.Join(", ",Enum.GetNames(typeof(T))));return (T)Enum.Parse(typeof(T),name);
        }
        internal static Vector3 Vector(string value)
        {
            var parts=(value??"").Split(',');Require(parts.Length==3,"Offset requires three comma-separated numbers.");
            var n=parts.Select(p=>float.Parse(p,NumberStyles.Float,CultureInfo.InvariantCulture)).ToArray();Finite(n);return new Vector3(n[0],n[1],n[2]);
        }
        internal static T Ensure<T>(GameObject go) where T:UnityComponent
        { var c=go.GetComponent<T>();if(c==null)c=Undo.AddComponent<T>(go);else Undo.RecordObject(c,"Configure XR "+typeof(T).Name);return c; }
        internal static GameObject Create(string name,UnityEngine.Transform parent=null)
        {
            var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Create XR Object");
            if(parent!=null)
            {
                Undo.SetTransformParent(go.transform,parent,"Parent XR Object");
                go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
            }
            return go;
        }
        internal static void Collider(GameObject go,bool trigger=false,float radius=0,bool convex=false,bool mesh=true)
        {
            if(go.GetComponent<Collider>()!=null)return;
            if(radius>0){var sphere=Undo.AddComponent<SphereCollider>(go);sphere.radius=radius;sphere.isTrigger=trigger;return;}
            var filter=mesh?go.GetComponent<MeshFilter>():null;
            var source=filter==null?null:filter.sharedMesh;
            if(mesh&&source!=null){var c=Undo.AddComponent<MeshCollider>(go);c.sharedMesh=source;c.convex=convex;c.isTrigger=trigger;}
            else Undo.AddComponent<BoxCollider>(go).isTrigger=trigger;
        }
        internal static string[] Issues(GameObject go)
        {
            if(go==null)return Array.Empty<string>();var issues=new List<string>();var c=go.GetComponent<Collider>();
            var needsTrigger=go.GetComponent<XRDirectInteractor>()!=null||go.GetComponent<XRSocketInteractor>()!=null;
            if(needsTrigger&&(c==null||!c.isTrigger))issues.Add("Direct/socket interactor requires a trigger collider; existing collider was preserved.");
            if(go.GetComponent<XRBaseInteractable>()!=null&&(c==null||c.isTrigger))issues.Add("Interactable needs a non-trigger detection collider; existing collider was preserved.");
            if(go.GetComponent<XRGrabInteractable>()!=null&&go.GetComponent<Rigidbody>()==null)issues.Add("Grab interactable has no Rigidbody.");
            if(c is MeshCollider mc&&!mc.convex&&go.GetComponent<Rigidbody>() is Rigidbody body&&!body.isKinematic)issues.Add("Dynamic mesh collider is not convex.");
            return issues.ToArray();
        }
        internal static Dictionary<string,object> Describe(UnityComponent c)
        {
            if(c==null)return new Dictionary<string,object>();
            var d=new Dictionary<string,object>{{"component",Exact.ExactId(c)},{"object",Exact.ExactId(c.gameObject)},{"name",c.gameObject.name},{"type",c.GetType().FullName},{"active",c.gameObject.activeInHierarchy},{"enabled",!(c is Behaviour b)||b.enabled},{"path",Path(c.transform)}};
            var col=c.GetComponent<Collider>();d["collider"]=col==null?null:new Dictionary<string,object>{{"component",Exact.ExactId(col)},{"type",col.GetType().Name},{"isTrigger",col.isTrigger},{"radius",col is SphereCollider s?(object)s.radius:null}};
            if(c is XRBaseInteractor i){d["manager"]=Exact.ExactId(i.interactionManager);d["interactionLayers"]=i.interactionLayers.value;}
            if(c is XRBaseInteractable a){d["manager"]=Exact.ExactId(a.interactionManager);d["interactionLayers"]=a.interactionLayers.value;d["selectMode"]=a.selectMode.ToString();d["isHovered"]=a.isHovered;d["isSelected"]=a.isSelected;}
            if(c is XRRayInteractor r){d["maxDistance"]=r.maxRaycastDistance;d["lineType"]=r.lineType.ToString();d["lineRenderer"]=Exact.ExactId(r.GetComponent<LineRenderer>());d["lineVisual"]=Exact.ExactId(r.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>());}
            if(c is XRSocketInteractor socket){d["showHoverMesh"]=socket.showInteractableHoverMeshes;d["recycleDelay"]=socket.recycleDelayTime;d["hoverSocketSnapping"]=socket.hoverSocketSnapping;d["socketActive"]=socket.socketActive;}
            if(c is XRGrabInteractable g){d["movementType"]=g.movementType.ToString();d["throwOnDetach"]=g.throwOnDetach;d["smoothPosition"]=g.smoothPosition;d["smoothRotation"]=g.smoothRotation;d["smoothPositionAmount"]=g.smoothPositionAmount;d["smoothRotationAmount"]=g.smoothRotationAmount;d["trackPosition"]=g.trackPosition;d["trackRotation"]=g.trackRotation;d["attachTransform"]=Exact.ExactId(g.attachTransform);var rb=g.GetComponent<Rigidbody>();d["useGravity"]=rb==null?(object)null:rb.useGravity;d["isKinematic"]=rb==null?(object)null:rb.isKinematic;}
            if(c is BaseTeleportationInteractable t){d["matchOrientation"]=t.matchOrientation.ToString();d["teleportationProvider"]=Exact.ExactId(t.teleportationProvider);}
            if(c is TeleportationAnchor anchor)d["destination"]=Exact.ExactId(anchor.teleportAnchorTransform);
            if(c is LocomotionProvider p){var mediator=p.mediator;d["mediator"]=Exact.ExactId(mediator);d["lifecycleTransformer"]=Exact.ExactId(mediator==null?null:mediator.bodyTransformer);var bt=mediator==null?null:mediator.GetComponent<XRBodyTransformer>();d["configuredTransformer"]=Exact.ExactId(bt);d["origin"]=Exact.ExactId(bt==null?null:bt.xrOrigin);}
            if(c is ContinuousMoveProvider m){d["speed"]=m.moveSpeed;d["strafe"]=m.enableStrafe;d["fly"]=m.enableFly;DescribeReaders(d,m.leftHandMoveInput,m.rightHandMoveInput);}
            if(c is SnapTurnProvider snap){d["turnAmount"]=snap.turnAmount;d["enableTurnLeftRight"]=snap.enableTurnLeftRight;d["enableTurnAround"]=snap.enableTurnAround;DescribeReaders(d,snap.leftHandTurnInput,snap.rightHandTurnInput);}
            if(c is ContinuousTurnProvider turn){d["turnSpeed"]=turn.turnSpeed;DescribeReaders(d,turn.leftHandTurnInput,turn.rightHandTurnInput);}
            return d;
        }
        private static void DescribeReaders(Dictionary<string,object> state,XRInputValueReader<Vector2> left,XRInputValueReader<Vector2> right)
        {
            var leftInput=ReaderSnapshot(left);var rightInput=ReaderSnapshot(right);
            state["leftInput"]=leftInput;state["rightInput"]=rightInput;
            state["leftBindings"]=leftInput["bindings"];state["rightBindings"]=rightInput["bindings"];
        }
        private static Dictionary<string,object> ReaderSnapshot(XRInputValueReader<Vector2> reader)
        {
            var inline=reader?.inputAction;var reference=reader?.inputActionReference;
            var referenced=reference==null?null:reference.action;
            var effective=reader?.inputSourceMode==XRInputValueReader.InputSourceMode.InputAction?inline:
                reader?.inputSourceMode==XRInputValueReader.InputSourceMode.InputActionReference?referenced:null;
            return new Dictionary<string,object>
            {
                {"mode",reader?.inputSourceMode.ToString()},
                {"inlineActionName",inline?.name},{"inlineBindings",BindingPaths(inline)},
                {"reference",Exact.ExactId(reference)},{"referencedActionName",referenced?.name},{"referenceBindings",BindingPaths(referenced)},
                {"actionName",effective?.name},{"bindings",BindingPaths(effective)},
                {"manualValue",reader==null?null:new[]{reader.manualValue.x,reader.manualValue.y}}
            };
        }
        private static string[] BindingPaths(InputAction action)=>action==null?Array.Empty<string>():action.bindings.Select(binding=>binding.effectivePath).ToArray();
        internal static string Path(UnityEngine.Transform transform)=>transform.parent==null?transform.name:Path(transform.parent)+"/"+transform.name;
        internal static CommandResult<XRResult> Run(string command,bool confirm,bool dryRun,Func<GameObject> validate,Action apply,Func<Dictionary<string,object>> read,Func<string[]> issues=null)
        {
            var compatibility=CompatibilityPolicy.CheckInstalled();if(!compatibility.Ok)return CommandResult<XRResult>.Failure(Schema(command),compatibility.Error);
            int group=-1;
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode,"XR scene authoring requires Edit mode.");
                Require(PrefabStageUtility.GetCurrentPrefabStage()==null,"Close Prefab Stage before XR scene authoring.");
                var go=validate();var scene=go==null?SceneManager.GetActiveScene():go.scene;
                Require(scene.IsValid()&&scene.isLoaded&&!EditorSceneManager.IsPreviewScene(scene),"A loaded regular scene is required.");
                Require(dryRun||confirm,"Set confirm=true or dryRun=true.");
                if(!dryRun)
                {
                    Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName(command);apply();
                    if(go!=null)foreach(var c in go.GetComponentsInChildren<UnityComponent>(true))if(c!=null){EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
                    EditorSceneManager.MarkSceneDirty(scene);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);
                }
                return CommandResult<XRResult>.Success(Schema(command),new XRResult{Target=Exact.ExactId(go),Applied=!dryRun,DryRun=dryRun,SceneDirty=scene.isDirty,State=read(),Issues=issues?.Invoke()??Issues(go)});
            }
            catch(Exception ex){if(group>=0)Undo.RevertAllDownToGroup(group);return Refuse(command,ex);}
        }
    }
}
