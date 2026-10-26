using System.Collections.Generic;
using Float3 = BatihanDev.UnityCliCommands.Transform.Float3;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public sealed class SmartQueryReport
    {
        public int Total { get; set; }
        public bool Truncated { get; set; }
        public string Predicate { get; set; }
        public List<SmartQueryItem> Items { get; set; } = new List<SmartQueryItem>();
        public List<SmartQueryDiagnostic> Diagnostics { get; set; } = new List<SmartQueryDiagnostic>();
    }
    public sealed class SmartQueryItem
    {
        public string Component { get; set; }
        public string GameObject { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Value { get; set; }
    }
    public sealed class SmartQueryDiagnostic
    {
        public string Component { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }
    public sealed class SmartSpatialReport
    {
        public int Total { get; set; }
        public bool Truncated { get; set; }
        public Float3 Center { get; set; }
        public float Radius { get; set; }
        public List<SmartSpatialItem> Items { get; set; } = new List<SmartSpatialItem>();
    }
    public sealed class SmartSpatialItem
    {
        public string Collider { get; set; }
        public string GameObject { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public float Distance { get; set; }
    }
}
