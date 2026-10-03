using System.Linq;
using BatihanDev.UnityCliCommands.UGUI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace BatihanDev.UnityCliCommands.Tests.UGUI
{
    public sealed class UGUILayoutQueryContractTests : UGUIContractFixture
    {
        [TestCase("Horizontal",false)][TestCase("Horizontal",true)]
        [TestCase("Vertical",false)][TestCase("Vertical",true)]
        [TestCase("Grid",false)][TestCase("Grid",true)]
        public void LayoutUsesOriginalDefaultsAndPreservesExistingFitter(string mode,bool existing)
        {
            var parent=Rect("Parent",new Vector2(500,500),Vector2.zero,new Vector2(.5f,.5f));
            Rect("First",new Vector2(73,29),Vector2.zero,new Vector2(.5f,.5f),parent);
            ContentSizeFitter fitter=null;
            if(existing) { fitter=parent.gameObject.AddComponent<ContentSizeFitter>(); fitter.horizontalFit=ContentSizeFitter.FitMode.MinSize; fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize; }
            var result=UILayoutCommands.LayoutChildren(Id(parent.gameObject),layoutType:mode,confirm:true); Assert.That(result.Ok, Is.True,result.Error?.Message);
            Assert.That(parent.GetComponents<LayoutGroup>().Length, Is.EqualTo(1));
            var group=parent.GetComponent<LayoutGroup>(); Assert.That(group.padding.left, Is.Zero); Assert.That(group.padding.right, Is.Zero); Assert.That(group.padding.top, Is.Zero); Assert.That(group.padding.bottom, Is.Zero);
            if(mode=="Grid") { var grid=parent.GetComponent<GridLayoutGroup>(); Assert.That(grid.spacing, Is.EqualTo(new Vector2(10,10))); Assert.That(grid.cellSize, Is.EqualTo(new Vector2(73,29))); Assert.That(grid.constraintCount, Is.EqualTo(3)); Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount)); }
            else { var linear=parent.GetComponent<HorizontalOrVerticalLayoutGroup>(); Assert.That(linear.spacing, Is.EqualTo(10)); Assert.That(linear.childForceExpandWidth, Is.False); Assert.That(linear.childForceExpandHeight, Is.False); Assert.That(linear.GetType().Name, Is.EqualTo(mode+"LayoutGroup")); }
            var actual=parent.GetComponent<ContentSizeFitter>(); Assert.That(actual, Is.Not.Null);
            if(existing) { Assert.That(actual, Is.EqualTo(fitter)); Assert.That(actual.horizontalFit, Is.EqualTo(ContentSizeFitter.FitMode.MinSize)); Assert.That(actual.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize)); }
            else { Assert.That(actual.horizontalFit, Is.EqualTo(mode=="Horizontal"?ContentSizeFitter.FitMode.PreferredSize:ContentSizeFitter.FitMode.Unconstrained)); Assert.That(actual.verticalFit, Is.EqualTo(mode=="Vertical"?ContentSizeFitter.FitMode.PreferredSize:ContentSizeFitter.FitMode.Unconstrained)); }
        }
        [TestCase("Left",-10,70,70,90)][TestCase("Center",40,40,70,90)][TestCase("Right",30,70,70,90)]
        [TestCase("Top",10,70,90,90)][TestCase("Middle",10,70,80,80)][TestCase("Bottom",10,70,70,90)]
        public void AlignmentKeepsParentRelativeFormulasAndOrthogonalAxis(string alignment,float ax,float bx,float ay,float by)
        {
            var p1=new GameObject("P1"); var p2=new GameObject("P2"); p2.transform.position=new Vector3(900,300,0);
            var a=Rect("A",new Vector2(40,20),new Vector2(10,70),new Vector2(0,0),p1.transform);
            var b=Rect("B",new Vector2(80,40),new Vector2(70,90),new Vector2(1,.5f),p2.transform);
            var result=UILayoutCommands.Align(targets:Id(a.gameObject)+","+Id(b.gameObject),alignment:alignment,confirm:true);
            Assert.That(result.Ok, Is.True,result.Error?.Message); Assert.That(a.anchoredPosition, Is.EqualTo(new Vector2(ax,ay))); Assert.That(b.anchoredPosition, Is.EqualTo(new Vector2(bx,by)));
        }
        [TestCase("Horizontal",50,7)][TestCase("Vertical",3,50)]
        public void DistributionPreservesEndpointsAndIgnoresExtents(string direction,float x,float y)
        {
            var a=Rect("A",new Vector2(1000,1200),Vector2.zero,Vector2.zero);
            var b=Rect("B",new Vector2(3,4),new Vector2(3,7),Vector2.one);
            var c=Rect("C",new Vector2(80,20),new Vector2(100,100),new Vector2(.5f,.5f));
            Selection.objects=new UnityEngine.Object[]{c.gameObject,a.gameObject,b.gameObject};
            var result=UILayoutCommands.Distribute(direction:direction,confirm:true); Assert.That(result.Ok, Is.True,result.Error?.Message);
            Assert.That(a.anchoredPosition, Is.EqualTo(Vector2.zero)); Assert.That(c.anchoredPosition, Is.EqualTo(new Vector2(100,100))); Assert.That(b.anchoredPosition, Is.EqualTo(new Vector2(x,y)));
        }
        [Test] public void AlignmentUsesCurrentSelectionWhenExactTargetsOmitted()
        {
            var a=Rect("A",new Vector2(10,10),new Vector2(4,9),Vector2.zero); var b=Rect("B",new Vector2(10,10),new Vector2(20,8),Vector2.zero);
            Selection.objects=new UnityEngine.Object[]{a.gameObject,b.gameObject}; Assert.That(UILayoutCommands.Align(confirm:true).Ok, Is.True);
            Assert.That(a.anchoredPosition, Is.EqualTo(new Vector2(12,9))); Assert.That(b.anchoredPosition, Is.EqualTo(new Vector2(12,8)));
        }
        [TestCase("Canvas")][TestCase("Button")][TestCase("Slider")][TestCase("Toggle")][TestCase("InputField")][TestCase("Text")][TestCase("Image")][TestCase("RawImage")][TestCase("RectTransform")]
        public void OriginalQueryFiltersRetainExactIdentity(string filter)
        {
            var canvas=new GameObject("Root",typeof(RectTransform),typeof(Canvas));
            var child=Rect("Expected",new Vector2(20,20),Vector2.zero,Vector2.zero,canvas.transform);
            switch(filter) {case "Button":child.gameObject.AddComponent<Button>();break;case "Slider":child.gameObject.AddComponent<Slider>();break;case "Toggle":child.gameObject.AddComponent<Toggle>();break;case "InputField":child.gameObject.AddComponent<InputField>();break;case "Text":child.gameObject.AddComponent<Text>();break;case "Image":child.gameObject.AddComponent<Image>();break;case "RawImage":child.gameObject.AddComponent<RawImage>();break;}
            var result=UIQueryCommands.Elements(uiType:filter); Assert.That(result.Ok, Is.True,result.Error?.Message);
            var expected=filter=="Canvas"?canvas:child.gameObject;
            Assert.That(result.Result.Elements.Any(e=>e.Target==Id(expected)&&e.Type==filter), Is.True);
            Assert.That(result.Result.Elements.All(e=>e.Type==filter), Is.True);
        }
        [TestCase("Text")][TestCase("InputField")]
        public void TMPQueryMapsToOriginalSupportedFilters(string filter)
        {
            var go=Created(filter=="Text"?UIWidgetCommands.Text(backend:"TMP",confirm:true):UIWidgetCommands.InputField(backend:"TMP",confirm:true));
            var result=UIQueryCommands.Elements(uiType:filter); Assert.That(result.Ok, Is.True); Assert.That(result.Result.Elements.Any(e=>e.Target==Id(go)&&e.Type==filter), Is.True);
        }
        [Test] public void QueryDeduplicatesNestedCanvasIncludesInactiveAndIsStable()
        {
            var root=new GameObject("Root",typeof(RectTransform),typeof(Canvas)); var nested=new GameObject("Nested",typeof(RectTransform),typeof(Canvas)); nested.transform.SetParent(root.transform,false);
            var inactive=Rect("Inactive",Vector2.one,Vector2.zero,Vector2.zero,nested.transform); inactive.gameObject.SetActive(false);
            var first=UIQueryCommands.Elements(); var second=UIQueryCommands.Elements(); Assert.That(first.Ok, Is.True); Assert.That(second.Ok, Is.True);
            Assert.That(first.Result.Count, Is.EqualTo(3)); Assert.That(first.Result.Elements.Select(e=>e.Target).Distinct().Count(), Is.EqualTo(3));
            Assert.That(first.Result.Elements.Select(e=>e.Target), Is.EqualTo(second.Result.Elements.Select(e=>e.Target)));
            var row=first.Result.Elements.Single(e=>e.Target==Id(inactive.gameObject)); Assert.That(row.Path, Is.EqualTo("Root/Nested/Inactive")); Assert.That(row.Active, Is.False); Assert.That(row.RectTransform, Is.EqualTo(Id(inactive)));
        }
        [TestCase(0,0)][TestCase(2,2)][TestCase(50,50)]
        public void QueryLimitIsAppliedToMatchingRows(int limit,int expected)
        {
            var root=new GameObject("Root",typeof(RectTransform),typeof(Canvas)); for(var i=0;i<60;i++)Rect("Child"+i,Vector2.one,Vector2.zero,Vector2.zero,root.transform).gameObject.AddComponent<Image>();
            var result=UIQueryCommands.Elements(uiType:"Image",limit:limit); Assert.That(result.Ok, Is.True); Assert.That(result.Result.Count, Is.EqualTo(expected)); Assert.That(result.Result.Elements.Length, Is.EqualTo(expected));
        }
        [Test] public void QueryDefaultsToFiftyRows()
        {var root=new GameObject("Root",typeof(RectTransform),typeof(Canvas));for(var i=0;i<60;i++)Rect("Child"+i,Vector2.one,Vector2.zero,Vector2.zero,root.transform);var result=UIQueryCommands.Elements();Assert.That(result.Ok,Is.True);Assert.That(result.Result.Count,Is.EqualTo(50));}
    }
}
