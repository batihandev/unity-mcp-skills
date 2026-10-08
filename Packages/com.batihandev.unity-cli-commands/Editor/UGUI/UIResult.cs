using System;
using System.Collections.Generic;
namespace BatihanDev.UnityCliCommands.UGUI
{
    [Serializable]
    public sealed class UIResult
    {
        public string Target { get; set; }
        public string Parent { get; set; }
        public string Canvas { get; set; }
        public string Backend { get; set; }
        public bool Applied { get; set; }
        public bool DryRun { get; set; }
        public Dictionary<string, object> State { get; set; }
        public string[] Issues { get; set; } = Array.Empty<string>();
    }
    [Serializable]
    public sealed class UIElement
    {
        public string Target { get; set; }
        public string RectTransform { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public bool Active { get; set; }
    }
    [Serializable]
    public sealed class UIElementsResult
    {
        public int Count { get; set; }
        public UIElement[] Elements { get; set; }
    }
}
