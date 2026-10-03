using System.Collections.Generic;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public sealed class SmartReplacementReport
    {
        public bool Preview { get; set; }
        public int SelectedCount { get; set; }
        public int ReplacedCount { get; set; }
        public string PrefabPath { get; set; }
        public int UndoGroup { get; set; }
        public bool SelectionUndoable { get; set; }
        public List<SmartReplacementItem> Replacements { get; set; } = new List<SmartReplacementItem>();
    }
    public sealed class SmartReplacementItem
    {
        public string Original { get; set; }
        public string Replacement { get; set; }
        public string ScenePath { get; set; }
        public string Parent { get; set; }
        public int SiblingIndex { get; set; }
    }
}
