using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.UGUI
{
    public static class UIQueryCommands
    {
        [CliCommand("ui.elements",
            "List bounded Canvas descendants including inactive UI with exact identities.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIElementsResult> Elements(string uiType = null, int limit = 50)
        {
            const string schema = "unity.ui.elements@1";
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<UIElementsResult>.Failure(schema, compatibility.Error);
            try
            {
                UISceneAuthoring.Require(limit >= 0 && limit <= 10000, "Limit must be between 0 and 10000.");
                var supported = new[] { "Canvas", "Button", "Slider", "Toggle", "InputField", "Text", "Image",
                    "RawImage", "RectTransform" };
                UISceneAuthoring.Require(
                    uiType == null ||
                        supported.Any(n => string.Equals(n, uiType, StringComparison.OrdinalIgnoreCase)),
                    "Supply a supported UI classification.");
                var elements =
                    UISceneAuthoring.All<Canvas>()
                        .SelectMany(c => c.GetComponentsInChildren<RectTransform>(true))
                        .Where(r => UISceneAuthoring.Regular(r.gameObject))
                        .Distinct()
                        .OrderBy(r => UISceneAuthoring.Path(r), StringComparer.Ordinal)
                        .ThenBy(r => Exact.ExactId(r), StringComparer.Ordinal)
                        .Select(r => new UIElement { Target = Exact.ExactId(r.gameObject),
                            RectTransform = Exact.ExactId(r), Name = r.name, Path = UISceneAuthoring.Path(r),
                            Type = Classify(r.gameObject), Active = r.gameObject.activeInHierarchy })
                        .Where(e => uiType == null ||
                                    string.Equals(uiType, e.Type, StringComparison.OrdinalIgnoreCase))
                        .Take(limit)
                        .ToArray();
                return CommandResult<UIElementsResult>.Success(
                    schema, new UIElementsResult { Count = elements.Length, Elements = elements });
            }
            catch (Exception exception)
            {
                return CommandResult<UIElementsResult>.Failure(schema, "UI_QUERY_INVALID", exception.Message);
            }
        }
        private static string Classify(GameObject go)
        {
            if (go.GetComponent<Canvas>() != null)
                return "Canvas";
            if (go.GetComponent<Button>() != null)
                return "Button";
            if (go.GetComponent<Slider>() != null)
                return "Slider";
            if (go.GetComponent<Toggle>() != null)
                return "Toggle";
            if (go.GetComponent<InputField>() != null || go.GetComponent<TMP_InputField>() != null)
                return "InputField";
            if (go.GetComponent<Text>() != null || go.GetComponent<TMP_Text>() != null)
                return "Text";
            if (go.GetComponent<Image>() != null)
                return "Image";
            if (go.GetComponent<RawImage>() != null)
                return "RawImage";
            return "RectTransform";
        }
    }
}
