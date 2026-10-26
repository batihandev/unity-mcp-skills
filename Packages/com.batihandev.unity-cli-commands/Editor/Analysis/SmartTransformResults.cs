using System.Collections.Generic;
using Float3 = BatihanDev.UnityCliCommands.Transform.Float3;
using Float4 = BatihanDev.UnityCliCommands.Transform.Float4;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public sealed class SmartTransformReport
    {
        public bool Preview { get; set; }
        public int SelectedCount { get; set; }
        public int ProcessedCount { get; set; }
        public int ChangedCount { get; set; }
        public int HitCount { get; set; }
        public int UndoGroup { get; set; }
        public string Operation { get; set; }
        public List<SmartTransformItem> Items { get; set; } = new List<SmartTransformItem>();
    }
    public sealed class SmartTransformItem
    {
        public string Target { get; set; }
        public Float3 OriginalPosition { get; set; }
        public Float3 ProposedPosition { get; set; }
        public Float4 OriginalRotation { get; set; }
        public Float4 ProposedRotation { get; set; }
        public Float3 OriginalScale { get; set; }
        public Float3 ProposedScale { get; set; }
        public bool Changed { get; set; }
        public bool Hit { get; set; }
        public string Collider { get; set; }
        public Float3 Normal { get; set; }
    }
}
