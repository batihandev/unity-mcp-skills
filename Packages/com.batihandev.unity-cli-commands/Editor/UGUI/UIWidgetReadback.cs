using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.UGUI
{
    internal static class UIWidgetReadback
    {
        internal static UIResult Read(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            var canvas = go.GetComponentInParent<Canvas>(true);
            var tmp = go.GetComponentInChildren<TMP_Text>(true);
            var legacy = go.GetComponentInChildren<Text>(true);
            var state = new Dictionary<string, object> { { "name", go.name },
                { "rectTransform", Exact.ExactId(rect) },
                { "sizeDelta", new[] { rect.sizeDelta.x, rect.sizeDelta.y } },
                { "anchorMin", new[] { rect.anchorMin.x, rect.anchorMin.y } },
                { "anchorMax", new[] { rect.anchorMax.x, rect.anchorMax.y } },
                { "components", go.GetComponents<UnityEngine.Component>()
                                    .Select(c => new Dictionary<string, object> { { "id", Exact.ExactId(c) },
                                        { "type", c.GetType().FullName } })
                                    .ToArray() } };
            var ownCanvas = go.GetComponent<Canvas>();
            if (ownCanvas != null)
            {
                state["renderMode"] = ownCanvas.renderMode.ToString();
                state["camera"] = Exact.ExactId(ownCanvas.worldCamera);
            }
            if (tmp != null)
            {
                state["font"] = Exact.ExactId(tmp.font);
                state["fontMaterial"] = Exact.ExactId(tmp.fontSharedMaterial);
                state["text"] = tmp.text;
            }
            else if (legacy != null)
            {
                state["font"] = Exact.ExactId(legacy.font);
                state["text"] = legacy.text;
            }
            var image = go.GetComponent<Image>();
            if (image != null)
            {
                state["sprite"] = Exact.ExactId(image.sprite);
                state["color"] = new[] { image.color.r, image.color.g, image.color.b, image.color.a };
            }
            var raw = go.GetComponent<RawImage>();
            if (raw != null)
            {
                state["texture"] = Exact.ExactId(raw.texture);
                state["hasTexture"] = raw.texture != null;
            }
            var selectable = go.GetComponent<Selectable>();
            if (selectable != null)
                state["targetGraphic"] = Exact.ExactId(selectable.targetGraphic);
            var slider = go.GetComponent<Slider>();
            if (slider != null)
            {
                state["minValue"] = slider.minValue;
                state["maxValue"] = slider.maxValue;
                state["value"] = slider.value;
                state["fillRect"] = Exact.ExactId(slider.fillRect);
                state["handleRect"] = Exact.ExactId(slider.handleRect);
            }
            var toggle = go.GetComponent<Toggle>();
            if (toggle != null)
            {
                state["isOn"] = toggle.isOn;
                state["graphic"] = Exact.ExactId(toggle.graphic);
            }
            var bar = go.GetComponent<Scrollbar>();
            if (bar != null)
            {
                state["direction"] = bar.direction.ToString();
                state["value"] = bar.value;
                state["size"] = bar.size;
                state["numberOfSteps"] = bar.numberOfSteps;
                state["handleRect"] = Exact.ExactId(bar.handleRect);
            }
            var input = go.GetComponent<InputField>();
            if (input != null)
            {
                state["textComponent"] = Exact.ExactId(input.textComponent);
                state["placeholder"] = Exact.ExactId(input.placeholder);
            }
            var tmpInput = go.GetComponent<TMP_InputField>();
            if (tmpInput != null)
            {
                state["textComponent"] = Exact.ExactId(tmpInput.textComponent);
                state["placeholder"] = Exact.ExactId(tmpInput.placeholder);
                state["textViewport"] = Exact.ExactId(tmpInput.textViewport);
            }
            var dropdown = go.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                state["options"] = dropdown.options.Select(o => o.text).ToArray();
                state["captionText"] = Exact.ExactId(dropdown.captionText);
                state["itemText"] = Exact.ExactId(dropdown.itemText);
                state["template"] = Exact.ExactId(dropdown.template);
            }
            var tmpDropdown = go.GetComponent<TMP_Dropdown>();
            if (tmpDropdown != null)
            {
                state["options"] = tmpDropdown.options.Select(o => o.text).ToArray();
                state["captionText"] = Exact.ExactId(tmpDropdown.captionText);
                state["itemText"] = Exact.ExactId(tmpDropdown.itemText);
                state["template"] = Exact.ExactId(tmpDropdown.template);
            }
            var scroll = go.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                state["horizontal"] = scroll.horizontal;
                state["vertical"] = scroll.vertical;
                state["movementType"] = scroll.movementType.ToString();
                state["viewport"] = Exact.ExactId(scroll.viewport);
                state["content"] = Exact.ExactId(scroll.content);
            }
            return new UIResult { Target = Exact.ExactId(go),
                Parent = Exact.ExactId(go.transform.parent == null ? null : go.transform.parent.gameObject),
                Canvas = Exact.ExactId(canvas),
                Backend = tmp != null      ? "TMP"
                          : legacy != null ? "Legacy"
                                           : null,
                State = state,
                Issues = ownCanvas != null && ownCanvas.renderMode == RenderMode.ScreenSpaceCamera &&
                                 ownCanvas.worldCamera == null
                             ? new[] { "ScreenSpaceCamera Canvas has no camera assigned." }
                             : System.Array.Empty<string>() };
        }
    }
}
