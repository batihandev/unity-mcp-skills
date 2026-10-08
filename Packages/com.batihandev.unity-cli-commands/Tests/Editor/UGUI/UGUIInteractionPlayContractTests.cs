using System.Collections;
using BatihanDev.UnityCliCommands.UGUI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Tests.UGUI
{
    public sealed class UGUIInteractionPlayContractTests
    {
        [UnityTest] public IEnumerator RealEventSystemMouseClickInvokesButtonAndChangesToggle()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var canvas=UIWidgetCommands.Canvas(name:"Interaction Canvas",confirm:true);Assert.That(canvas.Ok,Is.True,canvas.Error?.Message);
            var buttonResult=UIWidgetCommands.Button(name:"Interaction Button",parent:canvas.Result.Target,backend:"Legacy",confirm:true);Assert.That(buttonResult.Ok,Is.True,buttonResult.Error?.Message);
            var toggleResult=UIWidgetCommands.Toggle(name:"Interaction Toggle",parent:canvas.Result.Target,backend:"Legacy",confirm:true);Assert.That(toggleResult.Ok,Is.True,toggleResult.Error?.Message);
            Exact.Resolve<GameObject>(buttonResult.Result.Target).GetComponent<RectTransform>().anchoredPosition=new Vector2(-100,0);
            Exact.Resolve<GameObject>(toggleResult.Result.Target).GetComponent<RectTransform>().anchoredPosition=new Vector2(100,0);
            yield return new EnterPlayMode();
            yield return ExerciseControls();
            yield return new ExitPlayMode();
        }
        private static IEnumerator ExerciseControls()
        {
            var buttonObject = GameObject.Find("Interaction Button");
            var toggleObject = GameObject.Find("Interaction Toggle");
            Assert.That(buttonObject, Is.Not.Null, "Authored Button object did not survive EnterPlayMode.");
            Assert.That(toggleObject, Is.Not.Null, "Authored Toggle object did not survive EnterPlayMode.");
            var button = buttonObject.GetComponent<Button>();
            var toggle = toggleObject.GetComponent<Toggle>();
            Assert.That(button, Is.Not.Null, "Authored Button component did not survive EnterPlayMode.");
            Assert.That(toggle, Is.Not.Null, "Authored Toggle component did not survive EnterPlayMode.");
            Assert.That(button.onClick, Is.Not.Null, "Authored Button event did not survive EnterPlayMode.");
            var eventObject=new GameObject("Interaction EventSystem",typeof(EventSystem));var module=eventObject.AddComponent<InputSystemUIInputModule>();module.AssignDefaultActions();
            var clicks=0;button.onClick.AddListener(()=>clicks++);
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                yield return null;yield return null;
                var buttonPoint=RectTransformUtility.WorldToScreenPoint(null,button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center));
                yield return Click(mouse,buttonPoint);Assert.That(clicks,Is.EqualTo(1),"Real EventSystem raycast/click did not reach the authored Button.");
                var check=toggle.targetGraphic.rectTransform;var togglePoint=RectTransformUtility.WorldToScreenPoint(null,check.TransformPoint(check.rect.center));
                yield return Click(mouse,togglePoint);Assert.That(toggle.isOn,Is.True,"Real EventSystem click did not change the authored Toggle.");
            }
            finally {if(mouse.added)InputSystem.RemoveDevice(mouse);}
        }
        private static IEnumerator Click(Mouse mouse,Vector2 point)
        {
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(UnityEngine.Application.isPlaying)yield return new ExitPlayMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
}
