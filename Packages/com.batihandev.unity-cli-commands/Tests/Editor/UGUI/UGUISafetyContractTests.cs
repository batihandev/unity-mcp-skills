using BatihanDev.UnityCliCommands.Foundation;
using BatihanDev.UnityCliCommands.UGUI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace BatihanDev.UnityCliCommands.Tests.UGUI
{
    public sealed class UGUISafetyContractTests : UGUIContractFixture
    {
        [TestCase("Canvas")][TestCase("Panel")][TestCase("Button")][TestCase("Text")]
        [TestCase("Image")][TestCase("InputField")][TestCase("Slider")][TestCase("Toggle")]
        [TestCase("Dropdown")][TestCase("ScrollView")][TestCase("RawImage")][TestCase("Scrollbar")]
        public void DryRunCreatesNothingAndPreservesDirtyAndUndoGroup(string kind)
        {
            var before=Objects(); var dirty=SceneManager.GetActiveScene().isDirty; var group=Undo.GetCurrentGroup();
            var result=Widget(kind,confirm:false,dryRun:true); Assert.That(result.Ok, Is.True,result.Error?.Message); Assert.That(result.Result.DryRun, Is.True); Assert.That(result.Result.Applied, Is.False);
            Assert.That(Objects(), Is.EqualTo(before)); Assert.That(SceneManager.GetActiveScene().isDirty, Is.EqualTo(dirty)); Assert.That(Undo.GetCurrentGroup(), Is.EqualTo(group));
        }
        [TestCase("Canvas")][TestCase("Panel")][TestCase("Button")][TestCase("Text")]
        [TestCase("Image")][TestCase("InputField")][TestCase("Slider")][TestCase("Toggle")]
        [TestCase("Dropdown")][TestCase("ScrollView")][TestCase("RawImage")][TestCase("Scrollbar")]
        public void UnconfirmedCommandsRefuseWithoutObjectsDirtyOrUndoChanges(string kind)
        {
            var before=Objects(); var dirty=SceneManager.GetActiveScene().isDirty; var group=Undo.GetCurrentGroup();
            Assert.That(Widget(kind,confirm:false).Ok, Is.False); Assert.That(Objects(), Is.EqualTo(before)); Assert.That(SceneManager.GetActiveScene().isDirty, Is.EqualTo(dirty)); Assert.That(Undo.GetCurrentGroup(), Is.EqualTo(group));
        }
        [TestCase("BadParent")][TestCase("MultipleCanvases")][TestCase("BadSprite")][TestCase("BadTexture")]
        [TestCase("BadBackend")][TestCase("NumericMode")][TestCase("BadCamera")][TestCase("BadDirection")][TestCase("BadMovement")]
        [TestCase("NaNSize")][TestCase("InfiniteSize")][TestCase("NegativeSize")][TestCase("InvalidRange")][TestCase("OutOfRange")]
        [TestCase("NaNValue")][TestCase("BadScrollbarSize")][TestCase("NegativeSteps")][TestCase("BadFontSize")]
        public void InvalidCreationRequestsPrevalidateBeforeCanvasOrFactoryMutation(string request)
        {
            if(request=="MultipleCanvases") {new GameObject("C1",typeof(Canvas));new GameObject("C2",typeof(Canvas));}
            var before=Objects(); var group=Undo.GetCurrentGroup(); var dirty=SceneManager.GetActiveScene().isDirty; CommandResult<UIResult> result;
            switch(request)
            {
                case "BadParent": result=UIWidgetCommands.Button(parent:"99999999999999",confirm:true);break;
                case "MultipleCanvases":result=UIWidgetCommands.Image(confirm:true);break;
                case "BadSprite":result=UIWidgetCommands.Image(sprite:"Assets/Missing.png",confirm:true);break;
                case "BadTexture":result=UIWidgetCommands.RawImage(texture:"Assets/Missing.renderTexture",confirm:true);break;
                case "BadBackend":result=UIWidgetCommands.Text(backend:"other",confirm:true);break;
                case "NumericMode":result=UIWidgetCommands.Canvas(renderMode:"0",confirm:true);break;
                case "BadCamera":result=UIWidgetCommands.Canvas(renderMode:"ScreenSpaceCamera",camera:"99999999999999",confirm:true);break;
                case "BadDirection":result=UIWidgetCommands.Scrollbar(direction:"99",confirm:true);break;
                case "BadMovement":result=UIWidgetCommands.ScrollView(movementType:"99",confirm:true);break;
                case "NaNSize":result=UIWidgetCommands.Button(width:float.NaN,confirm:true);break;
                case "InfiniteSize":result=UIWidgetCommands.InputField(height:float.PositiveInfinity,confirm:true);break;
                case "NegativeSize":result=UIWidgetCommands.Image(width:-1,confirm:true);break;
                case "InvalidRange":result=UIWidgetCommands.Slider(minValue:2,maxValue:1,confirm:true);break;
                case "OutOfRange":result=UIWidgetCommands.Slider(value:2,confirm:true);break;
                case "NaNValue":result=UIWidgetCommands.Slider(value:float.NaN,confirm:true);break;
                case "BadScrollbarSize":result=UIWidgetCommands.Scrollbar(size:2,confirm:true);break;
                case "NegativeSteps":result=UIWidgetCommands.Scrollbar(numberOfSteps:-1,confirm:true);break;
                default:result=UIWidgetCommands.Text(fontSize:0,confirm:true);break;
            }
            Assert.That(result.Ok, Is.False); Assert.That(Objects(), Is.EqualTo(before)); Assert.That(Undo.GetCurrentGroup(), Is.EqualTo(group)); Assert.That(SceneManager.GetActiveScene().isDirty, Is.EqualTo(dirty));
        }
        [TestCase("BadLayout")][TestCase("BadSpacing")][TestCase("BadColumns")][TestCase("BadPadding")]
        public void InvalidLayoutLeavesExistingGroupAndFitterUntouched(string request)
        {
            var parent=Rect("Parent",new Vector2(100,100),Vector2.zero,Vector2.zero); var old=parent.gameObject.AddComponent<VerticalLayoutGroup>(); old.spacing=27;
            var fitter=parent.gameObject.AddComponent<ContentSizeFitter>(); fitter.horizontalFit=ContentSizeFitter.FitMode.MinSize;
            var group=Undo.GetCurrentGroup();var dirty=parent.gameObject.scene.isDirty;
            var result=UILayoutCommands.LayoutChildren(Id(parent.gameObject),layoutType:request=="BadLayout"?"other":"Grid",spacing:request=="BadSpacing"?float.NaN:10,gridColumns:request=="BadColumns"?0:3,paddingLeft:request=="BadPadding"?-1:0,confirm:true);
            Assert.That(result.Ok, Is.False); Assert.That(parent.GetComponent<VerticalLayoutGroup>(), Is.EqualTo(old)); Assert.That(old.spacing, Is.EqualTo(27)); Assert.That(parent.GetComponent<ContentSizeFitter>(), Is.EqualTo(fitter)); Assert.That(fitter.horizontalFit, Is.EqualTo(ContentSizeFitter.FitMode.MinSize));Assert.That(Undo.GetCurrentGroup(),Is.EqualTo(group));Assert.That(parent.gameObject.scene.isDirty,Is.EqualTo(dirty));
        }
        [TestCase("AlignDuplicate")][TestCase("AlignUnknown")][TestCase("DistributeTooFew")][TestCase("DistributeUnknown")]
        public void InvalidSelectionRequestPreservesAllCoordinates(string request)
        {
            var a=Rect("A",new Vector2(10,10),new Vector2(3,7),Vector2.zero);var b=Rect("B",new Vector2(10,10),new Vector2(30,70),Vector2.zero);
            var ids=Id(a.gameObject)+","+(request=="AlignDuplicate"?Id(a.gameObject):Id(b.gameObject));
            var result=request.StartsWith("Align")?UILayoutCommands.Align(targets:ids,alignment:request=="AlignUnknown"?"other":"Center",confirm:true):UILayoutCommands.Distribute(targets:ids,direction:request=="DistributeUnknown"?"other":"Horizontal",confirm:true);
            Assert.That(result.Ok, Is.False); Assert.That(a.anchoredPosition, Is.EqualTo(new Vector2(3,7))); Assert.That(b.anchoredPosition, Is.EqualTo(new Vector2(30,70)));
        }
        [TestCase(-1)][TestCase(10001)] public void QueryRejectsUnboundedOrNegativeLimits(int limit) { Assert.That(UIQueryCommands.Elements(limit:limit).Ok, Is.False); }
        [Test] public void QueryRejectsUnknownFilter() { Assert.That(UIQueryCommands.Elements(uiType:"Unknown").Ok, Is.False); }
    }
}
