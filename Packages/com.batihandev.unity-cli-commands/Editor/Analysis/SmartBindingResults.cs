namespace BatihanDev.UnityCliCommands.Analysis
{
    public sealed class SmartReferencePlan
    {
        public string Component { get; set; }
        public string PropertyPath { get; set; }
        public string[] OriginalReferences { get; set; }
        public string[] ProposedReferences { get; set; }
        public string PropertiesJson { get; set; }
        public string ElementType { get; set; }
        public int SourceCount { get; set; }
        public int SkippedSources { get; set; }
        public bool AppendMode { get; set; }
        public string AssignmentOwner { get; set; }
    }
    public sealed class SmartReferencePropertyReport
    {
        public bool Preview { get; set; }
        public string Component { get; set; }
        public string Property { get; set; }
        public string[] ProposedReferences { get; set; }
        public int BoundCount { get; set; }
        public bool SerializedStateChanged { get; set; }
        public string UndoScope { get; set; }
        public string PersistenceScope { get; set; }
        public bool ExternalSetterEffectsRollbackGuaranteed { get; set; }
        public int UndoGroup { get; set; }
    }
}
