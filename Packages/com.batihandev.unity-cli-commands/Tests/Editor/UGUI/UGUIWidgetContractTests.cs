using System.Collections;
using System.Linq;
using BatihanDev.UnityCliCommands.UGUI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
namespace BatihanDev.UnityCliCommands.Tests.UGUI
{
    public sealed class UGUIWidgetContractTests : UGUIContractFixture
    {
        [TestCase("Canvas")][TestCase("Panel")][TestCase("Button")][TestCase("Text")]
        [TestCase("Image")][TestCase("InputField")][TestCase("Slider")][TestCase("Toggle")]
        [TestCase("Dropdown")][TestCase("ScrollView")][TestCase("RawImage")][TestCase("Scrollbar")]
        public void FactoryDefaultsAndReferencesAreActual(string kind)
        {
            var result = Widget(kind); var go = Created(result); var rect = go.GetComponent<RectTransform>();
            Assert.That(rect, Is.Not.Null);
            if (kind != "Canvas") { Assert.That(go.GetComponentInParent<Canvas>(), Is.Not.Null); Assert.That(result.Result.Parent, Is.EqualTo(Id(go.transform.parent.gameObject))); }
            switch(kind)
            {
                case "Canvas":
                    Assert.That(go.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                    Assert.That(go.GetComponent<CanvasScaler>(), Is.Not.Null); Assert.That(go.GetComponent<GraphicRaycaster>(), Is.Not.Null); break;
                case "Panel":
                    Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero)); Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
                    Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero)); Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
                    Assert.That(go.GetComponent<Image>().color, Is.EqualTo(new Color(1,1,1,.5f))); break;
                case "Button":
                    Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(160,30))); Assert.That(go.GetComponent<Button>().targetGraphic, Is.EqualTo(go.GetComponent<Image>()));
                    var label=go.GetComponentInChildren<Text>(); Assert.That(label.text, Is.EqualTo("Button")); Assert.That(label.alignment, Is.EqualTo(TextAnchor.MiddleCenter)); CheckText(label); break;
                case "Text":
                    Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(200,50))); Assert.That(go.GetComponent<Text>().text, Is.EqualTo("New Text")); CheckText(go.GetComponent<Text>()); break;
                case "Image": Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(100,100))); Assert.That(go.GetComponent<Image>().sprite, Is.Null); break;
                case "RawImage": Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(100,100))); Assert.That(go.GetComponent<RawImage>().texture, Is.Null); Assert.That(result.Result.State["hasTexture"], Is.False); break;
                case "InputField":
                    Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(200,30))); var input=go.GetComponent<InputField>();
                    Assert.That(input.targetGraphic, Is.EqualTo(go.GetComponent<Image>())); CheckText(input.textComponent);
                    var ph=(Text)input.placeholder; Assert.That(ph.text, Is.EqualTo("Enter text...")); Assert.That(ph.color, Is.EqualTo(new Color(.5f,.5f,.5f,1))); Assert.That(ph.fontStyle, Is.EqualTo(FontStyle.Italic)); Assert.That(ph.fontSize, Is.EqualTo(14)); break;
                case "Slider":
                    Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(160,20))); var slider=go.GetComponent<Slider>();
                    Assert.That(slider.minValue, Is.Zero); Assert.That(slider.maxValue, Is.EqualTo(1)); Assert.That(slider.value, Is.EqualTo(.5f));
                    Assert.That(slider.fillRect, Is.Not.Null); Assert.That(slider.handleRect, Is.Not.Null); Assert.That(slider.targetGraphic, Is.EqualTo(slider.handleRect.GetComponent<Image>())); break;
                case "Toggle":
                    Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(160,20))); var toggle=go.GetComponent<Toggle>();
                    Assert.That(toggle.isOn, Is.False); Assert.That(toggle.targetGraphic, Is.Not.Null); Assert.That(toggle.graphic, Is.Not.Null); Assert.That(toggle.graphic, Is.Not.EqualTo(toggle.targetGraphic));
                    var toggleLabel=go.GetComponentInChildren<Text>(); Assert.That(toggleLabel.text, Is.EqualTo("Toggle")); CheckText(toggleLabel); break;
                case "Dropdown":
                    Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(160,30))); var dropdown=go.GetComponent<Dropdown>();
                    Assert.That(go.transform.Find("Arrow").GetComponent<Image>().color, Is.EqualTo(new Color(.2f,.2f,.2f,1)));
                    Assert.That(dropdown.captionText, Is.Not.Null); Assert.That(dropdown.itemText, Is.Not.Null); Assert.That(dropdown.targetGraphic, Is.Not.Null);
                    Assert.That(dropdown.options.Select(o=>o.text), Is.EqualTo(new[]{"A","B","C"})); Assert.That(dropdown.captionText.text, Is.EqualTo("A"));
                    Assert.That(dropdown.template.gameObject.activeSelf, Is.False); Assert.That(dropdown.template.sizeDelta.y, Is.EqualTo(150)); Assert.That(dropdown.itemText.GetComponentInParent<Toggle>(includeInactive: true), Is.Not.Null); break;
                case "ScrollView":
                    Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(300,200))); var scroll=go.GetComponent<ScrollRect>();
                    Assert.That(scroll.horizontal, Is.False); Assert.That(scroll.vertical, Is.True); Assert.That(scroll.movementType, Is.EqualTo(ScrollRect.MovementType.Elastic));
                    Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null); Assert.That(scroll.viewport.GetComponent<Mask>(), Is.Null);
                    Assert.That(scroll.viewport.anchorMin, Is.EqualTo(Vector2.zero)); Assert.That(scroll.viewport.anchorMax, Is.EqualTo(Vector2.one)); Assert.That(scroll.viewport.sizeDelta, Is.EqualTo(Vector2.zero));
                    Assert.That(scroll.content.parent, Is.EqualTo(scroll.viewport)); Assert.That(scroll.content.anchorMin, Is.EqualTo(new Vector2(0,1))); Assert.That(scroll.content.anchorMax, Is.EqualTo(Vector2.one));
                    Assert.That(scroll.content.pivot, Is.EqualTo(new Vector2(.5f,1))); Assert.That(scroll.content.sizeDelta, Is.EqualTo(new Vector2(0,400))); Assert.That(go.GetComponentsInChildren<Scrollbar>(true), Is.Empty); break;
                case "Scrollbar":
                    Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(20,160))); var bar=go.GetComponent<Scrollbar>();
                    Assert.That(go.GetComponent<Image>().color, Is.EqualTo(new Color(.8f,.8f,.8f,1)));
                    Assert.That(bar.direction, Is.EqualTo(Scrollbar.Direction.BottomToTop)); Assert.That(bar.value, Is.Zero); Assert.That(bar.size, Is.EqualTo(.2f)); Assert.That(bar.numberOfSteps, Is.Zero);
                    Assert.That(bar.handleRect, Is.Not.Null); Assert.That(bar.targetGraphic, Is.EqualTo(bar.handleRect.GetComponent<Image>())); break;
            }
        }
        private static void CheckText(Text text) { Assert.That(text, Is.Not.Null); Assert.That(text.font, Is.Not.Null); Assert.That(text.fontSize, Is.EqualTo(14)); Assert.That(text.color, Is.EqualTo(Color.black)); }
        [TestCase("Button")][TestCase("Text")][TestCase("InputField")][TestCase("Toggle")][TestCase("Dropdown")]
        public void ExplicitTMPUsesRenderableFontAndPublicFactoryReferences(string kind)
        {
            Assert.That(TMP_Settings.GetSettings(), Is.Not.Null, "Root prepares TMP resources before this suite.");
            var go=Created(Widget(kind,backend:"TMP")); var texts=go.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.That(texts.Length, Is.GreaterThan(0)); Assert.That(go.GetComponentsInChildren<Text>(true), Is.Empty);
            foreach(var text in texts) { Assert.That(text.font, Is.Not.Null); Assert.That(text.fontSharedMaterial, Is.Not.Null); Assert.That(text.font.atlasTexture, Is.Not.Null); Assert.That(text.fontSharedMaterial.shader.isSupported, Is.True); Assert.That(text.fontSize, Is.EqualTo(14)); }
            if(kind=="InputField") { var input=go.GetComponent<TMP_InputField>(); Assert.That(input.textComponent, Is.Not.Null); Assert.That(input.placeholder, Is.Not.Null); Assert.That(input.textViewport.GetComponent<RectMask2D>(), Is.Not.Null); Assert.That(input.textComponent.transform.parent, Is.EqualTo(input.textViewport)); Assert.That(((TMP_Text)input.placeholder).text, Is.EqualTo("Enter text...")); Assert.That(((TMP_Text)input.placeholder).fontStyle & FontStyles.Italic, Is.EqualTo(FontStyles.Italic)); Assert.That(input.textComponent.color, Is.EqualTo(Color.black)); }
            if(kind=="Dropdown") { var d=go.GetComponent<TMP_Dropdown>(); Assert.That(go.transform.Find("Arrow").GetComponent<Image>().color, Is.EqualTo(new Color(.2f,.2f,.2f,1))); Assert.That(d.captionText, Is.Not.Null); Assert.That(d.itemText, Is.Not.Null); Assert.That(d.template.gameObject.activeSelf, Is.False); Assert.That(d.template.sizeDelta.y, Is.EqualTo(150)); Assert.That(d.itemText.GetComponentInParent<Toggle>(includeInactive: true), Is.Not.Null); Assert.That(d.options.Select(o=>o.text), Is.EqualTo(new[]{"A","B","C"})); }
        }
        [TestCase("TMP")][TestCase("Auto")]
        public void MissingDefaultFontRefusesOrFallsBackBeforeCreatingCanvas(string backend)
        {
            Assert.That(TMP_Settings.GetSettings(), Is.Not.Null); var original=TMP_Settings.defaultFontAsset; var before=Objects();
            try { TMP_Settings.defaultFontAsset=null; var result=UIWidgetCommands.Text(backend:backend,confirm:true);
                if(backend=="TMP") { Assert.That(result.Ok, Is.False); Assert.That(Objects(), Is.EqualTo(before)); }
                else { var go=Created(result); Assert.That(go.GetComponent<Text>(), Is.Not.Null); Assert.That(go.GetComponent<Text>().font, Is.Not.Null); Assert.That(go.GetComponent<TMP_Text>(), Is.Null); Assert.That(result.Result.Backend, Is.EqualTo("Legacy")); }
            } finally { TMP_Settings.defaultFontAsset=original; }
        }
        [TestCase("Legacy")][TestCase("TMP")]
        public void DropdownTrimsOptionsAndCaptionReflectsActualFirstOption(string backend)
        {
            var go=Created(UIWidgetCommands.Dropdown(options:" First , Second , Third ",backend:backend,confirm:true));
            if(backend=="TMP") { var d=go.GetComponent<TMP_Dropdown>(); Assert.That(d.options.Select(o=>o.text), Is.EqualTo(new[]{"First","Second","Third"})); Assert.That(d.captionText.text, Is.EqualTo("First")); }
            else {var d=go.GetComponent<Dropdown>(); Assert.That(d.options.Select(o=>o.text), Is.EqualTo(new[]{"First","Second","Third"})); Assert.That(d.captionText.text, Is.EqualTo("First")); }
        }
        [TestCase("LeftToRight",160,20)][TestCase("RightToLeft",160,20)][TestCase("TopToBottom",20,160)]
        public void ScrollbarDirectionSetsPhysicalDimensions(string direction,float width,float height)
        { var go=Created(UIWidgetCommands.Scrollbar(direction:direction,confirm:true)); Assert.That(go.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(width,height))); Assert.That(go.GetComponent<Scrollbar>().direction.ToString(), Is.EqualTo(direction)); }
        [Test] public void ExactArbitraryParentIsPreservedWithoutCanvasSubstitution()
        { var parent=new GameObject("Plain parent"); var result=UIWidgetCommands.Button(parent:Id(parent),backend:"Legacy",confirm:true); var go=Created(result); Assert.That(go.transform.parent, Is.EqualTo(parent.transform)); Assert.That(go.GetComponentInParent<Canvas>(), Is.Null); Assert.That(result.Result.Canvas, Is.Null); }
        [Test] public void ExplicitInactiveCanvasParentReturnsActualParentAndCanvasIdentities()
        {
            var parent=new GameObject("Inactive Canvas",typeof(RectTransform),typeof(Canvas));
            var canvas=parent.GetComponent<Canvas>(); parent.SetActive(false);
            var result=UIWidgetCommands.Button(parent:Id(parent),backend:"Legacy",confirm:true); var go=Created(result);
            Assert.That(go.transform.parent, Is.EqualTo(parent.transform));
            Assert.That(go.activeInHierarchy, Is.False);
            Assert.That(go.GetComponentInParent<Canvas>(true), Is.EqualTo(canvas));
            Assert.That(result.Result.Parent, Is.EqualTo(Id(parent)));
            Assert.That(result.Result.Canvas, Is.EqualTo(Id(canvas)));
        }
        [Test] public void CameraModeReturnsActualCameraIdentity()
        { var camera=new GameObject("Camera",typeof(Camera)).GetComponent<Camera>(); var go=Created(UIWidgetCommands.Canvas(renderMode:"ScreenSpaceCamera",camera:Id(camera),confirm:true)); Assert.That(go.GetComponent<Canvas>().worldCamera, Is.EqualTo(camera)); Assert.That(go.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera)); }
        [UnityTest] public IEnumerator TwoFactoryCommandsHaveSeparateCompleteUndoAndRedo()
        {
            var first=Created(UIWidgetCommands.Button(backend:"Legacy",confirm:true)); var second=Created(UIWidgetCommands.Dropdown(backend:"Legacy",confirm:true));
            var firstId=Id(first); var secondId=Id(second); yield return null;
            Undo.PerformUndo(); yield return null; Assert.That(second==null, Is.True); Assert.That(first!=null, Is.True); Assert.That(first.GetComponentInParent<Canvas>(), Is.Not.Null);
            Undo.PerformUndo(); yield return null; Assert.That(Objects(), Is.Zero);
            Undo.PerformRedo(); yield return null; Assert.That(BatihanDev.UnityCliCommands.Editor.ExactObjectReference.Resolve<GameObject>(firstId), Is.Not.Null);
            Undo.PerformRedo(); yield return null; var restored=BatihanDev.UnityCliCommands.Editor.ExactObjectReference.Resolve<GameObject>(secondId); Assert.That(restored, Is.Not.Null); Assert.That(restored.GetComponent<Dropdown>().itemText, Is.Not.Null);
        }
    }
}
