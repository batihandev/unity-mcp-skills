using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.UGUI
{
    public static class UILayoutCommands
    {
        private enum LayoutMode
        {
            Horizontal,
            Vertical,
            Grid
        }
        private enum Alignment
        {
            Left,
            Center,
            Right,
            Top,
            Middle,
            Bottom
        }
        private enum Axis
        {
            Horizontal,
            Vertical
        }
        [CliCommand("ui.layout-children", "Arrange an exact RectTransform's children using a layout group.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> LayoutChildren(string target, string layoutType = "Vertical",
            float spacing = 10, int paddingLeft = 0, int paddingRight = 0, int paddingTop = 0,
            int paddingBottom = 0, int gridColumns = 3, bool childForceExpandWidth = false,
            bool childForceExpandHeight = false, bool confirm = false, bool dryRun = false)
        {
            RectTransform rect = null;
            LayoutMode mode = default;
            Vector2? cell = null;
            return UISceneAuthoring.Run("layout-children", confirm, dryRun,
                () =>
                {
                    rect = UISceneAuthoring.Rect(target);
                    mode = UISceneAuthoring.Named<LayoutMode>(layoutType);
                    UISceneAuthoring.Finite(spacing);
                    UISceneAuthoring.Require(
                        spacing >= 0 &&
                            new[] { paddingLeft, paddingRight, paddingTop, paddingBottom }.All(v => v >= 0) &&
                            gridColumns > 0,
                        "Spacing/padding must be nonnegative and grid columns positive.");
                    if (mode == LayoutMode.Grid && rect.childCount > 0 &&
                        rect.GetChild(0) is RectTransform first)
                    {
                        cell = first.sizeDelta;
                        UISceneAuthoring.Finite(cell.Value.x, cell.Value.y);
                    }
                    return rect.gameObject;
                },
                () =>
                {
                    foreach (var group in rect.GetComponents<LayoutGroup>())
                        Undo.DestroyObjectImmediate(group);
                    var padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
                    if (mode == LayoutMode.Grid)
                    {
                        var group = Undo.AddComponent<GridLayoutGroup>(rect.gameObject);
                        group.padding = padding;
                        group.spacing = new Vector2(spacing, spacing);
                        group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        group.constraintCount = gridColumns;
                        if (cell.HasValue)
                            group.cellSize = cell.Value;
                    }
                    else
                    {
                        HorizontalOrVerticalLayoutGroup group =
                            mode == LayoutMode.Horizontal
                                ? (HorizontalOrVerticalLayoutGroup)Undo.AddComponent<HorizontalLayoutGroup>(
                                      rect.gameObject)
                                : Undo.AddComponent<VerticalLayoutGroup>(rect.gameObject);
                        group.padding = padding;
                        group.spacing = spacing;
                        group.childForceExpandWidth = childForceExpandWidth;
                        group.childForceExpandHeight = childForceExpandHeight;
                    }
                    if (rect.GetComponent<ContentSizeFitter>() == null)
                    {
                        var fitter = Undo.AddComponent<ContentSizeFitter>(rect.gameObject);
                        fitter.horizontalFit = mode == LayoutMode.Horizontal
                                                   ? ContentSizeFitter.FitMode.PreferredSize
                                                   : ContentSizeFitter.FitMode.Unconstrained;
                        fitter.verticalFit = mode == LayoutMode.Vertical
                                                 ? ContentSizeFitter.FitMode.PreferredSize
                                                 : ContentSizeFitter.FitMode.Unconstrained;
                    }
                },
                () =>
                {
                    var group = rect.GetComponent<LayoutGroup>();
                    var fitter = rect.GetComponent<ContentSizeFitter>();
                    return new UIResult { Target = Exact.ExactId(rect.gameObject),
                        State = new Dictionary<string, object> { { "group", Exact.ExactId(group) },
                            { "layoutType", group.GetType().Name }, { "childCount", rect.childCount },
                            { "fitter", Exact.ExactId(fitter) },
                            { "horizontalFit", fitter.horizontalFit.ToString() },
                            { "verticalFit", fitter.verticalFit.ToString() } } };
                });
        }
        [CliCommand("ui.align",
            "Align exact or selected RectTransforms in their parent-relative coordinate contexts.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Align(
            string targets = null, string alignment = "Center", bool confirm = false, bool dryRun = false)
        {
            RectTransform[] rects = null;
            Alignment mode = default;
            return UISceneAuthoring.Run("align", confirm, dryRun,
                () =>
                {
                    mode = UISceneAuthoring.Named<Alignment>(alignment);
                    rects = UISceneAuthoring.Targets(targets, 2);
                    ValidateRects(rects);
                    return rects[0].gameObject;
                },
                () =>
                {
                    Undo.RecordObjects(rects.Cast<UnityEngine.Object>().ToArray(), "Align UI");
                    float value;
                    switch (mode)
                    {
                    case Alignment.Left:
                        value = rects.Min(r => r.anchoredPosition.x - r.rect.width * r.pivot.x);
                        foreach (var r in rects)
                            r.anchoredPosition =
                                new Vector2(value + r.rect.width * r.pivot.x, r.anchoredPosition.y);
                        break;
                    case Alignment.Right:
                        value = rects.Max(r => r.anchoredPosition.x + r.rect.width * (1 - r.pivot.x));
                        foreach (var r in rects)
                            r.anchoredPosition =
                                new Vector2(value - r.rect.width * (1 - r.pivot.x), r.anchoredPosition.y);
                        break;
                    case Alignment.Center:
                        value = rects.Average(r => r.anchoredPosition.x);
                        foreach (var r in rects)
                            r.anchoredPosition = new Vector2(value, r.anchoredPosition.y);
                        break;
                    case Alignment.Top:
                        value = rects.Max(r => r.anchoredPosition.y + r.rect.height * (1 - r.pivot.y));
                        foreach (var r in rects)
                            r.anchoredPosition =
                                new Vector2(r.anchoredPosition.x, value - r.rect.height * (1 - r.pivot.y));
                        break;
                    case Alignment.Bottom:
                        value = rects.Min(r => r.anchoredPosition.y - r.rect.height * r.pivot.y);
                        foreach (var r in rects)
                            r.anchoredPosition =
                                new Vector2(r.anchoredPosition.x, value + r.rect.height * r.pivot.y);
                        break;
                    default:
                        value = rects.Average(r => r.anchoredPosition.y);
                        foreach (var r in rects)
                            r.anchoredPosition = new Vector2(r.anchoredPosition.x, value);
                        break;
                    }
                    Changed(rects);
                },
                () => Read(rects));
        }
        [CliCommand("ui.distribute",
            "Evenly distribute exact or selected anchored positions while retaining endpoints.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Distribute(
            string targets = null, string direction = "Horizontal", bool confirm = false, bool dryRun = false)
        {
            RectTransform[] rects = null;
            Axis axis = default;
            return UISceneAuthoring.Run("distribute", confirm, dryRun,
                () =>
                {
                    axis = UISceneAuthoring.Named<Axis>(direction);
                    rects = UISceneAuthoring.Targets(targets, 3);
                    ValidateRects(rects);
                    rects = rects
                                .OrderBy(r => axis == Axis.Horizontal ? r.anchoredPosition.x
                                                                      : r.anchoredPosition.y)
                                .ToArray();
                    return rects[0].gameObject;
                },
                () =>
                {
                    Undo.RecordObjects(rects.Cast<UnityEngine.Object>().ToArray(), "Distribute UI");
                    var horizontal = axis == Axis.Horizontal;
                    var start = horizontal ? rects[0].anchoredPosition.x : rects[0].anchoredPosition.y;
                    var end = horizontal ? rects[rects.Length - 1].anchoredPosition.x
                                         : rects[rects.Length - 1].anchoredPosition.y;
                    for (var i = 1; i < rects.Length - 1; i++)
                    {
                        var value = start + (end - start) * i / (rects.Length - 1);
                        var position = rects[i].anchoredPosition;
                        rects[i].anchoredPosition =
                            horizontal ? new Vector2(value, position.y) : new Vector2(position.x, value);
                    }
                    Changed(rects);
                },
                () => Read(rects));
        }
        private static void ValidateRects(RectTransform[] rects)
        {
            foreach (var r in rects)
                UISceneAuthoring.Finite(r.anchoredPosition.x, r.anchoredPosition.y, r.rect.width,
                    r.rect.height, r.pivot.x, r.pivot.y);
        }
        private static void Changed(RectTransform[] rects)
        {
            foreach (var r in rects)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                EditorUtility.SetDirty(r);
                EditorSceneManager.MarkSceneDirty(r.gameObject.scene);
            }
        }
        private static UIResult Read(RectTransform[] rects) => new UIResult {
            Target = Exact.ExactId(rects[0].gameObject),
            State = new Dictionary<string, object> { { "count", rects.Length },
                { "elements", rects
                                  .Select(r => new Dictionary<string, object> {
                                      { "target", Exact.ExactId(r.gameObject) },
                                      { "rectTransform", Exact.ExactId(r) },
                                      { "anchoredPosition",
                                          new[] { r.anchoredPosition.x, r.anchoredPosition.y } }
                                  })
                                  .ToArray() } }
        };
    }
}
