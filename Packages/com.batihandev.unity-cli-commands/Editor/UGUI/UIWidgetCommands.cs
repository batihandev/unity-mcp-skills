using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.UGUI
{
    public static class UIWidgetCommands
    {
        [CliCommand("ui.canvas-create", "Create a Canvas with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Canvas(string name = "Canvas",
            string renderMode = "ScreenSpaceOverlay", string camera = null, bool confirm = false,
            bool dryRun = false)
        {
            GameObject go = null;
            RenderMode mode = default;
            Camera selected = null;
            return UISceneAuthoring.Run("canvas-create", confirm, dryRun,
                () =>
                {
                    ValidateName(name);
                    mode = UISceneAuthoring.Named<RenderMode>(renderMode);
                    if (camera != null)
                    {
                        selected = Exact.Resolve<Camera>(camera);
                        UISceneAuthoring.Require(
                            selected != null && UISceneAuthoring.Regular(selected.gameObject),
                            "Supply an exact scene Camera.");
                        UISceneAuthoring.Require(mode == RenderMode.ScreenSpaceCamera,
                            "Camera is only valid with ScreenSpaceCamera.");
                    }
                    return null;
                },
                () =>
                {
                    go = CreateCanvas(name);
                    var canvas = go.GetComponent<Canvas>();
                    canvas.renderMode = mode;
                    canvas.worldCamera = selected;
                },
                () => UIWidgetReadback.Read(go));
        }
        [CliCommand("ui.panel-create", "Create a Panel with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Panel(string name = "Panel", string parent = null, float r = 1,
            float g = 1, float b = 1, float a = .5f, bool confirm = false, bool dryRun = false)
        {
            return Create("panel-create", name, parent, confirm, dryRun,
                () => UISceneAuthoring.Color(r, g, b, a), () => DefaultControls.CreatePanel(default),
                go =>
                {
                    var rect = go.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    go.GetComponent<Image>().color = new Color(r, g, b, a);
                });
        }
        [CliCommand("ui.button-create", "Create a Button with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Button(string name = "Button", string parent = null,
            string text = "Button", float width = 160, float height = 30, string backend = "Auto",
            bool confirm = false, bool dryRun = false)
        {
            UITextAuthoring fonts = null;
            return Create("button-create", name, parent, confirm, dryRun,
                () =>
                {
                    UISceneAuthoring.Size(width, height);
                    fonts = UITextAuthoring.Resolve(backend);
                },
                () => fonts.Backend == "TMP" ? TMP_DefaultControls.CreateButton(default)
                                             : DefaultControls.CreateButton(default),
                go =>
                {
                    SetSize(go, width, height);
                    fonts.Apply(go);
                    var legacy = go.GetComponentInChildren<Text>(true);
                    if (legacy != null)
                    {
                        legacy.text = text;
                        legacy.alignment = TextAnchor.MiddleCenter;
                    }
                    var tmp = go.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null)
                    {
                        tmp.text = text;
                        tmp.alignment = TextAlignmentOptions.Center;
                    }
                    go.GetComponent<Button>().targetGraphic = go.GetComponent<Image>();
                });
        }
        [CliCommand("ui.text-create", "Create a Text with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Text(string name = "Text", string parent = null,
            string text = "New Text", int fontSize = 14, float r = 0, float g = 0, float b = 0,
            string backend = "Auto", bool confirm = false, bool dryRun = false)
        {
            UITextAuthoring fonts = null;
            return Create("text-create", name, parent, confirm, dryRun,
                () =>
                {
                    UISceneAuthoring.Require(fontSize > 0, "Font size must be positive.");
                    UISceneAuthoring.Color(r, g, b);
                    fonts = UITextAuthoring.Resolve(backend);
                },
                () => fonts.Backend == "TMP" ? TMP_DefaultControls.CreateText(default)
                                             : DefaultControls.CreateText(default),
                go =>
                {
                    SetSize(go, 200, 50);
                    fonts.Apply(go);
                    var legacy = go.GetComponent<Text>();
                    if (legacy != null)
                    {
                        legacy.text = text;
                        legacy.fontSize = fontSize;
                        legacy.color = new Color(r, g, b);
                        legacy.alignment = TextAnchor.MiddleLeft;
                    }
                    var tmp = go.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        tmp.text = text;
                        tmp.fontSize = fontSize;
                        tmp.color = new Color(r, g, b);
                        tmp.alignment = TextAlignmentOptions.Left;
                    }
                });
        }
        [CliCommand("ui.image-create", "Create a Image with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Image(string name = "Image", string parent = null,
            string sprite = null, float width = 100, float height = 100, bool confirm = false,
            bool dryRun = false)
        {
            Sprite selected = null;
            return Create("image-create", name, parent, confirm, dryRun,
                () =>
                {
                    UISceneAuthoring.Size(width, height);
                    selected = SpriteAsset(sprite);
                },
                () => DefaultControls.CreateImage(default),
                go =>
                {
                    SetSize(go, width, height);
                    go.GetComponent<Image>().sprite = selected;
                });
        }
        [CliCommand("ui.inputfield-create", "Create a InputField with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> InputField(string name = "InputField", string parent = null,
            string placeholder = "Enter text...", float width = 200, float height = 30,
            string backend = "Auto", bool confirm = false, bool dryRun = false)
        {
            UITextAuthoring fonts = null;
            return Create("inputfield-create", name, parent, confirm, dryRun,
                () =>
                {
                    UISceneAuthoring.Size(width, height);
                    fonts = UITextAuthoring.Resolve(backend);
                },
                () => fonts.Backend == "TMP" ? TMP_DefaultControls.CreateInputField(default)
                                             : DefaultControls.CreateInputField(default),
                go =>
                {
                    SetSize(go, width, height);
                    fonts.Apply(go);
                    var input = go.GetComponent<InputField>();
                    if (input != null)
                    {
                        var ph = (Text)input.placeholder;
                        ph.text = placeholder;
                        ph.color = new Color(.5f, .5f, .5f, 1);
                        ph.fontStyle = FontStyle.Italic;
                        input.textComponent.text = "";
                        input.textComponent.supportRichText = false;
                        input.targetGraphic = go.GetComponent<Image>();
                    }
                    var tmp = go.GetComponent<TMP_InputField>();
                    if (tmp != null)
                    {
                        var ph = (TMP_Text)tmp.placeholder;
                        ph.text = placeholder;
                        ph.color = new Color(.5f, .5f, .5f, 1);
                        ph.fontStyle = FontStyles.Italic;
                        tmp.textComponent.text = "";
                        tmp.targetGraphic = go.GetComponent<Image>();
                    }
                });
        }
        [CliCommand("ui.slider-create", "Create a Slider with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Slider(string name = "Slider", string parent = null,
            float width = 160, float height = 20, float minValue = 0, float maxValue = 1, float value = .5f,
            bool confirm = false, bool dryRun = false)
        {
            return Create("slider-create", name, parent, confirm, dryRun,
                () =>
                {
                    UISceneAuthoring.Size(width, height);
                    UISceneAuthoring.Finite(minValue, maxValue, value);
                    UISceneAuthoring.Require(minValue < maxValue && value >= minValue && value <= maxValue,
                        "Slider requires ordered range and in-range value.");
                },
                () => DefaultControls.CreateSlider(default),
                go =>
                {
                    SetSize(go, width, height);
                    var slider = go.GetComponent<Slider>();
                    slider.minValue = minValue;
                    slider.maxValue = maxValue;
                    slider.value = value;
                    slider.targetGraphic = slider.handleRect.GetComponent<Image>();
                    slider.fillRect.GetComponent<Image>().color = new Color(.3f, .6f, 1);
                    slider.handleRect.GetComponent<Image>().color = Color.white;
                    var background = go.transform.Find("Background");
                    background.GetComponent<Image>().color = new Color(.8f, .8f, .8f);
                });
        }
        [CliCommand("ui.toggle-create", "Create a Toggle with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Toggle(string name = "Toggle", string parent = null,
            string text = "Toggle", bool isOn = false, string backend = "Auto", bool confirm = false,
            bool dryRun = false)
        {
            UITextAuthoring fonts = null;
            return Create("toggle-create", name, parent, confirm, dryRun,
                () => fonts = UITextAuthoring.Resolve(backend), () => DefaultControls.CreateToggle(default),
                go =>
                {
                    SetSize(go, 160, 20);
                    fonts.ToggleLabel(go, text);
                    fonts.Apply(go);
                    var toggle = go.GetComponent<Toggle>();
                    toggle.isOn = isOn;
                    toggle.graphic.color = new Color(.3f, .6f, 1);
                    toggle.targetGraphic.color = Color.white;
                    var bg = toggle.targetGraphic.rectTransform;
                    bg.anchorMin = bg.anchorMax = new Vector2(0, 1);
                    bg.pivot = new Vector2(0, 1);
                    bg.sizeDelta = new Vector2(20, 20);
                    bg.anchoredPosition = Vector2.zero;
                    var check = toggle.graphic.rectTransform;
                    check.anchorMin = Vector2.zero;
                    check.anchorMax = Vector2.one;
                    check.sizeDelta = Vector2.zero;
                    check.anchoredPosition = Vector2.zero;
                });
        }
        [CliCommand("ui.dropdown-create", "Create a Dropdown with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Dropdown(string name = "Dropdown", string parent = null,
            string options = null, float width = 160, float height = 30, string backend = "Auto",
            bool confirm = false, bool dryRun = false)
        {
            UITextAuthoring fonts = null;
            string[] entries = null;
            return Create("dropdown-create", name, parent, confirm, dryRun,
                () =>
                {
                    UISceneAuthoring.Size(width, height);
                    fonts = UITextAuthoring.Resolve(backend);
                    entries =
                        (options ?? "").Split(',').Select(v => v.Trim()).Where(v => v.Length > 0).ToArray();
                    if (entries.Length == 0)
                        entries = new[] { "A", "B", "C" };
                },
                () => fonts.Backend == "TMP" ? TMP_DefaultControls.CreateDropdown(default)
                                             : DefaultControls.CreateDropdown(default),
                go =>
                {
                    SetSize(go, width, height);
                    fonts.Apply(go);
                    var legacy = go.GetComponent<Dropdown>();
                    go.transform.Find("Arrow").GetComponent<Image>().color = new Color(.2f, .2f, .2f, 1);
                    if (legacy != null)
                    {
                        legacy.ClearOptions();
                        legacy.AddOptions(entries.ToList());
                        legacy.value = 0;
                        legacy.RefreshShownValue();
                        legacy.template.sizeDelta = new Vector2(legacy.template.sizeDelta.x, 150);
                        legacy.template.gameObject.SetActive(false);
                    }
                    var tmp = go.GetComponent<TMP_Dropdown>();
                    if (tmp != null)
                    {
                        tmp.ClearOptions();
                        tmp.AddOptions(entries.ToList());
                        tmp.value = 0;
                        tmp.RefreshShownValue();
                        tmp.template.sizeDelta = new Vector2(tmp.template.sizeDelta.x, 150);
                        tmp.template.gameObject.SetActive(false);
                    }
                });
        }
        [CliCommand("ui.scrollview-create", "Create a ScrollView with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> ScrollView(string name = "ScrollView", string parent = null,
            float width = 300, float height = 200, bool horizontal = false, bool vertical = true,
            string movementType = "Elastic", bool confirm = false, bool dryRun = false)
        {
            ScrollRect.MovementType movement = default;
            return Create("scrollview-create", name, parent, confirm, dryRun,
                () =>
                {
                    UISceneAuthoring.Size(width, height);
                    movement = UISceneAuthoring.Named<ScrollRect.MovementType>(movementType);
                },
                () => DefaultControls.CreateScrollView(default),
                go =>
                {
                    SetSize(go, width, height);
                    var scroll = go.GetComponent<ScrollRect>();
                    scroll.horizontal = horizontal;
                    scroll.vertical = vertical;
                    scroll.movementType = movement;
                    var bars = go.GetComponentsInChildren<Scrollbar>(true);
                    scroll.horizontalScrollbar = null;
                    scroll.verticalScrollbar = null;
                    foreach (var bar in bars)
                        Undo.DestroyObjectImmediate(bar.gameObject);
                    var viewport = scroll.viewport;
                    var mask = viewport.GetComponent<Mask>();
                    if (mask != null)
                        Undo.DestroyObjectImmediate(mask);
                    if (viewport.GetComponent<RectMask2D>() == null)
                        Undo.AddComponent<RectMask2D>(viewport.gameObject);
                    viewport.anchorMin = Vector2.zero;
                    viewport.anchorMax = Vector2.one;
                    viewport.offsetMin = Vector2.zero;
                    viewport.offsetMax = Vector2.zero;
                    viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0);
                    scroll.content.anchorMin = new Vector2(0, 1);
                    scroll.content.anchorMax = Vector2.one;
                    scroll.content.pivot = new Vector2(.5f, 1);
                    scroll.content.sizeDelta = new Vector2(0, 400);
                    scroll.content.anchoredPosition = Vector2.zero;
                    go.GetComponent<Image>().color = new Color(.1f, .1f, .1f, .5f);
                });
        }
        [CliCommand("ui.rawimage-create", "Create a RawImage with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> RawImage(string name = "RawImage", string parent = null,
            string texture = null, float width = 100, float height = 100, bool confirm = false,
            bool dryRun = false)
        {
            Texture selected = null;
            return Create("rawimage-create", name, parent, confirm, dryRun,
                () =>
                {
                    UISceneAuthoring.Size(width, height);
                    if (texture != null)
                    {
                        selected = Exact.Resolve<Texture>(texture);
                        UISceneAuthoring.Require(selected != null && EditorUtility.IsPersistent(selected),
                            "Supply an existing Texture asset or exact asset handle.");
                    }
                },
                () => DefaultControls.CreateRawImage(default),
                go =>
                {
                    SetSize(go, width, height);
                    go.GetComponent<RawImage>().texture = selected;
                });
        }
        [CliCommand("ui.scrollbar-create", "Create a Scrollbar with validated public Unity authoring APIs.",
            Tags = new[] { "unity-cli-commands", "ui" })]
        public static CommandResult<UIResult> Scrollbar(string name = "Scrollbar", string parent = null,
            string direction = "BottomToTop", float value = 0, float size = .2f, int numberOfSteps = 0,
            bool confirm = false, bool dryRun = false)
        {
            UnityEngine.UI.Scrollbar.Direction mode = default;
            return Create("scrollbar-create", name, parent, confirm, dryRun,
                () =>
                {
                    mode = UISceneAuthoring.Named<UnityEngine.UI.Scrollbar.Direction>(direction);
                    UISceneAuthoring.Finite(value, size);
                    UISceneAuthoring.Require(
                        value >= 0 && value <= 1 && size >= 0 && size <= 1 && numberOfSteps >= 0,
                        "Scrollbar value/size must be [0,1] and steps nonnegative.");
                },
                () => DefaultControls.CreateScrollbar(default),
                go =>
                {
                    var horizontal = mode == UnityEngine.UI.Scrollbar.Direction.LeftToRight ||
                                     mode == UnityEngine.UI.Scrollbar.Direction.RightToLeft;
                    var bar = go.GetComponent<Scrollbar>();
                    bar.SetDirection(mode, true);
                    SetSize(go, horizontal ? 160 : 20, horizontal ? 20 : 160);
                    bar.value = value;
                    bar.size = size;
                    bar.numberOfSteps = numberOfSteps;
                    bar.targetGraphic = bar.handleRect.GetComponent<Image>();
                    go.GetComponent<Image>().color = new Color(.8f, .8f, .8f, 1);
                });
        }
        private static void ValidateName(string name) => UISceneAuthoring.Require(
            !string.IsNullOrWhiteSpace(name), "Name must not be empty.");
        private static void SetSize(GameObject go, float width,
            float height) => go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(
                name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create UI Canvas");
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            return go;
        }
        private static Sprite SpriteAsset(string reference)
        {
            if (reference == null)
                return null;
            if (reference.StartsWith("Assets/", StringComparison.Ordinal) ||
                reference.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var sprites = AssetDatabase.LoadAllAssetsAtPath(reference).OfType<Sprite>().ToArray();
                UISceneAuthoring.Require(sprites.Length == 1,
                    "Sprite path must identify one Sprite; use an exact subasset handle otherwise.");
                return sprites[0];
            }
            var sprite = Exact.Resolve<Sprite>(reference);
            UISceneAuthoring.Require(sprite != null && EditorUtility.IsPersistent(sprite),
                "Supply an existing Sprite asset or exact subasset handle.");
            return sprite;
        }
        private static CommandResult<UIResult> Create(string command, string name, string parent,
            bool confirm, bool dryRun, Action validate, Func<GameObject> factory, Action<GameObject> patch)
        {
            GameObject selected = null;
            GameObject go = null;
            return UISceneAuthoring.Run(command, confirm, dryRun,
                () =>
                {
                    ValidateName(name);
                    validate();
                    selected = UISceneAuthoring.Parent(parent);
                    return selected;
                },
                () =>
                {
                    if (selected == null)
                        selected = CreateCanvas("Canvas");
                    go = factory();
                    Undo.RegisterCreatedObjectUndo(go, "Create UI " + name);
                    UISceneAuthoring.Record(go, "Configure UI " + name);
                    go.name = name;
                    Undo.SetTransformParent(go.transform, selected.transform, "Parent UI " + name);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    foreach (var transform in go.GetComponentsInChildren<UnityEngine.Transform>(true))
                        transform.gameObject.layer = selected.layer;
                    patch(go);
                },
                () => UIWidgetReadback.Read(go));
        }
    }
}
