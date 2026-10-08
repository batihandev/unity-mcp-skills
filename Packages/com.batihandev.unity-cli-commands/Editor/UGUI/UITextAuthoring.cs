using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace BatihanDev.UnityCliCommands.UGUI
{
    internal sealed class UITextAuthoring
    {
        internal string Backend { get; private set; }
        internal Font LegacyFont { get; private set; }
        internal TMP_FontAsset TMPFont { get; private set; }
        internal static UITextAuthoring Resolve(string backend)
        {
            UISceneAuthoring.Require(new[] { "Auto", "Legacy", "TMP" }.Any(
                                         n => string.Equals(n, backend, StringComparison.OrdinalIgnoreCase)),
                "Backend must be Auto, Legacy, or TMP.");
            var resource = Resources.Load<TMP_Settings>("TMP Settings");
            var settings = resource == null ? null : TMP_Settings.GetSettings();
            var font = settings == null ? null : TMP_Settings.defaultFontAsset;
            var atlases = font == null ? null : font.atlasTextures;
            var ready = font != null && font.material != null && atlases != null && atlases.Length > 0 &&
                        atlases[0] != null && font.material.shader != null &&
                        font.material.shader.isSupported;
            if (string.Equals(backend, "TMP", StringComparison.OrdinalIgnoreCase))
                UISceneAuthoring.Require(ready,
                    "TMP settings, default font, atlas, material and supported shader are required. Import/configure existing TMP resources explicitly.");
            if (ready && !string.Equals(backend, "Legacy", StringComparison.OrdinalIgnoreCase))
                return new UITextAuthoring { Backend = "TMP", TMPFont = font };
            var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            UISceneAuthoring.Require(legacy != null, "Builtin LegacyRuntime.ttf is unavailable.");
            return new UITextAuthoring { Backend = "Legacy", LegacyFont = legacy };
        }
        internal void Apply(GameObject root)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                text.font = LegacyFont;
                text.fontSize = 14;
                text.color = Color.black;
            }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = TMPFont;
                text.fontSharedMaterial = TMPFont.material;
                text.fontSize = 14;
                text.color = Color.black;
            }
            var input = root.GetComponent<TMP_InputField>();
            if (input != null)
                input.fontAsset = TMPFont;
        }
        internal void ToggleLabel(GameObject root, string content)
        {
            var legacy = root.GetComponentInChildren<Text>(true);
            if (Backend == "Legacy")
            {
                legacy.text = content;
                return;
            }
            var label = legacy.gameObject;
            Undo.DestroyObjectImmediate(legacy);
            var text = Undo.AddComponent<TextMeshProUGUI>(label);
            text.font = TMPFont;
            text.fontSharedMaterial = TMPFont.material;
            text.text = content;
            text.fontSize = 14;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Left;
        }
    }
}
